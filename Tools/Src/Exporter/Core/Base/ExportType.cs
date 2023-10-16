namespace Fantasy.Exporter;

/// <summary>
/// 导出类型枚举，用于标识不同类型的导出操作。
/// </summary>
public enum ExportType
{
    /// <summary>
    /// 无导出类型。
    /// </summary>
    None = 0, 
    /// <summary>
    /// 导出ProtoBuf类型。
    /// </summary>
    ProtoBuf = 1,
    /// <summary>
    /// 导出网络协议并重新生成OpCode。
    /// </summary>
    ProtoBufAndOpCodeCache = 2,
    /// <summary>
    /// 所有数据的增量导出Excel类型。
    /// </summary>
    AllExcelIncrement = 3,
    /// <summary>
    /// 所有数据的全量导出Excel类型。
    /// </summary>
    AllExcel = 4,
    /// <summary>
    /// 导出类型枚举的最大值，一定要放在最后。
    /// </summary>
    Max,
}

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