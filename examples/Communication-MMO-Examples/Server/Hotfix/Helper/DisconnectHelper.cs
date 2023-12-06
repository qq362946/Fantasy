using Fantasy;

namespace BestGame;

public static class DisconnectHelper
{
    public static async FTask ForceDisconnect(this Session session,
        long delay = 1000)
    {
        session.Send(new G2C_ForceDisconnected() { Message = "账号他处登录顶下线" }, 0);

        await session.Disconnect(delay);
    }
    
    public static async FTask Disconnect(this Session session, long delay = 1000)
    {
        Log.Info($"Session {session.Scene.SceneType} Disconnect");
        // 设置离线原因
        var sessionPlayer = session.GetComponent<SessionPlayerComponent>();
        if (sessionPlayer != null){
            session.RemoveComponent<SessionPlayerComponent>();
            session.RemoveComponent<AddressableRouteComponent>();
        }
            
        if (delay > 0)
            await TimerScheduler.Instance.Core.WaitAsync(delay);
        
        if(session.IsDisposed)
            return;

        session.Dispose();
    }
}