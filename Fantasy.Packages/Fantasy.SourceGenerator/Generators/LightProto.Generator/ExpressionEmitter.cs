using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LightProto.Generator;

internal sealed class ExpressionEmitter
{
    private readonly SemanticModel _semanticModel;

    public ExpressionEmitter(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public string Emit(ExpressionSyntax expr)
    {
        if (expr is LiteralExpressionSyntax)
        {
            return expr.ToFullString();
        }

        if (expr is DefaultExpressionSyntax def)
        {
            var type = _semanticModel.GetTypeInfo(def.Type).Type;
            return type is null ? expr.ToFullString() : $"default({Fqn(type)})";
        }

        // 运行时检查 CollectionExpressionSyntax 类型（兼容 Roslyn 4.3 和 4.8+）
        if (expr.GetType().Name == "CollectionExpressionSyntax")
        {
            return EmitCollectionExpression(expr);
        }

        if (expr is ObjectCreationExpressionSyntax creation)
        {
            return EmitObjectCreation(creation);
        }

        if (expr is ImplicitObjectCreationExpressionSyntax implicitNew)
        {
            return EmitImplicitObjectCreation(implicitNew);
        }
        if (expr is ImplicitArrayCreationExpressionSyntax implicitArrayNew)
        {
            return EmitImplicitArrayCreation(implicitArrayNew);
        }

        if (
            expr is InvocationExpressionSyntax invocation
            && invocation.Expression is IdentifierNameSyntax id
        )
        {
            return $"{id}{EmitArgumentsExpression(invocation.ArgumentList)}";
        }
        if (_semanticModel.GetSymbolInfo(expr).Symbol is { } symbol)
        {
            return EmitSymbolExpression(expr, symbol);
        }

        return expr.ToFullString();
    }

    // ---------------- helpers ----------------

    private string EmitArgumentsExpression(ArgumentListSyntax? argumentListSyntax)
    {
        return argumentListSyntax is null
            ? "()"
            : EmitArgumentsExpression(argumentListSyntax.Arguments.Select(x => x.Expression));
    }

    private string EmitArgumentsExpression(IEnumerable<ExpressionSyntax> expressionSyntaxes)
    {
        return $"({string.Join(",", expressionSyntaxes.Select(Emit))})";
    }

    private string EmitInitializerExpression(
        InitializerExpressionSyntax? initializerExpressionSyntax
    )
    {
        return initializerExpressionSyntax is null
            ? ""
            : "{"
                + string.Join(",", initializerExpressionSyntax.Expressions.Select(x => Emit(x)))
                + "}";
    }

    private string EmitObjectCreation(ObjectCreationExpressionSyntax creation)
    {
        var type = (_semanticModel.GetSymbolInfo(creation.Type).Symbol as ITypeSymbol);

        if (type is null)
        {
            return creation.ToFullString();
        }
        var typeName = Fqn(type);
        var args = EmitArgumentsExpression(creation.ArgumentList);
        var init = EmitInitializerExpression(creation.Initializer);

        return $"new {typeName}{args}{init}";
    }

    private string EmitImplicitObjectCreation(ImplicitObjectCreationExpressionSyntax creation)
    {
        var type = _semanticModel.GetTypeInfo(creation).Type;

        if (type is null)
        {
            return creation.ToFullString();
        }
        var typeName = Fqn(type);
        var args = EmitArgumentsExpression(creation.ArgumentList);
        var init = EmitInitializerExpression(creation.Initializer);

        return $"new {typeName}{args}{init}";
    }

    private string EmitImplicitArrayCreation(ImplicitArrayCreationExpressionSyntax creation)
    {
        var init = EmitInitializerExpression(creation.Initializer);
        return $"new []{init}";
    }

    // 使用动态反射处理 CollectionExpression（兼容 Roslyn 4.3 和 4.8+）
    // 在 Roslyn 4.3 中，CollectionExpressionSyntax 类型不存在，但代码仍能编译
    // 在 Roslyn 4.8+ 中，会正确处理集合表达式
    private string EmitCollectionExpression(ExpressionSyntax expr)
    {
        try
        {
            // 使用反射获取 Elements 属性（避免直接引用 CollectionExpressionSyntax）
            var elementsProperty = expr.GetType().GetProperty("Elements");
            if (elementsProperty == null)
            {
                return expr.ToFullString();
            }

            var elements = elementsProperty.GetValue(expr);
            if (elements == null)
            {
                return expr.ToFullString();
            }

            // 获取 Count 属性
            var countProperty = elements.GetType().GetProperty("Count");
            if (countProperty == null || (int)countProperty.GetValue(elements)! == 0)
            {
                return expr.ToFullString();
            }

            // 获取元素集合
            var enumerable = elements as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return expr.ToFullString();
            }

            var elementStrings = new System.Collections.Generic.List<string>();
            foreach (var element in enumerable)
            {
                // 检查是否是 ExpressionElementSyntax
                if (element.GetType().Name == "ExpressionElementSyntax")
                {
                    var exprProperty = element.GetType().GetProperty("Expression");
                    if (exprProperty != null)
                    {
                        var innerExpr = exprProperty.GetValue(element) as ExpressionSyntax;
                        if (innerExpr != null)
                        {
                            elementStrings.Add(Emit(innerExpr));
                            continue;
                        }
                    }
                }
                // 其他类型直接转字符串
                elementStrings.Add(element.ToString() ?? "");
            }

            return $"[{string.Join(", ", elementStrings)}]";
        }
        catch
        {
            // 如果反射失败，回退到原始字符串
            return expr.ToFullString();
        }
    }

    static readonly SymbolDisplayFormat FullyQualifiedMemberFormat = SymbolDisplayFormat
        .FullyQualifiedFormat.WithMemberOptions(SymbolDisplayMemberOptions.IncludeContainingType)
        .WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters);

    private string EmitSymbolExpression(ExpressionSyntax expr, ISymbol symbol)
    {
        if (expr is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                var method = symbol as IMethodSymbol;
                if (method is null)
                {
                    return expr.ToFullString();
                }

                var reducedMethod = method;
                if (method.MethodKind is MethodKind.ReducedExtension)
                {
                    reducedMethod = method.ReducedFrom!;
                }

                var name = reducedMethod.IsStatic
                    ? reducedMethod.ContainingType.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    )
                        + "."
                        + reducedMethod.Name
                    : reducedMethod.Name;

                if (method.IsGenericMethod)
                {
                    name +=
                        "<"
                        + string.Join(
                            ", ",
                            method.TypeArguments.Select(t =>
                                t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            )
                        )
                        + ">";
                }
                string args;
                if (reducedMethod.IsStatic)
                {
                    if (method.MethodKind is MethodKind.ReducedExtension)
                    {
                        args = EmitArgumentsExpression(
                            Enumerable
                                .Repeat(memberAccessExpressionSyntax.Expression, 1)
                                .Concat(invocation.ArgumentList.Arguments.Select(x => x.Expression))
                        );
                    }
                    else
                    {
                        args = EmitArgumentsExpression(invocation.ArgumentList);
                    }
                    return $"{name}{args}";
                }
                else
                {
                    args = EmitArgumentsExpression(invocation.ArgumentList);
                    name = Emit(memberAccessExpressionSyntax.Expression) + "." + name;
                    return $"{name}{args}";
                }
            }
            else
            {
                return expr.ToFullString();
            }
        }
        else if (expr is PostfixUnaryExpressionSyntax postfixUnary)
        {
            var name = Emit(postfixUnary.Operand);
            return $"{name}{postfixUnary.OperatorToken}";
        }
        else if (expr is PrefixUnaryExpressionSyntax prefixUnary)
        {
            var name = Emit(prefixUnary.Operand);
            return $"{prefixUnary.OperatorToken}{name}";
        }
        else if (expr is MemberAccessExpressionSyntax memberAccess)
        {
            return symbol.ToDisplayString(FullyQualifiedMemberFormat);
        }

        return symbol.ToDisplayString(FullyQualifiedMemberFormat);
    }

    private static string Fqn(ITypeSymbol type)
    {
        return type.ToDisplayString(FullyQualifiedMemberFormat);
    }
}
