using System.Diagnostics;
using Fantasy.Cli.Language;
using Fantasy.Cli.Models;
using Fantasy.Cli.Utilities;
using Spectre.Console;

namespace Fantasy.Cli.Generators;

public class ProjectGenerator
{
    private readonly ProjectEngine _projectEngine;
    private readonly ToolExtractor _toolExtractor;

    public ProjectGenerator()
    {
        _projectEngine = new ProjectEngine();
        _toolExtractor = new ToolExtractor();
    }
    
    public async Task GenerateAsync(ProjectConfig config, CancellationToken ct = default)
    {
        var loc = LocalizationManager.Current;
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            )
            .StartAsync(async ctx =>
            {
                var task1 = ctx.AddTask(loc.CreatingDirectoryStructure);
                await CreateDirectoryStructureAsync(config);
                task1.Increment(100);

                var task2 = ctx.AddTask(loc.GeneratingSolutionFile);
                await GenerateSolutionAsync(config);
                task2.Increment(100);

                var task3 = ctx.AddTask(loc.GeneratingProjectFiles);
                await GenerateProjectsAsync(config);
                task3.Increment(100);

                var task5 = ctx.AddTask(loc.GeneratingSourceFiles);
                await GenerateSourceFilesAsync(config);
                task5.Increment(100);

                if (config.IsAddNLog)
                {
                    var addNLogTask = ctx.AddTask(loc.ExtractingNLog);
                    var programPath = Path.Combine(config.OutputDirectory, "Server", "Main");
                    await _toolExtractor.ExtractNLogByDirAsync(programPath, ct, askOverwrite: false);
                    addNLogTask.Increment(100);
                }

                var task6 = ctx.AddTask(loc.ExtractingProtocolExportTool);
                await _toolExtractor.ExtractProtocolExportToolAsync(config.OutputDirectory, ct, askOverwrite: false);
                task6.Increment(100);

                var task7 = ctx.AddTask(loc.ExtractingNetworkProtocol);
                await _toolExtractor.ExtractNetworkProtocolAsync(config.OutputDirectory, ct, askOverwrite: false);
                task7.Increment(100);

                var task8 = ctx.AddTask(loc.RestoringPackages);
                await RestorePackagesAsync(config);
                task8.Increment(100);
            });

        AnsiConsole.MarkupLine(loc.ProjectCreatedSuccessfully);
        AnsiConsole.MarkupLine(loc.NextStepCd(config.Name));
        AnsiConsole.MarkupLine(loc.NextStepBuild);
        AnsiConsole.MarkupLine(loc.NextStepRun);
    }
    
    private async Task CreateDirectoryStructureAsync(ProjectConfig config)
    {
        var baseDir = config.OutputDirectory;
        Directory.CreateDirectory(baseDir);

        // 创建服务器和客户端目录
        var serverDir = Path.Combine(baseDir, "Server");
        var clientDir = Path.Combine(baseDir, "Client");
        Directory.CreateDirectory(serverDir);
        Directory.CreateDirectory(clientDir);
        // 创建 Unity 客户端占位符
        Directory.CreateDirectory(Path.Combine(clientDir, "Unity"));
        // 基于程序集模式创建服务器项目目录
        Directory.CreateDirectory(Path.Combine(serverDir, "Main"));
        Directory.CreateDirectory(Path.Combine(serverDir, "Entity"));
        Directory.CreateDirectory(Path.Combine(serverDir, "Hotfix"));
        // 创建配置文件夹
        Directory.CreateDirectory(Path.Combine(baseDir, "Config"));
        // 创建工具目录
        Directory.CreateDirectory(Path.Combine(baseDir, "Tools"));
        await Task.CompletedTask;
    }
    
    private async Task GenerateSolutionAsync(ProjectConfig config)
    {
        // 将解决方案文件放置在服务器目录中
        var slnPath = Path.Combine(config.OutputDirectory, "Server", "Server.sln");
        var projects = new List<string>
        {
            "Main/Main.csproj",
            "Entity/Entity.csproj",
            "Hotfix/Hotfix.csproj"
        };
        var slnContent = GenerateSolutionContent("Server", projects);
        await File.WriteAllTextAsync(slnPath, slnContent);
    }
    
    private string GenerateSolutionContent(string name, List<string> projects)
    {
        var projectGuids = new List<string>();
        var slnContent = $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
";
        foreach (var project in projects)
        {
            var projectGuid = Guid.NewGuid().ToString().ToUpper();
            projectGuids.Add(projectGuid);
            var projectName = Path.GetFileNameWithoutExtension(project);
            slnContent += $@"Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}"", ""{project}"", ""{{{projectGuid}}}""
EndProject
";
        }
        
        slnContent += @"Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
";
        foreach (var projectGuid in projectGuids)
        {
            slnContent += $@"		{{{projectGuid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{{projectGuid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{{projectGuid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{{projectGuid}}}.Release|Any CPU.Build.0 = Release|Any CPU
";
        }

        slnContent += @"	EndGlobalSection
EndGlobal
";

        return slnContent.Trim();
    }
    
    private async Task GenerateProjectsAsync(ProjectConfig config)
    {
        // Generate Main project
        await GenerateMainProjectAsync(config);
        // Generate Entity project
        await GenerateEntityProjectAsync(config);
        // Generate Hotfix project
        await GenerateHotfixProjectAsync(config);
    }
    
    private async Task GenerateMainProjectAsync(ProjectConfig config)
    {
        var projectDir = Path.Combine(config.OutputDirectory, "Server", "Main");
        var csprojPath = Path.Combine(projectDir, "Main.csproj");

        var frameworkTag = config.IsMultiFramework ? "TargetFrameworks" : "TargetFramework";

        var nlogPackageRef = config.IsAddNLog
            ? @"  <ItemGroup>
    <PackageReference Include=""NLog"" Version=""*"" />
  </ItemGroup>
  <ItemGroup>
    <None Update=""NLog.config"">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>"
            : "";
        var template = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <{frameworkTag}>{{{{ target_framework }}}}</{frameworkTag}>
    <RootNamespace>Main</RootNamespace>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PublishAot>false</PublishAot>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""..\Entity\Entity.csproj"" />
    <ProjectReference Include=""..\Hotfix\Hotfix.csproj"" />
  </ItemGroup>
{nlogPackageRef}
</Project>";

        var content = _projectEngine.RenderTemplate(template, config);
        await File.WriteAllTextAsync(csprojPath, content);
        
        var launchSettings = $@"{{
  ""$schema"": ""http://json.schemastore.org/launchsettings.json"",
  ""profiles"": {{
    ""Main"": {{
      ""commandName"": ""Project"",
      ""environmentVariables"": {{}},
      ""commandLineArgs"": ""--m Develop""
    }}
  }}
}}";
        var properties = Path.Combine(config.OutputDirectory, "Server", "Main", "Properties");
        var launchSettingsPath = Path.Combine(properties, "launchSettings.json");
        
        if (!Directory.Exists(properties))
        {
            Directory.CreateDirectory(properties);
        }

        if (File.Exists(launchSettingsPath))
        {
            File.Delete(launchSettingsPath);
        }
        
        await File.WriteAllTextAsync(launchSettingsPath, launchSettings);
    }
    
    private async Task GenerateEntityProjectAsync(ProjectConfig config)
    {
        var projectDir = Path.Combine(config.OutputDirectory, "Server", "Entity");
        var csprojPath = Path.Combine(projectDir, "Entity.csproj");

        var frameworkTag = config.IsMultiFramework ? "TargetFrameworks" : "TargetFramework";

        var template = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <{frameworkTag}>{{{{ target_framework }}}}</{frameworkTag}>
    <RootNamespace>Entity</RootNamespace>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Fantasy-Net"" Version=""{{{{ fantasy_version }}}}"" />
  </ItemGroup>

</Project>";

        var content = _projectEngine.RenderTemplate(template, config);
        await File.WriteAllTextAsync(csprojPath, content);
    }
    
    private async Task GenerateHotfixProjectAsync(ProjectConfig config)
    {
        var projectDir = Path.Combine(config.OutputDirectory, "Server", "Hotfix");
        var csprojPath = Path.Combine(projectDir, "Hotfix.csproj");

        var frameworkTag = config.IsMultiFramework ? "TargetFrameworks" : "TargetFramework";

        var entityRef = @"  <ItemGroup>
    <ProjectReference Include=""..\Entity\Entity.csproj"" />
  </ItemGroup>";

        var template = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <{frameworkTag}>{{{{ target_framework }}}}</{frameworkTag}>
    <RootNamespace>Hotfix</RootNamespace>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

{entityRef}

</Project>";

        var content = _projectEngine.RenderTemplate(template, config);
        await File.WriteAllTextAsync(csprojPath, content);
    }
    
    private async Task GenerateSourceFilesAsync(ProjectConfig config)
    {
        // 为主项目生成 Program.cs 文件
        var programCs = GenerateProgramCs(config);
        var programPath = Path.Combine(config.OutputDirectory, "Server", "Main", "Program.cs");
        await File.WriteAllTextAsync(programPath, programCs);
        // 为实体项目生成 AssemblyHelper.cs 文件
        var assemblyHelperCs = GenerateAssemblyHelperCs(config);
        var assemblyHelperPath = Path.Combine(config.OutputDirectory, "Server", "Entity", "AssemblyHelper.cs");
        await File.WriteAllTextAsync(assemblyHelperPath, assemblyHelperCs);
    }
    
    private string GenerateProgramCs(ProjectConfig config)
    {
        var template = @"// ================================================================================
// Fantasy.Net 服务器应用程序入口
// ================================================================================
// 本文件是 Fantasy.Net 分布式游戏服务器的主入口点
//
// 初始化流程：
//   1. 强制加载引用程序集，触发 ModuleInitializer 执行
//   2. 配置日志基础设施{{ if is_add_nlog }}（NLog）{{ else }}（控制台日志）{{ end }}
//   3. 启动 Fantasy.Net 框架
// ================================================================================

using Fantasy;

try
{
    // 初始化引用的程序集，确保 ModuleInitializer 执行
    // .NET 采用延迟加载机制 - 仅当类型被引用时才加载程序集
    // 通过访问 AssemblyMarker 强制加载程序集并调用 ModuleInitializer
    // 注意：Native AOT 不存在延迟加载问题，所有程序集在编译时打包
    AssemblyHelper.Initialize();
{{ if is_add_nlog }}    // 配置 NLog 日志基础设施
    var logger = new Fantasy.NLog(""Server"");
    // 使用 NLog 日志系统启动 Fantasy.Net 框架
    await Fantasy.Platform.Net.Entry.Start(logger);
{{ else }}    // 使用默认控制台日志启动 Fantasy.Net 框架
    await Fantasy.Platform.Net.Entry.Start();
{{ end }}}
catch (Exception ex)
{
    Console.Error.WriteLine($""服务器初始化过程中发生致命错误：{ex}"");
    Environment.Exit(1);
}
";

        return _projectEngine.RenderTemplate(template, config);
    }
    
    private string GenerateAssemblyHelperCs(ProjectConfig config)
    {
        var template = @"using System.Runtime.Loader;
using Fantasy.Helper;

namespace Fantasy
{
    public static class AssemblyHelper
    {
        private const string HotfixDll = ""Hotfix"";
        private static AssemblyLoadContext? _assemblyLoadContext = null;

        public static void Initialize()
        {
            LoadEntityAssembly();
            LoadHotfixAssembly();
        }

        private static void LoadEntityAssembly()
        {
            // .NET 运行时采用延迟加载机制，如果代码中不使用程序集的类型，程序集不会被加载
            // 执行一下，触发运行时强制加载从而自动注册到框架中
            // 因为AssemblyHelper代码在Entity项目里，所以需要获取这个项目的Assembly
            // 然后调用EnsureLoaded方法强制加载一下
            typeof(AssemblyHelper).Assembly.EnsureLoaded();
        }

        public static System.Reflection.Assembly LoadHotfixAssembly()
        {
            if (_assemblyLoadContext != null)
            {
                _assemblyLoadContext.Unload();
                System.GC.Collect();
            }

            _assemblyLoadContext = new AssemblyLoadContext(HotfixDll, true);
            var dllBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, $""{HotfixDll}.dll""));
            var pdbBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, $""{HotfixDll}.pdb""));
            var assembly = _assemblyLoadContext.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
            // 强制触发 ModuleInitializer 执行
            // AssemblyLoadContext.LoadFromStream 只加载程序集到内存，不会自动触发 ModuleInitializer
            // 必须访问程序集中的类型才能触发初始化，这里通过反射调用生成的 AssemblyMarker
            // 注意：此方法仅用于热重载场景（JIT），Native AOT 不支持动态加载
            // 拿到Assembly就用EnsureLoaded()方法强制触发
            assembly.EnsureLoaded();
            return assembly;
        }
    }
}
";

        return _projectEngine.RenderTemplate(template, config);
    }
    
    private async Task RestorePackagesAsync(ProjectConfig config)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "restore",
                    WorkingDirectory = config.OutputDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
        catch
        {
            // Ignore restore errors
        }
    }
}