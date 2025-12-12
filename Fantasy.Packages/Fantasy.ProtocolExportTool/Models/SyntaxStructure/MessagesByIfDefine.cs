using System.Text;
using Fantasy.ProtocolExportTool.Generators;

namespace Fantasy.ProtocolExportTool.Models
{
    /// <summary>
    /// 根据条件编译符分组的消息字典。实现了<see cref="IfDefineDictionary{T}"/>。
    /// </summary>
    public sealed class MessagesByIfDefine : IfDefineDictionary<MessageDefinition>
    {
        /// <summary>
        /// 将自身转为代码字符串,写在条件编译符的中间部分。
        /// </summary>
        /// <param name="messageDefinition"> 消息定义,作为元素位于List当中 </param>
        /// <param name="arg"> 要求传入的参数必须是opcodeName </param>
        /// <returns></returns>
        public override string WriteCSharpLine(MessageDefinition messageDefinition, object? arg = null)
        {
            var builder = new StringBuilder();
            var disposeCode = new StringBuilder();
            var members = new List<string>();
            var memberAttribute = messageDefinition.Protocol.MemberAttribute;
            var ignoreAttribute = messageDefinition.Protocol.IgnoreAttribute;

            if (messageDefinition.HasOpCode)
            {
                string opcodeName = (arg as string)!; // 这里要求传入的参数必须是opcodeName
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

                var fieldType = CSharpExporter.ConvertType(field);
                var initializer = CSharpExporter.GetInitializer(field);
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

            // 自定义类标签
            if (messageDefinition.CustomClassAttributesByIfDefine.Count > 0)
            {
                StringBuilder attrsBuilder = messageDefinition.CustomClassAttributesByIfDefine.ToCSharpStringBuilder();
                builder.Append(attrsBuilder);
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
            return builder.ToString();
        }

        private static bool IsRequestType(MessageType type) =>
            type is MessageType.Request or MessageType.RouteTypeRequest or MessageType.RoamingRequest;

        private static bool IsResponseType(MessageType type) =>
            type is MessageType.Response or MessageType.RouteTypeResponse or MessageType.RoamingResponse;

        private static bool HasRouteType(MessageType type) =>
            type is MessageType.RouteTypeMessage or MessageType.RoamingMessage
                or MessageType.RoamingRequest or MessageType.RouteTypeRequest;
    }
}
