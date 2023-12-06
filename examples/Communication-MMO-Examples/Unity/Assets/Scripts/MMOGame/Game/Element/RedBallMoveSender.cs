using UnityEngine;
using Fantasy;

public class RedBallMoveSender: MoveSender
{
    private MoveInfo moveInfo = new MoveInfo();

    private C2M_StartMoveMessage startMsg = new C2M_StartMoveMessage();
    public override void SendPreMove(Vector3 prePos)
    {
        base.SendPreMove(prePos);
        var RoleId = GameManager.Ins?GameManager.Ins.RoleId:0;
        moveInfo.Position = prePos.ToPosition();
        moveInfo.Rotation = transform.rotation.ToRotation();
        moveInfo.RoleId = RoleId;
        if (RoleId == 0) return;
        
        startMsg.MoveInfo = moveInfo;
        // 发送开始移动位置信息
        Sender.Ins.Send(startMsg);
    }
    
    private C2M_StopMoveMessage stopMsg = new C2M_StopMoveMessage();
    public override void SendStopMove()
    {
        var RoleId = GameManager.Ins?GameManager.Ins.RoleId:0;
        if (RoleId == 0) return;
        
        // 发送停止移动信息
        Sender.Ins.Send(stopMsg);
    }
}