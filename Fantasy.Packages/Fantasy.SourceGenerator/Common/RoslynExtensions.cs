using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8601 // Possible null reference assignment.

namespace Fantasy.SourceGenerator.Common
{
    /// <summary>
    /// Roslyn 相关的扩展方法
    /// </summary>
    internal static class RoslynExtensions
    {
        /// <summary>
        /// 检查是否是Fantasy的核心程序集
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        public static bool IsFantasyAssembly(string assemblyName)
        {
            return assemblyName is GeneratorConstants.FantasyNetAssmbly or GeneratorConstants.FantasyUnityAssmbly;
        }
        
        /// <summary>
        /// 检查类型是否实现了指定的接口（通过完全限定名）
        /// </summary>
        public static bool ImplementsInterface(this INamedTypeSymbol typeSymbol, string interfaceFullName)
        {
            return typeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == interfaceFullName);
        }

        /// <summary>
        /// 检查类型是否继承自指定的基类（通过完全限定名）
        /// </summary>
        public static bool InheritsFrom(this INamedTypeSymbol typeSymbol, string baseTypeFullName)
        {
            var current = typeSymbol.BaseType;
            while (current != null)
            {
                if (current.ToDisplayString() == baseTypeFullName)
                {
                    return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        /// <summary>
        /// 获取一个根据当前程序集加指定flag的名字
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="flag"></param>
        /// <param name="assemblyName"></param>
        /// <param name="replaceAssemblyName"></param>
        /// <returns></returns>
        public static string GetAssemblyName(this Compilation compilation, string flag, out string assemblyName,
            out string replaceAssemblyName)
        {
            assemblyName = string.IsNullOrWhiteSpace(compilation.AssemblyName) ? "Unknown" : compilation.AssemblyName;
            replaceAssemblyName = assemblyName.Replace("-", "_").Replace(".", "_");

            // 确保返回的类名始终有程序集前缀，避免生成重复的类名
            if (string.IsNullOrWhiteSpace(replaceAssemblyName))
            {
                replaceAssemblyName = "Unknown";
            }

            return $"{replaceAssemblyName}_{flag}";
        }

        /// <summary>
        /// 获取类型的完全限定名（包括命名空间）
        /// </summary>
        public static string GetFullName(this ITypeSymbol typeSymbol, bool includeGlobal = true)
        {
            var displayString = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return includeGlobal ? displayString : displayString.Replace("global::", "");
        }

        /// <summary>
        /// 检查类型是否可以被实例化（非抽象、非接口、非无参泛型、非开放式泛型、非静态）
        /// </summary>
        public static bool IsInstantiable(this INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.IsSpecific() && !typeSymbol.IsOpenGeneric();
        }

        /// <summary>
        /// 是具体的。排除：抽象、静态、接口、无参泛型。
        /// </summary>
        public static bool IsSpecific(this INamedTypeSymbol typeSymbol) {
            return typeSymbol is { IsAbstract: false, IsStatic: false, TypeKind: TypeKind.Class, IsUnboundGenericType: false };
        }

        /// <summary>
        /// 是否为开放式泛型(递归检查)
        /// </summary>
        public static bool IsOpenGeneric(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
            {
                return false;
            }
            
            // 顶层类型参数是开放类型
            if (namedType.TypeArguments.Any(ta => ta.TypeKind == TypeKind.TypeParameter))
            {
                return true;
            }

            // 递归检查
            foreach (var ta in namedType.TypeArguments)
            {
                if (ta.IsOpenGeneric())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取所有实现指定接口的类型
        /// </summary>
        public static IEnumerable<INamedTypeSymbol> GetTypesImplementingInterface(
            this Compilation compilation,
            string interfaceFullName)
        {
            var visitor = new InterfaceImplementationVisitor(interfaceFullName);
            visitor.Visit(compilation.GlobalNamespace);
            return visitor.ImplementingTypes;
        }
        
        /// <summary>
        /// 转换为驼峰命名
        /// </summary>
        public static string ToCamelCase(this string name)
        {
            if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            {
                return name;
            }

            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// 访问器：查找实现特定接口的所有类型
        /// </summary>
        private class InterfaceImplementationVisitor : SymbolVisitor
        {
            private readonly string _interfaceFullName;
            private readonly List<INamedTypeSymbol> _implementingTypes = new List<INamedTypeSymbol>();

            public IReadOnlyList<INamedTypeSymbol> ImplementingTypes => _implementingTypes;

            public InterfaceImplementationVisitor(string interfaceFullName)
            {
                _interfaceFullName = interfaceFullName;
            }

            public override void VisitNamespace(INamespaceSymbol symbol)
            {
                foreach (var member in symbol.GetMembers())
                {
                    member.Accept(this);
                }
            }

            public override void VisitNamedType(INamedTypeSymbol symbol)
            {
                if (symbol.IsInstantiable() && symbol.ImplementsInterface(_interfaceFullName))
                {
                    _implementingTypes.Add(symbol);
                }

                // 递归访问嵌套类型
                foreach (var nestedType in symbol.GetTypeMembers())
                {
                    nestedType.Accept(this);
                }
            }
        }

        /// <summary>
        /// 尝试获取泛型接口的类型参数
        /// 例如：IAwakeSystem&lt;PlayerEntity&gt; 返回 PlayerEntity
        /// </summary>
        public static ITypeSymbol? GetGenericInterfaceTypeArgument(
            this INamedTypeSymbol typeSymbol,
            string genericInterfaceName)
        {
            var matchingInterface = typeSymbol.AllInterfaces.FirstOrDefault(i =>
                i.IsGenericType &&
                i.ConstructedFrom.ToDisplayString() == genericInterfaceName);

            return matchingInterface?.TypeArguments.FirstOrDefault();
        }
        
        /// <summary>
        /// 将字符串转换为 PascalCase
        /// </summary>
        public static string ToPascalCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // 移除特殊字符，只保留字母和数字
            var cleaned = new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            if (string.IsNullOrEmpty(cleaned))
            {
                return "Unknown";
            }

            // 首字母大写
            return char.ToUpper(cleaned[0]) + cleaned.Substring(1);
        }
    }
}
