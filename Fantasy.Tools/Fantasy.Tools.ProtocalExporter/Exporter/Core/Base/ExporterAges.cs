using CommandLine;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Exporter;

public class ExporterAges
{
    public static ExporterAges Instance;
    /// <summary>
    /// 导出目标平台枚举，用于标识导出到哪个平台
    /// </summary>
    [Option("ExportPlatform", Required = false, Default = ExportPlatform.None, HelpText = "导出目标平台枚举，用于标识导出到哪个平台")]
    public ExportPlatform ExportPlatform { get; set; }
}