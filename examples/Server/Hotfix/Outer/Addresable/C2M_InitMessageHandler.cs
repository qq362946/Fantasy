
namespace Fantasy;

public sealed class C2M_InitMessageHandler : AddressableRPC<Unit, C2M_RequestInit, M2C_ResponseInit>
{
    protected override async FTask Run(Unit unit, C2M_RequestInit request, M2C_ResponseInit response, Action reply)
    {
        var logicMgr = unit.Scene.GetComponent<LogicMgr>();
        var room = logicMgr.GetRoomById(unit.RoomID);
        if(room!=null)
            response.initData.AddRange(room.GetAllNeedInit());
        response.ClientID = unit.ClientID;
        response.SortID = room.GetNextSortId;
        await FTask.CompletedTask;

    }
}