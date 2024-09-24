using Fantasy;
using Fantasy.Helper;
using Fantasy.Platform.Net;
// 获取配置文件
// 比如通过远程获取这个配置文件，这样可以多组服务器共享一套配置了
var machineConfigText = await FileHelper.GetTextByRelativePath("../../../Config/Json/Server/MachineConfigData.Json");
var processConfigText = await FileHelper.GetTextByRelativePath("../../../Config/Json/Server/ProcessConfigData.Json");
var worldConfigText = await FileHelper.GetTextByRelativePath("../../../Config/Json/Server/WorldConfigData.Json");
var sceneConfigText = await FileHelper.GetTextByRelativePath("../../../Config/Json/Server/SceneConfigData.Json");
// 初始化配置文件
// 如果重复初始化方法会覆盖掉上一次的数据，非常适合热重载时使用
MachineConfigData.Initialize(machineConfigText);
ProcessConfigData.Initialize(processConfigText);
WorldConfigData.Initialize(worldConfigText);
SceneConfigData.Initialize(sceneConfigText);
// 注册日志模块到框架
// 开发者可以自己注册日志系统到框架，只要实现Fantasy.ILog接口就可以。
// 这里用的是NLog日志系统注册到框架中。
Fantasy.Log.Register(new Fantasy.NLog("Server"));
// 初始化框架，添加程序集到框架中
Fantasy.Platform.Net.Entry.Initialize(Fantasy.AssemblyHelper.Assemblies);
// 启动Fantasy.Net
await Fantasy.Platform.Net.Entry.Start();
// 也可以使用下面的Start方法来初始化并且启动Fantasy.Net
// 使用下面这个方法就不用使用上面的两个方法了。
// await Fantasy.Platform.Net.Entry.Start(Fantasy.AssemblyHelper.Assemblies);