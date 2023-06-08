using Fantasy.Model;

try
{
    // 配置框架导出的路径、如果不配置会使用框架默认的路径
    ExporterHelper.Initialize();
    // 初始化框架
    Application.Initialize(AssemblyName.Core);
    // 加载需要的程序集、这里因为每个人都框架规划都不一样、所以这块开放出自己定义
    AssemblyLoadHelper.Initialize();
    // 绑定框架需要的配置文件
    AssemblyLoadHelper.BindConfig();
    // 启动框架
    Application.Start().Coroutine();
    // 框架启动后需要执行的逻辑、现在是我是写的启动服务器的逻辑、同上这里也开放出来自定义
    // 但这里一定是异步的、不然框架部分功能可能不会正常、因为阻塞到这里、需要Update需要下面的才可以驱动
    Entry.Start().Coroutine();
    for (;;)
    {
        Thread.Sleep(1);
        ThreadSynchronizationContext.Main.Update();
        SingletonSystem.Update();
    }
}
catch (Exception e)
{
    Log.Error(e);
}





