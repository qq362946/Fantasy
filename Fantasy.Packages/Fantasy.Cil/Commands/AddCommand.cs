using System.CommandLine;
using System.CommandLine.Invocation;
using Fantasy.Cli.Language;
using Fantasy.Cli.Utilities;
using Spectre.Console;

namespace Fantasy.Cli.Commands;

/// <summary>
/// Command to add tools to an existing Fantasy project
/// </summary>
public class AddCommand : Command
{
    public AddCommand() : base("add", LocalizationManager.Current.AddCommandDescription)
    {
        var pathOption = new Option<string?>(
            aliases: ["-p", "--path"],
            description: "Path to the Fantasy project directory",
            getDefaultValue: Directory.GetCurrentDirectory
        );
        
        var toolOption = new Option<string?>(
            aliases: ["-t", "--tool"],
            description: "Tool to add (protocolExportTool, networkProtocol, nlog, fantasyNet, fantasyUnity, all)"
        );
        
        AddOption(pathOption);
        AddOption(toolOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var loc = LocalizationManager.Current;
            var path = context.ParseResult.GetValueForOption(pathOption);
            var tool = context.ParseResult.GetValueForOption(toolOption);
            var ct = context.GetCancellationToken();

            // 验证项目路径
            if (string.IsNullOrEmpty(path))
            {
                path = Directory.GetCurrentDirectory();
            }

            // 文件夹不存在
            if (!Directory.Exists(path))
            {
                AnsiConsole.MarkupLine(loc.DirectoryNotExists(path));
                context.ExitCode = 1;
                return;
            }

            try
            {
                var extractor = new ToolExtractor();

                // 如果未指定工具，则提示用户
                if (string.IsNullOrEmpty(tool))
                {
                    var choices = new Dictionary<string, string>
                    {
                        { $"Fantasy.Net - {loc.FantasyNetDescription}", "Fantasy.Net" },
                        { $"Fantasy.Unity - {loc.FantasyUnityDescription}", "Fantasy.Unity" },
                        { $"ProtocolExportTool - {loc.ProtocolExportToolDescription}", "ProtocolExportTool" },
                        { $"NetworkProtocol - {loc.NetworkProtocolDescription}", "NetworkProtocol" },
                        { $"NLog - {loc.NLogDescription}", "NLog" },
                        { $"All - {loc.AllToolsDescription}", "All" }
                    };

                    var selected = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title(loc.SelectWhatToAdd)
                            .AddChoices(choices.Keys)
                    );

                    tool = choices[selected];
                }

                tool = tool.ToLower().Replace(".", "").Replace(" ", "");

                await AnsiConsole.Status()
                    .StartAsync(loc.Extracting, async ctx =>
                    {
                        if (tool == "protocolexporttool" || tool == "all")
                        {
                            ctx.Status(loc.ExtractingProtocolExportTool);
                            await extractor.ExtractProtocolExportToolAsync(path, ct);
                        }

                        if (tool == "networkprotocol" || tool == "all")
                        {
                            ctx.Status(loc.ExtractingNetworkProtocol);
                            await extractor.ExtractNetworkProtocolAsync(path, ct);
                        }

                        if (tool == "nlog" || tool == "all")
                        {
                            ctx.Status(loc.ExtractingNLog);
                            await extractor.ExtractNLogAsync(path, ct);
                        }

                        if (tool == "fantasynet" || tool == "all")
                        {
                            ctx.Status(loc.ExtractingFantasyNet);
                            await extractor.ExtractFantasyNetAsync(path, ct);
                        }

                        if (tool == "fantasyunity" || tool == "all")
                        {
                            ctx.Status(loc.ExtractingFantasyUnity);
                            await extractor.ExtractFantasyUnityAsync(path, ct);
                        }
                    });

                AnsiConsole.MarkupLine(loc.ComponentsAddedSuccessfully);

                // 根据安装的内容显示位置信息
                if (tool == "protocolexporttool")
                {
                    AnsiConsole.MarkupLine(loc.ProtocolExportToolLocation(Path.Combine(path, "Tools", "ProtocolExportTool")));
                }
                else if (tool == "networkprotocol")
                {
                    AnsiConsole.MarkupLine(loc.NetworkProtocolLocation(Path.Combine(path, "Tools", "NetworkProtocol")));
                }
                else if (tool == "nlog")
                {
                    AnsiConsole.MarkupLine(loc.NLogLocation(Path.Combine(path, "Tools", "NLog")));
                }
                else if (tool == "fantasynet")
                {
                    AnsiConsole.MarkupLine(loc.FantasyNetLocation(Path.Combine(path, "Tools", "Fantasy.Net")));
                }
                else if (tool == "fantasyunity")
                {
                    AnsiConsole.MarkupLine(loc.FantasyUnityLocation(Path.Combine(path, "Tools", "Fantasy.Unity")));
                }
                else if (tool == "all")
                {
                    AnsiConsole.MarkupLine(loc.ProtocolExportToolLocation(Path.Combine(path, "Tools", "ProtocolExportTool")));
                    AnsiConsole.MarkupLine(loc.NetworkProtocolLocation(Path.Combine(path, "Tools", "NetworkProtocol")));
                    AnsiConsole.MarkupLine(loc.NLogLocation(Path.Combine(path, "Tools", "NLog")));
                    AnsiConsole.MarkupLine(loc.FantasyNetLocation(Path.Combine(path, "Tools", "Fantasy.Net")));
                    AnsiConsole.MarkupLine(loc.FantasyUnityLocation(Path.Combine(path, "Tools", "Fantasy.Unity")));
                }

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
}