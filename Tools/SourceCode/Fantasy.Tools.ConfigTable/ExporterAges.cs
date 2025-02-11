using CommandLine;
using Fantasy.Tools.ConfigTable;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Tools;

public class ExporterAges
{
    public static ExporterAges Instance;
    /// <summary>
    /// 导出目标平台枚举，用于标识导出到哪个平台
    /// </summary>
    [Option('p',"ExportPlatform", Required = false, Default = ExportPlatform.None, HelpText = "Export target platform:\n/// Client target platform \nClient = 1\n/// Server target platform\nServer = 2\n/// Client and Server target platform\nAll = 3")]
    public ExportPlatform ExportPlatform { get; set; }
    /// <summary>
    /// 导出类型
    /// </summary>
    [Option('e',"ExportType", Required = false, Default = ExportType.None, HelpText = "Export Type:\n/// Incremental export of all data in Excel format.\nAllExcelIncrement = 1\n/// Export all data to Excel format.\nAllExcel = 2")]
    public ExportType ExportType { get; set; }
}