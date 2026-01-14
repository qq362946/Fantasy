namespace Fantasy.ProtocolExportTool.Models;

public enum ProtocolExportType
{
    Client = 1,
    Server = 1 << 1,
    All = Client | Server,
}

public class ProtocolExportConfig
{
    public string ProtocolDir { get; set; } = string.Empty;
    public string ServerDir { get; set; } = string.Empty;
    public string ClientDir { get; set; } = string.Empty;
    public ProtocolExportType ExportType { get; set; } = ProtocolExportType.All;
}