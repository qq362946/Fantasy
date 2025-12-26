using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fantasy.Network;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;
#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace Fantasy.ProtocolExportTool.Generators.Parsers;

/// <summary>
/// 解析结果
/// </summary>
public struct ParseResult
{
    public HashSet<string> Namespaces { get; init; } = new();
    public List<EnumDefinition> Enums { get; init; } = new();
    public Dictionary<string, MessageDefinition> Messages { get; init; } = new();
    
    public ParseResult(HashSet<string> namespaces, Dictionary<string, MessageDefinition> messages, List<EnumDefinition> enums)
    {
        Namespaces = namespaces;
        Messages = messages;
        Enums = enums;
    }
}

/// <summary>
/// 协议文件解析器 - 将 .proto 文件内容解析为 MessageDefinition 对象
/// </summary>
public sealed partial class ProtocolFileParser(string filePath)
{
    private readonly List<string> _errors = new();

    private ParserState _state = ParserState.WaitingForMessage;
    private EnumDefinition? _currentEnum;
    private MessageDefinition? _currentMessage;
    private int _currentEnumValue = 0;
    private ProtocolSettings _currentProtocolSettings = ProtocolSettings.CreateProtoBuf();
    private int _currentKeyIndex = 1;
    private readonly List<string> _pendingComments = new();
    private readonly List<string> _defineConstants = new();
    
    /// <summary>
    /// 字段解析正则表达式: type name = number, 支持中日韩等非英文字符
    /// </summary>
    [GeneratedRegex(@"^\s*([\p{L}_][\p{L}\p{N}_]*)\s+([\p{L}_][\p{L}\p{N}_]*)\s*=\s*(\d+)\s*;$")]
    private static partial Regex FieldRegex();

    /// <summary>
    /// 枚举值解析正则表达式: Name = Value 或 Name, 支持中日韩等非英文字符
    /// </summary>
    [GeneratedRegex(@"^\s*([\p{L}_][\p{L}\p{N}_]*)(?:\s*=\s*(\d+))?\s*$")]
    private static partial Regex EnumValueRegex();

    /// <summary>
    /// Map 字段名称和编号解析正则表达式: name = number, 支持中日韩等非英文字符
    /// </summary>
    [GeneratedRegex(@"^\s*([\p{L}_][\p{L}\p{N}_]*)\s*=\s*(\d+)\s*;$")]
    private static partial Regex MapFieldRegex();

    /// <summary>
    /// 解析协议文件的所有行，返回解析结果。
    /// </summary>
    public ParseResult Parse(string[] lines)
    {
        var namespaces = new HashSet<string>();
        var enums = new List<EnumDefinition>();
        var messages = new Dictionary<string, MessageDefinition>();
        
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var lineNumber = i + 1;

            // 跳过空行
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // 处理条件编译指令 (////开头的行，去除前缀保留后面的内容)
            if (line.StartsWith("////"))
            {
                _defineConstants.Add(line.Substring(4));
                continue;
            }

            // 处理命名空间 (// using System.Runtime)
            if (line.StartsWith("// using"))
            {
                // "// using".Length = 9
                var @namespace = line.Substring(9).Trim();

                if (!string.IsNullOrWhiteSpace(@namespace))
                {
                    namespaces.Add(@namespace);
                }

                continue;
            }

            // 处理 XML 注释 (///) - 会参与代码生成
            if (line.StartsWith("///"))
            {
                _pendingComments.Add(line.Substring(3).Trim());
                continue;
            }

            // 处理协议声明 (// Protocol ProtoBuf/Bson/Custom)
            if (line.StartsWith("// Protocol"))
            {
                ParseProtocolDeclaration(line, lineNumber);
                continue;
            }

            // 处理普通注释 (//) - 仅在 .proto 文件中显示，不参与代码生成
            if (line.StartsWith("//"))
            {
                // 跳过普通注释，不加入 _pendingComments
                continue;
            }

            // 处理 message 声明
            if (line.StartsWith("message"))
            {
                var message = ParseMessageDeclaration(line, lineNumber);
                if (message != null)
                {
                    _currentMessage = message;
                    _state = ParserState.ParsingMessageHeader;
                    // Response 类型的 ErrorCode 占用了索引1，所以字段从2开始
                    _currentKeyIndex = IsResponseType(message.MessageType) ? 2 : 1;
                }
                continue;
            }

            // 处理 enum 声明
            if (line.StartsWith("enum"))
            {
                var enumDef = ParseEnumDeclaration(line, lineNumber);
                if (enumDef != null)
                {
                    _currentEnum = enumDef;
                    _state = ParserState.ParsingEnumHeader;
                    _currentEnumValue = 0;
                }
                continue;
            }

            // 处理枚举头部
            if (_state == ParserState.ParsingEnumHeader)
            {
                if (line == "{")
                {
                    _state = ParserState.ParsingEnumValues;
                    continue;
                }
            }

            // 处理枚举值
            if (_state == ParserState.ParsingEnumValues)
            {
                if (line == "}")
                {
                    // 枚举解析完成
                    if (_currentEnum != null)
                    {
                        enums.Add(_currentEnum);
                        _currentEnum = null;
                    }

                    _state = ParserState.WaitingForMessage;
                    continue;
                }

                // 解析枚举值
                ParseEnumValue(line, lineNumber);
                continue;
            }

            // 处理消息体
            if (_state == ParserState.ParsingMessageHeader)
            {
                if (line == "{")
                {
                    _state = ParserState.ParsingFields;
                    continue;
                }
            }

            if (_state == ParserState.ParsingFields)
            {
                if (line == "}")
                {
                    // 消息解析完成
                    if (_currentMessage != null)
                    {
                        messages.Add(_currentMessage.Name, _currentMessage);
                        _currentMessage = null;
                    }

                    _state = ParserState.WaitingForMessage;
                    _currentProtocolSettings = ProtocolSettings.CreateProtoBuf();
                    continue;
                }

                // 解析字段
                ParseField(line, lineNumber);
            }
        }

        return new ParseResult(namespaces, messages, enums);
    }

    /// <summary>
    /// 解析协议声明 (// Protocol ProtoBuf)
    /// </summary>
    private void ParseProtocolDeclaration(string line, int lineNumber)
    {
        var protocolName = line.Substring("// Protocol".Length).Trim();

        _currentProtocolSettings = protocolName switch
        {
            "ProtoBuf" => ProtocolSettings.CreateProtoBuf(),
            "Bson" => ProtocolSettings.CreateBson(),
            "MemoryPack" => ProtocolSettings.CreateMemoryPack(),
            _ => throw new NotSupportedException($"Unsupported protocol type '{protocolName}' at {filePath} line {lineNumber}. Only 'ProtoBuf' and 'Bson' are supported.")
        };
    }

    /// <summary>
    /// 解析消息声明 (message C2G_Login // IRequest,G2C_Login 或 message C2Chat_TestMessage // ICustomRouteMessage,ChatRoute)
    /// </summary>
    private MessageDefinition? ParseMessageDeclaration(string line, int lineNumber)
    {
        var parts = line.Split(["//"], StringSplitOptions.None);
        var messagePart = parts[0].Trim();
        var messageName = messagePart.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)[1];

        var message = new MessageDefinition
        {
            Name = messageName,
            SourceFilePath = filePath,
            SourceLineNumber = lineNumber,
            Protocol = CloneProtocolSettings(_currentProtocolSettings),
            DocumentationComments = new List<string>(_pendingComments),
            DefineConstants = new List<string>(_defineConstants)
        };

        _pendingComments.Clear();
        _defineConstants.Clear();

        // 解析接口类型和参数 (// IRequest,G2C_Login,ChatRouteType)
        if (parts.Length > 1)
        {
            var parameters = parts[1].Trim()
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (parameters.Length <= 0)
            {
                return message;
            }
            
            message.InterfaceType = parameters[0];

            switch (parameters.Length)
            {
                case 1:
                {
                    if (message.InterfaceType.Contains("IResponse") ||
                        message.InterfaceType.Contains("IAddressableResponse") ||
                        message.InterfaceType.Contains("IAddressResponse"))
                    {
                        message.MessageType = MessageType.Response;
                    }
                    else if (message.InterfaceType.Contains("ICustomRouteResponse"))
                    {
                        message.MessageType = MessageType.RouteTypeResponse;
                    }
                    else if (message.InterfaceType.Contains("IRoamingResponse"))
                    {
                        message.MessageType = MessageType.RoamingResponse;
                    }
                    else if(message.InterfaceType.Contains("IMessage") || 
                            message.InterfaceType.Contains("IAddressableMessage") ||
                            message.InterfaceType.Contains("IAddressMessage"))
                    {
                        message.MessageType = MessageType.Message;
                    }
                    break;
                }
                case 2:
                {
                    var secondParam = parameters[1];
                    
                    if (message.InterfaceType.Contains("ICustomRouteMessage"))
                    {
                        message.MessageType = MessageType.RouteTypeMessage;
                        message.CustomRouteType = $"Fantasy.RouteType.{secondParam}";
                    }
                    else if (message.InterfaceType.Contains("IRoamingMessage"))
                    {
                        message.MessageType = MessageType.RoamingMessage;
                        message.CustomRouteType = $"Fantasy.RoamingType.{secondParam}";
                    }
                    else if (message.InterfaceType.Contains("IRequest") ||
                             message.InterfaceType.Contains("IAddressableRequest") ||
                             message.InterfaceType.Contains("IAddressRequest"))
                    {
                        message.MessageType = MessageType.Request;
                        message.ResponseType = secondParam;
                    }
                    break;
                }
                case 3:
                {
                    var secondParam = parameters[1];
                    var thirdParam = parameters[2];
                    
                    if (message.InterfaceType.Contains("ICustomRouteRequest"))
                    {
                        message.MessageType = MessageType.RouteTypeRequest;
                        message.ResponseType = secondParam;
                        message.CustomRouteType = $"Fantasy.RouteType.{thirdParam}";
                    }
                    else if (message.InterfaceType.Contains("IRoamingRequest"))
                    {
                        message.MessageType = MessageType.RoamingRequest;
                        message.ResponseType = secondParam;
                        message.CustomRouteType = $"Fantasy.RoamingType.{thirdParam}";
                    }
                    break;
                }
            }
        }

        return message;
    }

    /// <summary>
    /// 解析字段定义
    /// </summary>
    private void ParseField(string line, int lineNumber)
    {
        if (_currentMessage == null)
        {
            return;
        }

        // 提取行尾的 /// 注释
        var commentIndex = line.IndexOf("///", StringComparison.Ordinal);
        
        if (commentIndex > 0)
        {
            var inlineComment = line.Substring(commentIndex + 3).Trim();
            if (!string.IsNullOrWhiteSpace(inlineComment))
            {
                _pendingComments.Add(inlineComment);
            }
            line = line.Substring(0, commentIndex).Trim();
        }
        else
        {
            // 移除普通的行尾注释 (// xxx)
            commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex > 0)
            {
                line = line.Substring(0, commentIndex).Trim();
            }
        }

        FieldDefinition? field = null;

        if (line.StartsWith("map"))
        {
            field = ParseMapField(line, lineNumber);
        }
        else if (line.StartsWith("repeatedArray"))
        {
            field = ParseRepeatedField(line, lineNumber, FieldCollectionType.RepeatedArray, "repeatedArray");
        }
        else if (line.StartsWith("repeatedList"))
        {
            field = ParseRepeatedField(line, lineNumber, FieldCollectionType.RepeatedList, "repeatedList");
        }
        else if (line.StartsWith("repeated"))
        {
            field = ParseRepeatedField(line, lineNumber, FieldCollectionType.Repeated, "repeated");
        }
        else
        {
            field = ParseRegularField(line, lineNumber);
        }

        if (field != null)
        {
            field.KeyIndex = _currentKeyIndex++;
            field.DocumentationComments = new List<string>(_pendingComments);
            _currentMessage.Fields.Add(field);
            _pendingComments.Clear();
        }
    }

    /// <summary>
    /// 解析重复字段 (repeated/repeatedArray/repeatedList type name = number)
    /// </summary>
    private FieldDefinition? ParseRepeatedField(string line, int lineNumber, FieldCollectionType collectionType, string keyword)
    {
        // 格式: repeated int Items = 1
        var content = line.Substring(keyword.Length).Trim();
        var match = FieldRegex().Match(content);

        if (!match.Success)
        {
            _errors.Add($"[red]  {filePath} line {lineNumber}: Invalid repeated field format: {Markup.Escape(line)}[/]");
            return null;
        }

        return new FieldDefinition
        {
            Type = match.Groups[1].Value,
            Name = match.Groups[2].Value,
            FieldNumber = int.Parse(match.Groups[3].Value),
            CollectionType = collectionType,
            SourceLineNumber = lineNumber
        };
    }

    /// <summary>
    /// 解析普通字段 (type name = number)
    /// </summary>
    private FieldDefinition? ParseRegularField(string line, int lineNumber)
    {
        // 格式: string Account = 1
        var match = FieldRegex().Match(line);

        if (!match.Success)
        {
            _errors.Add($"[red]  {filePath} line {lineNumber}: Invalid field format: {Markup.Escape(line)}[/]");
            return null;
        }

        return new FieldDefinition
        {
            Type = match.Groups[1].Value,
            Name = match.Groups[2].Value,
            FieldNumber = int.Parse(match.Groups[3].Value),
            CollectionType = FieldCollectionType.Normal,
            SourceLineNumber = lineNumber
        };
    }

    /// <summary>
    /// 解析 map 字段 (map<keyType, valueType> name = number)
    /// </summary>
    private FieldDefinition? ParseMapField(string line, int lineNumber)
    {
        // 格式: map<int32, string> Dic = 3;
        // 移除 "map" 前缀
        var content = line.Substring(3).Trim();

        // 提取 <keyType, valueType>
        var genericStart = content.IndexOf('<');
        var genericEnd = content.IndexOf('>');

        if (genericStart == -1 || genericEnd == -1 || genericEnd <= genericStart)
        {
            _errors.Add($"[red]  {filePath} line {lineNumber}: Invalid map format, expected 'map<keyType, valueType> name = number': {Markup.Escape(line)}[/]");
            return null;
        }

        var genericContent = content.Substring(genericStart + 1, genericEnd - genericStart - 1).Trim();
        var types = genericContent.Split(',');

        if (types.Length != 2)
        {
            _errors.Add($"[red]  {filePath} line {lineNumber}: Map must have exactly 2 type parameters (key, value): {Markup.Escape(line)}[/]");
            return null;
        }

        var keyType = types[0].Trim();
        var valueType = types[1].Trim();

        // 提取字段名和编号: name = number;
        // 注意：这里剩余部分是 "Dic = 3;" 格式（没有类型名）
        var remaining = content.Substring(genericEnd + 1).Trim();
        var match = MapFieldRegex().Match(remaining);

        if (!match.Success)
        {
            _errors.Add($"[red]  {filePath} line {lineNumber}: Invalid map field format after type parameters, expected 'name = number;': {Markup.Escape(line)}[/]");
            return null;
        }

        return new FieldDefinition
        {
            Type = $"Dictionary<{keyType}, {valueType}>",  // 存储为完整类型以便后续处理
            Name = match.Groups[1].Value,      // 第一个捕获组是字段名
            FieldNumber = int.Parse(match.Groups[2].Value),  // 第二个捕获组是字段编号
            CollectionType = FieldCollectionType.Map,
            MapKeyType = keyType,
            MapValueType = valueType,
            SourceLineNumber = lineNumber
        };
    }

    /// <summary>
    /// 解析枚举声明 (enum ErrorCode)
    /// </summary>
    private EnumDefinition? ParseEnumDeclaration(string line, int lineNumber)
    {
        // 格式: enum EnumName
        var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            _errors.Add($"[red]  {filePath} line {lineNumber}: Invalid enum declaration format: {Markup.Escape(line)}[/]");
            return null;
        }

        var enumName = parts[1];

        var enumDef = new EnumDefinition
        {
            Name = enumName,
            SourceFilePath = filePath,
            SourceLineNumber = lineNumber,
            DocumentationComments = new List<string>(_pendingComments)
        };

        _pendingComments.Clear();
        return enumDef;
    }

    /// <summary>
    /// 解析枚举值定义 (Success = 0 或 Error = 1,)
    /// </summary>
    private void ParseEnumValue(string line, int lineNumber)
    {
        if (_currentEnum == null)
        {
            return;
        }

        // 提取行尾的 /// 注释
        var commentIndex = line.IndexOf("///", StringComparison.Ordinal);

        if (commentIndex > 0)
        {
            var inlineComment = line.Substring(commentIndex + 3).Trim();
            if (!string.IsNullOrWhiteSpace(inlineComment))
            {
                _pendingComments.Add(inlineComment);
            }
            line = line.Substring(0, commentIndex).Trim();
        }
        else
        {
            // 移除普通的行尾注释 (// xxx)
            commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex > 0)
            {
                line = line.Substring(0, commentIndex).Trim();
            }
        }

        // 移除末尾的逗号
        line = line.TrimEnd(',').Trim();

        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        // 解析枚举值: Name = Value 或 Name
        var match = EnumValueRegex().Match(line);

        if (match.Success)
        {
            var valueName = match.Groups[1].Value;
            var hasExplicitValue = !string.IsNullOrWhiteSpace(match.Groups[2].Value);
            var valueNumber = hasExplicitValue ? int.Parse(match.Groups[2].Value) : _currentEnumValue;

            var enumValue = new EnumValueDefinition
            {
                Name = valueName,
                Value = valueNumber,
                SourceLineNumber = lineNumber,
                DocumentationComments = new List<string>(_pendingComments)
            };

            _currentEnum.Values.Add(enumValue);
            _currentEnumValue = valueNumber + 1;
            _pendingComments.Clear();
        }
        else
        {
            _errors.Add($"[red]  {filePath} line {lineNumber}: Invalid enum value format: {Markup.Escape(line)}[/]");
        }
    }

    /// <summary>
    /// 克隆协议设置
    /// </summary>
    private static ProtocolSettings CloneProtocolSettings(ProtocolSettings source)
    {
        return new ProtocolSettings
        {
            ProtocolName = source.ProtocolName,
            OpCodeType = source.OpCodeType,
            ClassAttribute = source.ClassAttribute,
            MemberAttribute = source.MemberAttribute,
            IgnoreAttribute = source.IgnoreAttribute,
            KeyStartIndex = source.KeyStartIndex
        };
    }

    /// <summary>
    /// 获取解析错误
    /// </summary>
    public List<string> GetErrors() => _errors;

    /// <summary>
    /// 判断是否为 Response 类型
    /// </summary>
    private static bool IsResponseType(MessageType type) =>
        type is MessageType.Response or MessageType.RouteTypeResponse or MessageType.RoamingResponse;
}

/// <summary>
/// 解析器状态
/// </summary>
internal enum ParserState
{
    /// <summary>
    /// 等待 message/enum 声明
    /// </summary>
    WaitingForMessage,

    /// <summary>
    /// 解析 message 头部（等待左大括号）
    /// </summary>
    ParsingMessageHeader,

    /// <summary>
    /// 解析字段
    /// </summary>
    ParsingFields,

    /// <summary>
    /// 解析 enum 头部（等待左大括号）
    /// </summary>
    ParsingEnumHeader,

    /// <summary>
    /// 解析枚举值
    /// </summary>
    ParsingEnumValues
}
