using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Fantasy.SourceGenerator.Analyzers
{
    /// <summary>
    /// 检查 AMessage 网络消息中的字段/属性：
    /// 1. 如果消息有 [ProtoContract]，不能包含 Entity 或继承 Entity 的字段/属性
    /// 2. 如果消息有 [MemoryPackable]，且字段/属性是继承自 Entity 的类型（非 Entity 本身），
    ///    则该类型必须有 [MemoryPackable] 和 partial
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪")]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:指定分析器禁止的 API 强制设置")]
    public class MessageEntityFieldAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString TitleProtoContract =
            "ProtoContract message cannot contain Entity fields";

        private static readonly LocalizableString MessageFormatProtoContract =
            "Message '{0}' with [ProtoContract] cannot contain field/property '{1}' of type '{2}' which is or inherits from Entity";

        private static readonly LocalizableString DescriptionProtoContract =
            "Messages with [ProtoContract] attribute cannot contain fields or properties that are Entity or inherit from Entity, as ProtoBuf cannot serialize Entity references.";

        private static readonly LocalizableString TitleMemoryPackable =
            "Entity field must be MemoryPackable and partial";

        private static readonly LocalizableString MessageFormatMemoryPackableAttr =
            "Message '{0}' contains field/property '{1}' of type '{2}' which inherits from Entity but does not have [MemoryPackable] attribute";

        private static readonly LocalizableString MessageFormatMemoryPackablePartial =
            "Message '{0}' contains field/property '{1}' of type '{2}' which inherits from Entity but is not declared as partial";

        private static readonly LocalizableString MessageFormatMemoryPackableBoth =
            "Message '{0}' contains field/property '{1}' of type '{2}' which inherits from Entity but does not have [MemoryPackable] attribute and is not declared as partial";

        private static readonly LocalizableString DescriptionMemoryPackable =
            "Messages with [MemoryPackable] attribute can only contain Entity-derived fields/properties if those types also have [MemoryPackable] and are declared as partial.";

        private static readonly DiagnosticDescriptor RuleProtoContract = new DiagnosticDescriptor(
            DiagnosticIds.MessageProtoContractEntityField,
            TitleProtoContract,
            MessageFormatProtoContract,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: DescriptionProtoContract);

        private static readonly DiagnosticDescriptor RuleMemoryPackableAttr = new DiagnosticDescriptor(
            DiagnosticIds.MessageMemoryPackableEntityField + "_MemoryPackable",
            TitleMemoryPackable,
            MessageFormatMemoryPackableAttr,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: DescriptionMemoryPackable);

        private static readonly DiagnosticDescriptor RuleMemoryPackablePartial = new DiagnosticDescriptor(
            DiagnosticIds.MessageMemoryPackableEntityField + "_Partial",
            TitleMemoryPackable,
            MessageFormatMemoryPackablePartial,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: DescriptionMemoryPackable);

        private static readonly DiagnosticDescriptor RuleMemoryPackableBoth = new DiagnosticDescriptor(
            DiagnosticIds.MessageMemoryPackableEntityField,
            TitleMemoryPackable,
            MessageFormatMemoryPackableBoth,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: DescriptionMemoryPackable);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RuleProtoContract, RuleMemoryPackableAttr, RuleMemoryPackablePartial, RuleMemoryPackableBoth);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // 注册类型声明分析
            context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;

            // 获取类型符号
            var typeSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (typeSymbol == null)
            {
                return;
            }

            // 检查是否继承自 AMessage
            if (!InheritsFromAMessage(typeSymbol))
            {
                return;
            }

            // 检查是否有 ProtoContract 或 MemoryPackable 特性
            bool hasProtoContract = HasAttribute(typeSymbol, "ProtoContractAttribute");
            bool hasMemoryPackable = HasAttribute(typeSymbol, "MemoryPackableAttribute", "MemoryPackable");

            if (!hasProtoContract && !hasMemoryPackable)
            {
                return;
            }

            // 获取所有字段和属性
            var members = typeSymbol.GetMembers()
                .Where(m => m is IFieldSymbol or IPropertySymbol)
                .Where(m => !m.IsStatic && !m.IsImplicitlyDeclared);

            foreach (var member in members)
            {
                ITypeSymbol? memberType = null;
                Location? location = null;

                if (member is IFieldSymbol field)
                {
                    memberType = field.Type;
                    location = field.Locations.FirstOrDefault();
                }
                else if (member is IPropertySymbol property)
                {
                    memberType = property.Type;
                    location = property.Locations.FirstOrDefault();
                }

                if (memberType == null || location == null)
                {
                    continue;
                }

                // 获取实际类型（处理 nullable）
                var actualType = UnwrapNullableType(memberType);

                // 规则1: ProtoContract 消息不能包含 Entity 字段
                if (hasProtoContract)
                {
                    if (IsEntityOrDerivedFromEntity(actualType))
                    {
                        var diagnostic = Diagnostic.Create(
                            RuleProtoContract,
                            location,
                            typeSymbol.Name,
                            member.Name,
                            actualType.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }

                // 规则2: MemoryPackable 消息中的 Entity 派生类字段必须是 MemoryPackable 和 partial
                if (hasMemoryPackable)
                {
                    // 只检查继承自 Entity 的类型，不检查 Entity 抽象类本身
                    if (IsDerivedFromEntity(actualType) && !IsEntityAbstractClass(actualType))
                    {
                        if (actualType is INamedTypeSymbol namedType)
                        {
                            bool hasMemoryPackableAttr = HasAttribute(namedType, "MemoryPackableAttribute", "MemoryPackable");
                            bool isPartial = IsPartialClass(namedType, hasMemoryPackableAttr);

                            if (!hasMemoryPackableAttr && !isPartial)
                            {
                                var diagnostic = Diagnostic.Create(
                                    RuleMemoryPackableBoth,
                                    location,
                                    typeSymbol.Name,
                                    member.Name,
                                    namedType.Name);
                                context.ReportDiagnostic(diagnostic);
                            }
                            else if (!hasMemoryPackableAttr)
                            {
                                var diagnostic = Diagnostic.Create(
                                    RuleMemoryPackableAttr,
                                    location,
                                    typeSymbol.Name,
                                    member.Name,
                                    namedType.Name);
                                context.ReportDiagnostic(diagnostic);
                            }
                            else if (!isPartial)
                            {
                                var diagnostic = Diagnostic.Create(
                                    RuleMemoryPackablePartial,
                                    location,
                                    typeSymbol.Name,
                                    member.Name,
                                    namedType.Name);
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查类型是否继承自 AMessage
        /// </summary>
        private static bool InheritsFromAMessage(INamedTypeSymbol typeSymbol)
        {
            var baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                var fullName = baseType.ToDisplayString();
                if (fullName == "Fantasy.Network.Interface.AMessage" ||
                    baseType.Name == "AMessage")
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        /// <summary>
        /// 检查类型是否是 Entity 或继承自 Entity
        /// </summary>
        private static bool IsEntityOrDerivedFromEntity(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            // 检查是否是 Entity 抽象类本身
            if (IsEntityAbstractClass(namedType))
            {
                return true;
            }

            // 检查是否继承自 Entity
            return IsDerivedFromEntity(namedType);
        }

        /// <summary>
        /// 检查类型是否继承自 Entity（不包括 Entity 本身）
        /// </summary>
        private static bool IsDerivedFromEntity(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            var baseType = namedType.BaseType;
            while (baseType != null)
            {
                var fullName = baseType.ToDisplayString();
                if (fullName == "Fantasy.Entitas.Entity" ||
                    fullName == "Fantasy.Entity" ||
                    baseType.Name == "Entity")
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        /// <summary>
        /// 检查类型是否是 Entity 抽象类本身
        /// </summary>
        private static bool IsEntityAbstractClass(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }

            var fullName = namedType.ToDisplayString();
            return (fullName == "Fantasy.Entitas.Entity" || fullName == "Fantasy.Entity" || namedType.Name == "Entity")
                   && namedType.IsAbstract;
        }

        /// <summary>
        /// 展开 nullable 类型
        /// </summary>
        private static ITypeSymbol UnwrapNullableType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedType)
            {
                // 处理 Nullable<T>
                if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    return namedType.TypeArguments[0];
                }
            }

            return typeSymbol;
        }

        /// <summary>
        /// 检查类型是否有指定特性
        /// </summary>
        private static bool HasAttribute(INamedTypeSymbol typeSymbol, params string[] attributeNames)
        {
            return typeSymbol.GetAttributes().Any(attr =>
                attributeNames.Any(name =>
                    attr.AttributeClass?.Name == name ||
                    attr.AttributeClass?.ToDisplayString().EndsWith("." + name) == true));
        }

        /// <summary>
        /// 检查类型是否声明为 partial
        /// </summary>
        private static bool IsPartialClass(INamedTypeSymbol typeSymbol, bool hasMemoryPackable)
        {
            // 如果有 MemoryPackable 特性，MemoryPack 要求必须是 partial
            // 所以如果有这个特性并且代码能编译通过，那么它一定是 partial 的
            if (hasMemoryPackable)
            {
                return true;
            }

            // 遍历类型的所有声明语法引用
            foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();

                // 检查是否是类声明
                if (syntax is ClassDeclarationSyntax classDecl)
                {
                    if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        return true;
                    }
                }

                // 检查是否是记录声明
                if (syntax is RecordDeclarationSyntax recordDecl)
                {
                    if (recordDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        return true;
                    }
                }

                // 检查是否是结构声明
                if (syntax is StructDeclarationSyntax structDecl)
                {
                    if (structDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
