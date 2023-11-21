using Fantasy;
using Microsoft.Extensions.Configuration;
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8632
namespace Exporter;

public static class ExporterSettingsHelper
{
    public static string ProtoBufTemplatePath { get; private set; }
    public static string? ProtoBufDirectory { get; private set; }
    public static string ProtoBufServerDirectory { get; private set; }
    public static string ProtoBufClientDirectory { get; private set; }
    public static string? ExcelProgramPath { get; private set; }
    public static string? ExcelVersionFile { get; private set; }
    public static string? ExcelServerFileDirectory { get; private set; }
    public static string? ExcelClientFileDirectory { get; private set; }
    public static string? ExcelServerBinaryDirectory { get; private set; }
    public static string? ExcelClientBinaryDirectory { get; private set; }
    public static string? ExcelServerJsonDirectory { get; private set; }
    public static string? ExcelClientJsonDirectory { get; private set; }
    public static string ExcelTemplatePath { get; private set; }
    public static string? ServerCustomExportDirectory { get; private set; }
    public static string? ClientCustomExportDirectory { get; private set; }
    
    public static void Initialize()
    {
        const string settingsName = "ExporterSettings.json";
        var currentDirectory = Directory.GetCurrentDirectory();

        if (!File.Exists(Path.Combine(currentDirectory, settingsName)))
        {
            throw new FileNotFoundException($"not found {settingsName} in OutputDirectory");
        }

        var root = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(settingsName).Build();
        ProtoBufTemplatePath = FileHelper.GetFullPath(root["Export:ProtoBufTemplatePath:Value"]);
        ProtoBufDirectory = FileHelper.GetFullPath(root["Export:ProtoBufDirectory:Value"]);
        ProtoBufServerDirectory = FileHelper.GetFullPath(root["Export:ProtoBufServerDirectory:Value"]);
        ProtoBufClientDirectory = FileHelper.GetFullPath(root["Export:ProtoBufClientDirectory:Value"]);
        ExcelProgramPath = FileHelper.GetFullPath(root["Export:ExcelProgramPath:Value"]);
        ExcelVersionFile = FileHelper.GetFullPath(root["Export:ExcelVersionFile:Value"]);
        ExcelServerFileDirectory = FileHelper.GetFullPath(root["Export:ExcelServerFileDirectory:Value"]);
        ExcelClientFileDirectory = FileHelper.GetFullPath(root["Export:ExcelClientFileDirectory:Value"]);
        ExcelServerBinaryDirectory = FileHelper.GetFullPath(root["Export:ExcelServerBinaryDirectory:Value"]);
        ExcelClientBinaryDirectory = FileHelper.GetFullPath(root["Export:ExcelClientBinaryDirectory:Value"]);
        ExcelServerJsonDirectory = FileHelper.GetFullPath(root["Export:ExcelServerJsonDirectory:Value"]);
        ExcelClientJsonDirectory = FileHelper.GetFullPath(root["Export:ExcelClientJsonDirectory:Value"]);
        ExcelTemplatePath = FileHelper.GetFullPath(root["Export:ExcelTemplatePath:Value"]);
        ServerCustomExportDirectory = FileHelper.GetFullPath(root["Export:ServerCustomExportDirectory:Value"]);
        ClientCustomExportDirectory = FileHelper.GetFullPath(root["Export:ClientCustomExportDirectory:Value"]);
    }
}