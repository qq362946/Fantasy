using Fantasy;
using Fantasy.Core.Network;
using Fantasy.DataStructure;
using Fantasy.Helper;

namespace BestGame;
public class MoveSyncDestroySystem : DestroySystem<MoveSyncComponent>
{
    protected override void Destroy(MoveSyncComponent self)
    {
        self.mDict.Clear();
            
        self.Message.Moves.Clear();
        
        self.IsWait = false;
    }
}

public class MoveSyncComponent : Entity 
{
    public M2C_MoveBroadcast Message = new M2C_MoveBroadcast();
    public OneToManyList<long, MoveInfo> mDict = new OneToManyList<long, MoveInfo>();
    public bool IsWait;

    public void AddMessage(long unitId, MoveInfo moveInfo)
    {
        mDict.Add(unitId, moveInfo);

        if (IsWait) return;
        AddSync().Coroutine();
    }

    public async FTask AddSync()
    {
        IsWait = true;
        long runtimeId = RuntimeId;

        // 延迟15毫秒发送，让mDict收集数据
        await TimerScheduler.Instance.Core.WaitAsync(15);
        
        // 防止异步的时候自己销毁了、下面逻辑执行的不对
        if (runtimeId != RuntimeId) return;
        
        IsWait = false;
        Send();
    }

    /// 是如何把玩家的移动状态广播给所有人的呢（有AOI就是附近所有人）？
    /// ==》玩家附近只要有人状态有变化，就把这些变化的状态加到消息队列，每15毫秒的消息量发送一次给玩家
    public void Send()
    {
        Message.Moves.Clear();

        /// 只是测试练习，还没有AOI组件
        /// 所有玩家，有移动状态变化的位置信息列表
        foreach (KeyValuePair<long, List<MoveInfo>> dic in mDict)
        { 
            foreach (MoveInfo info in dic.Value)
            {
                Message.Moves.Add(info);
            }
        }

        var unit = (Unit)Parent;
        /// 向玩家发送所有的移动状态
        MessageHelper.SendInnerRoute(unit.Scene,unit.SessionRuntimeId,
            Message
        );
    }
}