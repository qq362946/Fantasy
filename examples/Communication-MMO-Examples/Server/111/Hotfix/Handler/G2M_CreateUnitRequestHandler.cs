using Fantasy.Core.Network;
using Fantasy;

namespace BestGame;

public class G2M_CreateUnitRequestHandler : RouteRPC<Scene,G2M_CreateUnitRequest,M2G_CreateUnitResponse>
{
    protected override async FTask Run(Scene scene,G2M_CreateUnitRequest request, M2G_CreateUnitResponse response, Action reply)
    {
        var unitManager = scene.GetComponent<UnitComponent>();
        // 1、用PlayerId创建一个Unit
        var unit = Entity.Create<Unit>(scene, request.PlayerId);
        unit.SessionRuntimeId = request.SessionRuntimeId;
        unit.GateSceneRouteId = request.GateSceneRouteId;
        unit.moveInfo = MessageInfoHelper.MoveInfo(); //练习就给一个原点的默认位置，真实项目有地图配置文件的出生点位置，或者已经保存的玩家下线位置。
        unitManager.Add(unit);

        // 2、挂AddressableMessageComponent组件、让这个Unit支持Address（可被寻址）、并且会自动注册到网格中
        await unit.AddComponent<AddressableMessageComponent>().Register();

        // 3、挂移动组件，状态同步组件
        unit.AddComponent<MoveComponent>();
        unit.AddComponent<MoveSyncComponent>();
        unit.AddComponent<NoticeUnitSyncComponent>();
        response.AddressableId = unit.Id;

        // 发事件给NoticeUnit同步组件
        // 想想这里为什么要发事件给NoticeUnit同步组件,而不是在此处直接M2C发到客户端?
        EventSystem.Instance.Publish(new EventSystemStruct.NoticeUnitAdd{
            Unit = unit, RoleInfo = request.RoleInfo
        });

        response.Message = "-->创建unit成功";
    }
}