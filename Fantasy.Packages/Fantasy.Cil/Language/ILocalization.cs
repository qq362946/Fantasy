namespace Fantasy.Cli.Language;

public enum Language
{
    English,        // 英文
    Chinese         // 中文
}

public interface ILocalization
{
    // Common
    string Error { get; }
    string Warning { get; }
    string Success { get; }
    string SelectLanguage { get; }

    // Program
    string RootCommandTitle { get; }
    string RootCommandDescription { get; }
    string LanguageTip { get; }
    string PathWarningMessage { get; }

    // InitCommand
    string InitCommandDescription { get; }
    string InitProjectNameOption { get; }
    string InitInteractiveModeOption { get; }
    string DirectoryExistsAndNotEmpty(string name);

    // ProjectWizard
    string ProjectNamePrompt { get; }
    string ProjectNameDefault { get; }
    string TargetFrameworkPrompt { get; }
    string AddNLogPrompt { get; }
    string ProceedWithConfiguration { get; }
    string Cancelled { get; }
    string ConfigurationSummary { get; }
    string SettingColumn { get; }
    string ValueColumn { get; }
    string ProjectNameLabel { get; }
    string TargetFrameworkLabel { get; }
    string AddNLogLabel { get; }
    string OutputDirectoryLabel { get; }

    // ProjectGenerator
    string CreatingDirectoryStructure { get; }
    string GeneratingSolutionFile { get; }
    string GeneratingProjectFiles { get; }
    string GeneratingSourceFiles { get; }
    string RestoringPackages { get; }
    string ProjectCreatedSuccessfully { get; }
    string NextStepCd(string name);
    string NextStepBuild { get; }
    string NextStepRun { get; }

    // AddCommand
    string AddCommandDescription { get; }
    string DirectoryNotExists(string path);
    string SelectWhatToAdd { get; }
    string Extracting { get; }
    string ComponentsAddedSuccessfully { get; }
    string ProtocolExportToolLocation(string path);
    string FantasyNetLocation(string path);
    string FantasyUnityLocation(string path);
    string ShowStackTrace { get; }

    // Tool Descriptions
    string FantasyNetDescription { get; }
    string FantasyUnityDescription { get; }
    string ProtocolExportToolDescription { get; }
    string NetworkProtocolDescription { get; }
    string NLogDescription { get; }
    string AllToolsDescription { get; }

    // ToolExtractor
    string ExtractingProtocolExportTool { get; }
    string ExtractingFantasyNet { get; }
    string ExtractingFantasyUnity { get; }
    string ExtractingNetworkProtocol { get; }
    string ExtractingNLog { get; }
    string ProtocolExportToolOverwriteConfirm(string path);
    string FantasyNetOverwriteConfirm(string path);
    string FantasyUnityOverwriteConfirm(string path);
    string NetworkProtocolOverwriteConfirm(string path);
    string NLogOverwriteConfirm(string path);
    string SkippedProtocolExportTool { get; }
    string SkippedFantasyNet { get; }
    string SkippedFantasyUnity { get; }
    string SkippedNetworkProtocol { get; }
    string SkippedNLog { get; }
    string ProtocolExportToolExtracted(string path);
    string FantasyNetExtracted(string path);
    string FantasyUnityExtracted(string path);
    string NetworkProtocolExtracted(string path);
    string NLogExtracted(string path);
    string NetworkProtocolLocation(string path);
    string NLogLocation(string path);
    string MakeExecutableWarning(string filePath, string message);
}