using Fantasy.Async;
using Fantasy.Network;

namespace Fantasy.Console.Entity;

public static class Entry
{
    private static Scene _scene;
    private static Session _session;
    public static async FTask Show()
    {
        _scene = await Fantasy.Scene.Create(SceneRuntimeType.MainThread);
        _session = _scene.Connect(
            "127.0.0.1:20000",
            NetworkProtocolType.KCP,
            OnConnectComplete,
            OnConnectFail,
            OnConnectDisconnect,
            false, 5000);
    }
    
    private static void OnConnectComplete()
    {
        Log.Debug("连接成功");
        // Session.AddComponent<SessionHeartbeatComponent>();
        // 添加心跳组件给Session。
        // Start(2000)就是2000毫秒。
        _session.AddComponent<SessionHeartbeatComponent>().Start(2000);
            
        // _session.Send(new C2G_TestMessage()
        // {
        //     Tag = "111111111111"
        // });
        TestSend1000().Coroutine();
    }

    private static int Index;
    private static async FTask TestSend1000()
    {
        Log.Debug($"Call 1{Thread.CurrentThread.ManagedThreadId}");
        
        await _session.Call(new C2G_TestRequest()
        {
            Tag = "111"
        });
        
        Log.Debug($"Call 2{Thread.CurrentThread.ManagedThreadId}");
        
        // _session.Dispose();
        
        // for (int i = 0; i < 1000; i++)
        // {
        // _session.Send(new C2G_TestMessage()
        // {
        //     Tag = $"{++Index}"
        // });
        // // }
        // await _session.Scene.TimerComponent.Net.WaitAsync(3000);
        // _session.Dispose();
        await FTask.CompletedTask;
    }

    private static void OnConnectFail()
    {
        Log.Debug("连接失败");
    }

    private static void OnConnectDisconnect()
    {
        Log.Debug("连接断开");
    }
}