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

        var pathOption = new Option<string?>(
            aliases: ["-p", "--path"],
            description: "Path to the directory where the project will be created"
        );

        var interactiveOption = new Option<bool>(
            aliases: ["-i", "--interactive"],
            getDefaultValue: () => true,
            description: LocalizationManager.Current.InitInteractiveModeOption
        );

        AddOption(nameOption);
        AddOption(pathOption);
        AddOption(interactiveOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var loc = LocalizationManager.Current;
            var name = context.ParseResult.GetValueForOption(nameOption);
            var path = context.ParseResult.GetValueForOption(pathOption);
            var interactive = context.ParseResult.GetValueForOption(interactiveOption);
            var ct = context.GetCancellationToken();

            ProjectConfig config;

            if (interactive || string.IsNullOrEmpty(name))
            {
                config = await new ProjectWizard().RunAsync(ct);

                // Override the wizard's output directory only if -p is specified
                if (!string.IsNullOrEmpty(path))
                {
                    // If -p is specified, use: specified_path/project_name
                    var basePath = Path.IsPathRooted(path)
                        ? path
                        : Path.Combine(Directory.GetCurrentDirectory(), path);
                    config.OutputDirectory = Path.Combine(basePath, config.Name);
                }
            }
            else
            {
                config = CreateConfigFromArgs(name, path);
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
    
    private ProjectConfig CreateConfigFromArgs(string name, string? path = null)
    {
        string basePath;

        if (!string.IsNullOrEmpty(path))
        {
            basePath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(Directory.GetCurrentDirectory(), path);
        }
        else
        {
            basePath = Directory.GetCurrentDirectory();
        }
        
        var outputDirectory = Path.Combine(basePath, name);

        var config = new ProjectConfig
        {
            Name = name,
            TargetFramework = TargetFrameworkVersion.Net8,
            OutputDirectory = outputDirectory
        };

        return config;
    }
}