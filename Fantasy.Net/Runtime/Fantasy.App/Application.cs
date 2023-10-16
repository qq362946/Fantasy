using CommandLine;
using Fantasy.Core;
using Fantasy.Helper;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Fantasy
{
    /// <summary>
    /// Fantasy 应用程序入口类型
    /// </summary>
    public static class Application
    {
        /// <summary>
        /// 执行 Fantasy 应用程序的初始化操作。
        /// </summary>
        /// <exception cref="Exception">当命令行格式异常时抛出。</exception>
        /// <exception cref="NotSupportedException">不支持的 AppType 类型异常。</exception>
        public static void Initialize()
        {
            // 设置默认的线程的同步上下文
            SynchronizationContext.SetSynchronizationContext(ThreadSynchronizationContext.Main);
            // 解析命令行参数
            Parser.Default.ParseArguments<CommandLineOptions>(Environment.GetCommandLineArgs())
                .WithNotParsed(error => throw new Exception("Command line format error!"))
                .WithParsed(option => AppDefine.Options = option);
            // 加载配置
            FantasySettingsHelper.Initialize();
            // 检查启动参数
            switch (AppDefine.Options.AppType)
            {
                case "Game":
                {
                    break;
                }
                default:
                {
                    throw new NotSupportedException($"AppType is {AppDefine.Options.AppType} Unrecognized!");
                }
            }
            // 根据不同的运行模式来选择日志的方式
            switch (AppDefine.Options.Mode)
            {
                case "Develop":
                {
                    LogManager.Configuration.RemoveRuleByName("ServerDebug");
                    LogManager.Configuration.RemoveRuleByName("ServerTrace");
                    LogManager.Configuration.RemoveRuleByName("ServerInfo");
                    LogManager.Configuration.RemoveRuleByName("ServerWarn");
                    LogManager.Configuration.RemoveRuleByName("ServerError");
                    break;
                }
                case "Release":
                {
                    LogManager.Configuration.RemoveRuleByName("ConsoleTrace");
                    LogManager.Configuration.RemoveRuleByName("ConsoleDebug");
                    LogManager.Configuration.RemoveRuleByName("ConsoleInfo");
                    LogManager.Configuration.RemoveRuleByName("ConsoleWarn");
                    LogManager.Configuration.RemoveRuleByName("ConsoleError");
                    break;
                }
            }
            // 初始化SingletonSystemCenter这个一定要放到最前面
            // 因为SingletonSystem会注册AssemblyManager的OnLoadAssemblyEvent和OnUnLoadAssemblyEvent的事件
            // 如果不这样、会无法把程序集的单例注册到SingletonManager中
            SingletonSystem.Initialize();
            // 加载核心程序集
            AssemblyManager.Initialize();
        }

        /// <summary>
        /// 启动 Fantasy 应用程序。
        /// 在发布模式下，只会启动一个指定的 Server。您可以创建一个专门的 Server 来管理其他 Server 的启动。
        /// </summary>
        /// <returns><see cref="FTask"/></returns>
        public static async FTask Start()
        {
            switch (AppDefine.Options.Mode)
            {
                case "Develop":
                {
                    // 开发模式默认所有Server都在一个进程中、方便调试、但网络还都是独立的
                    var serverConfigInfos = ConfigTableManage.AllServerConfig();
                    
                    foreach (var serverConfig in serverConfigInfos)
                    {
                        await Server.Create(serverConfig.Id);
                    }
                
                    return;
                }
                case "Release":
                {
                    // 发布模式只会启动启动参数传递的Server、也就是只会启动一个Server
                    // 您可以做一个Server专门用于管理启动所有Server的工作
                    await Server.Create(AppDefine.Options.AppId);
                    return;
                }
            }
        }

        /// <summary>
        /// 关闭 Fantasy 应用程序，释放 SingletonSystem 中的实例和已加载的程序集。
        /// </summary>
        public static void Close()
        {
            SingletonSystem.Dispose();
            AssemblyManager.Dispose();
        }
    }
}