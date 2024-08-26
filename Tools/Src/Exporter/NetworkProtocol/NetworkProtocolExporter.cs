using System.Text;
using Exporter;
using Fantasy;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace Fantasy.Exporter;
/// <summary>
/// NetworkProtocol操作码类型枚举
/// </summary>
public enum NetworkProtocolOpCodeType
{
    /// <summary>
    /// 无
    /// </summary>
    None = 0,
    /// <summary>
    /// 外部操作码类型
    /// </summary>
    Outer = 1,
    /// <summary>
    /// 内部操作码类型
    /// </summary>
    Inner = 2,
    /// <summary>
    /// 使用BSON的内部操作码类型
    /// </summary>
    InnerBson = 3,
}
/// <summary>
/// 操作码信息类
/// </summary>
public sealed class OpcodeInfo
{
    /// <summary>
    /// 操作码
    /// </summary>
    public uint Code;
    /// <summary>
    /// 名称
    /// </summary>
    public string Name;
}
/// <summary>
/// ProtoBuf导出器类
/// </summary>
public sealed class NetworkProtocolExporter
{
    private uint _aMessage;
    private uint _aRequest;
    private uint _aResponse;
    private uint _aRouteMessage;
    private uint _aRouteRequest;
    private uint _aRouteResponse;
    private string _serverTemplate;
    private string _clientTemplate;
    private readonly string _networkProtocolDirectory;
    private readonly string _networkProtocolClientDirectory;
    private readonly string _networkProtocolServerDirectory;
    private readonly string _networkProtocolDirectoryOuter;
    private readonly string _networkProtocolDirectoryInner;
    private readonly string _networkProtocolDirectoryInnerBson;
    private readonly OpCodeCache _opCodeCache;
    private readonly List<OpcodeInfo> _opcodes = new();
    private static readonly char[] SplitChars = { ' ', '\t' };

    /// <summary>
    /// 构造函数，用于初始化导出器
    /// </summary>
    public NetworkProtocolExporter(bool regenerateOpCodeCache)
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

            _networkProtocolDirectoryInnerBson = $"{_networkProtocolDirectory}InnerBson";
            
            if (!Directory.Exists(_networkProtocolDirectoryInnerBson))
            {
                Directory.CreateDirectory(_networkProtocolDirectoryInnerBson);
            }
        }

        _networkProtocolDirectoryOuter = $"{_networkProtocolDirectory}Outer";
        
        if (!Directory.Exists(_networkProtocolDirectoryOuter))
        {
            Directory.CreateDirectory(_networkProtocolDirectoryOuter);
        }

        _opCodeCache = new OpCodeCache(regenerateOpCodeCache);

        var tasks = new Task[2];
        tasks[0] = Task.Run(RouteType);
        tasks[1] = Task.Run(async () =>
        {
            LoadTemplate();
            
            await Start(NetworkProtocolOpCodeType.Outer);
            
            if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
            {
                await Start(NetworkProtocolOpCodeType.Inner);
                await Start(NetworkProtocolOpCodeType.InnerBson);
            }
            
        });
        Task.WaitAll(tasks);
        _opCodeCache.Save();
    }

    private async Task Start(NetworkProtocolOpCodeType opCodeType)
    {
        var files = new List<string>();
        var opCodeName = "";
        OpcodeInfo opcodeInfo = null;
        _opcodes.Clear();
        var file = new StringBuilder();
        var messageStr = new StringBuilder();
        var disposeStr = new StringBuilder();
        var errorCodeStr = new StringBuilder();
        var saveDirectory = new Dictionary<string, string>();
        
        switch (opCodeType)
        {
            case NetworkProtocolOpCodeType.Outer:
            {
                _aMessage = OpCode.OuterMessage;
                _aRequest = OpCode.OuterRequest;
                _aResponse = OpCode.OuterResponse;
                _aRouteMessage = OpCode.OuterRouteMessage;
                _aRouteRequest = OpCode.OuterRouteRequest;
                _aRouteResponse = OpCode.OuterRouteResponse;
                opCodeName = "OuterOpcode";
                
                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Server))
                {
                    saveDirectory.Add(_networkProtocolServerDirectory, _serverTemplate);
                }

                if (ExporterAges.Instance.ExportPlatform.HasFlag(ExportPlatform.Client))
                {
                    saveDirectory.Add(_networkProtocolClientDirectory, _clientTemplate);
                }
                
                var protoBufFiles = FileHelper.GetDirectoryFile(_networkProtocolDirectoryOuter, "*.proto", SearchOption.AllDirectories);
                files.AddRange(protoBufFiles);
                break;
            }
            case NetworkProtocolOpCodeType.Inner:
            {
                // 预留1000个协议号给框架内部协议用
                _aMessage = OpCode.InnerMessage + 1000;
                _aRequest = OpCode.InnerRequest + 1000;
                _aResponse = OpCode.InnerResponse + 1000;
                _aRouteMessage = OpCode.InnerRouteMessage + 1000;
                _aRouteRequest = OpCode.InnerRouteRequest + 1000;
                _aRouteResponse = OpCode.InnerRouteResponse + 1000;
                opCodeName = "InnerOpcode";
                saveDirectory.Add(_networkProtocolServerDirectory, _serverTemplate);
                var protoBufFiles = FileHelper.GetDirectoryFile(_networkProtocolDirectoryInner, "*.proto", SearchOption.AllDirectories);
                files.AddRange(protoBufFiles);
                break;
            }
            case NetworkProtocolOpCodeType.InnerBson:
            {
                // 预留1000个协议号给框架内部协议用
                _aMessage = OpCode.InnerBsonMessage + 1000;
                _aRequest = OpCode.InnerBsonRequest + 1000;
                _aResponse = OpCode.InnerBsonResponse + 1000;
                _aRouteMessage = OpCode.InnerBsonRouteMessage + 1000;
                _aRouteRequest = OpCode.InnerBsonRouteRequest + 1000;
                _aRouteResponse = OpCode.InnerBsonRouteResponse + 1000;
                opCodeName = "InnerBsonOpcode";
                saveDirectory.Add(_networkProtocolServerDirectory, _serverTemplate);
                var protoBufFiles = FileHelper.GetDirectoryFile(_networkProtocolDirectoryInnerBson, "*.proto", SearchOption.AllDirectories);
                files.AddRange(protoBufFiles);
                break;
            }
        }

        #region GenerateProtoFiles
        foreach (var filePath in files)
        {
            var keyIndex = 0;
            var parameter = "";
            var className = "";
            var isMsgHead = false;
            var hasOpCode = false;
            string responseTypeStr = null;
            string customRouteType = null;
            
            var protoFileText = await File.ReadAllTextAsync(filePath);

            foreach (var line in protoFileText.Split('\n'))
            {
                var currentLine = line.Trim();

                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    continue;
                }

                if (currentLine.StartsWith("///"))
                {
                    messageStr.AppendFormat("	/// <summary>\r\n" + "	/// {0}\r\n" + "	/// </summary>\r\n", currentLine.TrimStart(new[] { '/', '/', '/' }));
                    continue;
                }

                if (currentLine.StartsWith("message"))
                {
                    isMsgHead = true;
                    messageStr.AppendLine("\t[MessagePackObject]");
                    className = currentLine.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries)[1];
                    var splits = currentLine.Split(new[] { "//" }, StringSplitOptions.RemoveEmptyEntries);

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
                        messageStr.AppendLine($"<<<<Dispose>>>#if FANTASY_NET || FANTASY_UNITY\n\t\t\tScene.MessagePoolComponent.Return<{className}>(this);\n#endif");
                        messageStr.AppendLine($"\t\t}}");

                        if(parameter == "IMessage")
                        {
                            opcodeInfo.Code = _opCodeCache.GetOpcodeCache(className, ref _aMessage);
                            messageStr.AppendLine($"\t\tpublic uint OpCode() {{ return {opCodeName}.{className}; }}");
                        }
                        else
                        {
                            if (responseTypeStr != null)
                            {
                                messageStr.AppendLine("\t\t[IgnoreMember]");
                                messageStr.AppendLine($"\t\tpublic {responseTypeStr} ResponseType {{ get; set; }}");
                                responseTypeStr = null;
                            }
                            else
                            {
                                if (parameter.Contains("RouteRequest"))
                                {
                                    Log.Error($"{opcodeInfo.Name} 没指定ResponseType");
                                }
                            }

                            if (hasOpCode)
                            {
                                messageStr.AppendLine($"\t\tpublic uint OpCode() {{ return {opCodeName}.{className}; }}");
                            }

                            if (customRouteType != null)
                            {
                                messageStr.AppendLine($"\t\tpublic long RouteTypeOpCode() {{ return (long)RouteType.{customRouteType}; }}");
                                customRouteType = null;
                            }
                            else if (parameter is "IAddressableRouteRequest" or "IAddressableRouteMessage")
                            {
                                messageStr.AppendLine($"\t\tpublic long RouteTypeOpCode() {{ return InnerRouteType.Addressable; }}");
                            }
                            else if (parameter.EndsWith("BsonRouteMessage") || parameter.EndsWith("BsonRouteRequest"))
                            {
                                messageStr.AppendLine($"\t\tpublic long RouteTypeOpCode() {{ return InnerRouteType.BsonRoute; }}");
                            }
                            else if (parameter is "IRouteMessage" or "IRouteRequest")
                            {
                                messageStr.AppendLine($"\t\tpublic long RouteTypeOpCode() {{ return InnerRouteType.Route; }}");
                            }

                            switch (parameter)
                            {
                                case "IRequest":
                                case "IBsonRequest":
                                {
                                    opcodeInfo.Code = _opCodeCache.GetOpcodeCache(className, ref _aRequest);
                                    break;
                                }
                                case "IResponse":
                                case "IBsonResponse":
                                {
                                    opcodeInfo.Code = _opCodeCache.GetOpcodeCache(className, ref _aResponse);
                                    errorCodeStr.AppendLine("\t\t[Key(ErrorCodeKeyIndex)]");
                                    errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                    disposeStr.AppendLine($"\t\t\tErrorCode = default;");
                                    break;
                                }
                                default:
                                {
                                    if (parameter.EndsWith("RouteMessage") || parameter == "IRouteMessage")
                                    {
                                        opcodeInfo.Code  = _opCodeCache.GetOpcodeCache(className, ref _aRouteMessage);
                                    }
                                    else if (parameter.EndsWith("RouteRequest") || parameter == "IRouteRequest")
                                    {
                                        opcodeInfo.Code = _opCodeCache.GetOpcodeCache(className, ref _aRouteRequest);
                                    }
                                    else if (parameter.EndsWith("RouteResponse") || parameter == "IRouteResponse")
                                    {
                                        opcodeInfo.Code = _opCodeCache.GetOpcodeCache(className, ref _aRouteResponse);
                                        errorCodeStr.AppendLine("\t\t[Key(ErrorCodeKeyIndex)]");
                                        errorCodeStr.AppendLine("\t\tpublic uint ErrorCode { get; set; }");
                                        disposeStr.AppendLine($"\t\t\tErrorCode = default;");
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
                        keyIndex = 0;
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
                    Repeated(messageStr, disposeStr, currentLine, ref keyIndex);
                }
                else
                {
                    Members(messageStr, disposeStr, currentLine, ref keyIndex);
                }
            }

            var csName = $"{Path.GetFileNameWithoutExtension(filePath)}.cs";

            foreach (var (directory, template) in saveDirectory)
            {
                var csFile = Path.Combine(directory, csName);
                var content = template.Replace("(Content)", file.ToString());
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
            file.AppendLine($"\t\t public const int {opcode.Name} = {opcode.Code};");
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

    private async Task RouteType()
    {
        var routeTypeFile = $"{_networkProtocolDirectory}RouteType.Config";
        var protoFileText = await File.ReadAllTextAsync(routeTypeFile);
        var routeTypeFileSb = new StringBuilder();
        routeTypeFileSb.AppendLine("namespace Fantasy\n{");
        routeTypeFileSb.AppendLine("\t// Route协议定义(需要定义1000以上、因为1000以内的框架预留)\t");
        routeTypeFileSb.AppendLine("\tpublic enum RouteType : long\n\t{");

        foreach (var line in protoFileText.Split('\n'))
        {
            var currentLine = line.Trim();

            if (currentLine.StartsWith("//"))
            {
                continue;
            }
            
            var splits = currentLine.Split(new[] {"//"}, StringSplitOptions.RemoveEmptyEntries);
            var routeTypeStr = splits[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            routeTypeFileSb.Append($"\t\t{routeTypeStr[0].Trim()} = {routeTypeStr[1].Trim()},");
            
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

    private void Repeated(StringBuilder file, StringBuilder disposeStr, string newline, ref int keyIndex)
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

            file.AppendLine($"\t\t[Key({keyIndex++})]");
            file.AppendLine($"\t\tpublic List<{type}> {name} = new List<{type}>();");
            disposeStr.AppendLine($"\t\t\t{name}.Clear();");
        }
        catch (Exception e)
        {
            Log.Error($"{newline}\n {e}");
        }
    }

    private void Members(StringBuilder file, StringBuilder disposeStr, string currentLine, ref int keyIndex)
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
            file.AppendLine($"\t\t[Key({keyIndex++})]");
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
    
    private string GetDefault(string type)
    {
        type = type.Trim();
            
        switch (type)
        {
            case "byte":
            case "short":
            case "int":
            case "long":
            case "float":
            case "double":
                return "0";
            case "bool":
                return "false";
            default:
                return "null";
        }
    }

    /// <summary>
    /// 加载模板
    /// </summary>
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
            else if(trim.StartsWith("#else"))
            {
                flag = 2;
                continue;
            }
            else if(trim.StartsWith($"#endif"))
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

        _serverTemplate = serverSb.ToString();
        _clientTemplate = clientSb.ToString();
    }
}