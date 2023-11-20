using Fantasy;
using Fantasy.Core.Network;
using Fantasy.DataStructure;
using Fantasy.Helper;

namespace BestGame;

public class NoticeUnitAddEventHanlder : EventSystem<EventSystemStruct.NoticeUnitAdd>
{
    public override void Handler(EventSystemStruct.NoticeUnitAdd self)
    {
        var unit = self.Unit;
        var noticeUnitSync = unit.GetComponent<NoticeUnitSyncComponent>();

        // 可以加BroadcastWithAoi，如果不是附近玩家就不添加到消息队列，略过...
        noticeUnitSync.AddMessage(unit.Id, self.RoleInfo);
    }
}

public class NoticeUnitSyncComponent : StateSync 
{
    public M2C_NoticeUnitAdd Message = new M2C_NoticeUnitAdd();

    public override void Send()
    {
        Message.RoleInfos.Clear();

        /// 所有玩家，有状态变化的数据列表
        foreach (KeyValuePair<long, List<AProto>> dic in mDict)
            foreach (AProto info in dic.Value)
                Message.RoleInfos.Add((RoleInfo)info);
           
        /// 发送消息
        SendMessage(Message);
    }
}

public class NoticeUnitSyncDestroySystem : DestroySystem<NoticeUnitSyncComponent>
{
    protected override void Destroy(NoticeUnitSyncComponent self)
    {
        self.Message.RoleInfos.Clear();
        self.Clear();
    }
}