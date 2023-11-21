using Fantasy;

namespace BestGame;

public class C2R_GetZoneListHandler : MessageRPC<C2R_GetZoneList, R2C_GetZoneList>
{
    // 获取区服网关列表
    protected override async FTask Run(Session session, C2R_GetZoneList request, R2C_GetZoneList response, Action reply)
    {
        var accountManager = session.Scene.GetComponent<AccountManager>();

        foreach (Zone world in accountManager.ZoneList)
        {
            var worldConfig = WorldConfigData.Instance.Get(world.World);

            response.ZoneId.Add(world.World);
            response.ZoneName.Add(worldConfig.WorldName);
            response.ZoneState.Add((int) world.ServerState);
        }

        await FTask.CompletedTask;
    }
}