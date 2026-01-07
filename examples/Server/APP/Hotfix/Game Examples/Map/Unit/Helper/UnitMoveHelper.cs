namespace Fantasy;

public static class UnitMoveHelper
{
    public static void MoveTo(this PlayerUnit playerUnit, Position targetPos)
    {
        // 从对象池创建消息，并且设置要手动回收
        var m2CUnitMoveState = M2C_UnitMoveState.Create(false);
        m2CUnitMoveState.UnitId = playerUnit.Id;
        m2CUnitMoveState.State = 1;
        m2CUnitMoveState.Pos = targetPos;
        targetPos.Transform(ref playerUnit.Transform.Position);
        // 发送位置消息给当前Scene下的所有客户端
        PlayerUnitManageHelper.BroadcastToAllPlayers(playerUnit.Scene, m2CUnitMoveState);
        // 回收这个消息
        m2CUnitMoveState.Return();
    }
}