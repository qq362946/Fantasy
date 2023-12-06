using UnityEngine;
using Fantasy;

public class RedBallSenderTest: MoveSender
{
    private MoveInfo moveInfo = new MoveInfo();

    private C2M_StartMoveMessage startMsg = new C2M_StartMoveMessage();
    public override void SendPreMove(Vector3 prePos)
    {
        base.SendPreMove(prePos);
        moveInfo.Position = prePos.ToPosition();
        moveInfo.Rotation = transform.rotation.ToRotation();
        followRole.GetComponent<NetCharaMovement>().MoveTarget(moveInfo);
        
    }
    
    private C2M_StopMoveMessage stopMsg = new C2M_StopMoveMessage();
    public override void SendStopMove()
    {
        followRole.GetComponent<NetCharaMovement>().MoveTarget(moveInfo);
        
    }
}