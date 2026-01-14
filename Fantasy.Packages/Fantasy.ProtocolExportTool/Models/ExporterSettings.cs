namespace Fantasy.ProtocolExportTool.Models;

public class ExporterSettings
{
    public ExportSettings Export { get; set; } = new();
}

public class ExportSettings
{
    public SettingItem NetworkProtocolDirectory { get; set; } = new();
    public SettingItem NetworkProtocolServerDirectory { get; set; } = new();
    public SettingItem NetworkProtocolClientDirectory { get; set; } = new();
}

public class SettingItem
{
    public string Value { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}
