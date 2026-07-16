using System.ComponentModel.DataAnnotations;

namespace Fantasy.ControlCenter.Options;

public sealed class ControlCenterOptions
{
    public const string SectionName = "ControlCenter";

    [Required]
    public string DataDirectory { get; set; } = "data";

    [Range(5, 300)]
    public int DefaultLeaseSeconds { get; set; } = 15;
}
