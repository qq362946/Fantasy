using Fantasy.Helper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8632
namespace Exporter;

public sealed class CustomSerialize
{
    public string NameSpace { get; set; }
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


        CustomSerializes = [];
        var sort = new SortedList<long, CustomSerialize>();
        var arrayOfValuesSection = root.GetSection("Export:SerializeHelpers");

        foreach (var item in arrayOfValuesSection.GetChildren())
        {
            var serializeItem = new CustomSerialize
            {
                NameSpace = item.GetChildren().First().GetSection("NameSpace").Value,
                Attribute = item.GetChildren().First().GetSection("Attribute").Value,
                Ignore = item.GetChildren().First().GetSection("Ignore").Value,
                Member = item.GetChildren().First().GetSection("Member").Value
            };

            sort.Add(HashCodeHelper.ComputeHash64(item.Key), serializeItem);
        }


        for (var i = 0; i < sort.Count; i++)
        {
            var customSerialize = sort.GetValueAtIndex(i);
            customSerialize.OpCodeType = (uint)(i + 2);
            CustomSerializes.Add(customSerialize.NameSpace, customSerialize);
        }
    }
}