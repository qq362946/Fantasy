#if FANTASY_NET
namespace Fantasy.Core;

/// <summary>
/// 自定义导出接口
/// </summary>
public interface ICustomExport
{
    /// <summary>
    /// 执行导出操作
    /// </summary>
    void Run();
}

/// <summary>
/// 抽象自定义导出基类
/// </summary>
public abstract class ACustomExport : ICustomExport
{
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
                if (!Directory.Exists(Define.ClientCustomExportDirectory))
                {
                    Directory.CreateDirectory(Define.ClientCustomExportDirectory);
                }
                
                File.WriteAllText($"{Define.ClientCustomExportDirectory}/{fileName}", fileContent);
                return;
            }
            case CustomExportType.Server:
            {
                if (!Directory.Exists(Define.ServerCustomExportDirectory))
                {
                    Directory.CreateDirectory(Define.ServerCustomExportDirectory);
                }
                
                File.WriteAllText($"{Define.ServerCustomExportDirectory}/{fileName}", fileContent);
                return;
            }
        }
    }
}
#endif
