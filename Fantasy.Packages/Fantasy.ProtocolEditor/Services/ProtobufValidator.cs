using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fantasy.ProtocolEditor.Services;

/// <summary>
/// Protobuf 语法验证器
/// </summary>
public class ProtobufValidator
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationError
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private static readonly HashSet<string> ValidKeywords = new()
    {
        "syntax", "package", "import", "option", "message", "enum", "service",
        "rpc", "returns", "stream", "repeated", "optional", "required",
        "reserved", "extensions", "extend", "oneof", "map", "public", "weak"
    };

    private static readonly HashSet<string> ValidTypes = new()
    {
        "double", "float", "int32", "int64", "uint32", "uint64",
        "sint32", "sint64", "fixed32", "fixed64", "sfixed32", "sfixed64",
        "bool", "string", "byte"
    };

    public List<ValidationError> Validate(string text)
    {
        var errors = new List<ValidationError>();
        var lines = text.Split('\n');

        // 第一遍：收集所有的 message 定义
        var definedMessages = new HashSet<string>();
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("message"))
            {
                var match = Regex.Match(trimmedLine, @"message\s+([\p{L}\p{N}_]+)");
                if (match.Success)
                {
                    definedMessages.Add(match.Groups[1].Value);
                }
            }
        }

        // 第二遍：验证语法
        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var line = lines[lineNum];
            var trimmedLine = line.Trim();

            // 跳过空行
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // 跳过纯注释行（不检查接口格式，因为只有 message 行的注释才需要检查）
            if (trimmedLine.StartsWith("//"))
            {
                continue;
            }

            // 检查 syntax 语法
            if (trimmedLine.StartsWith("syntax"))
            {
                ValidateSyntax(line, lineNum, errors, GetLineOffset(lines, lineNum));
            }
            // 检查 message 定义
            else if (trimmedLine.StartsWith("message"))
            {
                ValidateMessage(line, lineNum, errors, GetLineOffset(lines, lineNum));

                // 只检查 message 行的注释部分（接口格式验证）
                if (line.Contains("//"))
                {
                    ValidateRequestComment(line, lineNum, errors, GetLineOffset(lines, lineNum), definedMessages);
                }
            }
            // 检查字段定义（不检查字段注释中的接口格式）
            else if (Regex.IsMatch(trimmedLine, @"^\s*\w+\s+\w+\s*=\s*\d+"))
            {
                // 字段行，只验证字段语法，不检查注释
                ValidateField(line, lineNum, errors, GetLineOffset(lines, lineNum));
            }
            // 跳过其他行（如花括号、enum、service 等）
            else
            {
                // 不做任何验证
            }
        }

        return errors;
    }

    private void ValidateRequestComment(string line, int lineNum, List<ValidationError> errors, int lineOffset, HashSet<string> definedMessages)
    {
        // 定义所有有效的接口名称
        var validInterfaces = new HashSet<string>
        {
            // 单向消息接口
            "IMessage",
            "IResponse",
            "IAddressResponse",
            "IRoamingResponse",
            "IAddressableResponse",
            "ICustomRouteResponse",
            "IAddressMessage",
            "IAddressableMessage",
            "IRoamingMessage",
            "ICustomRouteMessage",
            // Request 接口
            "IRequest",
            "IAddressRequest",
            "IAddressableRequest",
            "IRoamingRequest",
            "ICustomRouteRequest"
        };

        // 检查注释中是否有以 'I' 开头且看起来像接口的标识符
        var interfacePattern = @"\b(I[A-Z][A-Za-z0-9]*)\b";
        var interfaceMatches = Regex.Matches(line, interfacePattern);

        foreach (Match interfaceMatch in interfaceMatches)
        {
            var interfaceName = interfaceMatch.Groups[1].Value;

            // 如果接口名称不在有效列表中，报错
            if (!validInterfaces.Contains(interfaceName))
            {
                var errorStart = lineOffset + interfaceMatch.Index;

                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = interfaceMatch.Index,
                    Offset = errorStart,
                    Length = interfaceName.Length,
                    Message = $"未知接口 '{interfaceName}'。有效接口包括：{string.Join(", ", validInterfaces)}"
                });
            }
        }

        // 检查是否包含 IRoamingMessage（需要漫游类型）
        var roamingMessageMatch = Regex.Match(line, @"\bIRoamingMessage\b");
        if (roamingMessageMatch.Success)
        {
            // IRoamingMessage 必须是格式：// IRoamingMessage,RoamingType
            var formatPattern = @"//\s*IRoamingMessage\s*,\s*[\p{L}\p{N}_]+";
            var formatMatch = Regex.Match(line, formatPattern);

            System.Diagnostics.Debug.WriteLine($"Found IRoamingMessage in line: {line.Trim()}");

            if (!formatMatch.Success)
            {
                var errorStart = lineOffset + roamingMessageMatch.Index;
                var errorLength = roamingMessageMatch.Length;

                // 扩展到整个注释部分
                var commentMatch = Regex.Match(line.Substring(roamingMessageMatch.Index), @"IRoamingMessage.*");
                if (commentMatch.Success)
                {
                    errorLength = commentMatch.Length;
                }

                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = roamingMessageMatch.Index,
                    Offset = errorStart,
                    Length = errorLength,
                    Message = "IRoamingMessage 必须指定漫游类型。正确格式：// IRoamingMessage,RoamingType"
                });
            }
            return; // IRoamingMessage 处理完毕
        }

        // 检查是否包含 ICustomRouteMessage（需要路由ID）
        var customRouteMessageMatch = Regex.Match(line, @"\bICustomRouteMessage\b");
        if (customRouteMessageMatch.Success)
        {
            // ICustomRouteMessage 必须是格式：// ICustomRouteMessage,RouteType
            var formatPattern = @"//\s*ICustomRouteMessage\s*,\s*[\p{L}\p{N}_]+";
            var formatMatch = Regex.Match(line, formatPattern);

            System.Diagnostics.Debug.WriteLine($"Found ICustomRouteMessage in line: {line.Trim()}");

            if (!formatMatch.Success)
            {
                var errorStart = lineOffset + customRouteMessageMatch.Index;
                var errorLength = customRouteMessageMatch.Length;

                // 扩展到整个注释部分
                var commentMatch = Regex.Match(line.Substring(customRouteMessageMatch.Index), @"ICustomRouteMessage.*");
                if (commentMatch.Success)
                {
                    errorLength = commentMatch.Length;
                }

                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = customRouteMessageMatch.Index,
                    Offset = errorStart,
                    Length = errorLength,
                    Message = "ICustomRouteMessage 必须指定路由ID。正确格式：// ICustomRouteMessage,RouteType"
                });
            }
            return; // ICustomRouteMessage 处理完毕
        }

        // 检查是否包含 IRoamingRequest（需要三段格式：响应类型和漫游类型）
        var roamingRequestMatch = Regex.Match(line, @"\bIRoamingRequest\b");
        if (roamingRequestMatch.Success)
        {
            // IRoamingRequest 必须是格式：// IRoamingRequest,ResponseType,RoamingType
            var formatPattern = @"//\s*IRoamingRequest\s*,\s*([\p{L}_][\p{L}\p{N}_]*)\s*,\s*[\p{L}\p{N}_]+";
            var formatMatch = Regex.Match(line, formatPattern);

            System.Diagnostics.Debug.WriteLine($"Found IRoamingRequest in line: {line.Trim()}");

            if (!formatMatch.Success)
            {
                var errorStart = lineOffset + roamingRequestMatch.Index;
                var errorLength = roamingRequestMatch.Length;

                // 扩展到整个注释部分
                var commentMatch = Regex.Match(line.Substring(roamingRequestMatch.Index), @"IRoamingRequest.*");
                if (commentMatch.Success)
                {
                    errorLength = commentMatch.Length;
                }

                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = roamingRequestMatch.Index,
                    Offset = errorStart,
                    Length = errorLength,
                    Message = "IRoamingRequest 必须指定响应类型和漫游类型。正确格式：// IRoamingRequest,ResponseTypeName,RoamingType"
                });
            }
            else
            {
                // 格式正确，检查响应类型是否存在
                var responseType = formatMatch.Groups[1].Value;
                if (!definedMessages.Contains(responseType))
                {
                    // 查找响应类型在行中的位置
                    var responseIndex = line.IndexOf(responseType, StringComparison.Ordinal);
                    var errorStart = lineOffset + responseIndex;

                    errors.Add(new ValidationError
                    {
                        Line = lineNum,
                        Column = responseIndex,
                        Offset = errorStart,
                        Length = responseType.Length,
                        Message = $"响应类型 '{responseType}' 未定义。请先定义 message {responseType}。"
                    });
                }
            }
            return; // IRoamingRequest 处理完毕
        }

        // 检查是否包含 ICustomRouteRequest（需要特殊格式）
        var customRouteMatch = Regex.Match(line, @"\bICustomRouteRequest\b");
        if (customRouteMatch.Success)
        {
            // ICustomRouteRequest 必须是格式：// ICustomRouteRequest,ResponseType,ChatRoute
            var formatPattern = @"//\s*ICustomRouteRequest\s*,\s*([\p{L}_][\p{L}\p{N}_]*)\s*,\s*[\p{L}\p{N}_]+";
            var formatMatch = Regex.Match(line, formatPattern);

            System.Diagnostics.Debug.WriteLine($"Found ICustomRouteRequest in line: {line.Trim()}");

            if (!formatMatch.Success)
            {
                var errorStart = lineOffset + customRouteMatch.Index;
                var errorLength = customRouteMatch.Length;

                // 扩展到整个注释部分
                var commentMatch = Regex.Match(line.Substring(customRouteMatch.Index), @"ICustomRouteRequest.*");
                if (commentMatch.Success)
                {
                    errorLength = commentMatch.Length;
                }

                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = customRouteMatch.Index,
                    Offset = errorStart,
                    Length = errorLength,
                    Message = "ICustomRouteRequest 必须指定响应类型和路由ID。正确格式：// ICustomRouteRequest,ResponseTypeName,RouteType"
                });
            }
            else
            {
                // 格式正确，检查响应类型是否存在
                var responseType = formatMatch.Groups[1].Value;
                if (!definedMessages.Contains(responseType))
                {
                    // 查找响应类型在行中的位置
                    var responseIndex = line.IndexOf(responseType, StringComparison.Ordinal);
                    var errorStart = lineOffset + responseIndex;

                    errors.Add(new ValidationError
                    {
                        Line = lineNum,
                        Column = responseIndex,
                        Offset = errorStart,
                        Length = responseType.Length,
                        Message = $"响应类型 '{responseType}' 未定义。请先定义 message {responseType}。"
                    });
                }
            }
            return; // ICustomRouteRequest 处理完毕
        }

        // 检查其他 Request 类型接口
        var requestInterfaces = new[]
        {
            "IRequest",
            "IAddressRequest",
            "IAddressableRequest"
        };

        foreach (var requestInterface in requestInterfaces)
        {
            var match = Regex.Match(line, $@"\b{requestInterface}\b");
            if (match.Success)
            {
                // 检查格式是否为 // IRequest,ResponseType
                var formatPattern = "//\\s*" + requestInterface + "\\s*,\\s*([\\p{L}][\\p{L}\\p{N}_]*)";

                var formatMatch = Regex.Match(line, formatPattern);

                System.Diagnostics.Debug.WriteLine($"Found {requestInterface} in line: {line.Trim()}");

                if (!formatMatch.Success)
                {
                    // 查找 Request 接口的位置
                    var errorStart = lineOffset + match.Index;
                    var errorLength = match.Length;

                    // 扩展到整个注释部分
                    var commentMatch = Regex.Match(line.Substring(match.Index), $@"{requestInterface}.*");
                    if (commentMatch.Success)
                    {
                        errorLength = commentMatch.Length;
                    }

                    errors.Add(new ValidationError
                    {
                        Line = lineNum,
                        Column = match.Index,
                        Offset = errorStart,
                        Length = errorLength,
                        Message = $"{requestInterface} 必须指定响应类型。正确格式：// {requestInterface},ResponseTypeName"
                    });
                }
                else
                {
                    // 格式正确，检查响应类型是否存在
                    var responseType = formatMatch.Groups[1].Value;
                    if (!string.IsNullOrEmpty(responseType) && !definedMessages.Contains(responseType))
                    {
                        // 查找响应类型在行中的位置
                        var responseIndex = line.IndexOf(responseType, StringComparison.Ordinal);
                        var errorStart = lineOffset + responseIndex;

                        errors.Add(new ValidationError
                        {
                            Line = lineNum,
                            Column = responseIndex,
                            Offset = errorStart,
                            Length = responseType.Length,
                            Message = $"响应类型 '{responseType}' 未定义。请先定义 message {responseType}。"
                        });
                    }
                }
                break; // 只检查第一个匹配的 Request 接口
            }
        }
    }

    private void ValidateSyntax(string line, int lineNum, List<ValidationError> errors, int lineOffset)
    {
        // syntax = "proto3";
        if (!Regex.IsMatch(line, @"syntax\s*=\s*""proto[23]""\s*;"))
        {
            var match = Regex.Match(line, @"syntax");
            if (match.Success)
            {
                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = match.Index,
                    Offset = lineOffset + match.Index,
                    Length = line.TrimEnd().Length - match.Index,
                    Message = "无效的 syntax 声明。正确格式：syntax = \"proto3\";"
                });
            }
        }
    }

    private void ValidateMessage(string line, int lineNum, List<ValidationError> errors, int lineOffset)
    {
        // message MessageName
        var match = Regex.Match(line, @"message\s+(\w+)");
        if (match.Success)
        {
            var messageName = match.Groups[1].Value;
            // 检查消息名称是否符合命名规范（大写字母开头）
            if (!char.IsUpper(messageName[0]))
            {
                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = match.Groups[1].Index,
                    Offset = lineOffset + match.Groups[1].Index,
                    Length = messageName.Length,
                    Message = $"消息名称 '{messageName}' 应以大写字母开头"
                });
            }
        }
        else
        {
            // message 后面没有名称
            var keywordMatch = Regex.Match(line, @"message");
            if (keywordMatch.Success && !line.Contains("{"))
            {
                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = keywordMatch.Index,
                    Offset = lineOffset + keywordMatch.Index,
                    Length = line.TrimEnd().Length - keywordMatch.Index,
                    Message = "无效的 message 声明。正确格式：message MessageName"
                });
            }
        }
    }

    private void ValidateField(string line, int lineNum, List<ValidationError> errors, int lineOffset)
    {
        // type fieldName = fieldNumber;
        var match = Regex.Match(line, @"(\w+)\s+(\w+)\s*=\s*(\d+)\s*;?");
        if (match.Success)
        {
            // var type = match.Groups[1].Value;
            // var fieldName = match.Groups[2].Value;
            var fieldNumber = int.Parse(match.Groups[3].Value);

            // 注释掉类型检查，允许任何自定义类型（如 float2、float3 等）
            // 导出工具会原样保留非框架内置类型
            // if (!ValidTypes.Contains(type) && !char.IsUpper(type[0]))
            // {
            //     errors.Add(new ValidationError
            //     {
            //         Line = lineNum,
            //         Column = match.Groups[1].Index,
            //         Offset = lineOffset + match.Groups[1].Index,
            //         Length = type.Length,
            //         Message = $"未知类型 '{type}'"
            //     });
            // }

            // 检查字段编号范围
            if (fieldNumber < 1 || fieldNumber > 536870911)
            {
                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = match.Groups[3].Index,
                    Offset = lineOffset + match.Groups[3].Index,
                    Length = match.Groups[3].Value.Length,
                    Message = $"字段编号 {fieldNumber} 超出范围 (1-536870911)"
                });
            }

            // 检查保留字段编号
            if (fieldNumber >= 19000 && fieldNumber <= 19999)
            {
                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = match.Groups[3].Index,
                    Offset = lineOffset + match.Groups[3].Index,
                    Length = match.Groups[3].Value.Length,
                    Message = $"字段编号 {fieldNumber} 是保留编号 (19000-19999)"
                });
            }

            // 检查是否缺少分号（去掉注释部分再检查）
            var lineWithoutComment = line;
            var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                lineWithoutComment = line.Substring(0, commentIndex);
            }

            if (!lineWithoutComment.TrimEnd().EndsWith(";"))
            {
                errors.Add(new ValidationError
                {
                    Line = lineNum,
                    Column = line.TrimEnd().Length,
                    Offset = lineOffset + line.TrimEnd().Length,
                    Length = 1,
                    Message = "缺少分号 ';'"
                });
            }
        }
    }

    private int GetLineOffset(string[] lines, int lineNum)
    {
        int offset = 0;
        for (int i = 0; i < lineNum; i++)
        {
            offset += lines[i].Length + 1; // +1 for newline
        }
        return offset;
    }
}
