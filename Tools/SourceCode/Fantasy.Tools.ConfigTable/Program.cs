using System.Text;
using CommandLine;
using Fantasy.Exporter;
using Fantasy.Tools;
using Fantasy.Tools.ConfigTable;

try
{
    Parser.Default.ParseArguments<ExporterAges>(Environment.GetCommandLineArgs())
        .WithNotParsed(error => throw new Exception("Command line format error!"))
        .WithParsed(ages => ExporterAges.Instance = ages);
    // 初始化配置
    ExporterSettingsHelper.Initialize();
    // 加载配置
    Console.OutputEncoding = Encoding.UTF8;
    // 判断启动参数，如果没有选择目标平台就让用户选择
    if (ExporterAges.Instance.ExportPlatform == ExportPlatform.None)
    {
        Log.Info("请输入你想要导出的目标平台:");
        Log.Info("1:Client（客户端）");
        Log.Info("2:Server（服务器）");
        Log.Info("3:All（客户端和服务器）");
        var inputKeyChar = Console.ReadKey().KeyChar;
        if (!int.TryParse(inputKeyChar.ToString(), out var exportPlatformKey) || exportPlatformKey is < 1 or >= (int)ExportPlatform.All)
        {
            Console.WriteLine("");
            Log.Error("无法识别的导出类型请，输入正确导出的目标平台。");
            return;
        }
        ExporterAges.Instance.ExportPlatform = (ExportPlatform)exportPlatformKey;
    }
    
    var selectExportType = ExporterAges.Instance.ExportType;

    if (selectExportType == ExportType.None)
    {
        // 检查启动参数
        Log.Info("请输入你想要做的操作:");
        Log.Info("1:所有增量导出Excel（包含常量枚举）");
        Log.Info("2:所有全量导出Excel（包含常量枚举）");
        // 获取用户输入
        var keyChar = Console.ReadKey().KeyChar;
        // 判断用户输入
        if (!int.TryParse(keyChar.ToString(), out var key) || key is < 1 or >= (int)ExportType.Max)
        {
            Console.WriteLine("");
            Log.Error("无法识别的导出类型请，输入正确的操作类型。");
            return;
        }

        selectExportType = (ExportType)key;
    }
    Log.Info($"selectExportType:{selectExportType} ExportPlatform:{ExporterAges.Instance.ExportPlatform}");
    // 转换用户输入    
    Log.Info("");
    new ExcelExporter(selectExportType).Run();
}
catch (Exception e)
{
    Log.Error(e);
}
finally
{
    Log.Info("按任意键退出程序");
    Console.ReadKey();
    Environment.Exit(0);
}
