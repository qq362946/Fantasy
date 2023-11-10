using System.Collections.Concurrent;
using Fantasy.Exporter;
using OfficeOpenXml;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Exporter.Excel;

/// <summary>
/// 自定义导出接口
/// </summary>
public interface ICustomExport
{
    /// <summary>
    /// 执行导出操作
    /// </summary>
    void Run();
    /// <summary>
    /// 内部操作用于初始化、不明白原理不要修改这里和调用这个方法
    /// </summary>
    /// <param name="excelExporter"></param>
    /// <param name="worksheets"></param>
    void Init(ExcelExporter excelExporter, ConcurrentDictionary<string, ExcelWorksheet> worksheets);
}

/// <summary>
/// 抽象自定义导出基类
/// </summary>
public abstract class ACustomExport : ICustomExport
{
    protected ExcelExporter ExcelExporter;
    protected ConcurrentDictionary<string, ExcelWorksheet> Worksheets;
    
    /// <summary>
    /// 自定义导出类型枚举：客户端、服务器
    /// </summary>
    protected enum CustomExportType
    {
        /// <summary>
        /// 客户端
        /// </summary>
        Client,
        /// <summary>
        /// 服务器
        /// </summary>
        Server
    }

    /// <summary>
    /// 内部操作用于初始化、不明白原理不要修改这里
    /// </summary>
    /// <param name="excelExporter"></param>
    /// <param name="worksheets"></param>
    public void Init(ExcelExporter excelExporter, ConcurrentDictionary<string, ExcelWorksheet> worksheets)
    {
        ExcelExporter = excelExporter;
        Worksheets = worksheets;
    }

    /// <summary>
    /// 执行导出操作的抽象方法
    /// </summary>
    public abstract void Run();

    /// <summary>
    /// 写入文件内容到指定位置
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="fileContent">文件内容</param>
    /// <param name="customExportType">自定义导出类型</param>
    protected void Write(string fileName, string fileContent, CustomExportType customExportType)
    {
        switch (customExportType)
        {
            case CustomExportType.Client:
            {
                if (!Directory.Exists(ExcelExporter.ClientCustomExportDirectory))
                {
                    Directory.CreateDirectory(ExcelExporter.ClientCustomExportDirectory);
                }
                
                File.WriteAllText($"{ExcelExporter.ClientCustomExportDirectory}/{fileName}", fileContent);
                Log.Info($"导出客户端自定义文件：{ExcelExporter.ClientCustomExportDirectory}/{fileName}");
                return;
            }
            case CustomExportType.Server:
            {
                if (!Directory.Exists(ExcelExporter.ServerCustomExportDirectory))
                {
                    Directory.CreateDirectory(ExcelExporter.ServerCustomExportDirectory);
                }
                
                File.WriteAllText($"{ExcelExporter.ServerCustomExportDirectory}/{fileName}", fileContent);
                Log.Info($"导出服务器自定义文件：{ExcelExporter.ServerCustomExportDirectory}/{fileName}");
                return;
            }
        }
    }
}