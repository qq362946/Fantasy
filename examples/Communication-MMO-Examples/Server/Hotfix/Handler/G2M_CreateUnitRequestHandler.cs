using Fantasy;

namespace BestGame;

public class G2M_CreateUnitRequestHandler : RouteRPC<Scene,G2M_CreateUnitRequest,M2G_CreateUnitResponse>
{
    protected override async FTask Run(Scene scene,G2M_CreateUnitRequest request, M2G_CreateUnitResponse response, Action reply)
    {
        var unitManager = scene.GetComponent<UnitManager>();
        // 1、用PlayerId创建一个Unit
        var unit = Entity.Create<Unit>(scene, request.PlayerId);
        unit.UnitType = UnitType.Player;
        unit.SessionRuntimeId = request.SessionRuntimeId;
        unit.GateRouteId = request.GateRouteId;
        var moveInfo = request.RoleInfo.LastMoveInfo;
        unit.Position = moveInfo.Position.ToVector3();
        unit.Rotation = moveInfo.Rotation.ToQuaternion();
        unit.RoleInfo = request.RoleInfo;
        unitManager.Add(unit);

        // 2、挂AddressableMessageComponent组件、让这个Unit支持Address（可被寻址）、并且会自动注册到网格中
        await unit.AddComponent<AddressableMessageComponent>().Register();

        // 3、挂移动组件，状态同步组件
        unit.AddComponent<MoveComponent>();
        unit.AddComponent<MoveSyncComponent>();

        // 4、添加到AOI管理器中
        AOIHelper.AddAOI(unit);

        response.AddressableId = unit.Id;
        response.Message = "-->创建unit成功";
    }
}