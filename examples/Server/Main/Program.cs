// 初始化框架，添加程序集到框架中
Fantasy.Platform.Net.Entry.Initialize(Fantasy.AssemblyHelper.Assemblies);
// 启动Fantasy.Net
await Fantasy.Platform.Net.Entry.Start();
// 也可以使用下面的Start方法来初始化并且启动Fantasy.Net
// 使用下面这个方法就不用使用上面的两个方法了。
// await Fantasy.Platform.Net.Entry.Start(Fantasy.AssemblyHelper.Assemblies);