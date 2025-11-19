using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fantasy.Network;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Generators.Parsers;

/// <summary>
/// 协议文件解析器 - 将 .proto 文件内容解析为 MessageDefinition 对象
/// </summary>
public sealed partial class ProtocolFileParser(string filePath)
{
    private readonly List<string> _errors = new();

    private ParserState _state = ParserState.WaitingForMessage;
    private MessageDefinition? _currentMessage;
    private ProtocolSettings _currentProtocolSettings = ProtocolSettings.CreateProtoBuf();
    private int _currentKeyIndex = 1;
    private readonly List<string> _pendingComments = new();

    /// <summary>
    /// 解析协议文件的所有行，返回消息定义列表
    /// </summary>
    public List<MessageDefinition> Parse(string[] lines)
    {
        var messages = new List<MessageDefinition>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var lineNumber = i + 1;

            // 跳过空行
            if (string.IsNullOrWhiteSpace(line))
            {
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
                        messages.Add(_currentMessage);
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

        return messages;
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
            DocumentationComments = new List<string>(_pendingComments)
        };

        _pendingComments.Clear();

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

        if (line.StartsWith("repeatedArray"))
        {
            field = ParseRepeatedField(line, lineNumber, RepeatedFieldType.RepeatedArray, "repeatedArray");
        }
        else if (line.StartsWith("repeatedList"))
        {
            field = ParseRepeatedField(line, lineNumber, RepeatedFieldType.RepeatedList, "repeatedList");
        }
        else if (line.StartsWith("repeated"))
        {
            field = ParseRepeatedField(line, lineNumber, RepeatedFieldType.Repeated, "repeated");
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
    private FieldDefinition? ParseRepeatedField(string line, int lineNumber, RepeatedFieldType repeatedType, string keyword)
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
            IsRepeated = true,
            RepeatedType = repeatedType,
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
            IsRepeated = false,
            RepeatedType = RepeatedFieldType.None,
            SourceLineNumber = lineNumber
        };
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
            KeyStartIndex = source.KeyStartIndex,
            RequiredNamespaces = new HashSet<string>(source.RequiredNamespaces)
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

    /// <summary>
    /// 字段解析正则表达式: type name = number (允许 Tab 和空格)
    /// </summary>
    [GeneratedRegex(@"^\s*(\w+)\s+(\w+)\s*=\s*(\d+)\s*;\s*$")]
    private static partial Regex FieldRegex();
}

/// <summary>
/// 解析器状态
/// </summary>
internal enum ParserState
{
    /// <summary>
    /// 等待 message 声明
    /// </summary>
    WaitingForMessage,

    /// <summary>
    /// 解析 message 头部（等待左大括号）
    /// </summary>
    ParsingMessageHeader,

    /// <summary>
    /// 解析字段
    /// </summary>
    ParsingFields
}
