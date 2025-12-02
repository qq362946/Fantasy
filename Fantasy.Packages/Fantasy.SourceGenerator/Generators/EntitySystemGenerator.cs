using System;
using Fantasy.SourceGenerator.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
// ReSharper disable VariableHidesOuterVariable
// ReSharper disable CollectionNeverUpdated.Local
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Fantasy.SourceGenerator.Generators
{
    /// <summary>
    /// Entity System 注册代码生成器
    /// 自动生成 EntityComponent 所需的 System 注册代码，替代运行时反射
    /// </summary>
    [Generator]
    public partial class EntitySystemGenerator : IIncrementalGenerator
    {
        const string SystemGenericMapAttributeFullName = $"{GeneratorConstants.GeneratedNamespace}.SystemGenericMapAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 查找当前程序集所有 System ,转换为包装器
            var systemWrappers = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsSystemClass(node),
                    transform: static (ctx, _) => GetSystemSymbol(ctx))
                .Where(static symbol => symbol != null)
                .Collect();

            // 查找引用程序集的泛型 System           
            var referencedAssemblies = context.CompilationProvider
                .GetAllReferencedAssembliesProvider(filterAttr: SystemGenericMapAttributeFullName, selfContained: false);

            // 引用程序集中的泛型 System 转换为包装器
            var genericWrappers = referencedAssemblies
                .SelectMany((assemblySymbol, _) => GetSystemSymbolsFromAssemblyAttr(assemblySymbol))
                .Collect();

#if SG_DEBUG
            // 打印标记了[SystemGenericMap]的引用程序集
            context.RegisterSourceOutput(referencedAssemblies, (spc, assemblySymbol) =>
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DebugFantasySG.OneInfoDescriptor,
                    Location.None,
                    "SystemGenericMapDebug",
                    $"Referenced assembly:{assemblySymbol.Name}"
                ));
            });
            // 打印泛型System映射
            context.RegisterSourceOutput(genericWrappers, (spc, wrappers) =>
            {
                foreach (var wrapper in wrappers)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DebugFantasySG.OneInfoDescriptor,
                        Location.None,
                        "SystemGenericMapDebug",
                        "Found: " + wrapper.Symbol.ToDisplayString() + " mapping to" + wrapper.TargetEntityArg?.ToDisplayString() ?? "null"
                    ));
                }
            });
#endif
            // 合并所有包装器
            var allSystemWrappers = systemWrappers
                .Combine(genericWrappers)
                .Select(static (tuple, _) =>
                {
                    var (systems, generics) = tuple;
                    var capacity = systems.Length + generics.Length;

                    var builder = ImmutableArray.CreateBuilder<SystemSymbolWrapper>(capacity);
                    builder.AddRange(systems!);
                    builder.AddRange(generics);

                    return builder.MoveToImmutable();
                });

            // 查找代码中所有使用到的闭合泛型 Entity
            var usedClosedGenericEntities = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsGenericEntityUsage(node),
                    transform: static (ctx, _) => GetClosedEntitySymbol(ctx))
                .Where(static symbol => symbol != null)
                .Collect()
                .Select(static (types, _) =>
                    types.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToImmutableArray());

#if SG_DEBUG
            // 收集全面的调试信息, 以便分析 EntitySystem 构造异常的原因
            var debugInfo = allSystemWrappers
            .Combine(usedClosedGenericEntities)
            .Select((tuple, _) =>
            {
                var systemWrappers = tuple.Left;
                var usedEntities = tuple.Right;
                var debugInfoList = new List<DebugInfoForSystem>();
                var entitiesByConstructedFrom = GroupEntitiesByConstructedFrom(usedEntities);

                foreach (SystemSymbolWrapper? systemWrapper in systemWrappers)
                {
                    var systemSymbol = systemWrapper.Symbol;
                    string systemName = systemSymbol.ToDisplayString();
                    string entityName = systemWrapper.TargetEntityArg.ToDisplayString();

                    if (!systemWrapper.IsOpenGeneric)
                    {
                        // 非泛型System
                        debugInfoList.Add(new DebugInfoForSystem(systemName, entityName, false, 0, 0, false,systemSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), null));
                        continue;
                    }

                    var targetEntityArg = systemWrapper.TargetEntityArg;
                    int systemTypeParamCount = systemWrapper.TypeParameterCount;

                    if (!entitiesByConstructedFrom.TryGetValue(targetEntityArg.ConstructedFrom, out var matchingEntities))
                    {
                        // 没有匹配的实体
                        debugInfoList.Add(new DebugInfoForSystem(systemName, entityName, true,systemTypeParamCount,0, true, null, null));
                        continue;
                    }

                    foreach (var entitySymbol in matchingEntities)
                    {
                        int entityTypeArgCount = entitySymbol.TypeArguments.Length;

                        if (entityTypeArgCount != systemTypeParamCount)
                        {
                            // 泛型参数数量不匹配
                            debugInfoList.Add(new DebugInfoForSystem(systemName, entityName, true,systemTypeParamCount, entityTypeArgCount,false, null,null));
                            continue;
                        }

                        try
                        {
                            // 确保获取的是开放泛型定义
                            INamedTypeSymbol openSystemSymbol = systemSymbol.ConstructedFrom;

                            // 构造闭合的 System 类型
                            INamedTypeSymbol constructedSystemSymbol = openSystemSymbol.Construct(entitySymbol.TypeArguments.ToArray());
                            debugInfoList.Add(new DebugInfoForSystem(systemName, entityName, true, systemTypeParamCount, entityTypeArgCount, false, constructedSystemSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), null));
                        }
                        catch (Exception ex)
                        {
                            // 构造失败
                            debugInfoList.Add(new DebugInfoForSystem(systemName, entityName, true,systemTypeParamCount,entityTypeArgCount,false,null,ex));
                        }
                    }
                }

                return debugInfoList;
            });
            // Debug输出
            context.RegisterSourceOutput(debugInfo, (spc, info) =>
            {
                // 分析问题, 构造调试信息
                foreach (DebugInfoForSystem? item in info)
                {
                    string messageCategory;
                    string messageDetail;

                    if (!item.IsOpenGeneric)
                    {
                        messageCategory = "CommonSystem(NonGeneric)";
                        messageDetail = $"System: {item.ConstructedSystemSymbolName}";
                    }
                    else if (item.NoMatchingEntities)
                    {
                        messageCategory = "OpenGenericSystem_NoMatch";
                        messageDetail = $"{item.SystemType} with {item.EntityType} System Type Params: {item.SystemTypeParameterCount}. No matching Entities found.";
                    }
                    else if (item.Exception != null)
                    {
                        messageCategory = "OpenGenericSystem_ConstructionError";
                        messageDetail = $"{item.SystemType} with {item.EntityType} System Type Params: {item.SystemTypeParameterCount}, Entity Type Args: {item.EntityTypeArgumentCount}. Exception: {item.Exception.Message}";
                    }
                    else if (item.ConstructedSystemSymbolName != null)
                    {
                        messageCategory = "OpenGenericSystem_Success";
                        messageDetail = $"{item.SystemType} with {item.EntityType} . Constructed System: {item.ConstructedSystemSymbolName}";
                    }
                    else
                    {
                        messageCategory = "OpenGenericSystem_Mismatch";
                        messageDetail = $"{item.SystemType} with {item.EntityType} System Type Params: {item.SystemTypeParameterCount}, Entity Type Args: {item.EntityTypeArgumentCount}. Count Mismatch.";
                    }
                    spc.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DebugFantasySG.OneInfoDescriptor,
                        location: Location.None,
                        messageCategory, // {0}
                        messageDetail // {1}
                    ));
                }
            });
#endif

            // 根据 闭合Entity 处理 System
            var finalSystemInfos = allSystemWrappers.Combine(usedClosedGenericEntities)
                .Select((tuple, _) =>
                {
                    var systemWrappers = tuple.Left;
                    var usedEntities = tuple.Right;
                    var result = new List<EntitySystemTypeInfo>();
                    var entitiesByConstructedFrom = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

                    foreach (INamedTypeSymbol? entity in usedEntities)
                    {
                        var key = entity.ConstructedFrom;

                        if (!entitiesByConstructedFrom.TryGetValue(key, out var list))
                        {
                            list = new List<INamedTypeSymbol>();
                            entitiesByConstructedFrom.Add(key, list);
                        }

                        list.Add(entity);
                    }

                    foreach (SystemSymbolWrapper? systemWrapper in systemWrappers)
                    {
                        var systemSymbol = systemWrapper.Symbol;

                        // 创建System信息
                        var info = CreateInfoFromInstantiableSymbol(systemSymbol, systemWrapper.SceneTypes);
                        if (info != null)
                        {
                            result.Add(info);
                        }

                        if (systemWrapper.IsOpenGeneric)
                        {
                            var targetEntityArg = systemWrapper.TargetEntityArg;

                            var systemTypeParamCount = systemWrapper.TypeParameterCount;

                            if (!entitiesByConstructedFrom.TryGetValue(targetEntityArg.ConstructedFrom, out var matchingEntities))
                                continue; // 没有匹配的实体，跳过

                            foreach (var entitySymbol in matchingEntities)
                            {
                                if (entitySymbol.TypeArguments.Length != systemTypeParamCount)
                                    continue;  // 快速验证泛型参数数量（提前过滤）

                                try
                                {
                                    // 确保获取的是开放泛型定义
                                    INamedTypeSymbol openSystemSymbol = systemSymbol.ConstructedFrom;
                                    // 构造闭合的 System 类型并创建System信息
                                    var constructedSystemSymbol = openSystemSymbol.Construct(entitySymbol.TypeArguments.ToArray());
                                    var closedGnericInfo = CreateInfoFromInstantiableSymbol(constructedSystemSymbol, systemWrapper.SceneTypes);
                                    if (closedGnericInfo != null)
                                    {
                                        result.Add(closedGnericInfo);
                                    }
                                }
                                catch
                                {
                                    // 构造失败（泛型约束不满足等）
                                }
                            }
                        }
                    }

                    return result;
                });

            //组合编译信息输出
            var source = context.CompilationProvider.Combine(finalSystemInfos);
            context.RegisterSourceOutput(source, (spc, source) =>
            {
                if (CompilationHelper.IsSourceGeneratorDisabled(source.Left)) return;
                if (!CompilationHelper.HasFantasyDefine(source.Left)) return;
                if (source.Left.GetTypeByMetadataName("Fantasy.Assembly.IEntitySystemRegistrar") == null) return;

                GenerateRegistrationCode(spc, source.Left, source.Right);
            });
        }


        #region Predicate & Transform

        private static SystemSymbolWrapper? GetSystemSymbol(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

            if (symbol == null || symbol.IsAbstract)
            {
                return null; // 忽略抽象类
            }

            // 计算目标实体类型
            var targetEntityArg = GetSystemTargetEntityArg(symbol);

            if (targetEntityArg == null) 
                return null;

            // 排除闭合泛型, 忽略用户自行闭合的情况 (避免和生成器自动生成的闭合冲突)
            if (targetEntityArg.IsGenericType && !targetEntityArg.IsOpenGeneric()) 
                return null;

            // 提取 SceneType 值
            var sceneTypes = GetSceneTypesFromAttributes(symbol);

            return new SystemSymbolWrapper(symbol, targetEntityArg, sceneTypes.ToArray());
        }

        /// <summary>
        /// 从符号的 SceneAttribute 中提取 SceneType 列表
        /// </summary>
        private static HashSet<int> GetSceneTypesFromAttributes(ISymbol symbol)
        {
            var sceneTypes = new HashSet<int>();

            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "SceneAttribute" &&
                    attribute.AttributeClass.ContainingNamespace?.ToDisplayString() == "Fantasy")
                {
                    if (attribute.ConstructorArguments.Length > 0)
                    {
                        var arg = attribute.ConstructorArguments[0];
                        if (arg.Value is int sceneType)
                        {
                            sceneTypes.Add(sceneType);
                        }
                    }
                }
            }
            return sceneTypes;
        }

        private static List<SystemSymbolWrapper> GetSystemSymbolsFromAssemblyAttr(IAssemblySymbol assemblySymbol)
        {
            var genericWrappers = new List<SystemSymbolWrapper>();

            // 获取作用在程序集上的所有 Attribute
            foreach (AttributeData attributeData in assemblySymbol.GetAttributes())
            {
                if (attributeData.AttributeClass is null) 
                    continue;

                // 检查 Attribute 的完全限定名是否匹配              
                string attributeFullName = attributeData.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (attributeFullName.Contains(SystemGenericMapAttributeFullName))
                {
                    // 提取参数
                    if (attributeData.ConstructorArguments[0].Value is INamedTypeSymbol systemTypeSymbol &&
                        attributeData.ConstructorArguments[1].Value is INamedTypeSymbol entityTypeSymbol)
                    {
                        genericWrappers.Add(new SystemSymbolWrapper(
                            symbol: systemTypeSymbol,
                            targetEntityArg: entityTypeSymbol,
                            null!  /// Note : 注意这里, SceneTypes 目前不进行跨程序集传递，留空了, 以后处理!
                        ));
                    }
                }
            }
            return genericWrappers;
        }

        // 判断代码中使用到的闭合泛型 Entity
        private static bool IsGenericEntityUsage(SyntaxNode node) => node is GenericNameSyntax;

        private static INamedTypeSymbol? GetClosedEntitySymbol(GeneratorSyntaxContext context)
        {
            if (context.Node is not GenericNameSyntax genericName)
            {
                return null;
            }

            var symbolInfo = context.SemanticModel.GetSymbolInfo(genericName);
            var symbol = symbolInfo.Symbol as INamedTypeSymbol;

            if (symbol == null)
            {
                return null;
            }
            
            if (symbol.IsOpenGeneric())
            {
                return null; // 确保是闭合泛型
            }
            
            return EntityTypeCollectionGenerator.InheritsFromEntitySymbol(symbol) ? symbol : null;
        }

        /// <summary>
        /// 通过 ConstructedFrom 对实体进行分组
        /// </summary>
        private static Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>> GroupEntitiesByConstructedFrom(IEnumerable<INamedTypeSymbol> entities)
        {
            var dict = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
            foreach (var entity in entities)
            {
                var key = entity.ConstructedFrom;
                if (!dict.TryGetValue(key, out var list))
                {
                    list = new List<INamedTypeSymbol>();
                    dict.Add(key, list);
                }
                list.Add(entity);
            }
            return dict;
        }

        #endregion

        #region Generic Analysis
              
        /// <summary>
        /// 获取System类的目标实体参数
        /// </summary>
        private static INamedTypeSymbol? GetSystemTargetEntityArg(INamedTypeSymbol systemSymbol) 
        {
            var baseType = systemSymbol.BaseType;

            if (!baseType.IsGenericType)
            {
                return null;
            }

            return baseType.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        }

        /// <summary>
        /// 从可实例化的System的Symbol中创建<see cref="EntitySystemTypeInfo"/>。
        /// </summary>
        private static EntitySystemTypeInfo? CreateInfoFromInstantiableSymbol(INamedTypeSymbol instantiableSystemSymbol,int[] sceneTypes)
        {
            var baseType = instantiableSystemSymbol.BaseType;
            
            if (baseType == null)
            {
                return null;
            }

            var typeName = baseType.ConstructedFrom.Name;

            return typeName switch
            {
                "AwakeSystem" => EntitySystemTypeInfo.Create(EntitySystemType.AwakeSystem, instantiableSystemSymbol, sceneTypes),
                "UpdateSystem" => EntitySystemTypeInfo.Create(EntitySystemType.UpdateSystem, instantiableSystemSymbol, sceneTypes),
                "DestroySystem" => EntitySystemTypeInfo.Create(EntitySystemType.DestroySystem, instantiableSystemSymbol, sceneTypes),
                "DeserializeSystem" => EntitySystemTypeInfo.Create(EntitySystemType.DeserializeSystem, instantiableSystemSymbol, sceneTypes),
                "LateUpdateSystem" => EntitySystemTypeInfo.Create(EntitySystemType.LateUpdateSystem, instantiableSystemSymbol, sceneTypes),
                _ => null
            };
        }

        #endregion

        #region Generate

        private static void GenerateRegistrationCode(
            SourceProductionContext context,
            Compilation compilation,
            IEnumerable<EntitySystemTypeInfo> systemTypes)
        {
            var entitySystemTypeInfos = systemTypes.ToList();
            // 获取当前程序集名称（仅用于注释）
            var markerClassName = compilation.GetAssemblyName("EntitySystemRegistrar", out var assemblyName, out _);
            // 生成代码文件
            var builder = new SourceCodeBuilder();
            // 添加文件头
            builder.AppendLine(GeneratorConstants.AutoGeneratedHeader);
            // 添加 using
            builder.AddUsings(
                "System",
                "System.Collections.Generic",
                "Fantasy.Assembly",
                "Fantasy.Entitas",
                "Fantasy.Entitas.Interface"
            );
            builder.AppendLine();
            // 开始命名空间
            builder.BeginDefaultNamespace();
            // 开始类定义（实现 IEntitySystemRegistrar 接口）
            builder.AddXmlComment($"Auto-generated Entity System registration class for {assemblyName}");
            builder.BeginClass(markerClassName, "internal sealed partial", "global::Fantasy.Assembly.IEntitySystemRegistrar");
            // 生成Code并获取额外元数据
            string metadata = GenerateCode(builder, entitySystemTypeInfos);
            // 结束类和命名空间
            builder.EndClass();
            builder.EndNamespace();
            // 输出源代码
            context.AddSource($"{markerClassName}.g.cs", builder.ToString());

            // 如果是框架顶层程序集，生成 SystemGenericMapAttribute 定义文件
            if (compilation.IsFantasyNetOrFantasyUnity())
            {
                var attributeBuilder = new SourceCodeBuilder();
                attributeBuilder.AppendLine(GeneratorConstants.AutoGeneratedHeader);
                attributeBuilder.AddUsings(
                    "System",
                    "System.Reflection"
                    );
                attributeBuilder.AppendLine();
                attributeBuilder.BeginDefaultNamespace();
                GenerateSystemGenericMapAttributeDefine(attributeBuilder);
                attributeBuilder.EndNamespace();
                context.AddSource($"SystemGenericMapAttribute.g.cs", attributeBuilder.ToString());
            }

            // 如果 metadata 不为空, 输出额外的元数据文件
            if ( !string.IsNullOrEmpty(metadata))
            {
                var metadataBuilder = new SourceCodeBuilder();
                metadataBuilder.AppendLine(GeneratorConstants.AutoGeneratedHeader);
                metadataBuilder.AddUsings("System");
                metadataBuilder.AddUsings(GeneratorConstants.GeneratedNamespace);
                metadataBuilder.AppendLine();
                metadataBuilder.Append(metadata);
                context.AddSource($"{assemblyName}_EntitySystem_Metadata.g.cs", metadataBuilder.ToString());
            }
        }

        /// <summary>
        /// 生成代码, 返回额外的元数据信息(用于跨程序集流式分析)
        /// </summary>
        private static string GenerateCode(SourceCodeBuilder builder, List<EntitySystemTypeInfo> entitySystemTypeInfos)
        {
            string metadata = string.Empty;
            var awake = new List<EntitySystemTypeInfo>();
            var update = new List<EntitySystemTypeInfo>();
            var destroy = new List<EntitySystemTypeInfo>();
            var deserialize = new List<EntitySystemTypeInfo>();
            var lateUpdate = new List<EntitySystemTypeInfo>();
            
            foreach (var systemTypeInfo in entitySystemTypeInfos)
            {
                if(systemTypeInfo.IsGeneric)  // 泛型, 打上 Attribute 以便跨程序集元数据分析使用
                {
                    metadata += MakeSystemGenericMapAttribute(systemTypeInfo)+"\n";
                }
                else  //非泛型, 正常生成代码
                {   
                    switch (systemTypeInfo.EntitySystemType)
                    {
                        case EntitySystemType.AwakeSystem:
                            {
                                awake.Add(systemTypeInfo);

                                continue;
                            }
                        case EntitySystemType.UpdateSystem:
                            {
                                update.Add(systemTypeInfo);
                                continue;
                            }
                        case EntitySystemType.DestroySystem:
                            {
                                destroy.Add(systemTypeInfo);
                                continue;
                            }
                        case EntitySystemType.DeserializeSystem:
                            {
                                deserialize.Add(systemTypeInfo);
                                continue;
                            }
                        case EntitySystemType.LateUpdateSystem:
                            {
                                lateUpdate.Add(systemTypeInfo);
                                continue;
                            }
                    }
                }
            }
            // Awake
            GenerateSystemCode(builder, string.Empty, "AwakeTypeHandles", "AwakeHandles", awake);
            // Update
            GenerateSystemCode(builder, string.Empty, "UpdateTypeHandles", "UpdateHandles", update);
            // Destroy
            GenerateSystemCode(builder, string.Empty, "DestroyTypeHandles", "DestroyHandles", destroy);
            // Deserialize
            GenerateSystemCode(builder, string.Empty, "DeserializeTypeHandles", "DeserializeHandles", deserialize);
            // LateUpdate
            GenerateSystemCode(builder, "#if FANTASY_UNITY", "LateUpdateTypeHandles", "LateUpdateHandles", lateUpdate);
            // Metadata
            return metadata;
        }
        
        private static void GenerateSystemCode(SourceCodeBuilder builder, string defineConstants, string typeHandle, string handler, List<EntitySystemTypeInfo> systemTypeInfos)
        {
            var typeHandleBuilder = new SourceCodeBuilder(2);
            var handlerBuilder = new SourceCodeBuilder(2);

            if (!string.IsNullOrEmpty(defineConstants))
            {
                typeHandleBuilder.AppendLine(defineConstants, false);
                handlerBuilder.AppendLine(defineConstants, false);
            }
            
            typeHandleBuilder.AddXmlComment(typeHandle);
            typeHandleBuilder.BeginMethod($"public global::System.RuntimeTypeHandle[] {typeHandle}()");
            handlerBuilder.AddXmlComment(handler);
            handlerBuilder.BeginMethod($"public global::System.Action<global::Fantasy.Entitas.Entity>[] {handler}()");

            if (systemTypeInfos.Any())
            {
                typeHandleBuilder.AppendLine($"var array = new global::System.RuntimeTypeHandle[{systemTypeInfos.Count}];");
                handlerBuilder.AppendLine($"var array = new global::System.Action<global::Fantasy.Entitas.Entity>[{systemTypeInfos.Count}];");
                for (var i = 0; i < systemTypeInfos.Count; i++)
                {
                    var entitySystemTypeInfo = systemTypeInfos[i];
                    typeHandleBuilder.AppendLine($"array[{i}] = typeof({entitySystemTypeInfo.EntityTypeFullName}).TypeHandle;");
                    handlerBuilder.AppendLine($"array[{i}] = new {entitySystemTypeInfo.GlobalTypeFullName}().Invoke;");
                }
                typeHandleBuilder.AppendLine("return array;");
                handlerBuilder.AppendLine("return array;");
            }
            else
            {
                typeHandleBuilder.AppendLine("return Array.Empty<global::System.RuntimeTypeHandle>();");
                handlerBuilder.AppendLine("return Array.Empty<global::System.Action<global::Fantasy.Entitas.Entity>>();");
            }

            typeHandleBuilder.EndMethod();
            handlerBuilder.EndMethod();
            
            if (!string.IsNullOrEmpty(defineConstants))
            {
                typeHandleBuilder.AppendLine("#endif", false);
                handlerBuilder.AppendLine("#endif", false);
            }

            builder.AppendLine(typeHandleBuilder.ToString(), false);
            builder.AppendLine(handlerBuilder.ToString(), false);
        }

        private static void GenerateSystemGenericMapAttributeDefine(SourceCodeBuilder attributeBuilder)
        {
            attributeBuilder.AppendLine($$"""
                    /// <summary>
                       /// Auto-generated [SystemGenericMap] class define. It will be automatically marked in the Assembly, 
                       /// mainly used for cross-assembly generic information analysis.
                       /// <para>
                       /// 自动生成的 泛型System 映射标签，会在各个程序集中自动标记 ，主要用于跨程序集 泛型System 信息分析。
                       /// </para>
                       /// </summary>
                       [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                       public class SystemGenericMapAttribute : Attribute
                       {
                           /// <summary>
                           /// System Generic Define 
                           /// </summary>
                           public Type SystemType { get; }
                           /// <summary>
                           /// Entity Generic Define 
                           /// </summary>
                           public Type EntityType { get; }

                           /// <summary>
                           /// Needs 2 type args。
                           /// </summary>
                           /// <param name="systemType">Unbound generic system type，such as typeof(System{}).</param>
                           /// <param name="entityType">Unbound entity type，such as typeof(Entity{}).</param>
                           public SystemGenericMapAttribute(Type systemType, Type entityType)
                           {
                               if (!systemType.IsGenericType || !entityType.IsGenericType)
                               {
                                   throw new ArgumentException("[SystemGenericMap] needs 2 Generic Types.");
                               }
        
                               this.SystemType = systemType;
                               this.EntityType = entityType;
                           }
                       }
                    """
                );
        }

        private static string MakeSystemGenericMapAttribute(EntitySystemTypeInfo info)
        {
            // 取得泛型未绑定类型( 传入的信息 需确保已是类似 GenericEntity<,> )
            var unboundEntityName = info.EntityTypeFullName;
            var unboundSystemName = info.GlobalTypeFullName;

            return $@"[assembly:SystemGenericMap(typeof({unboundSystemName}), typeof({unboundEntityName}))]";
        }

        private static bool IsSystemClass(SyntaxNode node)
        {
            if (node is not ClassDeclarationSyntax classDecl)
            {
                return false;
            }

            // 必须有基类型列表（继承抽象类）
            if (classDecl.BaseList == null || classDecl.BaseList.Types.Count == 0)
            {
                return false;
            }

            // 快速检查是否包含可能的 EntitySystem 基类名称
            var baseListText = classDecl.BaseList.ToString();
            return baseListText.Contains("AwakeSystem") ||
                   baseListText.Contains("UpdateSystem") ||
                   baseListText.Contains("DestroySystem") ||
                   baseListText.Contains("DeserializeSystem") ||
                   baseListText.Contains("LateUpdateSystem");
        }
        #endregion

        /// <summary>
        /// 系统符号包装类，携带预先计算的元数据以避免重复判断和访问
        /// </summary>
        private sealed class SystemSymbolWrapper
        {
            public INamedTypeSymbol Symbol { get; }
            public bool IsOpenGeneric { get; }
            public INamedTypeSymbol? TargetEntityArg { get; }  // 预存的目标实体类型
            public int TypeParameterCount { get; }  // 预存的泛型参数数量
            public int[] SceneTypes { get; }  // Note : SceneAttribute 中的 SceneType 值数组, 以后可能会用到

            // ReSharper disable once ConvertToPrimaryConstructor
            public SystemSymbolWrapper(INamedTypeSymbol symbol, INamedTypeSymbol? targetEntityArg, int[] sceneTypes)
            {
                Symbol = symbol;
                TargetEntityArg = targetEntityArg;
                IsOpenGeneric = symbol.IsOpenGeneric();
                TypeParameterCount = symbol.TypeParameters.Length;
                SceneTypes = sceneTypes;
            }
        }

        private enum EntitySystemType
        {
            None,
            AwakeSystem,
            UpdateSystem,
            DestroySystem,
            DeserializeSystem,
            LateUpdateSystem
        }

        private sealed class EntitySystemTypeInfo
        {
            public readonly bool IsGeneric;
            public readonly int[] SceneTypes; //Note : SceneAttribute 中的 SceneType 值数组, 以后可能会用到
            public readonly EntitySystemType EntitySystemType;
            public readonly string GlobalTypeFullName;
            public readonly string EntityTypeFullName; // 实体类型的完整名称（用于注释）

            private EntitySystemTypeInfo(
                bool isGeneric,
                EntitySystemType entitySystemType,
                int[] sceneTypes,
                string globalTypeFullName,
                string entityTypeFullName)
            {
                IsGeneric = isGeneric;
                SceneTypes = sceneTypes;
                EntitySystemType = entitySystemType;
                GlobalTypeFullName = globalTypeFullName;
                EntityTypeFullName = entityTypeFullName;
            }

            public static EntitySystemTypeInfo Create(EntitySystemType entitySystemType, INamedTypeSymbol symbol, int[] sceneTypes)
            {
                // 获取泛型参数 T (例如：AwakeSystem<T> 中的 T)
                var entityType = GetSystemTargetEntityArg(symbol);
                string entityTypeFullName;
                string systemFullName;
                bool isGeneric = symbol.IsOpenGeneric();
                if (isGeneric) //泛型
                {
                    var unbound = entityType.ConstructUnboundGenericType();
                    entityTypeFullName = unbound.ToDisplayString(
                        new SymbolDisplayFormat(
                            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters// 带泛型类型
                        )
                     );
                    var unboundSystem = symbol.ConstructUnboundGenericType();
                    systemFullName = "global::"+unboundSystem.ToDisplayString(
                        new SymbolDisplayFormat(
                            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters // 带泛型类型
                        )
                    );
                }
                else //非泛型
                {
                    entityTypeFullName = entityType?.GetFullName(includeGlobal: false) ?? "Unknown";
                    systemFullName = symbol.GetFullName(includeGlobal: true);
                }

                return new EntitySystemTypeInfo(
                    isGeneric,
                    entitySystemType,
                    sceneTypes,
                    systemFullName,
                    entityTypeFullName);
            }
        }
#if SG_DEBUG
        private record DebugInfoForSystem(
            string SystemType,
            string EntityType,
            bool IsOpenGeneric,
            int SystemTypeParameterCount,
            int EntityTypeArgumentCount,
            bool NoMatchingEntities,
            string? ConstructedSystemSymbolName,
            Exception? Exception
        );
#endif
    }
}
