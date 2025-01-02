using System.Text;
using Fantasy.Exporter;
using Fantasy.Helper;
using Fantasy.Network;
using OpCode = Fantasy.Network.OpCode;
using OpCodeType = Fantasy.Network.OpCodeType;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
// ReSharper disable PossibleNullReferenceException
// ReSharper disable ConditionIsAlwaysTrueOrFalse
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8604 // Possible null reference argument.

namespace Fantasy.Tools.ProtocalExporter;
public enum NetworkProtocolOpCodeType
{
    None = 0,
    Outer = 1,
    Inner = 2,
}
public sealed class OpcodeInfo
{
    public uint Code;
    public string Name;
}

public sealed class ProtocolExporter
{
    private string _serverTemplate;
    private string _clientTemplate;
    private readonly List<OpcodeInfo> _opcodes = new();
    private static readonly char[] SplitChars = [' ', '\t'];
    private readonly string _networkProtocolDirectory;
    private readonly string _networkProtocolClientDirectory;
    private readonly string _networkProtocolServerDirectory;
    private readonly string _networkProtocolDirectoryOuter;
    private readonly string _networkProtocolDirectoryInner;
    
    public ProtocolExporter()
    {
        Console.OutputEncoding = Encoding.UTF8;
        
        if (ExporterSettingsHelper.NetworkProtocolDirectory == null || ExporterSettingsHelper.NetworkProtocolDirectory.Trim() == "")
        {
            Log.Info($"NetworkProtocolDirectory Can not be empty!");
            return;
        }

        _networkProtocolDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.NetworkProtocolDirectory);
        
        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
        {
            if (ExporterSettingsHelper.NetworkProtocolClientDirectory?.Trim() == "")
            {
                Log.Info($"NetworkProtocolClientDirectory Can not be empty!");
                return;
            }

            _networkProtocolClientDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.NetworkProtocolClientDirectory);

            if (!Directory.Exists(_networkProtocolClientDirectory))
            {
                Directory.CreateDirectory(_networkProtocolClientDirectory);
            }
        }

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
        {
            if (ExporterSettingsHelper.NetworkProtocolServerDirectory?.Trim() == "")
            {
                Log.Info($"NetworkProtocolServerDirectory Can not be empty!");
                return;
            }

            _networkProtocolServerDirectory = FileHelper.GetFullPath(ExporterSettingsHelper.NetworkProtocolServerDirectory);

            if (!Directory.Exists(_networkProtocolServerDirectory))
            {
                Directory.CreateDirectory(_networkProtocolServerDirectory);
            }

            _networkProtocolDirectoryInner = $"{_networkProtocolDirectory}Inner";

            if (!Directory.Exists(_networkProtocolDirectoryInner))
            {
                Directory.CreateDirectory(_networkProtocolDirectoryInner);
            }
        }

        _networkProtocolDirectoryOuter = $"{_networkProtocolDirectory}Outer";

        if (!Directory.Exists(_networkProtocolDirectoryOuter))
        {
            Directory.CreateDirectory(_networkProtocolDirectoryOuter);
        }
    }

    public void Run()
    {
        var tasks = new Task[2];
        tasks[0] = Task.Run(RouteType);
        tasks[1] = Task.Run(async () =>
        {
            LoadTemplate();
            await Start(NetworkProtocolOpCodeType.Outer);
            if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
            {
                await Start(NetworkProtocolOpCodeType.Inner);
            }
        });
        Task.WaitAll(tasks);
    }

    private async Task Start(NetworkProtocolOpCodeType opCodeType)
    {
        var className = "";
        var opCodeName = "";
        var file = new StringBuilder();
        var messageStr = new StringBuilder();
        var disposeStr = new StringBuilder();
        var errorCodeStr = new StringBuilder();
        var usingNamespace = new HashSet<string>();
        var saveDirectory = new Dictionary<string, string>();
        
        OpcodeInfo opcodeInfo = null;
        ProtocolOpCode protocolOpCode = null;
        string[] protocolFiles = null;
        _opcodes.Clear();
        
        switch (opCodeType)
        {
            case NetworkProtocolOpCodeType.Outer:
            {
                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
                {
                    saveDirectory.Add(_networkProtocolServerDirectory, _serverTemplate);
                }
                
                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
                {
                    saveDirectory.Add(_networkProtocolClientDirectory, _clientTemplate);
                }
                
                protocolOpCode = new ProtocolOpCode()
                {
                    Message = OpCodeType.OuterMessage,
                    Request = OpCodeType.OuterRequest,
                    Response = OpCodeType.OuterResponse,
                    RouteMessage = 0,
                    RouteRequest = 0,
                    RouteResponse = 0,
                    AddressableMessage = OpCodeType.OuterAddressableMessage,
                    AddressableRequest = OpCodeType.OuterAddressableRequest,
                    AddressableResponse = OpCodeType.OuterAddressableResponse,
                    CustomRouteMessage = OpCodeType.OuterCustomRouteMessage,
                    CustomRouteRequest = OpCodeType.OuterCustomRouteRequest,
                    CustomRouteResponse = OpCodeType.OuterCustomRouteResponse,
                };
                opCodeName = "OuterOpcode";
                protocolFiles = FileHelper.GetDirectoryFile(_networkProtocolDirectoryOuter, "*.proto", SearchOption.AllDirectories);
                break;
            }
            case NetworkProtocolOpCodeType.Inner:
            {
                protocolOpCode = new ProtocolOpCode()
                {
                    Message = OpCodeType.InnerMessage,
                    Request = OpCodeType.InnerRequest,
                    Response = OpCodeType.InnerResponse,
                    RouteMessage = OpCodeType.InnerRouteMessage,
                    RouteRequest = OpCodeType.InnerRouteRequest,
                    RouteResponse = OpCodeType.InnerRouteResponse,
                    AddressableMessage = OpCodeType.InnerAddressableMessage,
                    AddressableRequest = OpCodeType.InnerAddressableRequest,
                    AddressableResponse = OpCodeType.InnerAddressableResponse,
                    CustomRouteMessage = 0,
                    CustomRouteRequest = 0,
                    CustomRouteResponse = 0,
                };
                opCodeName = "InnerOpcode";
                saveDirectory.Add(_networkProtocolServerDirectory, _serverTemplate);
                protocolFiles = FileHelper.GetDirectoryFile(_networkProtocolDirectoryInner, "*.proto", SearchOption.AllDirectories);
                break;
            }
        }

        if (protocolFiles == null || protocolFiles.Length == 0)
        {
            return;
        }
        
        #region GenerateFiles

        foreach (var filePath in protocolFiles)
        {
            var keyIndex = 1;
            var parameter = "";
            var hasOpCode = false;
            var isMsgHead = false;
            var isSetProtocol = false;
            string responseTypeStr = null;
            string customRouteType = null;
            string protocolMember = "ProtoMember";
            string protocolType = "\t[ProtoContract]";
            string protocolIgnore = "\t\t[ProtoIgnore]";
            var protocolOpCodeType = OpCodeProtocolType.ProtoBuf;
            var fileText = await File.ReadAllTextAsync(filePath);

            foreach (var line in fileText.Split('\n'))
            {
                var currentLine = line.Trim();

                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    continue;
                }

                if (currentLine.StartsWith("///"))
                {
                    messageStr.AppendFormat("	/// <summary>\r\n" + "	/// {0}\r\n" + "	/// </summary>\r\n", currentLine.Substring("///".Length));
                    continue;
                }

                if (currentLine.StartsWith("// Protocol"))
                {
                    isSetProtocol = true;
                    var protocol = currentLine.Substring("// Protocol".Length).Trim();

                    switch (protocol)
                    {
                        case "ProtoBuf":
                        {
                            protocolType = "\t[ProtoContract]";
                            protocolIgnore = "\t\t[ProtoIgnore]";
                            protocolMember = "ProtoMember";
                            protocolOpCodeType = OpCodeProtocolType.ProtoBuf;
                            break;
                        }
                        // case "MemoryPack":
                        // {
                        //     keyIndex = 0;
                        //     protocolType = "\t[MemoryPackable]";
                        //     protocolIgnore = "\t\t[MemoryPackIgnore]";
                        //     protocolMember = "MemoryPackOrder";
                        //     // protocolOpCodeType = OpCodeProtocolType.MemoryPack;
                        //     break;
                        // }
                        case "Bson":
                        {
                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                            {
                                Log.Error("Under Outer, /// does not support the Bson protocol!");
                                return;
                            }
                            protocolType = null;
                            protocolIgnore = "\t\t[BsonIgnore]";
                            protocolMember = null;
                            protocolOpCodeType = OpCodeProtocolType.Bson;
                            break;
                        }
                        default:
                        {
                            if (!ExporterSettingsHelper.CustomSerializes.TryGetValue(protocol, out var customSerialize))
                            {
                                Log.Error($"// Protocol {protocol} is not supported!");
                                return;
                            }

                            usingNamespace.Add(customSerialize.NameSpace);
                            keyIndex = customSerialize.KeyIndex;
                            protocolType = customSerialize.Attribute;
                            protocolIgnore = customSerialize.Ignore;
                            protocolMember = customSerialize.Member;
                            protocolOpCodeType = customSerialize.OpCodeType;
                            break;
                        }
                    }
                }

                if (currentLine.StartsWith("message"))
                {
                    isMsgHead = true;
                    className = currentLine.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    var splits = currentLine.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

                    if (isSetProtocol)
                    {
                        if (protocolType != null)
                        {
                            messageStr.AppendLine(protocolType);
                        }
                    }
                    else
                    {
                        messageStr.AppendLine("\t[ProtoContract]");
                    }
                    
                    if (splits.Length > 1)
                    {
                        hasOpCode = true;
                        var parameterArray = currentLine.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim().Split(',');
                        parameter = parameterArray[0].Trim();
                        opcodeInfo = new OpcodeInfo()
                        {
                            Name = className
                        };
                        switch (parameterArray.Length)
                        {
                            case 2:
                            {
                                if (parameter == "ICustomRouteMessage")
                                {
                                    customRouteType = parameterArray[1].Trim();
                                    break;
                                }

                                responseTypeStr = parameterArray[1].Trim();
                                break;
                            }
                            case 3:
                            {
                                responseTypeStr = parameterArray[1].Trim();
                                customRouteType = parameterArray[2].Trim();
                                break;
                            }
                        }
                    }
                    else
                    {
                        parameter = "";
                        hasOpCode = false;
                    }

                    messageStr.Append(string.IsNullOrWhiteSpace(parameter)
                        ? $"\tpublic partial class {className} : AMessage"
                        : $"\tpublic partial class {className} : AMessage, {parameter}");
                    if (protocolMember == "ProtoMember")
                    {
                        messageStr.Append(", IProto");
                    }
                    continue;
                }

                if (!isMsgHead)
                {
                    continue;
                }

                switch (currentLine)
                {
                    case "{":
                    {
                        messageStr.AppendLine("\n\t{");
                        messageStr.AppendLine($"\t\tpublic static {className} Create(Scene scene)");
                        messageStr.AppendLine($"\t\t{{\n\t\t\treturn scene.MessagePoolComponent.Rent<{className}>();\n\t\t}}");
                        messageStr.AppendLine($"\t\tpublic override void Dispose()");
                        messageStr.AppendLine($"\t\t{{");
                        messageStr.AppendLine($"<<<<Dispose>>>#if FANTASY_NET || FANTASY_UNITY\n\t\t\tGetScene().MessagePoolComponent.Return<{className}>(this);\n#endif");
                        messageStr.AppendLine($"\t\t}}");

                        if (parameter == "IMessage")
                        {
                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.Message, protocolOpCode.AMessage++);
                            messageStr.AppendLine($"\t\tpublic uint OpCode() {{ return {opCodeName}.{className}; }}");
                        }
                        else
                        {
                            if (responseTypeStr != null)
                            {
                                messageStr.AppendLine(protocolIgnore);
                                messageStr.AppendLine($"\t\tpublic {responseTypeStr} ResponseType {{ get; set; }}");
                                responseTypeStr = null;
                            }
                            else
                            {
                                if (parameter.Contains("RouteRequest"))
                                {
                                    Log.Error($"{opcodeInfo.Name} 没指定ResponseType");
                                    return;
                                }
                            }

                            if (hasOpCode)
                            {
                                messageStr.AppendLine($"\t\tpublic uint OpCode() {{ return {opCodeName}.{className}; }}");
                            }

                            if (customRouteType != null)
                            {
                                messageStr.AppendLine(protocolIgnore);
                                messageStr.AppendLine($"\t\tpublic int RouteType => Fantasy.RouteType.{customRouteType};");
                                customRouteType = null;
                            }

                            switch (parameter)
                            {
                                case "IRequest":
                                {
                                    opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.Request, protocolOpCode.ARequest++);
                                    break;
                                }
                                case "IResponse":
                                {
                                    opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.Response, protocolOpCode.AResponse++);
                                    errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                    errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                    disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                    break;
                                }
                                default:
                                {
                                    switch (parameter)
                                    {
                                        case "IAddressableRouteMessage":
                                        {
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressableMessage, protocolOpCode.AAddressableMessage++);
                                            break;
                                        }
                                        case "IAddressableRouteRequest":
                                        {
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressableRequest, protocolOpCode.AAddressableRequest++);
                                            break;
                                        }
                                        case "IAddressableRouteResponse":
                                        {
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.AddressableResponse, protocolOpCode.AAddressableResponse++);
                                            errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                            errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                            disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                            break;
                                        }
                                        case "ICustomRouteMessage":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.CustomRouteMessage, protocolOpCode.ACustomRouteMessage++);
                                            break;
                                        }
                                        case "ICustomRouteRequest":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.CustomRouteRequest, protocolOpCode.ACustomRouteRequest++);
                                            break;
                                        }
                                        case "ICustomRouteResponse":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Inner)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.CustomRouteResponse, protocolOpCode.ACustomRouteResponse++);
                                            errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                            errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                            disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                            break;
                                        }
                                        case "IRouteMessage":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.RouteMessage, protocolOpCode.ARouteMessage++);
                                            break;
                                        }
                                        case "IRouteRequest":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.RouteRequest, protocolOpCode.ARouteRequest++);
                                            break;
                                        }
                                        case "IRouteResponse":
                                        {
                                            if (opCodeType == NetworkProtocolOpCodeType.Outer)
                                            {
                                                throw new NotSupportedException("Under Inner, /// does not support the ICustomRouteMessage!");
                                            }
                                            opcodeInfo.Code = OpCode.Create(protocolOpCodeType, protocolOpCode.RouteResponse, protocolOpCode.ARouteResponse++);
                                            errorCodeStr.AppendLine($"\t\t[{protocolMember}(ErrorCodeKeyIndex)]");
                                            errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                            disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                            break;
                                        }
                                    }

                                    break;
                                }
                            }
                        }

                        if (hasOpCode)
                        {
                            _opcodes.Add(opcodeInfo);
                        }

                        continue;
                    }
                    case "}":
                    {
                        isMsgHead = false;
                        errorCodeStr = errorCodeStr.Replace("ErrorCodeKeyIndex", keyIndex.ToString());
                        messageStr = messageStr.Replace("<<<<Dispose>>>", disposeStr.ToString());
                        messageStr.Append(errorCodeStr);
                        messageStr.AppendLine("\t}");
                        file.Append(messageStr);
                        messageStr.Clear();
                        disposeStr.Clear();
                        errorCodeStr.Clear();
                        keyIndex = 1;
                        protocolType = "\t[ProtoContract]";
                        protocolIgnore = "\t\t[ProtoIgnore]";
                        protocolMember = "ProtoMember";
                        protocolOpCodeType = OpCodeProtocolType.ProtoBuf;
                        continue;
                    }
                    case "":
                    {
                        continue;
                    }
                }

                if (currentLine.StartsWith("//"))
                {
                    messageStr.AppendFormat("\t\t///<summary>\r\n" + "\t\t/// {0}\r\n" + "\t\t///</summary>\r\n", currentLine.TrimStart('/', '/'));
                    continue;
                }

                if (currentLine.StartsWith("repeated"))
                {
                    Repeated(messageStr, disposeStr, currentLine, protocolMember, ref keyIndex);
                }
                else
                {
                    Members(messageStr, disposeStr, currentLine, protocolMember, ref keyIndex);
                }
            }
            
            var namespaceBuilder = new StringBuilder();
            
            foreach (var @namespace in usingNamespace)
            {
                namespaceBuilder.Append($"using {@namespace};\n");
            }
            
            var csName = $"{Path.GetFileNameWithoutExtension(filePath)}.cs";
            foreach (var (directory, template) in saveDirectory)
            {
                var csFile = Path.Combine(directory, csName);
                var content = template.Replace("(Content)", file.ToString());
                content = content.Replace("(UsingNamespace)", namespaceBuilder.ToString());
                await File.WriteAllTextAsync(csFile, content);
            }
            file.Clear();
        }
        
        #endregion

        #region GenerateOpCode

        file.Clear();
        file.AppendLine("namespace Fantasy");
        file.AppendLine("{");
        file.AppendLine($"\tpublic static partial class {opCodeName}");
        file.AppendLine("\t{");

        foreach (var opcode in _opcodes)
        {
            file.AppendLine($"\t\t public const uint {opcode.Name} = {opcode.Code};");
        }
        
        _opcodes.Clear();
        file.AppendLine("\t}");
        file.AppendLine("}");

        foreach (var (directory, _) in saveDirectory)
        {
            var csFile = Path.Combine(directory, $"{opCodeName}.cs");
            await File.WriteAllTextAsync(csFile, file.ToString());
        }
        #endregion
    }
    
    private void Repeated(StringBuilder file, StringBuilder disposeStr, string newline, string protocolMember, ref int keyIndex)
    {
        try
        {
            var index = newline.IndexOf(";", StringComparison.Ordinal);
            newline = newline.Remove(index);
            var property = newline.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
            var type = property[1];
            var name = property[2];
            // var memberIndex = int.Parse(property[4]);
            type = ConvertType(type);

            file.AppendLine($"\t\t[{protocolMember}({keyIndex++})]");
            file.AppendLine($"\t\tpublic List<{type}> {name} = new List<{type}>();");
            disposeStr.AppendLine($"\t\t\t{name}.Clear();");
        }
        catch (Exception e)
        {
            Log.Error($"{newline}\n {e}");
        }
    }
    
    private void Members(StringBuilder file, StringBuilder disposeStr, string currentLine, string protocolMember, ref int keyIndex)
    {
        try
        {
            var index = currentLine.IndexOf(";", StringComparison.Ordinal);
            currentLine = currentLine.Remove(index);
            var property = currentLine.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
            var type = property[0];
            var name = property[1];
            // var memberIndex = int.Parse(property[3]);
            var typeCs = ConvertType(type);
            if (protocolMember != null)
            {
                file.AppendLine($"\t\t[{protocolMember}({keyIndex++})]");
            }
            file.AppendLine($"\t\tpublic {typeCs} {name} {{ get; set; }}");
            disposeStr.AppendLine($"\t\t\t{name} = default;");
        }
        catch (Exception e)
        {
            Log.Error($"{currentLine}\n {e}");
        }
    }
    
    private string ConvertType(string type)
    {
        return type switch
        {
            "int[]" => "int[] { }",
            "int32[]" => "int[] { }",
            "int64[]" => "long[] { }",
            "int32" => "int",
            "uint32" => "uint",
            "int64" => "long",
            "uint64" => "ulong",
            _ => type
        };
    }
    
    private async Task RouteType()
    {
        var routeTypeFile = $"{_networkProtocolDirectory}RouteType.Config";
        var protoFileText = await File.ReadAllTextAsync(routeTypeFile);
        var routeTypeFileSb = new StringBuilder();
        routeTypeFileSb.AppendLine("namespace Fantasy\n{");
        routeTypeFileSb.AppendLine("\t// Route协议定义(需要定义1000以上、因为1000以内的框架预留)\t");
        routeTypeFileSb.AppendLine("\tpublic static class RouteType\n\t{");

        foreach (var line in protoFileText.Split('\n'))
        {
            var currentLine = line.Trim();

            if (currentLine.StartsWith("//"))
            {
                continue;
            }

            var splits = currentLine.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);
            var routeTypeStr = splits[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            routeTypeFileSb.Append($"\t\tpublic const int {routeTypeStr[0].Trim()} = {routeTypeStr[1].Trim()};");

            if (splits.Length > 1)
            {
                routeTypeFileSb.Append($" // {splits[1].Trim()}\n");
            }
            else
            {
                routeTypeFileSb.Append('\n');
            }
        }

        routeTypeFileSb.AppendLine("\t}\n}");
        var file = routeTypeFileSb.ToString();

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
        {
            await File.WriteAllTextAsync($"{_networkProtocolServerDirectory}RouteType.cs", file);
        }

        if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
        {
            await File.WriteAllTextAsync($"{_networkProtocolClientDirectory}RouteType.cs", file);
        }
    }
    
    private void LoadTemplate()
    {
        string[] lines = NetworkProtocolTemplate.Template.Split(["\r\n", "\n"], StringSplitOptions.None);

        StringBuilder serverSb = new StringBuilder();
        StringBuilder clientSb = new StringBuilder();

        int flag = 0;
        foreach (string line in lines)
        {
            string trim = line.Trim();

            if (trim.StartsWith("#if") && trim.Contains("SERVER"))
            {
                flag = 1;
                continue;
            }
            else if (trim.StartsWith("#else"))
            {
                flag = 2;
                continue;
            }
            else if (trim.StartsWith($"#endif"))
            {
                flag = 0;
                continue;
            }

            switch (flag)
            {
                case 1: // 服务端
                {
                    serverSb.AppendLine(line);
                    break;
                }
                case 2: // 客户端
                {
                    clientSb.AppendLine(line);
                    break;
                }
                default: // 双端
                {
                    serverSb.AppendLine(line);
                    clientSb.AppendLine(line);
                    break;
                }
            }
        }

        _serverTemplate = serverSb.Replace("(NetworkProtocol)", "ProtoBuf").ToString();
        _clientTemplate = clientSb.Replace("(NetworkProtocol)", "ProtoBuf").ToString();
    }
}