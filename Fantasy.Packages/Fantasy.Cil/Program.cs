using System.CommandLine;
using Fantasy.Cli.Commands;
using Fantasy.Cli.Language;
using Spectre.Console;

namespace Fantasy.Cli;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        InitializeLocalization();

        var loc = LocalizationManager.Current;
        var rootCommand = new RootCommand(loc.RootCommandTitle)
        {
            new InitCommand(),
            new AddCommand()
        };

        rootCommand.Description = loc.RootCommandDescription;
        return await rootCommand.InvokeAsync(args);
    }

    private static void InitializeLocalization()
    {
        // 检查语言是否已通过环境变量设置
        var envLang = Environment.GetEnvironmentVariable("FANTASY_CLI_LANG");
        if (!string.IsNullOrEmpty(envLang))
        {
            if (Enum.TryParse<Language.Language>(envLang, true, out var language))
            {
                LocalizationManager.Initialize(language);
                return;
            }
        }
        
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("Fantasy CLI")
            .Centered()
            .Color(Color.White));

        // 请用户选择语言
        var selectedLanguage = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("请选择语言 / Select Language:")
                .AddChoices("English", "中文 (Chinese)")
        );
        var lang = selectedLanguage.StartsWith("English") ? Language.Language.English : Language.Language.Chinese;

        LocalizationManager.Initialize(lang);

        // （可选）保存此选择以供将来使用
        AnsiConsole.MarkupLine(string.Format(LocalizationManager.Current.LanguageTip, lang));
        AnsiConsole.WriteLine();
    }
}
