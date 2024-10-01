using Fantasy.Helper;
using Microsoft.Extensions.Configuration;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8604 // Possible null reference argument.

namespace Fantasy.Tools.ProtocalExporter;

public class ExporterSettingsHelper
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