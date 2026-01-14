namespace Fantasy.Cli.Language;

public class LocalizationChinese : ILocalization
{
    // Common
    public string Error => "[red]错误:[/]";
    public string Warning => "[yellow]警告:[/]";
    public string Success => "[green]✓[/]";
    public string SelectLanguage => "请选择语言 / Select Language:";

    // Program
    public string RootCommandTitle => "Fantasy 框架命令行工具";
    public string RootCommandDescription => """
        Fantasy 框架 CLI - 项目脚手架和管理工具

        示例：
          fantasy init                          # 创建新项目（交互式）
          fantasy init -n MyGame                # 使用项目名快速开始

          fantasy add                              # 添加工具到现有项目（交互式）
          fantasy add -t protocolexporttool        # 添加协议导出工具
          fantasy add -t networkprotocol           # 添加网络协议
          fantasy add -t nlog                      # 添加 NLog
          fantasy add -t fantasynet                # 添加 Fantasy.Net
          fantasy add -t fantasyunity              # 添加 Fantasy.Unity
          fantasy add -p /path/to/project          # 添加工具到指定项目
        """;
    public string LanguageTip => "[dim]提示: 设置环境变量 FANTASY_CLI_LANG={0} 可跳过此步骤。[/]";
    public string PathWarningMessage => "请将以下内容添加到你的 shell 配置文件中以使用 Fantasy CLI:";

    // InitCommand
    public string InitCommandDescription => "初始化一个新的 Fantasy 项目";
    public string InitProjectNameOption => "项目名称";
    public string InitInteractiveModeOption => "以交互模式运行";

    public string DirectoryExistsAndNotEmpty(string name) =>
        $"[red]错误:[/] 目录 '{name}' 已存在且不为空。";

    // ProjectWizard
    public string ProjectNamePrompt => "[cyan]项目名称:[/]";
    public string ProjectNameDefault => "MyGameServer";
    public string TargetFrameworkPrompt => "[cyan]目标框架:[/]";
    public string AddNLogPrompt => "[cyan]添加 NLog:[/]";
    public string ProceedWithConfiguration => "\n[yellow]是否继续使用此配置？[/]";
    public string Cancelled => "[red]已取消。[/]";
    public string ConfigurationSummary => "[yellow]配置摘要[/]";
    public string SettingColumn => "[bold]设置项[/]";
    public string ValueColumn => "[bold]值[/]";
    public string ProjectNameLabel => "项目名称:";
    public string TargetFrameworkLabel => "目标框架:";
    public string AddNLogLabel => "添加 NLog:";
    public string OutputDirectoryLabel => "输出目录";

    // ProjectGenerator
    public string CreatingDirectoryStructure => "[green]创建目录结构[/]";
    public string GeneratingSolutionFile => "[green]生成解决方案文件[/]";
    public string GeneratingProjectFiles => "[green]生成项目文件[/]";
    public string GeneratingSourceFiles => "[green]生成源代码文件[/]";
    public string RestoringPackages => "[green]还原包[/]";
    public string ProjectCreatedSuccessfully => "\n[green]✓[/] 项目创建成功！";
    public string NextStepCd(string name) => $"[blue]→[/] cd {name}";
    public string NextStepBuild => "[blue]→[/] dotnet build Server/Server.sln";
    public string NextStepRun => "[blue]→[/] dotnet run --project Server/Main/Main.csproj";

    // AddCommand
    public string AddCommandDescription => "添加工具到现有的 Fantasy 项目";

    public string DirectoryNotExists(string path) =>
        $"[red]错误:[/] 目录 '{path}' 不存在。";

    public string SelectWhatToAdd => "[cyan]选择要添加的内容:[/]";
    public string Extracting => "正在提取...";
    public string ComponentsAddedSuccessfully => "\n[green]✓[/] 组件添加成功！";

    public string ProtocolExportToolLocation(string path) =>
        $"[blue]→[/] ProtocolExportTool 位置: {path}";

    public string FantasyNetLocation(string path) =>
        $"[blue]→[/] Fantasy.Net 位置: {path}";

    public string FantasyUnityLocation(string path) =>
        $"[blue]→[/] Fantasy.Unity 位置: {path}";

    public string ShowStackTrace => "显示堆栈跟踪？";

    // Tool Descriptions
    public string FantasyNetDescription => "核心框架库 (包含运行时和源代码生成器)";
    public string FantasyUnityDescription => "Unity 客户端框架 (Unity 项目专用)";
    public string ProtocolExportToolDescription => "协议导出工具 (根据协议定义文件导出代码)";
    public string NetworkProtocolDescription => "网络协议文件 (用于定义和管理网络协议)";
    public string NLogDescription => "NLog 日志组件 (需要先安装 NLog 包才能使用)";
    public string AllToolsDescription => "安装所有工具和组件";

    // ToolExtractor
    public string ExtractingProtocolExportTool => "正在提取 ProtocolExportTool 工具...";
    public string ExtractingFantasyNet => "正在提取 Fantasy.Net 框架...";
    public string ExtractingFantasyUnity => "正在提取 Fantasy.Unity 框架...";
    public string ExtractingNetworkProtocol => "正在提取 NetworkProtocol 工具...";
    public string ExtractingNLog => "正在提取 NLog 组件...";

    public string ProtocolExportToolOverwriteConfirm(string path) =>
        $"[yellow]ProtocolExportTool 工具已存在于 {path}。是否覆盖？[/]";

    public string FantasyNetOverwriteConfirm(string path) =>
        $"[yellow]Fantasy.Net 已存在于 {path}。是否覆盖？[/]";

    public string FantasyUnityOverwriteConfirm(string path) =>
        $"[yellow]Fantasy.Unity 已存在于 {path}。是否覆盖？[/]";

    public string NetworkProtocolOverwriteConfirm(string path) =>
        $"[yellow]NetworkProtocol 工具已存在于 {path}。是否覆盖？[/]";

    public string NLogOverwriteConfirm(string path) =>
        $"[yellow]NLog 组件已存在于 {path}。是否覆盖？[/]";

    public string SkippedProtocolExportTool => "[yellow]已跳过 ProtocolExportTool 工具提取。[/]";
    public string SkippedFantasyNet => "[yellow]已跳过 Fantasy.Net 提取。[/]";
    public string SkippedFantasyUnity => "[yellow]已跳过 Fantasy.Unity 提取。[/]";
    public string SkippedNetworkProtocol => "[yellow]已跳过 NetworkProtocol 工具提取。[/]";
    public string SkippedNLog => "[yellow]已跳过 NLog 组件提取。[/]";

    public string ProtocolExportToolExtracted(string path) =>
        $"[green]✓[/] ProtocolExportTool 工具已提取到 {path}";

    public string FantasyNetExtracted(string path) =>
        $"[green]✓[/] Fantasy.Net 框架已提取到 {path}";

    public string FantasyUnityExtracted(string path) =>
        $"[green]✓[/] Fantasy.Unity 框架已提取到 {path}";

    public string NetworkProtocolExtracted(string path) =>
        $"[green]✓[/] NetworkProtocol 工具已提取到 {path}";

    public string NLogExtracted(string path) =>
        $"[green]✓[/] NLog 组件已提取到 {path}";

    public string NetworkProtocolLocation(string path) =>
        $"[blue]→[/] NetworkProtocol 位置: {path}";

    public string NLogLocation(string path) =>
        $"[blue]→[/] NLog 位置: {path}";

    public string MakeExecutableWarning(string filePath, string message) =>
        $"[yellow]警告: 无法将 {filePath} 设置为可执行: {message}[/]";
}