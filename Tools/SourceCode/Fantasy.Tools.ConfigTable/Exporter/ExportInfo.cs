#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy.Tools.ConfigTable;

/// <summary>
/// 导出信息类，用于存储导出操作的名称和文件信息。
/// </summary>
public class ExportInfo
{
    /// <summary>
    /// 导出操作的名称。
    /// </summary>
    public string Name;
    /// <summary>
    /// 导出操作生成的文件信息。
    /// </summary>
    public FileInfo FileInfo;
}