# 快速开始- Quickly Start
本教程引导从空项目开始体验Fantasy。目标平台为Windows\Mac\Linux的情况都是一样的。
</br>需要提前把Fantasy下载到本地，用GIT或压缩包都可以。
### 系统需求

支持版本: Unity2021.3.14f1c1及以上

支持平台: Windows、OSX、Android、iOS、WebGL

开发环境: .NET8.x及以上
## 客户端导入Fantasy
### 1.使用Unity2021.3.14f1c1打开项目工程。
### 2.点击Unity的菜单Window->Package Manager,在弹出的窗口点击左上角的+号。
### 3.在下拉菜单里选择Add package from disk,选择你本地的Fantasy的Fantasy.Unity目录里的Fantasy.Unity目录里的package.json。
### 4.如果导入出现错误或者无法正常使用，是因为Fantasy使用了预编译指令，虽然Fantasy已经用csc.rsp解决了，但Unity在这里部分人并不能正常使用。
### 5.如果不能正常使用，需要手动在Player Settings->Other Settings里的Script Define Symbols里添加Fantasy.Unity和FANTASY_KCPUNSAFE。
### 6.如果是开发WebGL要再加一个FANTASY_WEBGL预编译指令，如果不是WebGL开发不要加这个指令。
## 服务器导入Fantasy
### 1.在服务器的解决方案中添加下载到本地的Fantasy.Net工程。
### 2.在启动的工程里，添加NLog.xsd和NLog.config文件并且都设置为每次编译都复制到输出目录（vs:右键这个文件点击属性->有个复制到出目录,rider:右键这个文件点击Properties->Copy to output directory设置为Copy always），这两个文件在Fantasy/Examples/Server/Main目录里可以找到。
### 3.在启动工程里引用添加的Fantasy.Net工程。
## 客户端初始化Fantasy
``` csharp
// 使用Fantasy.Entry.Initialize初始化Fantasy
// Initialize方法可以接收多个需要装载程序集，本例子就把当前程序集装载到Fantasy里。
// 因为生成的网络协议在当前程序集里，如果不装载就无法正常通过Fantasy发送协议到服务器中。
// 初始化完成后会返回一个Scene,Fantasy的所有功能都在这个Scene下面。
// 如果只使用网络部分、只需要找一个地方保存这个Scene供其他地方调用就可以了。
var scene = await Fantasy.Entry.Initialize(GetType().Assembly);
// 使用Scene.Connect连接到目标服务器
// 一个Scene只能创建一个连接不能多个，如果想要创建多个可以重复第一步创建多个Scene。
// 但一般一个Scene已经足够了，根本没有创建多个Scene的使用场景。
// Connect方法总共有7个参数:
// remoteAddress:目标服务器的地址
// 格式为:IP地址:端口号 例如:127.0.0.1:20000
// 如果是WebGL平台使用的是WebSocket也是这个格式，框架会转换成WebSocket的连接地址
// networkProtocolType:创建连接的协议类型（KCP、TCP、WebSocket）
// onConnectComplete:跟服务器建立连接完成执行的回调。
// onConnectFail:跟服务器建立连接失败的回调。
// onConnectDisconnect:跟服务器连接断开执行的回调。
// isHttps:当WebGL平台时要指定服务器是否是Https类型。
// connectTimeout:跟服务器建立连接超时时间，如果建立连接超过connectTimeout就会执行onConnectFail。
// Scene.Connect会返回一个Session会话、后面可以通过Session和服务器通讯
// 这里建立一个KCP通讯做一个例子
 _session = _scene.Connect(
  "127.0.0.1:20000",
  NetworkProtocolType.KCP,
  OnConnectComplete,
  OnConnectFail,
  OnConnectDisconnect,
  false, 5000);
// 注意由于服务器有心跳检测、客户端不加心跳组件的话会再一定时间内断开连接。
// 可以在OnConnectComplete的时候加上心跳组件，和服务器保持连接。
```
### 客户端发送普通消息
``` csharp
_session.Send(new C2G_TestMessage() { Tag = "Hello C2G_TestMessage" });
```
### 客户端发送RPC消息
``` csharp
var response = (G2C_TestResponse)await _session.Call(new C2G_TestRequest()
{
     Tag = "Hello C2G_TestRequest"
});
Text.text = $"收到G2C_TestResponse Tag = {response.Tag}";
```
## 服务端初始化Fantasy
``` csharp
// 使用Fantasy.Entry.Start初始化Fantasy
// Start方法可以接收多个需要装载程序集，本例子就把当前程序集装载到Fantasy里。
// 本例子就把当前程序集装载到Fantasy里。
// 有个工程使用了Fantasy就添加几个就可以了
await Fantasy.Entry.Start(GetType().Assembly);
```
### 服务端接收普通消息
``` csharp
public sealed class C2G_TestMessageHandler : Message<C2G_TestMessage>
{
    protected override async FTask Run(Session session, C2G_TestMessage message)
    {
        Log.Debug($"Receive C2G_TestMessage Tag={message.Tag}");
        await FTask.CompletedTask;
    }
}
```
### 服务端接收RPC消息
``` csharp
public sealed class C2G_TestRequestHandler : MessageRPC<C2G_TestRequest, G2C_TestResponse>
{
    protected override async FTask Run(Session session, C2G_TestRequest request, G2C_TestResponse response, Action reply)
    {
        Log.Debug($"Receive C2G_TestRequest Tag = {request.Tag}");
        response.Tag = "Hello G2C_TestResponse";
        await FTask.CompletedTask;
    }
}
```


