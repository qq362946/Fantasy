using System.Text;
using CommandLine;
using Exporter;
using Fantasy.Exporter;

try
{
    // 解析命令行参数
    Parser.Default.ParseArguments<ExporterAges>(Environment.GetCommandLineArgs())
        .WithNotParsed(error => throw new Exception("Command line format error!"))
        .WithParsed(ages => ExporterAges.Instance = ages);
    // 初始化配置
    ExporterSettingsHelper.Initialize();
    // 加载配置
    Console.OutputEncoding = Encoding.UTF8;

    var exportProtocalType = ExporterSettingsHelper.CustomSerializes.Select((kvp, index) => $"{index + 1}: 导出协议为{kvp.Key} ")
        .ToList();

    if (exportProtocalType.Count <= 0)
    {
        Log.Error("ExporterSettings.json 没有检索到序列化辅助器 请手动配置!");
        return;
    }

    // 检查启动参数
    Log.Info("请选择导出的协议类型:");
    Log.Info(string.Join(Environment.NewLine, exportProtocalType));
    // 获取用户输入
    var keyChar = Console.ReadKey().KeyChar;
    // 判断用户输入
    if (!int.TryParse(keyChar.ToString(), out var key) || key < 1 || key > exportProtocalType.Count)
    {
        Console.WriteLine("");
        Log.Error("无法识别的导出类型请，输入正确的操作类型。");
    }


    // 转换用户输入    
    Log.Info("");
    var customeSerialize = ExporterSettingsHelper.CustomSerializes.Select(a => a.Value).ToArray()[key - 1];
    _ = new ProtocolExporter(customeSerialize);
}
catch (Exception e)
{
    Log.Error(e);
    throw;
}
finally
{
    Log.Info("按任意键退出程序");
    Console.ReadKey();
    Environment.Exit(0);
}