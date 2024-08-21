using Fantasy;
using Microsoft.Extensions.Configuration;
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8632
namespace Exporter;

public static class ExporterSettingsHelper
{
    public static string? NetworkProtocolDirectory { get; private set; }
    public static string NetworkProtocolServerDirectory { get; private set; }
    public static string NetworkProtocolClientDirectory { get; private set; }
    public static string? ExcelProgramPath { get; private set; }
    public static string? ExcelVersionFile { get; private set; }
    public static string? ExcelServerFileDirectory { get; private set; }
    public static string? ExcelClientFileDirectory { get; private set; }
    public static string? ExcelServerBinaryDirectory { get; private set; }
    public static string? ExcelClientBinaryDirectory { get; private set; }
    public static string? ExcelServerJsonDirectory { get; private set; }
    public static string? ExcelClientJsonDirectory { get; private set; }
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
        NetworkProtocolDirectory = FileHelper.GetFullPath(root["Export:NetworkProtocolDirectory:Value"]);
        NetworkProtocolServerDirectory = FileHelper.GetFullPath(root["Export:NetworkProtocolServerDirectory:Value"]);
        NetworkProtocolClientDirectory = FileHelper.GetFullPath(root["Export:NetworkProtocolClientDirectory:Value"]);
        ExcelProgramPath = FileHelper.GetFullPath(root["Export:ExcelProgramPath:Value"]);
        ExcelVersionFile = FileHelper.GetFullPath(root["Export:ExcelVersionFile:Value"]);
        ExcelServerFileDirectory = FileHelper.GetFullPath(root["Export:ExcelServerFileDirectory:Value"]);
        ExcelClientFileDirectory = FileHelper.GetFullPath(root["Export:ExcelClientFileDirectory:Value"]);
        ExcelServerBinaryDirectory = FileHelper.GetFullPath(root["Export:ExcelServerBinaryDirectory:Value"]);
        ExcelClientBinaryDirectory = FileHelper.GetFullPath(root["Export:ExcelClientBinaryDirectory:Value"]);
        ExcelServerJsonDirectory = FileHelper.GetFullPath(root["Export:ExcelServerJsonDirectory:Value"]);
        ExcelClientJsonDirectory = FileHelper.GetFullPath(root["Export:ExcelClientJsonDirectory:Value"]);
        ServerCustomExportDirectory = FileHelper.GetFullPath(root["Export:ServerCustomExportDirectory:Value"]);
        ClientCustomExportDirectory = FileHelper.GetFullPath(root["Export:ClientCustomExportDirectory:Value"]);
    }
}