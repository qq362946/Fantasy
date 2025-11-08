using System.Reflection;
using Fantasy.Cli.Models;
using Scriban;

namespace Fantasy.Cli.Generators;

public sealed class ProjectEngine
{
    private readonly Assembly _assembly;
    
    public ProjectEngine()
    {
        _assembly = Assembly.GetExecutingAssembly();
    }

    public string RenderTemplate(string templateContent, ProjectConfig config)
    {
        var template = Template.Parse(templateContent);
        var result = template.Render(new
        {
            project = config,
            name = config.Name,
            fantasy_version = config.FantasyVersion,
            target_framework = config.GetTargetFrameworkString(),
            is_add_nlog = config.IsAddNLog,
        });
        return result;
    }
}