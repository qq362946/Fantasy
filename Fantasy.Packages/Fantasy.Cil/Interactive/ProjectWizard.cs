using Fantasy.Cli.Language;
using Fantasy.Cli.Models;
using Spectre.Console;

namespace Fantasy.Cli.Interactive;

public sealed class ProjectWizard
{
    public async Task<ProjectConfig> RunAsync(CancellationToken ct = default)
    {
        var loc = LocalizationManager.Current;
        var config = new ProjectConfig()
        {
            // 1. Project name
            Name = AnsiConsole.Ask<string>(loc.ProjectNamePrompt, loc.ProjectNameDefault),
            // 2. Target framework version
            TargetFramework = AnsiConsole.Prompt(
                new SelectionPrompt<TargetFrameworkVersion>()
                    .Title(loc.TargetFrameworkPrompt)
                    .AddChoices(TargetFrameworkVersion.Net8, TargetFrameworkVersion.Net9, TargetFrameworkVersion.Multi)
                    .UseConverter(GetFrameworkText)
            ),
            // 3.Add NLog
            IsAddNLog = AnsiConsole.Confirm(loc.AddNLogPrompt)
        };
            
        // 设置输出目录
        config.OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), config.Name);
        // 预览配置
        ShowConfigPreview(config);

        if (AnsiConsole.Confirm(loc.ProceedWithConfiguration, true))
        {
            return config;
        }

        AnsiConsole.MarkupLine(loc.Cancelled);
        return await RunAsync(ct);
    }
    
    public static string GetFrameworkText(TargetFrameworkVersion framework)
    {
        return framework switch
        {
            Models.TargetFrameworkVersion.Net8 => ".NET 8.0 (net8.0)",
            Models.TargetFrameworkVersion.Net9 => ".NET 9.0 (net9.0)",
            Models.TargetFrameworkVersion.Multi => "Multi-target (net8.0;net9.0)",
            _ => framework.ToString()
        };
    }
    
    private void ShowConfigPreview(ProjectConfig config)
    {
        var loc = LocalizationManager.Current;
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn(loc.SettingColumn).Centered())
            .AddColumn(new TableColumn(loc.ValueColumn).Centered());

        table.AddRow(loc.ProjectNameLabel, $"[green]{config.Name}[/]");
        table.AddRow(loc.TargetFrameworkLabel, $"[green]{config.GetTargetFrameworkString()}[/]");
        table.AddRow(loc.AddNLogLabel, $"[green]{config.IsAddNLog}[/]");
        table.AddRow(loc.OutputDirectoryLabel, $"[blue]{config.OutputDirectory}[/]");

        AnsiConsole.Write(new Panel(table)
            .Header(loc.ConfigurationSummary)
            .BorderColor(Color.Yellow));
    }
}