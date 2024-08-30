
namespace Fantasy;

public sealed class C2M_RequestNetworkObjectIdHandler : AddressableRPC<Unit, C2M_RequestNetworkObjectId, M2C_ResponseNetworkObjectId>
{
    protected override async FTask Run(Unit entity, C2M_RequestNetworkObjectId request, M2C_ResponseNetworkObjectId response, Action reply)
    {
        var logmgr = entity.Scene.GetComponent<LogicMgr>();
        var room = logmgr.GetRoomById(entity.RoomID);
        var id = await room.AddGameEntity(entity,request.PrefabID,request.NetworkScriptsID,request.Transform);
        //创建完毕拿到ID
        response.AddressableId = id;
        response.Authority = true;
        response.ClientID = entity.ClientID;
        await FTask.CompletedTask;
    }
}