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
    /// 检查 Send&lt;T&gt; 和 Call&lt;T&gt; 方法调用时，泛型参数 T 不应该是接口类型
    /// 因为这些方法需要具体的类型信息，传递接口会导致运行时错误
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:启用分析器发布跟踪")]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:指定分析器禁止的 API 强制设置")]
    public class SendCallInterfaceTypeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString Title =
            "Send<T> and Call<T> should not use interface type as generic argument";

        private static readonly LocalizableString MessageFormat =
            "Method '{0}<{1}>' should not use interface type '{1}' as generic argument. Use a concrete class type instead";

        private static readonly LocalizableString Description =
            "Send<T> and Call<T> methods require concrete class types for proper serialization and type resolution. " +
            "Interface types (like IMessage, IRoamingMessage, IRequest, etc.) cannot be used as generic arguments. " +
            "Use the actual message class instead.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.SendCallInterfaceType,
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

            // 检查方法名是否是 Send 或 Call
            if (methodSymbol.Name != "Send" && methodSymbol.Name != "Call")
            {
                return;
            }

            // 检查是否是泛型方法
            if (!methodSymbol.IsGenericMethod || methodSymbol.TypeArguments.Length == 0)
            {
                return;
            }

            // 获取第一个泛型参数（推断或显式指定的）
            var typeArgument = methodSymbol.TypeArguments[0];

            // 检查类型参数是否是接口类型
            if (typeArgument.TypeKind != TypeKind.Interface)
            {
                return;
            }

            // 确定报告错误的位置
            Location diagnosticLocation;

            // 尝试获取显式泛型参数的位置
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is GenericNameSyntax genericName)
            {
                // 显式泛型调用：Send<IMessage>(msg)
                diagnosticLocation = genericName.TypeArgumentList.Arguments[0].GetLocation();
            }
            else
            {
                // 隐式泛型推断：Send(msg) 其中 msg 是接口类型
                // 报告在整个方法调用表达式上
                if (invocation.ArgumentList.Arguments.Count > 0)
                {
                    // 优先报告在第一个参数上（导致推断的参数）
                    diagnosticLocation = invocation.ArgumentList.Arguments[0].GetLocation();
                }
                else
                {
                    diagnosticLocation = invocation.GetLocation();
                }
            }

            var diagnostic = Diagnostic.Create(
                Rule,
                diagnosticLocation,
                methodSymbol.Name,
                typeArgument.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
