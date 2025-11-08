namespace Fantasy.Cli.Language;

public class LocalizationEnglish : ILocalization
{
    // Common
    public string Error => "[red]Error:[/]";
    public string Warning => "[yellow]Warning:[/]";
    public string Success => "[green]✓[/]";
    public string SelectLanguage => "请选择语言 / Select Language:";

    // Program
    public string RootCommandTitle => "Fantasy Framework CLI Tool";
    public string RootCommandDescription => """
        Fantasy Framework CLI - Project scaffolding and management tool

        Examples:
          fantasy init                          # Create new project (interactive)
          fantasy init -n MyGame                # Quick start with name

          fantasy add                              # Add tools to existing project (interactive)
          fantasy add -t protocolexporttool        # Add ProtocolExportTool
          fantasy add -t networkprotocol           # Add NetworkProtocol
          fantasy add -t nlog                      # Add NLog
          fantasy add -t fantasynet                # Add Fantasy.Net
          fantasy add -t fantasyunity              # Add Fantasy.Unity
          fantasy add -p /path/to/project          # Add tools to specific project
        """;
    public string LanguageTip => "[dim]Tip: Set environment variable FANTASY_CLI_LANG={0} to skip this step next time.[/]";

    // InitCommand
    public string InitCommandDescription => "Initialize a new Fantasy project";
    public string InitProjectNameOption => "Project name";
    public string InitInteractiveModeOption => "Run in interactive mode";

    public string DirectoryExistsAndNotEmpty(string name) =>
        $"[red]Error:[/] Directory '{name}' already exists and is not empty.";

    // ProjectWizard
    public string ProjectNamePrompt => "[cyan]Project name:[/]";
    public string ProjectNameDefault => "MyGameServer";
    public string TargetFrameworkPrompt => "[cyan]Target framework:[/]";
    public string AddNLogPrompt => "[cyan]Add NLog:[/]";
    public string ProceedWithConfiguration => "\n[yellow]Proceed with this configuration?[/]";
    public string Cancelled => "[red]Cancelled.[/]";
    public string ConfigurationSummary => "[yellow]Configuration Summary[/]";
    public string SettingColumn => "[bold]Setting[/]";
    public string ValueColumn => "[bold]Value[/]";
    public string ProjectNameLabel => "Project name:";
    public string TargetFrameworkLabel => "Target framework:";
    public string AddNLogLabel => "Add NLog:";
    public string OutputDirectoryLabel => "Output Directory";

    // ProjectGenerator
    public string CreatingDirectoryStructure => "[green]Creating directory structure[/]";
    public string GeneratingSolutionFile => "[green]Generating solution file[/]";
    public string GeneratingProjectFiles => "[green]Generating project files[/]";
    public string GeneratingSourceFiles => "[green]Generating source files[/]";
    public string RestoringPackages => "[green]Restoring packages[/]";
    public string ProjectCreatedSuccessfully => "\n[green]✓[/] Project created successfully!";
    public string NextStepCd(string name) => $"[blue]→[/] cd {name}";
    public string NextStepBuild => "[blue]→[/] dotnet build Server/Server.sln";
    public string NextStepRun => "[blue]→[/] dotnet run --project Server/Main/Main.csproj";

    // AddCommand
    public string AddCommandDescription => "Add tools to an existing Fantasy project";

    public string DirectoryNotExists(string path) =>
        $"[red]Error:[/] Directory '{path}' does not exist.";

    public string SelectWhatToAdd => "[cyan]Select what to add:[/]";
    public string Extracting => "Extracting...";
    public string ComponentsAddedSuccessfully => "\n[green]✓[/] Components added successfully!";

    public string ProtocolExportToolLocation(string path) =>
        $"[blue]→[/] ProtocolExportTool location: {path}";

    public string FantasyNetLocation(string path) =>
        $"[blue]→[/] Fantasy.Net location: {path}";

    public string FantasyUnityLocation(string path) =>
        $"[blue]→[/] Fantasy.Unity location: {path}";

    public string ShowStackTrace => "Show stack trace?";

    // Tool Descriptions
    public string FantasyNetDescription => "Core framework library (includes runtime and source generators)";
    public string FantasyUnityDescription => "Unity client framework (for Unity projects)";
    public string ProtocolExportToolDescription => "Protocol export tool (generates code from protocol definition files)";
    public string NetworkProtocolDescription => "Network protocol files (for defining and managing network protocols)";
    public string NLogDescription => "NLog component (requires NLog package installation first)";
    public string AllToolsDescription => "Install all tools and components";

    // ToolExtractor
    public string ExtractingProtocolExportTool => "Extracting ProtocolExportTool...";
    public string ExtractingFantasyNet => "Extracting Fantasy.Net framework...";
    public string ExtractingFantasyUnity => "Extracting Fantasy.Unity framework...";
    public string ExtractingNetworkProtocol => "Extracting NetworkProtocol...";
    public string ExtractingNLog => "Extracting NLog component...";

    public string ProtocolExportToolOverwriteConfirm(string path) =>
        $"[yellow]ProtocolExportTool already exists in {path}. Overwrite?[/]";

    public string FantasyNetOverwriteConfirm(string path) =>
        $"[yellow]Fantasy.Net already exists in {path}. Overwrite?[/]";

    public string FantasyUnityOverwriteConfirm(string path) =>
        $"[yellow]Fantasy.Unity already exists in {path}. Overwrite?[/]";

    public string NetworkProtocolOverwriteConfirm(string path) =>
        $"[yellow]NetworkProtocol already exists in {path}. Overwrite?[/]";

    public string NLogOverwriteConfirm(string path) =>
        $"[yellow]NLog component already exists in {path}. Overwrite?[/]";

    public string SkippedProtocolExportTool => "[yellow]Skipped ProtocolExportTool extraction.[/]";
    public string SkippedFantasyNet => "[yellow]Skipped Fantasy.Net extraction.[/]";
    public string SkippedFantasyUnity => "[yellow]Skipped Fantasy.Unity extraction.[/]";
    public string SkippedNetworkProtocol => "[yellow]Skipped NetworkProtocol extraction.[/]";
    public string SkippedNLog => "[yellow]Skipped NLog component extraction.[/]";

    public string ProtocolExportToolExtracted(string path) =>
        $"[green]✓[/] ProtocolExportTool extracted to {path}";

    public string FantasyNetExtracted(string path) =>
        $"[green]✓[/] Fantasy.Net framework extracted to {path}";

    public string FantasyUnityExtracted(string path) =>
        $"[green]✓[/] Fantasy.Unity framework extracted to {path}";

    public string NetworkProtocolExtracted(string path) =>
        $"[green]✓[/] NetworkProtocol extracted to {path}";

    public string NLogExtracted(string path) =>
        $"[green]✓[/] NLog component extracted to {path}";

    public string NetworkProtocolLocation(string path) =>
        $"[blue]→[/] NetworkProtocol location: {path}";

    public string NLogLocation(string path) =>
        $"[blue]→[/] NLog location: {path}";

    public string MakeExecutableWarning(string filePath, string message) =>
        $"[yellow]Warning: Could not make {filePath} executable: {message}[/]";
}