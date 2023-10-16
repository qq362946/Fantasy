using OfficeOpenXml;

namespace Exporter.Excel;

/// <summary>
/// Excel表格类，用于存储表格的名称和列信息。
/// </summary>
public sealed class ExcelTable
{
    /// <summary>
    /// 表格的名称。
    /// </summary>
    public readonly string Name;
    /// <summary>
    /// 客户端列信息，使用排序字典存储列名和列索引列表。
    /// </summary>
    public readonly SortedDictionary<string, List<int>> ClientColInfos = new();
    /// <summary>
    /// 服务器端列信息，使用排序字典存储列名和列索引列表。
    /// </summary>
    public readonly SortedDictionary<string, List<int>> ServerColInfos = new();
    /// <summary>
    /// 构造函数，初始化Excel表格对象并设置表格名称。
    /// </summary>
    /// <param name="name">表格名称。</param>
    public ExcelTable(string name)
    {
        Name = name;
    }
}