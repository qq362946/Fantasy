using System;
using Fantasy.SourceGenerator.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
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
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 查找所有 System 类
            var systemTypes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsSystemClass(node),
                    transform: static (ctx, _) => GetSystemSymbol(ctx))
                .Where(static symbol => symbol != null)
                .Collect();

            // 查找代码中所有使用到的闭合泛型 Entity
            var usedClosedGenericEntities = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsGenericEntityUsage(node),
                    transform: static (ctx, _) => GetClosedEntitySymbol(ctx))
                .Where(static symbol => symbol != null)
                .Collect()
                .Select(static (types, _) => types.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToImmutableArray());

            // 根据 闭合Entity 处理 System
            var finalSystemInfos = systemTypes.Combine(usedClosedGenericEntities)
                .Select((tuple, _) =>
            {
                var systems = tuple.Left;
                var usedEntities = tuple.Right;
                var result = new List<EntitySystemTypeInfo>();

                foreach (var systemSymbol in systems)
                {
                    // 泛型System
                    if (systemSymbol!.IsOpenGeneric()) 
                    {
                        foreach (var entitySymbol in usedEntities)
                        {
                            // 尝试为 闭合Entity 构造 闭合System
                            if (TryCreateClosedSystemInfo(systemSymbol!, entitySymbol!, out var info))
                            {
                                result.Add(info!);
                            }
                        }
                    }
                    else // 普通 System
                    {
                        var info = CreateInfoFromSymbol(systemSymbol!);
                        if (info != null)
                        {
                            result.Add(info);
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

        private static INamedTypeSymbol? GetSystemSymbol(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
            if (symbol == null || symbol.IsAbstract) return null; // 忽略抽象类

            // 目标实体不能是闭合泛型
            var targetEntityArg = GetSystemTargetEntityArg(symbol!);
            if(targetEntityArg == null) return null;
            if(targetEntityArg.IsGenericType && !targetEntityArg.IsOpenGeneric()) return null;

            return symbol;
        }

        // 判断代码中使用到的闭合泛型 Entity
        private static bool IsGenericEntityUsage(SyntaxNode node) => node is GenericNameSyntax;

        private static INamedTypeSymbol? GetClosedEntitySymbol(GeneratorSyntaxContext context)
        {
            if (context.Node is not GenericNameSyntax genericName) return null;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(genericName);
            var symbol = symbolInfo.Symbol as INamedTypeSymbol;

            if (symbol == null) return null;
            if (symbol.IsOpenGeneric()) return null; // 确保是闭合泛型

            // 检查继承自 Entity
            if (!EntityTypeCollectionGenerator.InheritsFromEntitySymbol(symbol)) return null;

            return symbol;
        }

        #endregion

        #region Generic Analysis

        /// <summary>
        /// 获取System类的目标实体参数
        /// </summary>
        private static INamedTypeSymbol? GetSystemTargetEntityArg(INamedTypeSymbol systemSymbol) {
            var baseType = systemSymbol.BaseType;

            if (!baseType.IsGenericType) 
                return null;

            return baseType.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        }

        ///<summary>
        /// NOTE : 需要提高完备性
        /// 尝试将一个 开放泛型System 和一个 闭合泛型Entity 进行匹配, 
        /// 并生成闭合的 System 信息。
        ///</summary>
        private static bool TryCreateClosedSystemInfo(INamedTypeSymbol systemSymbol, INamedTypeSymbol entitySymbol, out EntitySystemTypeInfo? info)
        {
            // 1. 获取 System 关注的实体基类型 (从 AwakeSystem<T> 中提取 T)
            var targetEntityArg = GetSystemTargetEntityArg(systemSymbol);
            info = null;

            // 2. 检查 Entity 是否匹配 System 的定义

            // 比如 泛型System 的参数结构 AGenericEntity<T1, T2> 与对应的实体 AGenericEntity<EnumA, EnumB>, 
            // 它们的 ConstructedFrom 必须吻合

            if (!SymbolEqualityComparer.Default.Equals(targetEntityArg.ConstructedFrom, entitySymbol.ConstructedFrom))
                return false;

            // 3. 检查参数数量是否相等
            var systemTypeArgs = entitySymbol.TypeArguments.ToArray();
            if (systemTypeArgs.Length != systemSymbol.TypeParameters.Length) return false;

            // 4. 构造闭合的 System 类型
            // 使用 Entity 的泛型参数 (EnumA, EnumB) 来填充 System 的泛型参数
            try
            {
                var constructedSystemSymbol = systemSymbol.Construct(systemTypeArgs);
                info = CreateInfoFromSymbol(constructedSystemSymbol);
                return info != null;
            }
            catch
            {
                return false;
            }
        }

        private static EntitySystemTypeInfo? CreateInfoFromSymbol(INamedTypeSymbol symbol)
        {
            var baseType = symbol.BaseType;
            if (baseType == null) return null;

            var typeName = baseType.ConstructedFrom.Name;

            return typeName switch
            {
                "AwakeSystem" => EntitySystemTypeInfo.Create(EntitySystemType.AwakeSystem, symbol),
                "UpdateSystem" => EntitySystemTypeInfo.Create(EntitySystemType.UpdateSystem, symbol),
                "DestroySystem" => EntitySystemTypeInfo.Create(EntitySystemType.DestroySystem, symbol),
                "DeserializeSystem" => EntitySystemTypeInfo.Create(EntitySystemType.DeserializeSystem, symbol),
                "LateUpdateSystem" => EntitySystemTypeInfo.Create(EntitySystemType.LateUpdateSystem, symbol),
                _ => null
            };
        }

        #endregion

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
            // 开始命名空间（固定使用 Fantasy.Generated）
            builder.BeginNamespace("Fantasy.Generated");
            // 开始类定义（实现 IEntitySystemRegistrar 接口）
            builder.AddXmlComment($"Auto-generated Entity System registration class for {assemblyName}");
            builder.BeginClass(markerClassName, "internal sealed", "global::Fantasy.Assembly.IEntitySystemRegistrar");
            // 生成Code
            GenerateCode(builder, entitySystemTypeInfos);
            // 结束类和命名空间
            builder.EndClass();
            builder.EndNamespace();
            // 输出源代码
            context.AddSource($"{markerClassName}.g.cs", builder.ToString());
        }

        private static void GenerateCode(SourceCodeBuilder builder, List<EntitySystemTypeInfo> entitySystemTypeInfos)
        {
            var awake = new List<EntitySystemTypeInfo>();
            var update = new List<EntitySystemTypeInfo>();
            var destroy = new List<EntitySystemTypeInfo>();
            var deserialize = new List<EntitySystemTypeInfo>();
            var lateUpdate = new List<EntitySystemTypeInfo>();
            
            if (entitySystemTypeInfos.Any())
            {
                foreach (var systemTypeInfo in entitySystemTypeInfos)
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
        
        private static bool IsSystemClass(SyntaxNode node)
        {
            if (node is not ClassDeclarationSyntax classDecl)
                return false;

            // 必须有基类型列表（继承抽象类）
            if (classDecl.BaseList == null || classDecl.BaseList.Types.Count == 0)
                return false;

            // 快速检查是否包含可能的 EntitySystem 基类名称
            var baseListText = classDecl.BaseList.ToString();
            return baseListText.Contains("AwakeSystem") ||
                   baseListText.Contains("UpdateSystem") ||
                   baseListText.Contains("DestroySystem") ||
                   baseListText.Contains("DeserializeSystem") ||
                   baseListText.Contains("LateUpdateSystem");
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
            public readonly EntitySystemType EntitySystemType;
            public readonly string GlobalTypeFullName;
            // public readonly string TypeFullName;
            // public readonly string TypeName;
            // public readonly long EntityTypeHashCode;  // 预计算的实体类型哈希值
            public readonly string EntityTypeFullName; // 实体类型的完整名称（用于注释）

            private EntitySystemTypeInfo(
                EntitySystemType entitySystemType,
                string globalTypeFullName,
                // string typeFullName,
                // string typeName,
                // long entityTypeHashCode,
                string entityTypeFullName)
            {
                EntitySystemType = entitySystemType;
                GlobalTypeFullName = globalTypeFullName;
                // TypeFullName = typeFullName;
                // TypeName = typeName;
                // EntityTypeHashCode = entityTypeHashCode;
                EntityTypeFullName = entityTypeFullName;
            }

            public static EntitySystemTypeInfo Create(EntitySystemType entitySystemType, INamedTypeSymbol symbol)
            {
                // 获取泛型参数 T (例如：AwakeSystem<T> 中的 T)
                var entityType = GetSystemTargetEntityArg(symbol);
                var entityTypeFullName = entityType?.GetFullName(false) ?? "Unknown";

                // // 使用与运行时相同的算法计算哈希值
                // var entityTypeHashCode = HashCodeHelper.ComputeHash64(entityTypeFullName);

                return new EntitySystemTypeInfo(
                    entitySystemType,
                    symbol.GetFullName(),
                    // symbol.GetFullName(false),
                    // symbol.Name,
                    // entityTypeHashCode,
                    entityTypeFullName);
            }
        }
    }
}
