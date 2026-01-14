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

            // 收集所有闭合泛型类型（扫描字段和属性的类型使用）
            // 用于发现实际使用的泛型实例化
            var closedGenericTypesProvider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax ||
                                            node is Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax,
                    transform: (ctx, _) =>
                    {
                        ITypeSymbol? typeSymbol = null;
                        
                        // 从字段或属性声明中提取类型
                        if (ctx.Node is Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax fieldDecl)
                        {
                            typeSymbol = ctx.SemanticModel.GetTypeInfo(fieldDecl.Declaration.Type).Type;
                        }
                        else if (ctx.Node is Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax propDecl)
                        {
                            typeSymbol = ctx.SemanticModel.GetTypeInfo(propDecl.Type).Type;
                        }
                        
                        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                            return null;

                        // 只收集闭合泛型类型（排除开放泛型和非泛型）
                        if (!namedTypeSymbol.IsGenericType || namedTypeSymbol.IsOpenGeneric())
                            return null;

                        // 检查是否是 Entity 或 SphereEventArgs 子类
                        var isEntitySubclass = IsEntitySubclass(namedTypeSymbol);
                        var isSphereEventArgsSubclass = IsSphereEventArgsSubclass(namedTypeSymbol);

                        return new ClosedGenericTypeInfo
                        {
                            Symbol = namedTypeSymbol,
                            IsEntity = isEntitySubclass,
                            IsSphereEventArgs = isSphereEventArgsSubclass
                        };
                    })
                .Where(info => info != null)
                .Collect();

            // 组合编译信息、MemoryPackable 类型和闭合泛型类型
            var compilationAndTypes = context.CompilationProvider
                .Combine(memoryPackableTypesProvider)
                .Combine(closedGenericTypesProvider);

            // 注册源代码输出 - 生成 MemoryPackInitializer
            context.RegisterSourceOutput(compilationAndTypes, (spc, source) =>
            {
                var ((compilation, types), closedGenericTypes) = source;

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

                GenerateMemoryPackCode(spc, compilation, types!, closedGenericTypes!);
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
            IEnumerable<TypeInfo> types,
            IEnumerable<ClosedGenericTypeInfo> closedGenericTypes)
        {
            var typesList = types.ToList();
            var closedTypesList = closedGenericTypes?.ToList() ?? new List<ClosedGenericTypeInfo>();
            
            var targetPlatform = CompilationHelper.GetTargetPlatform(compilation);
            var className = compilation.GetAssemblyName("MemoryPackInitializer", out var assemblyName, out _);

            // 按 ConstructedFrom 分组闭合泛型类型
            var closedTypesByConstructedFrom = GroupTypesByConstructedFrom(closedTypesList);

            // 为每个标记了 [MemoryPackable] 的类型进行处理
            foreach (var typeInfo in typesList)
            {
                var symbol = typeInfo.Symbol;
                
                // 判断是否为开放泛型
                typeInfo.IsOpenGeneric = symbol.IsOpenGeneric();
                
                if (typeInfo.IsOpenGeneric)
                {
                    // 开放泛型：匹配闭合泛型实例
                    var constructedFrom = symbol.ConstructedFrom;
                    
                    // 查找匹配的闭合泛型类型
                    if (closedTypesByConstructedFrom.TryGetValue(constructedFrom, out var matchingClosedTypes))
                    {
                        foreach (var closedType in matchingClosedTypes)
                        {
                            // 验证泛型参数数量是否匹配
                            if (closedType.TypeArguments.Length != symbol.TypeParameters.Length)
                                continue;
                            
                            try
                            {
                                // 构造闭合的 MemoryPackable 类型
                                var openSymbol = symbol.ConstructedFrom;
                                var constructedSymbol = openSymbol.Construct(closedType.TypeArguments.ToArray());
                                
                                // 添加到闭合泛型集合（HashSet 自动去重）
                                typeInfo.ClosedGenerics.Add(constructedSymbol);
                            }
                            catch
                            {
                                // 构造失败（泛型约束不满足等），跳过
                            }
                        }
                    }
                    
                    // 关键：开放泛型定义本身不添加到输出
                    // 只有其闭合泛型实例会被输出
                }
            }

            // 分离类型：只输出非泛型和闭合泛型
            // 1. Entity 类型（非泛型 + 从开放泛型构造的闭合泛型）
            var entityTypes = typesList.Where(t => t.IsEntity && !t.IsOpenGeneric).ToList();
            
            // 2. SphereEventArgs 类型
            var sphereEventArgs = typesList.Where(t => t.IsSphereEventArgs && !t.IsOpenGeneric).ToList();
            
            // 3. 普通类型（非 Entity，非泛型 + 从开放泛型构造的闭合泛型）
            var normalTypes = typesList.Where(t => !t.IsEntity && !t.IsSphereEventArgs && !t.IsOpenGeneric).ToList();
            
            // 4. 收集所有有闭合泛型的开放泛型定义（用于传递闭合泛型信息）
            var openGenericTypesWithClosures = typesList.Where(t => t.IsOpenGeneric && t.ClosedGenerics.Any()).ToList();

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
            builder.AddXmlComment("Note: Only closed generic types are registered, not open generic definitions");

            // 开始类定义，实现 IMemoryPackEntityGenerator 接口
            builder.BeginClass(className, "public sealed", "Fantasy.Assembly.IMemoryPackEntityGenerator");

            // 添加字段
            builder.AppendLine("private static bool _initialized;");
            builder.AppendLine();

            // 生成 Entity和SphereEventArgs 序列化方法
            GenerateEntityArrays(builder, entityTypes, sphereEventArgs, openGenericTypesWithClosures);
            builder.AppendLine();

            // 生成初始化方法
            GenerateInitializeMethod(builder, normalTypes, openGenericTypesWithClosures);

            // 结束类
            builder.EndClass();
            builder.AppendLine();

            // 结束命名空间
            builder.EndNamespace();

            // 输出源代码
            context.AddSource($"{className}.g.cs", builder.ToString());
        }

        private static void GenerateEntityArrays(
            SourceCodeBuilder builder, 
            List<TypeInfo> entityTypes,  // 非泛型 Entity
            List<TypeInfo> sphereEventArgs,  // 非泛型 SphereEventArgs
            List<TypeInfo> openGenericTypesWithClosures)  // 开放泛型（用于获取闭合泛型）
        {
            // 生成 EntityTypeHashCodes() 方法
            builder.AddXmlComment("Gets the TypeHashCode array of all Entity subclasses marked with [MemoryPackable] in this assembly");
            builder.AddXmlComment("Includes non-generic types and closed generic instantiations only");
            builder.AddXmlComment("Open generic definitions (e.g., TestEntity<T>) are NOT included");
            builder.BeginMethod("public long[] EntityTypeHashCodes()");
            builder.AppendLine("return new long[]");
            builder.OpenBrace();
            
            // 1. 添加非泛型 Entity 类型
            foreach (var typeInfo in entityTypes)
            {
                var typeHashCode = HashCodeHelper.ComputeHash64(typeInfo.Symbol.GetFullName(false));
                builder.AppendLine($"{typeHashCode}L, // {typeInfo.Symbol.ToDisplayString()}");
            }
            
            // 2. 添加从开放泛型构造的闭合泛型 Entity
            foreach (var openGeneric in openGenericTypesWithClosures.Where(t => t.IsEntity))
            {
                foreach (var closedGeneric in openGeneric.ClosedGenerics)
                {
                    var closedHashCode = HashCodeHelper.ComputeHash64(closedGeneric.GetFullName(false));
                    builder.AppendLine($"{closedHashCode}L, // {closedGeneric.ToDisplayString()}");
                }
            }

            // 3. 添加非泛型 SphereEventArgs 类型
            foreach (var typeInfo in sphereEventArgs)
            {
                var typeHashCode = HashCodeHelper.ComputeHash64(typeInfo.Symbol.GetFullName(false));
                builder.AppendLine($"{typeHashCode}L, // {typeInfo.Symbol.ToDisplayString()}");
            }
            
            // 4. 添加从开放泛型构造的闭合泛型 SphereEventArgs
            foreach (var openGeneric in openGenericTypesWithClosures.Where(t => t.IsSphereEventArgs))
            {
                foreach (var closedGeneric in openGeneric.ClosedGenerics)
                {
                    var closedHashCode = HashCodeHelper.ComputeHash64(closedGeneric.GetFullName(false));
                    builder.AppendLine($"{closedHashCode}L, // {closedGeneric.ToDisplayString()}");
                }
            }

            builder.CloseBrace(semicolon: true);
            builder.EndMethod();
            builder.AppendLine();

            // 生成 EntityTypes() 方法
            builder.AddXmlComment("Gets the Type array of all Entity subclasses marked with [MemoryPackable] in this assembly");
            builder.AddXmlComment("Includes non-generic types and closed generic instantiations only");
            builder.AddXmlComment("Open generic definitions (e.g., TestEntity<T>) are NOT included");
            builder.BeginMethod("public System.Type[] EntityTypes()");
            builder.AppendLine("return new System.Type[]");
            builder.OpenBrace();

            // 1. 添加非泛型 Entity 类型
            foreach (var typeInfo in entityTypes)
            {
                var fullName = typeInfo.Symbol.GetFullName(true);
                builder.AppendLine($"typeof({fullName}),");
            }
            
            // 2. 添加从开放泛型构造的闭合泛型 Entity
            foreach (var openGeneric in openGenericTypesWithClosures.Where(t => t.IsEntity))
            {
                foreach (var closedGeneric in openGeneric.ClosedGenerics)
                {
                    var closedFullName = closedGeneric.GetFullName(true);
                    builder.AppendLine($"typeof({closedFullName}),");
                }
            }

            // 3. 添加非泛型 SphereEventArgs 类型
            foreach (var typeInfo in sphereEventArgs)
            {
                var fullName = typeInfo.Symbol.GetFullName(true);
                builder.AppendLine($"typeof({fullName}),");
            }
            
            // 4. 添加从开放泛型构造的闭合泛型 SphereEventArgs
            foreach (var openGeneric in openGenericTypesWithClosures.Where(t => t.IsSphereEventArgs))
            {
                foreach (var closedGeneric in openGeneric.ClosedGenerics)
                {
                    var closedFullName = closedGeneric.GetFullName(true);
                    builder.AppendLine($"typeof({closedFullName}),");
                }
            }

            builder.CloseBrace(semicolon: true);
            builder.EndMethod();
        }

        private static void GenerateInitializeMethod(
            SourceCodeBuilder builder,
            List<TypeInfo> normalTypes,  // 非泛型的普通类型
            List<TypeInfo> openGenericTypesWithClosures)  // 开放泛型（用于获取闭合泛型）
        {
            // 生成实例 Initialize() 方法
            builder.AddXmlComment("Initializes MemoryPack serializers and triggers static constructors of all MemoryPackable types");
            builder.AddXmlComment("Includes non-generic types and closed generic instantiations for AOT compatibility");
            builder.AddXmlComment("Open generic definitions (e.g., TestEntity<T>) are NOT registered");
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

            // 1. 为每个非泛型 MemoryPackable 类型触发静态构造函数
            foreach (var typeInfo in normalTypes)
            {
                var fullTypeName = typeInfo.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                
                builder.AddComment($"Non-generic type: {typeInfo.Symbol.Name}");
                builder.AppendLine($"RuntimeHelpers.RunClassConstructor(typeof({fullTypeName}).TypeHandle);");
            }
            
            if (normalTypes.Any())
            {
                builder.AppendLine();
            }

            // 2. 为每个闭合泛型实例触发静态构造函数（关键：不注册开放泛型定义）
            foreach (var openGeneric in openGenericTypesWithClosures)
            {
                if (!openGeneric.ClosedGenerics.Any())
                    continue;
                    
                builder.AddComment($"Closed generic instantiations of {openGeneric.Symbol.Name}");
                
                foreach (var closedGeneric in openGeneric.ClosedGenerics)
                {
                    var closedFullTypeName = closedGeneric.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    builder.AppendLine($"RuntimeHelpers.RunClassConstructor(typeof({closedFullTypeName}).TypeHandle);");
                }
                builder.AppendLine();
            }

            // 结束方法
            builder.EndMethod();
        }

        /// <summary>
        /// 按 ConstructedFrom 对闭合泛型类型进行分组
        /// 用于后续与开放泛型定义进行匹配
        /// </summary>
        private static Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> GroupTypesByConstructedFrom(
            IEnumerable<ClosedGenericTypeInfo> closedTypes)
        {
            var dict = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
            
            foreach (var closedType in closedTypes)
            {
                var key = closedType.Symbol.ConstructedFrom;
                if (!dict.TryGetValue(key, out var list))
                {
                    list = new List<INamedTypeSymbol>();
                    dict.Add(key, list);
                }
                list.Add(closedType.Symbol);
            }
            
            return dict;
        }

        private class TypeInfo
        {
            public INamedTypeSymbol Symbol { get; set; }
            public bool IsEntity { get; set; }
            public bool IsSphereEventArgs { get; set; }
            public bool IsOpenGeneric { get; set; }  // 是否为开放泛型
            public HashSet<INamedTypeSymbol> ClosedGenerics { get; set; } = new(SymbolEqualityComparer.Default);  // 去重的闭合泛型集合
        }

        private class ClosedGenericTypeInfo
        {
            public INamedTypeSymbol Symbol { get; set; }
            public bool IsEntity { get; set; }
            public bool IsSphereEventArgs { get; set; }
        }
    }
}
