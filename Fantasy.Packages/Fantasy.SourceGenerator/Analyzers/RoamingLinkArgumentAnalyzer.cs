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
    /// 检查 SessionRoamingComponent.Link 方法调用时传递的 Entity 参数是否：
    /// 1. 实现了 [MemoryPackable] 特性
    /// 2. 使用了 partial 关键字
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪")]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:指定分析器禁止的 API 强制设置")]
    public class RoamingLinkArgumentAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString Title =
            "SessionRoamingComponent.Link Entity argument must be MemoryPackable and partial";

        private static readonly LocalizableString MessageFormatMemoryPackable =
            "Entity '{0}' passed to SessionRoamingComponent.Link must have [MemoryPackable] attribute";

        private static readonly LocalizableString MessageFormatPartial =
            "Entity '{0}' passed to SessionRoamingComponent.Link must be declared as partial";

        private static readonly LocalizableString MessageFormatBoth =
            "Entity '{0}' passed to SessionRoamingComponent.Link must have [MemoryPackable] attribute and be declared as partial";

        private static readonly LocalizableString Description =
            "Entity arguments passed to SessionRoamingComponent.Link method must be serializable with MemoryPack and declared as partial class to support code generation.";

        private static readonly DiagnosticDescriptor RuleMemoryPackable = new DiagnosticDescriptor(
            DiagnosticIds.RoamingLinkArgument + "_MemoryPackable",
            Title,
            MessageFormatMemoryPackable,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        private static readonly DiagnosticDescriptor RulePartial = new DiagnosticDescriptor(
            DiagnosticIds.RoamingLinkArgument + "_Partial",
            Title,
            MessageFormatPartial,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        private static readonly DiagnosticDescriptor RuleBoth = new DiagnosticDescriptor(
            DiagnosticIds.RoamingLinkArgument,
            Title,
            MessageFormatBoth,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RuleMemoryPackable, RulePartial, RuleBoth);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // 注册语法节点分析 - 检测方法调用表达式
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // 获取被调用的方法符号
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            // 简单检查：方法名必须是 Link
            if (methodSymbol.Name != "Link")
            {
                return;
            }

            // 检查包含类型是否是 SessionRoamingComponent
            var containingType = methodSymbol.ContainingType;
            if (containingType == null || containingType.Name != "SessionRoamingComponent")
            {
                return;
            }

            // 检查参数数量
            if (methodSymbol.Parameters.Length != 4)
            {
                return;
            }

            // 检查最后一个参数名是否是 args
            var lastParam = methodSymbol.Parameters[3];
            if (lastParam.Name != "args")
            {
                return;
            }

            // 查找 Entity args 参数（最后一个参数）
            var entityArgument = FindEntityArgument(invocation.ArgumentList, methodSymbol);
            if (entityArgument == null)
            {
                return;
            }

            // 获取参数的类型
            var argumentType = context.SemanticModel.GetTypeInfo(entityArgument.Expression, context.CancellationToken);
            if (argumentType.Type is not INamedTypeSymbol typeSymbol)
            {
                return;
            }

            // 检查是否继承自 Entity
            if (!InheritsFromEntity(typeSymbol))
            {
                return;
            }

            // 检查是否有 MemoryPackable 特性和 partial 关键字
            bool hasMemoryPackable = HasMemoryPackableAttribute(typeSymbol);
            bool isPartial = IsPartialClass(typeSymbol);

            // 根据检查结果报告错误
            if (!hasMemoryPackable && !isPartial)
            {
                var diagnostic = Diagnostic.Create(
                    RuleBoth,
                    entityArgument.GetLocation(),
                    typeSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
            else if (!hasMemoryPackable)
            {
                var diagnostic = Diagnostic.Create(
                    RuleMemoryPackable,
                    entityArgument.GetLocation(),
                    typeSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
            else if (!isPartial)
            {
                var diagnostic = Diagnostic.Create(
                    RulePartial,
                    entityArgument.GetLocation(),
                    typeSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        /// <summary>
        /// 查找 Entity args 参数
        /// </summary>
        private static ArgumentSyntax? FindEntityArgument(ArgumentListSyntax? argumentList, IMethodSymbol methodSymbol)
        {
            if (argumentList == null || argumentList.Arguments.Count == 0)
            {
                return null;
            }

            // Entity args 是最后一个参数 (index 3)
            var arguments = argumentList.Arguments;

            // 检查命名参数
            foreach (var arg in arguments)
            {
                if (arg.NameColon?.Name.Identifier.Text == "args")
                {
                    return arg;
                }
            }

            // 检查位置参数 - 如果有4个参数，最后一个就是 args
            if (arguments.Count >= 4)
            {
                return arguments[3];
            }

            return null;
        }

        /// <summary>
        /// 检查类型是否继承自 Entity
        /// </summary>
        private static bool InheritsFromEntity(INamedTypeSymbol typeSymbol)
        {
            var baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                var fullName = baseType.ToDisplayString();

                // 检查是否是 Fantasy.Entitas.Entity
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
        /// 检查类型是否有 MemoryPackable 特性
        /// </summary>
        private static bool HasMemoryPackableAttribute(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetAttributes().Any(attr =>
                attr.AttributeClass?.ToDisplayString() == "MemoryPack.MemoryPackableAttribute" ||
                attr.AttributeClass?.Name == "MemoryPackableAttribute" ||
                attr.AttributeClass?.Name == "MemoryPackable");
        }

        /// <summary>
        /// 检查类型是否声明为 partial
        /// </summary>
        private static bool IsPartialClass(INamedTypeSymbol typeSymbol)
        {
            // 如果类型定义在外部程序集中，无法访问语法树
            // 但是如果有 MemoryPackable 特性，MemoryPack 生成器会生成代码
            // 这意味着该类型必须是 partial 的，否则无法编译
            // 所以如果有 MemoryPackable，我们假设它已经是 partial
            if (HasMemoryPackableAttribute(typeSymbol))
            {
                // MemoryPack 要求类型必须是 partial，所以如果有这个特性
                // 并且代码能编译通过，那么它一定是 partial 的
                return true;
            }

            // 遍历类型的所有声明语法引用
            foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxRef.GetSyntax();

                // 检查是否是类声明
                if (syntax is ClassDeclarationSyntax classDecl)
                {
                    // 检查是否有 partial 修饰符
                    if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        return true;
                    }
                }

                // 检查是否是记录声明
                if (syntax is RecordDeclarationSyntax recordDecl)
                {
                    // 检查是否有 partial 修饰符
                    if (recordDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        return true;
                    }
                }

                // 检查是否是结构声明
                if (syntax is StructDeclarationSyntax structDecl)
                {
                    // 检查是否有 partial 修饰符
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
