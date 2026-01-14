using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy.SourceGenerator.Analyzers
{
    /// <summary>
    /// 禁止直接使用 new 创建 Entity 的子类实例（Scene 除外）
    /// 强制使用 Entity.Create&lt;T&gt;() 方法
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪")]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:指定分析器禁止的 API 强制设置")]
    public class EntityCreationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString Title =
            "Cannot instantiate Entity directly";
        private static readonly LocalizableString MessageFormat =
            "Cannot create instance of '{0}' using 'new'. Use 'Entity.Create<{0}>()' instead.";
        private static readonly LocalizableString Description =
            "Entity and its derived classes (except Scene) must be created using the Create<T>() factory method to ensure proper initialization.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.EntityCreation,
            Title,
            MessageFormat,
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // 注册语法节点分析 - 检测 new 表达式
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            // 获取创建的类型符号
            var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
            if (typeInfo.Type is not INamedTypeSymbol typeSymbol)
            {
                return;
            }

            // 检查是否继承自 Entity（排除 Scene）
            if (!InheritsFromEntity(typeSymbol) || IsScene(typeSymbol))
            {
                return;
            }

            // 检查是否在 Entity.Create 方法内部（允许内部创建）
            if (IsInCreateMethod(context))
            {
                return;
            }

            // 检查是否在对象池 Rent 方法内部（允许对象池创建）
            if (IsInPoolRentMethod(context))
            {
                return;
            }

            // 检查是否在源生成器生成的代码中（允许生成器创建）
            if (IsInGeneratedCode(context))
            {
                return;
            }

            // 报告错误
            var diagnostic = Diagnostic.Create(
                Rule,
                objectCreation.GetLocation(),
                typeSymbol.Name);

            context.ReportDiagnostic(diagnostic);
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
                    fullName == "Fantasy.Entity")
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }
            return false;
        }

        /// <summary>
        /// 检查类型是否是 Scene（Scene 允许直接 new）
        /// </summary>
        private static bool IsScene(INamedTypeSymbol typeSymbol)
        {
            var fullName = typeSymbol.ToDisplayString();

            // Scene 及其子类允许直接 new
            return fullName == "Fantasy.Entitas.Scene" ||
                   fullName == "Fantasy.Scene" ||
                   IsSceneDerivedClass(typeSymbol);
        }

        /// <summary>
        /// 检查是否是 Scene 的子类
        /// </summary>
        private static bool IsSceneDerivedClass(INamedTypeSymbol typeSymbol)
        {
            var baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                var fullName = baseType.ToDisplayString();
                if (fullName == "Fantasy.Entitas.Scene" || fullName == "Fantasy.Scene")
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        /// <summary>
        /// 检查是否在 Entity.Create 方法内部
        /// </summary>
        private static bool IsInCreateMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = context.Node.Ancestors()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();

            if (methodDeclaration == null)
            {
                return false;
            }

            // 检查方法名和所在类
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
            if (methodSymbol == null)
            {
                return false;
            }

            // 允许 Entity.Create 方法内部创建
            if (methodSymbol.Name == "Create" &&
                (methodSymbol.ContainingType?.Name == "Entity" ||
                 methodSymbol.ContainingType?.ToDisplayString() == "Fantasy.Entitas.Entity"))
            {
                return true;
            }

            // 允许继承自 Entity 的类中的 Create 方法
            var containingType = methodSymbol.ContainingType;
            if (methodSymbol.Name == "Create" && containingType != null)
            {
                if (InheritsFromEntity(containingType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检查是否在对象池 Rent 方法内部
        /// </summary>
        private static bool IsInPoolRentMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = context.Node.Ancestors()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();

            if (methodDeclaration == null)
            {
                return false;
            }

            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
            if (methodSymbol == null)
            {
                return false;
            }

            // 允许对象池的 Rent 方法内部创建
            return methodSymbol.Name == "Rent" &&
                   (methodSymbol.ContainingType?.Name.Contains("Pool") ?? false);
        }

        /// <summary>
        /// 检查是否在源生成器生成的代码中
        /// </summary>
        private static bool IsInGeneratedCode(SyntaxNodeAnalysisContext context)
        {
            // 检查文件路径是否包含 .g.cs 或 .generated.cs
            var filePath = context.Node.SyntaxTree.FilePath;
            if (filePath != null &&
                (filePath.Contains(".g.cs") ||
                 filePath.Contains(".generated.cs") ||
                 filePath.Contains("Generated")))
            {
                return true;
            }

            // 检查命名空间是否包含 Generated
            var namespaceDeclaration = context.Node.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            if (namespaceDeclaration != null)
            {
                var namespaceName = namespaceDeclaration.Name.ToString();
                if (namespaceName.Contains("Generated"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
