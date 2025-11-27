using System;
using Fantasy.SourceGenerator.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// ReSharper disable AssignNullToNotNullAttribute

namespace Fantasy.SourceGenerator.Generators
{
    /// <summary>
    /// Event System 注册代码生成器
    /// 自动生成 EventComponent 所需的 Event System 注册代码，替代运行时反射
    /// </summary>
    [Generator]
    public partial class EventSystemGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 查找所有实现了 EventSystem 相关抽象类的类
            var eventSystemTypes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsEventSystemClass(node),
                    transform: static (ctx, _) => GetEventSystemTypeInfo(ctx))
                .Where(static info => info != null)
                .Collect();
            // 组合编译信息和找到的类型
            var compilationAndTypes = context.CompilationProvider.Combine(eventSystemTypes);
            // 注册源代码输出
            context.RegisterSourceOutput(compilationAndTypes, static (spc, source) =>
            {
                if (CompilationHelper.IsSourceGeneratorDisabled(source.Left))
                {
                    return;
                }
                
                if (!CompilationHelper.HasFantasyDefine(source.Left))
                {
                    return;
                }
                
                if (source.Left.GetTypeByMetadataName("Fantasy.Assembly.IEventSystemRegistrar") == null)
                {
                    return;
                }

                GenerateCode(spc, source.Left, source.Right!);
            });
        }

        /// <summary>
        /// 提取 EventSystem 类型信息
        /// </summary>
        private static EventSystemTypeInfo? GetEventSystemTypeInfo(GeneratorSyntaxContext context)
        {
            var classDecl = (ClassDeclarationSyntax)context.Node;

            if (context.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol || !symbol.IsInstantiable())
            {
                return null;
            }

            if (symbol.BaseType == null)
            {
                return null;
            }

            // 向上遍历继承链，查找 EventSystem、AsyncEventSystem 或 SphereEventSystem
            var currentType = symbol.BaseType;
            while (currentType != null)
            {
                if (currentType.IsGenericType)
                {
                    var eventSystemType = currentType.Name switch
                    {
                        "EventSystem" => EventSystemType.EventSystem,
                        "AsyncEventSystem" => EventSystemType.AsyncEventSystem,
                        "SphereEventSystem" => EventSystemType.SphereEventSystem,
                        _ => EventSystemType.None
                    };

                    if (eventSystemType != EventSystemType.None)
                    {
                        return EventSystemTypeInfo.Create(eventSystemType, symbol);
                    }
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// 生成注册代码
        /// </summary>
        private static void GenerateCode(
            SourceProductionContext context,
            Compilation compilation,
            IEnumerable<EventSystemTypeInfo> eventSystemTypes)
        {
            var eventSystems = new List<EventSystemTypeInfo>();
            var asyncEventSystem = new List<EventSystemTypeInfo>();
            var sphereEventSystem = new List<EventSystemTypeInfo>();
            
            foreach (var eventSystemTypeInfo in eventSystemTypes)
            {
                switch (eventSystemTypeInfo.EventSystemType)
                {
                    case EventSystemType.EventSystem:
                    {
                        eventSystems.Add(eventSystemTypeInfo);
                        break;
                    }
                    case EventSystemType.AsyncEventSystem:
                    {
                        asyncEventSystem.Add(eventSystemTypeInfo);
                        break;
                    }
                    case EventSystemType.SphereEventSystem:
                    {
                        sphereEventSystem.Add(eventSystemTypeInfo);
                        break;
                    }
                }
            }

            // 生成Event的注册代码
            GenerateEventCode(context, compilation, eventSystems, asyncEventSystem);
            // 生成SphereEvent
            GenerateSphereCode(context, compilation, sphereEventSystem);
        }

        private static void GenerateEventCode(SourceProductionContext context, Compilation compilation,
            List<EventSystemTypeInfo> eventSystems, List<EventSystemTypeInfo> asyncEventSystem)
        {
            // 获取当前程序集名称（仅用于注释）
            var markerClassName = compilation.GetAssemblyName("EventSystemRegistrar", out var assemblyName, out _);
            // 生成代码文件
            var builder = new SourceCodeBuilder();
            // 添加文件头
            builder.AppendLine(GeneratorConstants.AutoGeneratedHeader);
            // 添加 using
            builder.AddUsings(
                "System",
                "System.Collections.Generic",
                "Fantasy.Assembly",
                "Fantasy.DataStructure.Collection",
                "Fantasy.Event"
            );
            builder.AppendLine();
            // 开始命名空间（固定使用 Fantasy.Generated）
            builder.BeginNamespace("Fantasy.Generated");
            // 开始类定义（实现 IEventSystemRegistrar 接口）
            builder.AddXmlComment($"Auto-generated Event System registration class for {assemblyName}");
            builder.BeginClass(markerClassName, "internal sealed", "global::Fantasy.Assembly.IEventSystemRegistrar");
            // 生成私有字段
            foreach (var eventSystemTypeInfo in eventSystems)
            {
                builder.AppendLine($"private {eventSystemTypeInfo.TypeFullName} {eventSystemTypeInfo.FieldName} = new {eventSystemTypeInfo.TypeFullName}();");
            }
            foreach (var eventSystemTypeInfo in asyncEventSystem)
            {
                builder.AppendLine($"private {eventSystemTypeInfo.TypeFullName} {eventSystemTypeInfo.FieldName} = new {eventSystemTypeInfo.TypeFullName}();");
            }
            builder.AppendLine();
            // EventTypeHandles
            builder.AddXmlComment("EventTypeHandles");
            builder.BeginMethod("public global::System.RuntimeTypeHandle[] EventTypeHandles()");
            if (eventSystems.Any())
            {
                builder.AppendLine($"var handles = new global::System.RuntimeTypeHandle[{eventSystems.Count}];");
                for (var i = 0; i < eventSystems.Count; i++)
                {
                    builder.AppendLine($"handles[{i}] = {eventSystems[i].FieldName}.EventType().TypeHandle;");
                }
                builder.AppendLine("return handles;");
            }
            else
            {
                builder.AppendLine("return Array.Empty<global::System.RuntimeTypeHandle>();");
            }
            builder.EndMethod();
            // Events
            builder.AddXmlComment("Events");
            builder.BeginMethod("public global::Fantasy.Event.IEvent[] Events()");
            if (eventSystems.Any())
            {
                builder.AppendLine($"var events = new global::Fantasy.Event.IEvent[{eventSystems.Count}];");
                for (var i = 0; i < eventSystems.Count; i++)
                {
                    builder.AppendLine($"events[{i}] = {eventSystems[i].FieldName};");
                }
                builder.AppendLine("return events;");
            }
            else
            {
                builder.AppendLine("return Array.Empty<global::Fantasy.Event.IEvent>();");
            }
            builder.EndMethod();
            // AsyncEventTypeHandles
            builder.AddXmlComment("AsyncEventTypeHandles");
            builder.BeginMethod("public global::System.RuntimeTypeHandle[] AsyncEventTypeHandles()");
            if (asyncEventSystem.Any())
            {
                builder.AppendLine($"var handles = new global::System.RuntimeTypeHandle[{asyncEventSystem.Count}];");
                for (var i = 0; i < asyncEventSystem.Count; i++)
                {
                    builder.AppendLine($"handles[{i}] = {asyncEventSystem[i].FieldName}.EventType().TypeHandle;");
                }
                builder.AppendLine("return handles;");
            }
            else
            {
                builder.AppendLine("return Array.Empty<global::System.RuntimeTypeHandle>();");
            }
            builder.EndMethod();
            // AsyncEvents
            builder.AddXmlComment("AsyncEvents");
            builder.BeginMethod("public global::Fantasy.Event.IEvent[] AsyncEvents()");
            if (asyncEventSystem.Any())
            {
                builder.AppendLine($"var events = new global::Fantasy.Event.IEvent[{asyncEventSystem.Count}];");
                for (var i = 0; i < asyncEventSystem.Count; i++)
                {
                    builder.AppendLine($"events[{i}] = {asyncEventSystem[i].FieldName};");
                }
                builder.AppendLine("return events;");
            }
            else
            {
                builder.AppendLine("return Array.Empty<global::Fantasy.Event.IEvent>();");
            }
            builder.EndMethod();
            // 结束类和命名空间
            builder.EndClass();
            builder.EndNamespace();
            // 输出源代码
            context.AddSource($"{markerClassName}.g.cs", builder.ToString());
        }

        private static void GenerateSphereCode(SourceProductionContext context, Compilation compilation, List<EventSystemTypeInfo> sphereEventSystem)
        {
            // 获取当前程序集名称（仅用于注释）
            var markerClassName = compilation.GetAssemblyName("SphereEventRegistrar", out var assemblyName, out _);
            // 生成代码文件
            var builder = new SourceCodeBuilder();
            builder.AppendLine("#if FANTASY_NET",false);
            // 添加文件头
            builder.AppendLine(GeneratorConstants.AutoGeneratedHeader);
            // 添加 using
            builder.AddUsings(
                "System",
                "System.Collections.Generic",
                "Fantasy.Assembly",
                "Fantasy.DataStructure.Collection",
                "Fantasy.Event",
                "Fantasy.Sphere",
                "Fantasy.Async"
            );
            builder.AppendLine();
            // 开始命名空间（固定使用 Fantasy.Generated）
            builder.BeginNamespace("Fantasy.Generated");
            // 开始类定义（实现 ISphereEventRegistrar 接口）
            builder.AddXmlComment($"Auto-generated Sphere Event System registration class for {assemblyName}");
            builder.BeginClass(markerClassName, "internal sealed", "global::Fantasy.Assembly.ISphereEventRegistrar");
            // 生成字段用于存储已注册的事件处理器（用于 UnRegister）
            foreach (var eventSystemTypeInfo in sphereEventSystem)
            {
                builder.AppendLine($"private {eventSystemTypeInfo.TypeFullName} {eventSystemTypeInfo.FieldName} = new {eventSystemTypeInfo.TypeFullName}();");
            }
            // EventTypeHandles
            builder.AddXmlComment("TypeHashCodes");
            builder.BeginMethod("public long[] TypeHashCodes()");
            if (sphereEventSystem.Any())
            {
                builder.AppendLine($"var typeHashCodes = new long[{sphereEventSystem.Count}];");
                for (var i = 0; i < sphereEventSystem.Count; i++)
                {
                    builder.AppendLine($"typeHashCodes[{i}] = {sphereEventSystem[i].FieldName}.TypeHashCode;");
                }
                builder.AppendLine("return typeHashCodes;");
            }
            else
            {
                builder.AppendLine("return Array.Empty<long>();");
            }
            builder.EndMethod();
            // SphereEvent
            builder.AddXmlComment("SphereEvent");
            builder.BeginMethod("public Func<global::Fantasy.Scene, global::Fantasy.Sphere.SphereEventArgs, global::Fantasy.Async.FTask>[] SphereEvent()");
            if (sphereEventSystem.Any())
            {
                builder.AppendLine($"var sphereEvents = new Func<global::Fantasy.Scene, global::Fantasy.Sphere.SphereEventArgs, global::Fantasy.Async.FTask>[{sphereEventSystem.Count}];");
                for (var i = 0; i < sphereEventSystem.Count; i++)
                {
                    builder.AppendLine($"sphereEvents[{i}] = {sphereEventSystem[i].FieldName}.Invoke;");
                }
                builder.AppendLine("return sphereEvents;");
            }
            else
            {
                builder.AppendLine("return Array.Empty<Func<global::Fantasy.Scene, global::Fantasy.Sphere.SphereEventArgs, global::Fantasy.Async.FTask>>();");
            }
            builder.EndMethod();
            // 结束类和命名空间
            builder.EndClass();
            builder.EndNamespace();
            builder.AppendLine("#endif",false);
            // 输出源代码
            context.AddSource($"{markerClassName}.g.cs", builder.ToString());
        }

        /// <summary>
        /// 快速判断语法节点是否可能是 EventSystem 类
        /// </summary>
        private static bool IsEventSystemClass(SyntaxNode node)
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

            // 快速检查是否包含可能的 EventSystem 基类名称
            var baseListText = classDecl.BaseList.ToString();
            return baseListText.Contains("EventSystem") ||
                   baseListText.Contains("AsyncEventSystem") ||
                   baseListText.Contains("SphereEventSystem") ||
                   baseListText.Contains("TimerHandler");
        }
        
        private enum EventSystemType
        {
            None,
            EventSystem,            // EventSystem<T> 事件类
            AsyncEventSystem,       // AsyncEventSystem<T> 事件类
            SphereEventSystem,      // SphereEventSystem<T> 事件类
        }
        
        private sealed class EventSystemTypeInfo
        {
            public readonly EventSystemType EventSystemType;
            public readonly string TypeFullName;
            public readonly string TypeName;
            public readonly string FieldName;  // 预先计算的字段名（_camelCase）

            private EventSystemTypeInfo(EventSystemType eventSystemType, string typeFullName, string typeName, string fieldName)
            {
                EventSystemType = eventSystemType;
                TypeFullName = typeFullName;
                TypeName = typeName;
                FieldName = fieldName;
            }

            public static EventSystemTypeInfo Create(EventSystemType eventSystemType, INamedTypeSymbol symbol)
            {
                var typeName = symbol.Name;
                return new EventSystemTypeInfo(
                    eventSystemType,
                    symbol.GetFullName(),
                    typeName,
                    $"_{typeName.ToCamelCase()}");  // 预先计算字段名
            }
        }
    }
}
