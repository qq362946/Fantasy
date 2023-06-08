#if FANTASY_SERVER
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Fantasy.Helper;

namespace Fantasy.Core;

public sealed class Exporter
{
    public static Action ExporterAction;
    public void Start()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var exportType = Define.Options.ExportType;

        if (exportType != ExportType.None)
        {
            return;
        }

        LogInfo("请输入你想要做的操作:");
        LogInfo("1:导出网络协议（ProtoBuf）");
        LogInfo("2:增量导出Excel（包含常量枚举）");
        LogInfo("3:全量导出Excel（包含常量枚举）");

        var keyChar = Console.ReadKey().KeyChar;
            
        if (!int.TryParse(keyChar.ToString(), out var key) || key is < 1 or >= (int) ExportType.Max)
        {
            Console.WriteLine("");
            LogInfo("无法识别的导出类型请，输入正确的操作类型。");
            return;
        }
            
        LogInfo("");
        exportType = (ExportType) key;
        
        switch (exportType)
        {
            case ExportType.ProtoBuf:
            {
                _ = new ProtoBufExporter();
                break;
            }
            case ExportType.AllExcel:
            case ExportType.AllExcelIncrement:
            {
                _ = new ExcelExporter(exportType);
                break;
            }
        }
           
        LogInfo("操作完成,按任意键关闭程序");
        Console.ReadKey();
        Environment.Exit(0);
    }
    
    public static void ClearDirectoryFile(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }
        
        var files = Directory.GetFiles(folderPath);
        
        foreach (var file in files)
        {
            File.Delete(file);
        }
    }

    public static void LogInfo(string msg)
    {
        Console.WriteLine(msg);
    }

    public static void LogError(string msg)
    {
        ConsoleColor color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"{msg}\n{new StackTrace(1, true)}");
        Console.ForegroundColor = color;
    }

    public static void LogError(Exception e)
    {
        ConsoleColor color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(e.Data.Contains("StackTrace") ? $"{e.Data["StackTrace"]}\n{e}" : e.ToString());
        Console.ForegroundColor = color;
    }
}
#endif
