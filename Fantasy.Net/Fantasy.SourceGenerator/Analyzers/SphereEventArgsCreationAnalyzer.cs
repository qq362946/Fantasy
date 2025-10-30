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
    /// 禁止直接使用 new 创建 SphereEventArgs 的子类实例
    /// 强制使用 SphereEventArgs.Create&lt;T&gt;() 方法
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪")]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:指定分析器禁止的 API 强制设置")]
    public class SphereEventArgsCreationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "FANTASY001";
        private static readonly LocalizableString Title = 
            "Cannot instantiate SphereEventArgs directly";
        private static readonly LocalizableString MessageFormat =
            "Cannot create instance of '{0}' using 'new'. Use 'SphereEventArgs.Create<{0}>()' instead.";
        private static readonly LocalizableString Description =
            "SphereEventArgs and its derived classes must be created using the Create<T>() factory method to ensure proper initialization.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
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

            // 检查是否继承自 SphereEventArgs
            if (!InheritsFromSphereEventArgs(typeSymbol))
            {
                return;
            }

            // 检查是否在 SphereEventArgs.Create 方法内部（允许内部创建）
            if (IsInCreateMethod(context))
            {
                return;
            }

            // 检查是否在对象池 Rent 方法内部（允许对象池创建）
            if (IsInPoolRentMethod(context))
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
        /// 检查类型是否继承自 SphereEventArgs
        /// </summary>
        private static bool InheritsFromSphereEventArgs(INamedTypeSymbol typeSymbol)
        {
            var baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                if (baseType.ToDisplayString() == "Fantasy.Sphere.SphereEventArgs")
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        /// <summary>
        /// 检查是否在 SphereEventArgs.Create 方法内部
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

            return methodSymbol.Name == "Create" &&
                   methodSymbol.ContainingType?.Name == "SphereEventArgs" &&
                   methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() == "Fantasy.Sphere";
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
    }
}
