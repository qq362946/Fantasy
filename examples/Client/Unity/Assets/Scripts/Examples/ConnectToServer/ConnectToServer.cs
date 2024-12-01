using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

public class ConnectToServer : MonoBehaviour
{
    private Scene _scene;
    private Session _session;
    private void Start()
    {
        StartAsync().Coroutine();
    }

    private async FTask StartAsync()
    {
        // 1:
        // 使用Fantasy.Entry.Initialize初始化Fantasy
        // Initialize方法可以接收多个需要装载程序集，本例子就把当前程序集装载到Fantasy里。
        // 因为生成的网络协议在当前程序集里，如果不装载就无法正常通过Fantasy发送协议到服务器中。
        // 初始化框架
        Fantasy.Platform.Unity.Entry.Initialize(GetType().Assembly);
        // 创建一个Scene，这个Scene代表一个客户端的场景，客户端的所有逻辑都可以写这里
        // 如果有自己的框架，也可以就单纯拿这个Scene做网络通讯也没问题。
        // Create完成后会返回一个Scene,Fantasy的所有功能都在这个Scene下面。
        // 如果只使用网络部分、只需要找一个地方保存这个Scene供其他地方调用就可以了。
        _scene = await Scene.Create(SceneRuntimeType.MainThread);
        // 2:
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
    }

    private void OnConnectComplete()
    {
        Log.Debug("连接成功");
        // 添加心跳组件给Session。
        // Start(2000)就是2000毫秒。
        _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
    }

    private void OnConnectFail()
    {
        Log.Debug("连接失败");
    }

    private void OnConnectDisconnect()
    {
        Log.Debug("连接断开");
    }

    private void OnDestroy()
    {
        // 当Unity关闭或当前脚本销毁的时候，销毁这个Scene。
        // 这样网络和Fantasy的相关功能都会销毁掉了。
        // 这里只是展示一下如何销毁这个Scene的地方。
        // 但这里销毁的时机明显是不对的，应该放到一个全局的地方。
        _scene?.Dispose();
        _session?.Dispose();
    }
}
