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
