using Fantasy;

namespace BestGame;

public class StartMoveEventHanlder : EventSystem<StartMove>
{
    public override void Handler(StartMove self)
    {
        var unit = self.Unit;
        MoveSyncComponent moveSyncComponent = unit.GetComponent<MoveSyncComponent>();

        // BroadcastWithAoi,这条消息添加到附件能看到此unit的玩家的MoveSyncComponent消息队列中
        void Action(Unit aoiUnit)
        {
            MoveSyncComponent moveSyncComponent = aoiUnit.GetComponent<MoveSyncComponent>();
        
            if (moveSyncComponent != null)
            {
                moveSyncComponent.AddMessage(unit.Id, self.MoveInfo);
            }
        }

        AOIHelper.AoiLimitAction(unit, Action);
    }
}

public class MoveSyncComponent : StateSync 
{
    public M2C_MoveBroadcast Message = new M2C_MoveBroadcast();

    public override void Send()
    {
        Message.Moves.Clear();

        /// 附件玩家，移动数据列表
        foreach (KeyValuePair<long, List<AProto>> dic in mDict)
            foreach (AProto info in dic.Value)
                Message.Moves.Add((MoveInfo)info);
        
        /// 发送消息
        if (Message.Moves.Count > 0)
            SendMessage(Message);
    }
}

public class MoveSyncComponentAwakeSystem : AwakeSystem<MoveSyncComponent>
{
    protected override void Awake(MoveSyncComponent self)
    {
        self.Interval = 10;
    }
}

public class MoveSyncDestroySystem : DestroySystem<MoveSyncComponent>
{
    protected override void Destroy(MoveSyncComponent self)
    {
        self.Message.Moves.Clear();
        self.Clear();
    }
}