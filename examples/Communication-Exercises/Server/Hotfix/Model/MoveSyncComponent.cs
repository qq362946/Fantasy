using Fantasy;

namespace BestGame;

public class StartMoveEventHanlder : EventSystem<EventSystemStruct.StartMove>
{
    public override void Handler(EventSystemStruct.StartMove self)
    {
        var unit = self.unit;
        MoveSyncComponent moveSyncComponent = unit.GetComponent<MoveSyncComponent>();

        // 可以加BroadcastWithAoi，略过...

        moveSyncComponent.AddMessage(unit.Id, self.moveInfo);
    }
}

public class MoveSyncDestroySystem : DestroySystem<MoveSyncComponent>
{
    protected override void Destroy(MoveSyncComponent self)
    {
        self.mDict.Clear();
            
        self.Message.Moves.Clear();
        
        self.IsWait = false;
    }
}

public class MoveSyncComponent : StateSync 
{
    public M2C_MoveBroadcast Message = new M2C_MoveBroadcast();

    public override void Send()
    {
        Message.Moves.Clear();

        /// 所有玩家，有状态变化的数据列表
        foreach (KeyValuePair<long, List<AProto>> dic in mDict)
        { 
            foreach (AProto info in dic.Value)
            {
                Message.Moves.Add((MoveInfo)info);
            }
        }

        SendMessage(Message);
    }
}