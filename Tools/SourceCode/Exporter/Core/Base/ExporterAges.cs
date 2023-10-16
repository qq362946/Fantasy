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
    /// <summary>
    /// ProtoBuf生成代码模板的位置
    /// </summary>
    [Option("ProtoBufTemplatePath", Required = false, Default = null, HelpText = "ProtoBuf生成代码模板的位置")]
    public string? ProtoBufTemplatePath { get; set; }
    /// <summary>
    /// ProtoBuf文件所在的位置文件夹位置
    /// </summary>
    [Option("ProtoBufDirectory", Required = false, Default = null, HelpText = "ProtoBuf文件所在的位置文件夹位置")]
    public string? ProtoBufDirectory { get; set; }
    /// <summary>
    /// ProtoBuf生成到服务端的文件夹位置
    /// </summary>
    [Option("ProtoBufServerDirectory", Required = false, Default = null, HelpText = "ProtoBuf生成到服务端的文件夹位置")]
    public string? ProtoBufServerDirectory { get; set; }
    /// <summary>
    /// ProtoBuf生成到客户端的文件夹位置
    /// </summary>
    [Option("ProtoBufClientDirectory", Required = false, Default = null, HelpText = "ProtoBuf生成到客户端的文件夹位置")]
    public string? ProtoBufClientDirectory { get; set; }
    /// <summary>
    /// Excel配置文件根目录
    /// </summary>
    [Option("ExcelProgramPath", Required = false, Default = null, HelpText = "Excel配置文件根目录")]
    public string? ExcelProgramPath { get; set; }
    /// <summary>
    /// Excel版本文件的位置
    /// </summary>
    [Option("ExcelVersionFile", Required = false, Default = null, HelpText = "Excel版本文件的位置")]
    public string? ExcelVersionFile { get; set; }
    /// <summary>
    /// Excel生成服务器代码的文件夹位置
    /// </summary>
    [Option("ExcelServerFileDirectory", Required = false, Default = null, HelpText = "Excel生成服务器代码的文件夹位置")]
    public string? ExcelServerFileDirectory { get; set; }
    /// <summary>
    /// Excel生成客户端代码文件夹位置
    /// </summary>
    [Option("ExcelClientFileDirectory", Required = false, Default = null, HelpText = "Excel生成服务器代码的文件夹位置")]
    public string? ExcelClientFileDirectory { get; set; }
    /// <summary>
    /// Excel生成服务器二进制数据文件夹位置
    /// </summary>
    [Option("ExcelServerBinaryDirectory", Required = false, Default = null, HelpText = "Excel生成服务器二进制数据文件夹位置")]
    public string? ExcelServerBinaryDirectory { get; set; }
    /// <summary>
    /// Excel生成客户端二进制数据文件夹位置
    /// </summary>
    [Option("ExcelClientBinaryDirectory", Required = false, Default = null, HelpText = "Excel生成客户端二进制数据文件夹位置")]
    public string? ExcelClientBinaryDirectory { get; set; }
    /// <summary>
    /// Excel生成服务器Json数据文件夹位置
    /// </summary>
    [Option("ExcelServerJsonDirectory", Required = false, Default = null, HelpText = "Excel生成服务器Json数据文件夹位置")]
    public string? ExcelServerJsonDirectory { get; set; }
    /// <summary>
    /// Excel生成客户端Json数据文件夹位置
    /// </summary>
    [Option("ExcelClientJsonDirectory", Required = false, Default = null, HelpText = "Excel生成客户端Json数据文件夹位置")]
    public string? ExcelClientJsonDirectory { get; set; }
    /// <summary>
    /// Excel生成代码模板的位置
    /// </summary>
    [Option("ExcelTemplatePath", Required = false, Default = null, HelpText = "Excel生成代码模板的位置")]
    public string? ExcelTemplatePath { get; set; }
    /// <summary>
    /// 服务器自定义导出代码文件夹位置
    /// </summary>
    [Option("ServerCustomExportDirectory", Required = false, Default = null, HelpText = "服务器自定义导出代码文件夹位置")]
    public string? ServerCustomExportDirectory { get; set; }
    /// <summary>
    /// 客户端自定义导出代码文件夹位置
    /// </summary>
    [Option("ClientCustomExportDirectory", Required = false, Default = null, HelpText = "客户端自定义导出代码文件夹位置")]
    public string? ClientCustomExportDirectory { get; set; }
    /// <summary>
    /// 自定义导出代码存放的程序集
    /// </summary>
    [Option("CustomExportAssembly", Required = false, Default = null, HelpText = "自定义导出代码存放的程序集")]
    public string? CustomExportAssembly { get; set; }
}