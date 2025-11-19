using Fantasy.Network;

namespace Fantasy.ProtocolExportTool.Models;

/// <summary>
/// OpCode 信息
/// </summary>
public sealed record OpcodeInfo
{
    /// <summary>
    /// 消息名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// OpCode 数值
    /// </summary>
    public uint Code { get; set; }

    /// <summary>
    /// OpCode 协议类型
    /// </summary>
    public uint ProtocolType { get; set; }
}
