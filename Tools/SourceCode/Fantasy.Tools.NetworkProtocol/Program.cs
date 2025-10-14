using System.Text;
using CommandLine;
using Fantasy.Exporter;
using Fantasy.Tools;
using Fantasy.Tools.ProtocalExporter;
// 解析命令行参数
Parser.Default.ParseArguments<ExporterAges>(Environment.GetCommandLineArgs())
    .WithNotParsed(error => throw new Exception("Command line format error!"))
    .WithParsed(ages => ExporterAges.Instance = ages);
try
{
    // 初始化配置
    ExporterSettingsHelper.Initialize();
    // 加载配置
    Console.OutputEncoding = Encoding.UTF8;
    // 运行导出协议的代码
    new ProtocolExporter().Run();
}
catch (Fantasy.Tools.ProtocalExporter.ProtocolFormatException)
{
    // 协议格式错误已经打印过详细信息，这里不再重复打印
}
catch (Exception e)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"程序执行出错: {e.Message}");
    Console.ResetColor();
}
finally
{
    Log.Info("按任意键退出程序");
    Console.ReadKey();
    Environment.Exit(0);
}
