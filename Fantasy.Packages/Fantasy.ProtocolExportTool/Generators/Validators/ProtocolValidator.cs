using System.Collections.Generic;
using System.Linq;
using Fantasy.ProtocolExportTool.Models;
using Spectre.Console;

namespace Fantasy.ProtocolExportTool.Generators.Validators;

/// <summary>
/// 协议验证器 - 验证 MessageDefinition 的完整性和正确性
/// </summary>
public sealed class ProtocolValidator
{
    private readonly HashSet<string> _allMessageNames = new();
    private readonly List<string> _errors = new();

    /// <summary>
    /// 验证单个消息定义
    /// </summary>
    public void Validate(MessageDefinition message)
    {
        ValidateMessageName(message);
        ValidateInterfaceType(message);
        ValidateResponseType(message);
        ValidateFields(message);
    }

    /// <summary>
    /// 验证消息名称唯一性
    /// </summary>
    private void ValidateMessageName(MessageDefinition message)
    {
        if (string.IsNullOrWhiteSpace(message.Name))
        {
            _errors.Add($"[red]  {message.SourceFilePath} line {message.SourceLineNumber}: Message name is empty[/]");
            return;
        }

        if (!_allMessageNames.Add(message.Name))
        {
            _errors.Add($"[red]  {message.SourceFilePath} line {message.SourceLineNumber}: Duplicate message name: {Markup.Escape(message.Name)}[/]");
        }
    }

    /// <summary>
    /// 验证接口类型
    /// </summary>
    private void ValidateInterfaceType(MessageDefinition message)
    {
        if (string.IsNullOrWhiteSpace(message.InterfaceType))
        {
            return; // 没有接口类型是允许的（纯数据类）
        }

        var validInterfaces = new[]
        {
            "IMessage",
            "IRequest",
            "IResponse",
            "IAddressableMessage",
            "IAddressableRequest",
            "IAddressableResponse",
            "ICustomRouteMessage",
            "ICustomRouteRequest",
            "ICustomRouteResponse",
            "IRoamingMessage",
            "IRoamingRequest",
            "IRoamingResponse",
            "IAddressMessage",
            "IAddressRequest",
            "IAddressResponse"
        };

        if (!validInterfaces.Contains(message.InterfaceType))
        {
            _errors.Add($"[red]  {message.SourceFilePath} line {message.SourceLineNumber}: Invalid interface type '{message.InterfaceType}' for message {message.Name}[/]");
        }
    }

    /// <summary>
    /// 验证响应类型
    /// </summary>
    private void ValidateResponseType(MessageDefinition message)
    {
        var requestTypes = new[]
        {
            "IRequest",
            "IAddressableRequest",
            "ICustomRouteRequest",
            "IRoamingRequest",
            "IAddressRequest"
        };

        if (requestTypes.Contains(message.InterfaceType))
        {
            if (string.IsNullOrWhiteSpace(message.ResponseType))
            {
                _errors.Add($"[red]  {message.SourceFilePath} line {message.SourceLineNumber}: Message {message.Name} is {message.InterfaceType} but missing ResponseType[/]");
            }
        }
    }

    /// <summary>
    /// 验证字段定义
    /// </summary>
    private void ValidateFields(MessageDefinition message)
    {
        var fieldNames = new HashSet<string>();
        var fieldNumbers = new HashSet<int>();

        foreach (var field in message.Fields)
        {
            // 验证字段名唯一性
            if (!fieldNames.Add(field.Name))
            {
                _errors.Add($"[red]  {message.SourceFilePath} line {field.SourceLineNumber}: Duplicate field name '{field.Name}' in message {message.Name}[/]");
            }

            // 验证字段编号唯一性
            if (!fieldNumbers.Add(field.FieldNumber))
            {
                _errors.Add($"[red]  {message.SourceFilePath} line {field.SourceLineNumber}: Duplicate field number {field.FieldNumber} in message {message.Name}[/]");
            }

            // 验证字段编号范围 (ProtoBuf 要求 1-536870911)
            if (field.FieldNumber < 1 || field.FieldNumber > 536870911)
            {
                _errors.Add($"[red]  {message.SourceFilePath} line {field.SourceLineNumber}: Field number {field.FieldNumber} out of range (1-536870911) for field {field.Name} in message {message.Name}[/]");
            }

            // 验证字段名称不为空
            if (string.IsNullOrWhiteSpace(field.Name))
            {
                _errors.Add($"[red]  {message.SourceFilePath} line {field.SourceLineNumber}: Field name is empty in message {message.Name}[/]");
            }

            // 验证字段类型不为空
            if (string.IsNullOrWhiteSpace(field.Type))
            {
                _errors.Add($"[red]  {message.SourceFilePath} line {field.SourceLineNumber}: Field type is empty for field {field.Name} in message {message.Name}[/]");
            }
        }
    }

    /// <summary>
    /// 获取所有验证错误
    /// </summary>
    public List<string> GetErrors() => _errors;

    /// <summary>
    /// 清空验证状态
    /// </summary>
    public void Clear()
    {
        _allMessageNames.Clear();
        _errors.Clear();
    }
}
