namespace Fantasy.Tools;

/// <summary>
/// 导出目标平台枚举，用于标识导出到哪个平台。
/// </summary>
[Flags]
public enum ExportPlatform : byte
{
    None = 0,
    Client = 1,
    Server = 1 << 1,
    All = Client | Server,
}