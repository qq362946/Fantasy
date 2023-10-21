using Fantasy.Core.Network;

#pragma warning disable CS8618
namespace Fantasy;

/// <summary>
/// 控制台程序定义类型
/// </summary>
public static class AppDefine
{
    /// <summary>
    /// 命令行选项
    /// </summary>
    public static CommandLineOptions Options;
    /// <summary>
    /// App程序Id
    /// </summary>
    public static uint AppId => Options.AppId;
    /// <summary>
    /// 内部网络通讯协议类型
    /// </summary>
    public static NetworkProtocolType InnerNetwork;
}