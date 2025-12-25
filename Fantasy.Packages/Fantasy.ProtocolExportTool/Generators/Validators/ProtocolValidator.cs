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
    private readonly HashSet<string> _allEnumNames = new();
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
    /// 验证单个枚举定义
    /// </summary>
    public void Validate(EnumDefinition enumDef)
    {
        ValidateEnumName(enumDef);
        ValidateEnumValues(enumDef);
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

            // 验证 map 字段的 key 和 value 类型
            if (field.IsMap)
            {
                ValidateMapField(field, message);
            }
        }
    }

    /// <summary>
    /// 验证 map 字段
    /// </summary>
    private void ValidateMapField(FieldDefinition field, MessageDefinition message)
    {
        // 验证 key 类型不为空
        if (string.IsNullOrWhiteSpace(field.MapKeyType))
        {
            _errors.Add($"[red]  {message.SourceFilePath} line {field.SourceLineNumber}: Map key type is empty for field {field.Name} in message {message.Name}[/]");
        }

        // 验证 value 类型不为空
        if (string.IsNullOrWhiteSpace(field.MapValueType))
        {
            _errors.Add($"[red]  {message.SourceFilePath} line {field.SourceLineNumber}: Map value type is empty for field {field.Name} in message {message.Name}[/]");
        }

        // 验证 map 的 key 类型必须是基本类型或枚举 (不能是复杂对象)
        var validKeyTypes = new[]
        {
            "int", "int32", "uint", "uint32", "long", "int64", "ulong", "uint64",
            "string", "bool", "byte"
        };

        var isValidKeyType = validKeyTypes.Contains(field.MapKeyType) ||
                             (!string.IsNullOrEmpty(field.MapKeyType) && char.IsUpper(field.MapKeyType[0]));

        if (!isValidKeyType && !string.IsNullOrWhiteSpace(field.MapKeyType))
        {
            _errors.Add($"[red]  {message.SourceFilePath} line {field.SourceLineNumber}: Invalid map key type '{field.MapKeyType}' for field {field.Name} in message {message.Name}. Key type must be a primitive type or enum.[/]");
        }
    }

    /// <summary>
    /// 验证枚举名称唯一性
    /// </summary>
    private void ValidateEnumName(EnumDefinition enumDef)
    {
        if (string.IsNullOrWhiteSpace(enumDef.Name))
        {
            _errors.Add($"[red]  {enumDef.SourceFilePath} line {enumDef.SourceLineNumber}: Enum name is empty[/]");
            return;
        }

        if (!_allEnumNames.Add(enumDef.Name))
        {
            _errors.Add($"[red]  {enumDef.SourceFilePath} line {enumDef.SourceLineNumber}: Duplicate enum name: {Markup.Escape(enumDef.Name)}[/]");
        }

        // 检查枚举名称和消息名称是否冲突
        if (_allMessageNames.Contains(enumDef.Name))
        {
            _errors.Add($"[red]  {enumDef.SourceFilePath} line {enumDef.SourceLineNumber}: Enum name '{Markup.Escape(enumDef.Name)}' conflicts with a message name[/]");
        }
    }

    /// <summary>
    /// 验证枚举值定义
    /// </summary>
    private void ValidateEnumValues(EnumDefinition enumDef)
    {
        if (enumDef.Values.Count == 0)
        {
            _errors.Add($"[red]  {enumDef.SourceFilePath} line {enumDef.SourceLineNumber}: Enum {Markup.Escape(enumDef.Name)} has no values[/]");
            return;
        }

        var valueNames = new HashSet<string>();
        var valueNumbers = new HashSet<int>();

        foreach (var value in enumDef.Values)
        {
            // 验证枚举值名称不为空
            if (string.IsNullOrWhiteSpace(value.Name))
            {
                _errors.Add($"[red]  {enumDef.SourceFilePath} line {value.SourceLineNumber}: Enum value name is empty in enum {Markup.Escape(enumDef.Name)}[/]");
                continue;
            }

            // 验证枚举值名称唯一性
            if (!valueNames.Add(value.Name))
            {
                _errors.Add($"[red]  {enumDef.SourceFilePath} line {value.SourceLineNumber}: Duplicate enum value name '{Markup.Escape(value.Name)}' in enum {Markup.Escape(enumDef.Name)}[/]");
            }

            // 验证枚举值数字唯一性
            if (!valueNumbers.Add(value.Value))
            {
                _errors.Add($"[red]  {enumDef.SourceFilePath} line {value.SourceLineNumber}: Duplicate enum value {value.Value} for '{Markup.Escape(value.Name)}' in enum {Markup.Escape(enumDef.Name)}[/]");
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
        _allEnumNames.Clear();
        _errors.Clear();
    }
}
