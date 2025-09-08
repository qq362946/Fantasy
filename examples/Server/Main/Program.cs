using Fantasy.ConfigTable;
// ================================================================================
// Fantasy.Net 框架启动配置
// ================================================================================
// 1. 初始化配置表系统
// 配置表路径：存放所有框架需要的配置数据文件(二进制格式)
// 包含：技能表、道具表、怪物表等业务逻辑配置
ConfigTableHelper.Initialize("../../../Config/Binary");
// 2. 注册日志系统到框架
// 开发者可以自己注册日志系统到框架，只要实现Fantasy.ILog接口即可
// 这里使用的是NLog日志系统，支持文件输出、控制台输出等多种方式
// 也可以不创建日志，只需要传递null，这情况就会使用默认控制台打印日志
var nLog = new Fantasy.NLog("Server");
// 3. 获取需要注册到框架的程序集
// AssemblyHelper会自动扫描并加载所有相关的程序集
// 包含：业务逻辑Assembly、热更新Assembly等
var assemblies = Fantasy.AssemblyHelper.Assemblies;
// 4. 初始化Fantasy.Net框架
// 注册日志系统和程序集到框架中，准备启动环境
await Fantasy.Platform.Net.Entry.Initialize(nLog, assemblies);
// 5. 启动Fantasy.Net框架
// 开始监听网络连接、启动各种服务组件
await Fantasy.Platform.Net.Entry.Start();
