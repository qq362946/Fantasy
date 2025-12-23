using Fantasy.SourceGenerator.Common;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy.SourceGenerator.Generators
{
    /// <summary>
    /// MemoryPack 生成器
    /// 为每个程序集生成 MemoryPack formatter 注册代码和 Entity 序列化数组
    /// 解决 AOT 环境下静态构造函数可能不执行的问题
    /// </summary>
    [Generator]
    public class MemoryPackGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 收集所有标记了 [MemoryPackable] 特性的类型
            var memoryPackableTypesProvider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax ||
                                            node is Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax,
                    transform: (ctx, _) =>
                    {
                        var symbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                        if (symbol == null) return null;

                        // 检查是否有 MemoryPackable 特性
                        var hasMemoryPackableAttr = symbol.GetAttributes().Any(attr =>
                        {
                            var attrClass = attr.AttributeClass;
                            return attrClass != null &&
                                   attrClass.Name == "MemoryPackableAttribute" &&
                                   attrClass.ContainingNamespace?.ToDisplayString() == "MemoryPack";
                        });

                        if (!hasMemoryPackableAttr) return null;

                        // 检查是否是 Entity 子类
                        var isEntitySubclass = IsEntitySubclass(symbol);

                        // 检查是否是 SphereEventArgs 子类
                        var isSphereEventArgsSubclass = IsSphereEventArgsSubclass(symbol);

                        return new TypeInfo
                        {
                            Symbol = symbol,
                            IsEntity = isEntitySubclass,
                            IsSphereEventArgs = isSphereEventArgsSubclass
                        };
                    })
                .Where(info => info != null)
                .Collect();

            // 组合编译信息和收集的类型
            var compilationAndTypes = context.CompilationProvider
                .Combine(memoryPackableTypesProvider);

            // 注册源代码输出 - 生成 MemoryPackInitializer
            context.RegisterSourceOutput(compilationAndTypes, (spc, source) =>
            {
                var (compilation, types) = source;

                if (CompilationHelper.IsSourceGeneratorDisabled(compilation))
                {
                    return;
                }

                if (!CompilationHelper.HasFantasyDefine(compilation))
                {
                    return;
                }
                
                if (compilation.GetTypeByMetadataName("Fantasy.Assembly.IMemoryPackEntityGenerator") == null)
                {
                    return;
                }

                GenerateMemoryPackCode(spc, compilation, types!);
            });
        }

        private static bool IsEntitySubclass(INamedTypeSymbol symbol)
        {
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == "Entity" &&
                    baseType.ContainingNamespace.ToDisplayString() == "Fantasy.Entitas")
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        private static bool IsSphereEventArgsSubclass(INamedTypeSymbol symbol)
        {
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                // 检查类名和命名空间
                if (baseType.Name == "SphereEventArgs")
                {
                    var namespaceStr = baseType.ContainingNamespace?.ToDisplayString();
                    // SphereEventArgs 在 Fantasy.Sphere 命名空间中
                    if (namespaceStr == "Fantasy.Sphere")
                    {
                        return true;
                    }
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        private static void GenerateMemoryPackCode(
            SourceProductionContext context,
            Compilation compilation,
            IEnumerable<TypeInfo> types)
        {
            var typesList = types.ToList();
            var targetPlatform = CompilationHelper.GetTargetPlatform(compilation);
            var className = compilation.GetAssemblyName("MemoryPackInitializer", out var assemblyName, out _);

            // 分离 Entity 和普通类型
            var entityTypes = typesList.Where(t => t.IsEntity).Select(t => t.Symbol).ToList();
            var sphereEventArgs = typesList.Where(t => t.IsSphereEventArgs).ToList();
            var normalTypes = typesList.Where(t => !t.IsEntity).Select(t => t.Symbol).ToList();

            var builder = new SourceCodeBuilder();

            // 添加文件头
            builder.AppendLine(GeneratorConstants.AutoGeneratedHeader);

            // 添加 using
            builder.AddUsings("System", "System.Runtime.CompilerServices");

            builder.AppendLine();

            // 开始命名空间
            builder.BeginDefaultNamespace();

            // 添加类注释
            builder.AddXmlComment($"Auto-generated MemoryPack initializer for {assemblyName}");
            builder.AddXmlComment("Ensures all MemoryPack formatters are registered before first use in AOT environment");

            // 开始类定义，实现 IMemoryPackEntityGenerator 接口
            builder.BeginClass(className, "public sealed", "Fantasy.Assembly.IMemoryPackEntityGenerator");

            // 添加字段
            builder.AppendLine("private static bool _initialized;");
            builder.AppendLine();

            // 生成 Entity和SphereEventArgs 序列化方法
            GenerateEntityArrays(builder, entityTypes, sphereEventArgs);
            builder.AppendLine();

            // 生成初始化方法
            GenerateInitializeMethod(builder, normalTypes);

            // 结束类
            builder.EndClass();
            builder.AppendLine();

            // 结束命名空间
            builder.EndNamespace();

            // 输出源代码
            context.AddSource($"{className}.g.cs", builder.ToString());
        }

        private static void GenerateEntityArrays(SourceCodeBuilder builder, List<INamedTypeSymbol> entityTypes, IEnumerable<TypeInfo> sphereEventArgs)
        {
            // 生成 EntityTypeHashCodes() 方法
            builder.AddXmlComment("Gets the TypeHashCode array of all Entity subclasses marked with [MemoryPackable] in this assembly");
            builder.BeginMethod("public long[] EntityTypeHashCodes()");
            builder.AppendLine("return new long[]");
            builder.OpenBrace();
            foreach (var entity in entityTypes)
            {
                var typeHashCode = HashCodeHelper.ComputeHash64(entity.GetFullName(false));
                builder.AppendLine($"{typeHashCode}L,");
            }
            foreach (var sphereEventArg in sphereEventArgs)
            {
                var typeHashCode = HashCodeHelper.ComputeHash64(sphereEventArg.Symbol.GetFullName(false));
                builder.AppendLine($"{typeHashCode}L,");
            }
            builder.CloseBrace(semicolon: true);
            builder.EndMethod();
            builder.AppendLine();

            // 生成 EntityTypes() 方法
            builder.AddXmlComment("Gets the Type array of all Entity subclasses marked with [MemoryPackable] in this assembly");
            builder.BeginMethod("public System.Type[] EntityTypes()");
            builder.AppendLine("return new System.Type[]");
            builder.OpenBrace();
            foreach (var entity in entityTypes)
            {
                
                var fullName = entity.GetFullName(true);
                builder.AppendLine($"typeof({fullName}),");
            }
            foreach (var sphereEventArg in sphereEventArgs)
            {
                var fullName = sphereEventArg.Symbol.GetFullName(true);
                builder.AppendLine($"typeof({fullName}),");
            }
            builder.CloseBrace(semicolon: true);
            builder.EndMethod();
        }

        private static void GenerateInitializeMethod(
            SourceCodeBuilder builder,
            List<INamedTypeSymbol> memoryPackableTypes)
        {
            // 生成实例 Initialize() 方法
            builder.AddXmlComment("Initializes MemoryPack serializers and triggers static constructors of all MemoryPackable types");
            builder.BeginMethod("public void Initialize()");

            // 防止重复初始化
            builder.AppendLine("if (_initialized)");
            builder.OpenBrace();
            builder.AppendLine("return;");
            builder.CloseBrace();
            builder.AppendLine();
            builder.AppendLine("_initialized = true;");
            builder.AppendLine();

            // 添加注释
            builder.AddComment("Trigger static constructors of all MemoryPackable types");
            builder.AddComment("This ensures both MemoryPack registration and user-defined StaticConstructor() are executed");
            builder.AddComment("RuntimeHelpers.RunClassConstructor is supported in Native AOT");
            builder.AppendLine();

            // 生成 try-catch 块来保护初始化过程
            builder.AppendLine("try");
            builder.OpenBrace();

            // 为每个 MemoryPackable 类型触发静态构造函数
            foreach (var type in memoryPackableTypes)
            {
                var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // 使用 RuntimeHelpers.RunClassConstructor 触发静态构造函数
                builder.AddComment($"Trigger static constructor for {type.Name}");
                builder.AppendLine($"RuntimeHelpers.RunClassConstructor(typeof({fullTypeName}).TypeHandle);");
            }

            builder.CloseBrace();
            builder.AppendLine("catch (Exception ex)");
            builder.OpenBrace();
            builder.AddComment("Log error but don't throw - avoid breaking app startup");
            builder.AppendLine("#if FANTASY_NET");
            builder.AppendLine("Fantasy.Log.Error($\"Failed to initialize MemoryPack formatters: {ex}\");");
            builder.AppendLine("#elif UNITY_EDITOR");
            builder.AppendLine("UnityEngine.Debug.LogError($\"Failed to initialize MemoryPack formatters: {ex}\");");
            builder.AppendLine("#else");
            builder.AppendLine("Console.WriteLine($\"Failed to initialize MemoryPack formatters: {ex}\");");
            builder.AppendLine("#endif");
            builder.CloseBrace();

            // 结束方法
            builder.EndMethod();
        }

        private class TypeInfo
        {
            public INamedTypeSymbol Symbol { get; set; }
            public bool IsEntity { get; set; }
            public bool IsSphereEventArgs { get; set; }
        }
    }
}
