using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fantasy.ProtocolExportTool.Abstract;
using Fantasy.ProtocolExportTool.Models;
// ReSharper disable PossibleMultipleEnumeration

namespace Fantasy.ProtocolExportTool.Generators;

public sealed class CSharpExporter(
    string protocolDirectory,
    string clientDirectory,
    string serverDirectory,
    ProtocolExportType protocolExportType)
    : AProtocolExporter(protocolDirectory, clientDirectory, serverDirectory, protocolExportType)
{
    protected override string GenerateRouteTypes(IReadOnlyDictionary<string, int> routeTypes)
    {
        if (routeTypes.Count == 0)
        {
            return string.Empty;
        }

        var routeTypeEnumerable = routeTypes.OrderBy(d => d.Value);

        var constants = string.Join(Environment.NewLine, routeTypeEnumerable
            .Select(x =>
            {
                var comment = x.Key.EndsWith("Route")
                    ? x.Key.Substring(0, x.Key.Length - "Route".Length)
                    : x.Key;
                return $"        public const int {x.Key} = {x.Value}; // {comment}";
            }));
        
        var enumerable = string.Join(Environment.NewLine, 
            routeTypeEnumerable.Select(x => $"                yield return {x.Key};"));

        return $$"""
                 using System.Collections.Generic;
                 namespace Fantasy
                 {
                     /// <summary>
                     /// 本代码有编辑器生成,请不要再这里进行编辑。
                     /// Route协议定义(需要定义1000以上、因为1000以内的框架预留)
                     /// </summary>
                     public static partial class RouteType
                     {
                 {{constants}}
                 
                         public static IEnumerable<int> RoamingTypes
                         {
                             get
                             {
                 {{enumerable}}
                             }
                         }
                     }
                 }
                 """;
    }

    protected override string GenerateRoamingTypes(IReadOnlyDictionary<string, int> roamingTypes)
    {
        if (roamingTypes.Count == 0)
        {
            return string.Empty;
        }

        var roamingTypeEnumerable = roamingTypes.OrderBy(d => d.Value);

        var constants = string.Join(Environment.NewLine, roamingTypeEnumerable
            .Select(x =>
            {
                var comment = x.Key.EndsWith("RoamingType")
                    ? x.Key.Substring(0, x.Key.Length - "RoamingType".Length)
                    : x.Key.EndsWith("Type")
                        ? x.Key.Substring(0, x.Key.Length - "Type".Length)
                        : x.Key;
                return $"        public const int {x.Key} = {x.Value}; // {comment}";
            }));

        var enumerable = string.Join(Environment.NewLine, 
            roamingTypeEnumerable.Select(x => $"                yield return {x.Key};"));

        return $$"""
                 using System.Collections.Generic;
                 namespace Fantasy
                 {
                     /// <summary>
                     /// 本代码有编辑器生成,请不要再这里进行编辑。
                     /// Roaming协议定义(需要定义10000以上、因为10000以内的框架预留)
                     /// </summary>
                     public static partial class RoamingType
                     {
                 {{constants}}
                 
                         public static IEnumerable<int> RoamingTypes
                         {
                             get
                             {
                 {{enumerable}}
                             }
                         }
                     }
                 }
                 """;
    }

    protected override string GenerateOuterOpcode(IReadOnlyList<OpcodeInfo> opcodeInfos)
    {
        if (opcodeInfos.Count == 0)
        {
            return string.Empty;
        }

        return $$"""
                 // ReSharper disable InconsistentNaming
                 namespace Fantasy
                 {
                     /// <summary>
                     /// 本代码有编辑器生成,请不要再这里进行编辑。
                     /// </summary>
                     public static partial class OuterOpcode
                     {
                 {{string.Join(Environment.NewLine, opcodeInfos
                     .Select(x => $"        public const uint {x.Name} = {x.Code};"))}}
                     }
                 }
                 """;
    }

    protected override string GenerateInnerOpcode(IReadOnlyList<OpcodeInfo> opcodeInfos)
    {
        if (opcodeInfos.Count == 0)
        {
            return string.Empty;
        }

        return $$"""
                 // ReSharper disable InconsistentNaming
                 namespace Fantasy
                 {
                     /// <summary>
                     /// 本代码有编辑器生成,请不要再这里进行编辑。
                     /// </summary>
                     public static partial class InnerOpcode
                     {
                 {{string.Join(Environment.NewLine, opcodeInfos
                     .Select(x => $"        public const uint {x.Name} = {x.Code};"))}}
                     }
                 }
                 """;
    }

    protected override string GenerateOuterMessages(IReadOnlyList<MessageDefinition> messageDefinitions)
    {
        return messageDefinitions.Count == 0 ? string.Empty : $$"""
                                                                using ProtoBuf;
                                                                using System;
                                                                using MemoryPack;
                                                                using System.Collections.Generic;
                                                                using Fantasy;
                                                                using Fantasy.Network.Interface;
                                                                using Fantasy.Serialize;
                                                                #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                                                                #pragma warning disable CS8618
                                                                // ReSharper disable InconsistentNaming
                                                                // ReSharper disable CollectionNeverUpdated.Global
                                                                // ReSharper disable RedundantTypeArgumentsOfMethod
                                                                // ReSharper disable PartialTypeWithSinglePart
                                                                // ReSharper disable UnusedAutoPropertyAccessor.Global
                                                                // ReSharper disable PreferConcreteValueOverDefault
                                                                // ReSharper disable RedundantNameQualifier
                                                                // ReSharper disable MemberCanBePrivate.Global
                                                                // ReSharper disable CheckNamespace
                                                                // ReSharper disable FieldCanBeMadeReadOnly.Global
                                                                // ReSharper disable RedundantUsingDirective
                                                                namespace Fantasy
                                                                {
                                                                {{GenerateMessages("OuterOpcode", messageDefinitions)}}
                                                                }
                                                                """;
    }

    protected override string GenerateInnerMessages(IReadOnlyList<MessageDefinition> messageDefinitions)
    {
        return messageDefinitions.Count == 0 ? string.Empty : $$"""
                                                                using ProtoBuf;
                                                                using MemoryPack;
                                                                using System;
                                                                using System.Collections.Generic;
                                                                using MongoDB.Bson.Serialization.Attributes;
                                                                using Fantasy;
                                                                using Fantasy.Network.Interface;
                                                                using Fantasy.Serialize;
                                                                // ReSharper disable InconsistentNaming
                                                                // ReSharper disable CollectionNeverUpdated.Global
                                                                // ReSharper disable RedundantTypeArgumentsOfMethod
                                                                // ReSharper disable PartialTypeWithSinglePart
                                                                // ReSharper disable UnusedAutoPropertyAccessor.Global
                                                                // ReSharper disable PreferConcreteValueOverDefault
                                                                // ReSharper disable RedundantNameQualifier
                                                                // ReSharper disable MemberCanBePrivate.Global
                                                                // ReSharper disable CheckNamespace
                                                                // ReSharper disable FieldCanBeMadeReadOnly.Global
                                                                // ReSharper disable RedundantUsingDirective
                                                                #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                                                                #pragma warning disable CS8618
                                                                namespace Fantasy
                                                                {
                                                                {{GenerateMessages("InnerOpcode", messageDefinitions)}}
                                                                }
                                                                """;
    }

    protected override string GenerateOuterMessageHelper(IReadOnlyList<MessageDefinition> messageDefinitions)
    {
        if (messageDefinitions.Count == 0)
        {
            return string.Empty;
        }

        var helper = new StringBuilder();
        
        foreach (var messageDefinition in messageDefinitions)
        {
            if (string.IsNullOrEmpty(messageDefinition.InterfaceType))
            {
                continue;
            }
            
            switch (messageDefinition.MessageType)
            {
                case MessageType.Message:
                case MessageType.RoamingMessage:
                case MessageType.RouteTypeMessage:
                {
                    helper.AppendLine("\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    helper.AppendLine($"\t\tpublic static void {messageDefinition.Name}(this Session session, {messageDefinition.Name} message)");
                    helper.AppendLine("\t\t{");
                    helper.AppendLine("\t\t\tsession.Send(message);");
                    helper.AppendLine("\t\t}");

                    if (messageDefinition.Fields.Count > 0)
                    {
                        var parameters = string.Join(", ",
                            messageDefinition.Fields.Select(f =>
                                $"{ConvertType(f)} {char.ToLower(f.Name[0])}{f.Name[1..]}"));
                        helper.AppendLine("\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        helper.AppendLine($"\t\tpublic static void {messageDefinition.Name}(this Session session, {parameters})");
                        helper.AppendLine("\t\t{");
                        helper.AppendLine($"\t\t\tusing var message = Fantasy.{messageDefinition.Name}.Create(session.Scene);");
                        
                        foreach (var field in messageDefinition.Fields)
                        {
                            helper.AppendLine($"\t\t\tmessage.{field.Name} = {char.ToLower(field.Name[0])}{field.Name[1..]};");
                        }
                        
                        helper.AppendLine("\t\t\tsession.Send(message);");
                        helper.AppendLine("\t\t}");
                    }
                    else
                    {
                        helper.AppendLine("\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        helper.AppendLine($"\t\tpublic static void {messageDefinition.Name}(this Session session)");
                        helper.AppendLine("\t\t{");
                        helper.AppendLine($"\t\t\tusing var message = Fantasy.{messageDefinition.Name}.Create(session.Scene);");
                        helper.AppendLine("\t\t\tsession.Send(message);");
                        helper.AppendLine("\t\t}");
                    }
                    
                    break;
                }
                case MessageType.Request:
                case MessageType.RouteTypeRequest:
                case MessageType.RoamingRequest:
                {
                    helper.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    helper.AppendLine($"\t\tpublic static async FTask<{messageDefinition.ResponseType}> {messageDefinition.Name}(this Session session, {messageDefinition.Name} request)");
                    helper.AppendLine("\t\t{");
                    helper.AppendLine($"\t\t\treturn ({messageDefinition.ResponseType})await session.Call(request);");
                    helper.AppendLine("\t\t}");

                    if (messageDefinition.Fields.Count > 0)
                    {
                        var parameters = string.Join(", ",
                            messageDefinition.Fields.Select(f =>
                                $"{ConvertType(f)} {char.ToLower(f.Name[0])}{f.Name[1..]}"));
                        helper.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        helper.AppendLine($"\t\tpublic static async FTask<{messageDefinition.ResponseType}> {messageDefinition.Name}(this Session session, {parameters})");
                        helper.AppendLine("\t\t{");
                        helper.AppendLine($"\t\t\tusing var request = Fantasy.{messageDefinition.Name}.Create(session.Scene);");
                        foreach (var field in messageDefinition.Fields)
                        {
                            helper.AppendLine($"\t\t\trequest.{field.Name} = {char.ToLower(field.Name[0])}{field.Name[1..]};");
                        }
                        helper.AppendLine($"\t\t\treturn ({messageDefinition.ResponseType})await session.Call(request);");
                        helper.AppendLine("\t\t}");
                    }
                    else
                    {
                        helper.AppendLine($"\t\t[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        helper.AppendLine($"\t\tpublic static async FTask<{messageDefinition.ResponseType}> {messageDefinition.Name}(this Session session)");
                        helper.AppendLine("\t\t{");
                        helper.AppendLine($"\t\t\tusing var request = Fantasy.{messageDefinition.Name}.Create(session.Scene);");
                        helper.AppendLine($"\t\t\treturn ({messageDefinition.ResponseType})await session.Call(request);");
                        helper.AppendLine("\t\t}");
                    }
                    break;
                }
            }
        }
        
        return $$"""
                 using System.Runtime.CompilerServices;
                 using Fantasy;
                 using Fantasy.Async;
                 using Fantasy.Network;
                 using System.Collections.Generic;
                 #pragma warning disable CS8618
                 namespace Fantasy
                 {
                    public static class NetworkProtocolHelper
                    {
                 {{helper}}
                    }
                 }
                 """;
    }

    private string GenerateMessages(string opcodeName, IReadOnlyList<MessageDefinition> messageDefinitions)
    {
        var builder = new StringBuilder();

        foreach (var messageDefinition in messageDefinitions)
        {
            var disposeCode = new StringBuilder();
            var members = new List<string>();
            var memberAttribute = messageDefinition.Protocol.MemberAttribute;
            var ignoreAttribute = messageDefinition.Protocol.IgnoreAttribute;
            
            if (messageDefinition.HasOpCode)
            {
                members.Add($"        public uint OpCode() {{ return {opcodeName}.{messageDefinition.Name}; }} ");
            }
            
            if (IsRequestType(messageDefinition.MessageType))
            {
                members.Add($"        {ignoreAttribute}");
                members.Add($"        public {messageDefinition.ResponseType} ResponseType {{ get; set; }}");
            }
            
            if (IsResponseType(messageDefinition.MessageType))
            {
                if (memberAttribute != null)
                {
                    members.Add($"        [{memberAttribute}(1)]");
                }

                members.Add("        public uint ErrorCode { get; set; }");
            }
            
            if (HasRouteType(messageDefinition.MessageType))
            {
                if (memberAttribute != null)
                {
                    members.Add($"        {ignoreAttribute}");
                }

                members.Add($"        public int RouteType => {messageDefinition.CustomRouteType};");
            }
            
            foreach (var field in messageDefinition.Fields)
            {
                var disposeStatement = field.IsRepeated switch
                {
                    true when field.RepeatedType == RepeatedFieldType.Repeated => $"{field.Name}.Clear();",
                    true => $"{field.Name} = null;",
                    false => $"{field.Name} = default;"
                };
                
                disposeCode.AppendLine($"            {disposeStatement}");

                if (field.DocumentationComments.Count > 0)
                {
                    members.Add("        /// <summary>");
                    foreach (var messageDefinitionDocumentationComment in field.DocumentationComments)
                    {
                        members.Add($"        /// {messageDefinitionDocumentationComment}");
                    }
                    members.Add("        /// </summary>");
                }
                
                if (memberAttribute != null)
                {
                    members.Add($"        [{memberAttribute}({field.KeyIndex})]");
                }
                
                var fieldType = ConvertType(field);
                var initializer = GetInitializer(field);
                members.Add($"        public {fieldType} {field.Name} {{ get; set; }}{initializer}");
            }
            
            disposeCode.AppendLine("#if FANTASY_NET || FANTASY_UNITY");
            disposeCode.AppendLine($"            GetScene().MessagePoolComponent.Return<{messageDefinition.Name}>(this);");
            disposeCode.Append("#endif");

            if (messageDefinition.DocumentationComments.Count > 0)
            {
                builder.AppendLine("    /// <summary>");
                foreach (var messageDefinitionDocumentationComment in messageDefinition.DocumentationComments)
                {
                    builder.AppendLine($"    /// {messageDefinitionDocumentationComment}");
                }
                builder.AppendLine("    /// </summary>");
            }

            if (!string.IsNullOrEmpty(messageDefinition.Protocol.ClassAttribute))
            {
                if (messageDefinition.Fields.Any())
                {
                    builder.AppendLine($"    [Serializable]\n" +
                                       $"    {messageDefinition.Protocol.ClassAttribute}");
                }
                else
                {
                    if (IsResponseType(messageDefinition.MessageType) || messageDefinition.Protocol.ProtocolName != "ProtoBuf")
                    {
                        builder.AppendLine($"    [Serializable]\n" +
                                           $"    {messageDefinition.Protocol.ClassAttribute}");
                    }
                    else
                    {
                        // 针对Protobuf空消息：使用 EmptyMessageSerializer
                        builder.AppendLine($"    [Serializable]\n" +
                                           $"    [ProtoContract(Serializer = typeof(global::Fantasy.Serialize.EmptyMessageSerializer<{messageDefinition.Name}>))]");
                    }
                }
            }
            
            if (string.IsNullOrEmpty(messageDefinition.InterfaceType))
            {
                builder.AppendLine($"    public partial class {messageDefinition.Name} : AMessage");
            }
            else
            {
                builder.AppendLine($"    public partial class {messageDefinition.Name} : AMessage, {messageDefinition.InterfaceType}");
            }

            builder.AppendLine($$"""
                                     {
                                         public static {{messageDefinition.Name}} Create(Scene scene)
                                         {
                                             return scene.MessagePoolComponent.Rent<{{messageDefinition.Name}}>();
                                         }

                                         public override void Dispose()
                                         {
                                 {{disposeCode}}
                                         }
                                 {{string.Join(Environment.NewLine, members)}}
                                     }
                                 """);
        }
        
        if (builder.Length >= Environment.NewLine.Length)
        {
            builder.Length -= Environment.NewLine.Length;
        }

        return builder.ToString();
    }
    
    private static string ConvertType(FieldDefinition field)
    {
        var baseType = ConvertBaseType(field.Type);

        if (!field.IsRepeated)
        {
            return baseType;
        }

        return field.RepeatedType switch
        {
            RepeatedFieldType.Repeated => $"List<{baseType}>",
            RepeatedFieldType.RepeatedArray => $"{baseType}[]",
            RepeatedFieldType.RepeatedList => $"List<{baseType}>",
            _ => throw new NotSupportedException($"Unsupported repeated type '{field.RepeatedType}' at line {field.SourceLineNumber}")
        };
    }

    private static string GetInitializer(FieldDefinition field)
    {
        if (field.IsRepeated && field.RepeatedType == RepeatedFieldType.Repeated)
        {
            var baseType = ConvertBaseType(field.Type);
            return $" = new List<{baseType}>();";
        }

        return string.Empty;
    }

    private static string ConvertBaseType(string type)
    {
        return type switch
        {
            "int32" => "int",
            "uint32" => "uint",
            "int64" => "long",
            "uint64" => "ulong",
            "float" => "float",
            "double" => "double",
            "bool" => "bool",
            "string" => "string",
            "int" => "int",
            "uint" => "uint",
            "long" => "long",
            "ulong" => "ulong",
            "byte" => "byte",
            _ when IsCustomType(type) => type,
            _ => throw new NotSupportedException($"Unsupported type '{type}'")
        };
    }

    private static bool IsCustomType(string type)
    {
        return !string.IsNullOrEmpty(type) && char.IsUpper(type[0]);
    }
    
    private static bool IsRequestType(MessageType type) =>
        type is MessageType.Request or MessageType.RouteTypeRequest or MessageType.RoamingRequest;

    private static bool IsResponseType(MessageType type) =>
        type is MessageType.Response or MessageType.RouteTypeResponse or MessageType.RoamingResponse;

    private static bool HasRouteType(MessageType type) =>
        type is MessageType.RouteTypeMessage or MessageType.RoamingMessage
            or MessageType.RoamingRequest or MessageType.RouteTypeRequest;
}