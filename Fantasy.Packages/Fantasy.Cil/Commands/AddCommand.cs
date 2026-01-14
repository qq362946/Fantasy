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

                // Pre-check for overwrite confirmations BEFORE starting Status display
                // This prevents "Trying to run one or more interactive functions concurrently" error
                var skipTools = new HashSet<string>();

                if (tool == "protocolexporttool" || tool == "all")
                {
                    var toolDir = Path.Combine(path, "Tools", "ProtocolExportTool");
                    if (Directory.Exists(toolDir) && Directory.GetFileSystemEntries(toolDir).Length > 0)
                    {
                        if (!AnsiConsole.Confirm(loc.ProtocolExportToolOverwriteConfirm(toolDir), false))
                        {
                            AnsiConsole.MarkupLine(loc.SkippedProtocolExportTool);
                            skipTools.Add("protocolexporttool");
                        }
                    }
                }

                if (tool == "networkprotocol" || tool == "all")
                {
                    var toolDir = Path.Combine(path, "Tools", "NetworkProtocol");
                    if (Directory.Exists(toolDir) && Directory.GetFileSystemEntries(toolDir).Length > 0)
                    {
                        if (!AnsiConsole.Confirm(loc.NetworkProtocolOverwriteConfirm(toolDir), false))
                        {
                            AnsiConsole.MarkupLine(loc.SkippedNetworkProtocol);
                            skipTools.Add("networkprotocol");
                        }
                    }
                }

                if (tool == "nlog" || tool == "all")
                {
                    var nlogDir = Path.Combine(path, "Tools", "NLog");
                    if (Directory.Exists(nlogDir) && Directory.GetFileSystemEntries(nlogDir).Length > 0)
                    {
                        if (!AnsiConsole.Confirm(loc.NLogOverwriteConfirm(nlogDir), false))
                        {
                            AnsiConsole.MarkupLine(loc.SkippedNLog);
                            skipTools.Add("nlog");
                        }
                    }
                }

                if (tool == "fantasynet" || tool == "all")
                {
                    var fantasyNetDir = Path.Combine(path, "Tools", "Fantasy.Net");
                    if (Directory.Exists(fantasyNetDir) && Directory.GetFileSystemEntries(fantasyNetDir).Length > 0)
                    {
                        if (!AnsiConsole.Confirm(loc.FantasyNetOverwriteConfirm(fantasyNetDir), false))
                        {
                            AnsiConsole.MarkupLine(loc.SkippedFantasyNet);
                            skipTools.Add("fantasynet");
                        }
                    }
                }

                if (tool == "fantasyunity" || tool == "all")
                {
                    var fantasyUnityDir = Path.Combine(path, "Tools", "Fantasy.Unity");
                    if (Directory.Exists(fantasyUnityDir) && Directory.GetFileSystemEntries(fantasyUnityDir).Length > 0)
                    {
                        if (!AnsiConsole.Confirm(loc.FantasyUnityOverwriteConfirm(fantasyUnityDir), false))
                        {
                            AnsiConsole.MarkupLine(loc.SkippedFantasyUnity);
                            skipTools.Add("fantasyunity");
                        }
                    }
                }

                // If all tools were skipped, exit early
                if (tool != "all" && skipTools.Contains(tool))
                {
                    context.ExitCode = 0;
                    return;
                }

                // Now perform extraction with askOverwrite=false (we already asked above)
                await AnsiConsole.Status()
                    .StartAsync(loc.Extracting, async ctx =>
                    {
                        if ((tool == "protocolexporttool" || tool == "all") && !skipTools.Contains("protocolexporttool"))
                        {
                            ctx.Status(loc.ExtractingProtocolExportTool);
                            await extractor.ExtractProtocolExportToolAsync(path, ct, askOverwrite: false);
                        }

                        if ((tool == "networkprotocol" || tool == "all") && !skipTools.Contains("networkprotocol"))
                        {
                            ctx.Status(loc.ExtractingNetworkProtocol);
                            await extractor.ExtractNetworkProtocolAsync(path, ct, askOverwrite: false);
                        }

                        if ((tool == "nlog" || tool == "all") && !skipTools.Contains("nlog"))
                        {
                            ctx.Status(loc.ExtractingNLog);
                            await extractor.ExtractNLogAsync(path, ct, askOverwrite: false);
                        }

                        if ((tool == "fantasynet" || tool == "all") && !skipTools.Contains("fantasynet"))
                        {
                            ctx.Status(loc.ExtractingFantasyNet);
                            await extractor.ExtractFantasyNetAsync(path, ct, askOverwrite: false);
                        }

                        if ((tool == "fantasyunity" || tool == "all") && !skipTools.Contains("fantasyunity"))
                        {
                            ctx.Status(loc.ExtractingFantasyUnity);
                            await extractor.ExtractFantasyUnityAsync(path, ct, askOverwrite: false);
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