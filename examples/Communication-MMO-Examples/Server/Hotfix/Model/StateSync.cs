using Fantasy;

namespace BestGame;

public class StateSync : Entity 
{
    //public IMessage Message;
    public OneToManyList<long, AProto> mDict = new OneToManyList<long, AProto>();
    public long Interval = 15;
    public bool IsWait;

    public void Clear()
    {
        mDict.Clear();
        IsWait = false;
    }

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

    public virtual void Send()
    {
        
    }

    public void SendMessage(IMessage message)
    {
        var unit = (Unit)Parent;
        
        if(!unit.IsDisposed)
            MessageHelper.SendInnerRoute(unit.Scene,unit.SessionRuntimeId,
                (IRouteMessage)message
            );
        
        mDict.Clear();
    }
}