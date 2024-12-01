#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy.Tools.ProtocalExporter;

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