using UnityEngine;
using Fantasy;
using MicroCharacterController;
using BestGame;

public class PlayerUnits : BaseUnits
{
    protected override void Awake() {
        base.Awake();
        unitViewer.isMulti = true;

        Sender.Ins.RegisterMessageHandler(ReciveType.CreateUnits,AddUnit2Scene);
        Sender.Ins.RegisterMessageHandler(ReciveType.UnitMove,UnitMove);
    }

    // unit移动
    public void UnitMove(IAddressableRouteMessage message)
    {
        var messageInfo = message as M2C_MoveBroadcast;
        foreach (var moveInfo in messageInfo.Moves)
        {
            var go = GetUnit(moveInfo.RoleId);
            if(go == null) continue;
            go.GetComponent<Movement>().MoveTarget(moveInfo);
        }
    }

    // 添加本地角色unit到场景
    public void AddLocalUnit2Scene(RoleInfo roleInfo)
    {
        var temp = new GameObject();
        temp.transform.position = roleInfo.LastMoveInfo.Position.ToVector3();
        temp.transform.rotation = roleInfo.LastMoveInfo.Rotation.ToQuaternion();

        // 添加unitViewer动态内容
        var go = unitViewer.ViewUnit(roleInfo.ClassName,roleInfo.RoleId.ToString(),temp.transform);
        AddUnit(roleInfo.RoleId,go);
        GameObject.Destroy(temp);

        // 设置Player.isLocalPlayer
        go.GetComponent<Player>().isLocalPlayer = true;

        // ==> 进入场景后，设置玩家组件
        go.GetComponent<GameEntity>().SetPlayerComponent();
        go.GetComponent<GameEntity>().EnableController();

        // CameraMMO激活，设置目标为本地玩家
        GameFacade.Ins.SetMMOCamera(go.transform); 
    }

    // 添加其它角色到场景
    public void AddUnit2Scene(IAddressableRouteMessage message)
    {
        var messageInfo = message as M2C_UnitCreate;
        foreach (var unit in messageInfo.UnitInfos)
        {
            var temp = new GameObject();
            var moveInfo = unit.LastMoveInfo;
            temp.transform.position = moveInfo.Position.ToVector3();
            temp.transform.rotation = moveInfo.Rotation.ToQuaternion();
            Log.Info("添加其它角色到场景: "+temp.transform.position);
            
            // 添加unitViewer动态内容
            var go = unitViewer.ViewUnit(unit.ClassName,unit.RoleId.ToString(),temp.transform);
            AddUnit(unit.RoleId,go);
            GameObject.Destroy(temp);
            
            // ==> 进入场景后，设置玩家组件
            go.GetComponent<GameEntity>().SetPlayerComponent();
        }
    }

    public override void ExitScene()
    {
        base.ExitScene();
        // 移除unitViewer动态内容
        unitViewer.ClearPreview(PoolType.Unit);
    }
}