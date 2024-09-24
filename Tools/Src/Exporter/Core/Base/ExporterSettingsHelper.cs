using Fantasy.Helper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8632
namespace Exporter;

public sealed class CustomSerialize
{
    public string NameSpace { get; set; }
    public int KeyIndex { get; set; }
    public string SerializeName { get; set; }
    public string Attribute { get; set; }
    public string Ignore { get; set; }
    public string Member { get; set; }
    public uint OpCodeType { get; set; }
}

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
    public static Dictionary<string, CustomSerialize> CustomSerializes { get; private set; }

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

        CustomSerializes = [];
        var sort = new SortedList<long, CustomSerialize>();
        var arrayOfValuesSection = root.GetSection("Export:Serializes:Value");
        
        foreach (var item in arrayOfValuesSection.GetChildren())
        {
            var serializeItem = new CustomSerialize
            {
                KeyIndex = Convert.ToInt32(item.GetSection("KeyIndex").Value),
                NameSpace = item.GetSection("NameSpace").Value,
                SerializeName = item.GetSection("SerializeName").Value,
                Attribute = item.GetSection("Attribute").Value,
                Ignore = item.GetSection("Ignore").Value,
                Member = item.GetSection("Member").Value
            };
           
            sort.Add(HashCodeHelper.ComputeHash64(serializeItem.SerializeName), serializeItem);
        }

        for (var i = 0; i < sort.Count; i++)
        {
            var customSerialize = sort.GetValueAtIndex(i);
            customSerialize.OpCodeType = (uint)(i + 2);
            CustomSerializes.Add(customSerialize.SerializeName, customSerialize);
        }
    }
}