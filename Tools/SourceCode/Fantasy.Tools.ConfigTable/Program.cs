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
    // 检查启动参数
    Log.Info("请输入你想要做的操作:");
    Log.Info("1:增量导出Excel（包含常量枚举）");
    Log.Info("2:全量导出Excel（包含常量枚举）");
    // 获取用户输入
    var keyChar = Console.ReadKey().KeyChar;
    // 判断用户输入
    if (!int.TryParse(keyChar.ToString(), out var key) || key is < 1 or >= (int)ExportType.Max)
    {
        Console.WriteLine("");
        Log.Error("无法识别的导出类型请，输入正确的操作类型。");
        return;
    }
    // 转换用户输入    
    Log.Info("");
    new ExcelExporter((ExportType)key).Run();
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
