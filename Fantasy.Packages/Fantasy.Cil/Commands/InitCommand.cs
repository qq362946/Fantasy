using System.CommandLine;
using System.CommandLine.Invocation;
using Fantasy.Cli.Generators;
using Fantasy.Cli.Interactive;
using Fantasy.Cli.Language;
using Fantasy.Cli.Models;
using Spectre.Console;

namespace Fantasy.Cli.Commands;

public class InitCommand : Command
{
    public InitCommand() : base("init", LocalizationManager.Current.InitCommandDescription)
    {
        var nameOption = new Option<string?>(
            aliases: ["-n", "--name"],
            description: LocalizationManager.Current.InitProjectNameOption
        );

        var interactiveOption = new Option<bool>(
            aliases: ["-i", "--interactive"],
            getDefaultValue: () => true,
            description: LocalizationManager.Current.InitInteractiveModeOption
        );
        
        AddOption(nameOption);
        AddOption(interactiveOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var loc = LocalizationManager.Current;
            var name = context.ParseResult.GetValueForOption(nameOption);
            var interactive = context.ParseResult.GetValueForOption(interactiveOption);
            var ct = context.GetCancellationToken();

            ProjectConfig config;

            if (interactive || string.IsNullOrEmpty(name))
            {
                config = await new ProjectWizard().RunAsync(ct);
            }
            else
            {
                config = CreateConfigFromArgs(name);
            }

            // 验证输出目录
            if (Directory.Exists(config.OutputDirectory) && Directory.GetFileSystemEntries(config.OutputDirectory).Length > 0)
            {
                AnsiConsole.MarkupLine(loc.DirectoryExistsAndNotEmpty(config.Name));
                context.ExitCode = 1;
                return;
            }

            try
            {
                await new ProjectGenerator().GenerateAsync(config, ct);
                context.ExitCode = 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"{loc.Error} {ex.Message}");
                if (AnsiConsole.Confirm(loc.ShowStackTrace, false))
                {
                    AnsiConsole.WriteException(ex);
                }
                context.ExitCode = 1;
            }
        });
    }
    
    private ProjectConfig CreateConfigFromArgs(string name)
    {
        var config = new ProjectConfig
        {
            Name = name,
            TargetFramework = TargetFrameworkVersion.Net8,
            OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), name)
        };

        return config;
    }
}