using Fantasy;

namespace BestGame;

public class StateSync : Entity 
{
    //public IMessage Message;
    public OneToManyList<long, AProto> mDict = new OneToManyList<long, AProto>();
    public long Interval = 15;
    public bool IsWait;

    public void AddMessage(long unitId, AProto stateInfo)
    {
        mDict.Add(unitId, stateInfo);

        if (IsWait) return;
        AddSync().Coroutine();
    }

    public async FTask AddSync()
    {
        IsWait = true;
        long runtimeId = RuntimeId;

        // 延迟15毫秒发送，让mDict收集数据
        await TimerScheduler.Instance.Core.WaitAsync(Interval);
        
        // 防止异步的时候自己销毁了、下面逻辑执行的不对
        if (runtimeId != RuntimeId) return;
        
        IsWait = false;
        Send();
    }

    /// 是如何把玩家的移动状态广播给所有人的呢（有AOI就是附近所有人）？
    /// ==》玩家附近只要有人状态有变化，就把这些变化的状态加到消息队列，每15毫秒的消息量发送一次给玩家
    public virtual void Send()
    {
        
    }

    public void SendMessage(IMessage message)
    {
        
        var unit = (Unit)Parent;
        /// 向玩家发送状态数据
        MessageHelper.SendInnerRoute(unit.Scene,unit.SessionRuntimeId,
            (IRouteMessage)message
        );
    }
}