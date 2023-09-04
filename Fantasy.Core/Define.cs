#if FANTASY_NET
namespace Fantasy.Core;

/// <summary>
/// 定义包含 Fantasy 系统设置的静态类。
/// </summary>
public static class Define
{
#if FANTASY_NET

    #region ProtoBuf

    /// <summary>
    /// 用于拆分字符串的字符数组。
    /// </summary>
    public static readonly char[] SplitChars = { ' ', '\t' };
    /// <summary>
    /// ProtoBuf 文件夹的路径。
    /// </summary>
    public static string ProtoBufDirectory;
    /// <summary>
    /// 生成服务端代码的文件夹路径。
    /// </summary>
    public static string ProtoBufServerDirectory;
    /// <summary>
    /// 生成客户端代码的文件夹路径。
    /// </summary>
    public static string ProtoBufClientDirectory;
    /// <summary>
    /// ProtoBuf 代码模板的路径。
    /// </summary>
    public static string ProtoBufTemplatePath;

    #endregion

    #region Excel

    /// <summary>
    /// Excel 配置文件的根目录。
    /// </summary>
    public static string ExcelProgramPath;
    /// <summary>
    /// 版本文件的 Excel 路径。
    /// </summary>
    public static string ExcelVersionFile;
    /// <summary>
    /// 服务器代码生成文件夹
    /// </summary>
    public static string ExcelServerFileDirectory;
    /// <summary>
    /// 客户端代码生成文件夹
    /// </summary>
    public static string ExcelClientFileDirectory;
    /// <summary>
    /// 服务器二进制数据文件夹
    /// </summary>
    public static string ExcelServerBinaryDirectory;
    /// <summary>
    /// 客户端二进制数据文件夹
    /// </summary>
    public static string ExcelClientBinaryDirectory;
    /// <summary>
    /// 服务器Json数据文件夹
    /// </summary>
    public static string ExcelServerJsonDirectory;
    /// <summary>
    /// 客户端Json数据文件夹
    /// </summary>
    public static string ExcelClientJsonDirectory;
    /// <summary>
    /// 服务器自定义导出代码
    /// </summary>
    public static string ServerCustomExportDirectory;
    /// <summary>
    /// 客户端自定义导出代码
    /// </summary>
    public static string ClientCustomExportDirectory;
    /// <summary>
    /// SceneConfig.xlsx的位置
    /// </summary>
    public static string SceneConfigPath;
    /// <summary>
    /// 自定义导出代码存放的程序集
    /// </summary>
    public static string CustomExportAssembly;
    /// <summary>
    /// 导表支持的数据类型集合。
    /// </summary>
    public static readonly HashSet<string> ColTypeSet = new HashSet<string>()
    {
        "", "0", "bool", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "string",
        "IntDictionaryConfig", "StringDictionaryConfig",
        "short[]", "int[]", "long[]", "float[]", "string[]"
    };
    /// <summary>
    /// Excel 生成代码模板的位置
    /// </summary>
    public static string ExcelTemplatePath;
    /// <summary>
    /// 获取或设置 Excel 代码模板的内容。
    /// </summary>
    public static string ExcelTemplate
    {
        get
        {
            return _template ??= File.ReadAllText(ExcelTemplatePath);
        }
    }
    private static string _template;

    #endregion

    #region Network
    /// <summary>
    /// 会话空闲检查间隔。
    /// </summary>
    public static int SessionIdleCheckerInterval;
    /// <summary>
    /// 会话空闲检查超时时间。
    /// </summary>
    public static int SessionIdleCheckerTimeout;

    #endregion

#endif
}
#endif