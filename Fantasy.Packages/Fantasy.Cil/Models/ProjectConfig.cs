namespace Fantasy.Cli.Models;

public class ProjectConfig
{
    public string FantasyVersion { get; set; } = "2.0.91";
    public string Name { get; set; } = string.Empty;
    public bool IsAddNLog { get; set; } = false;
    public TargetFrameworkVersion TargetFramework { get; set; } = TargetFrameworkVersion.Net8;
    public string OutputDirectory { get; set; } = string.Empty;
    
    public bool IsMultiFramework => TargetFramework == TargetFrameworkVersion.Multi;
    
    public string GetTargetFrameworkString()
    {
        return TargetFramework switch
        {
            TargetFrameworkVersion.Net8 => "net8.0",
            TargetFrameworkVersion.Net9 => "net9.0",
            TargetFrameworkVersion.Multi => "net8.0;net9.0",
            _ => "net8.0"
        };
    }
}