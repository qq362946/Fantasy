using CommandLine;

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

    [Option('f',"Folder", Required = false, HelpText = "ExporterSetting.json file path")]
    public string Folder { get; set; }
}