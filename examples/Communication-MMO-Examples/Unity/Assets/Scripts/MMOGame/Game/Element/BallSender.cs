using UnityEngine;
using Fantasy;

public partial class PreBall : BehaviourNonAlloc
{
    private MoveInfo moveInfo = new MoveInfo();
    private Player player;

    void InitSender()
    {
        player = GetComponent<Player>();
    }
    void CollecMoveData()
    {
        moveInfo.Position = transform.position.ToPosition();
        moveInfo.Rotation = transform.rotation.ToRotation();
        moveInfo.RoleId = GameManager.Ins.RoleId;

    }

    private C2M_StartMoveMessage startMsg = new C2M_StartMoveMessage();
    private void SendStartMove()
    {
        CollecMoveData();
        startMsg.MoveInfo = moveInfo;
        // 发送开始移动位置信息
        Sender.Ins.Send(startMsg);
    }
    private C2M_StopMoveMessage stopMsg = new C2M_StopMoveMessage();
    private void SendStopMove()
    {
        // 发送停止移动信息
        Sender.Ins.Send(stopMsg);
    }
}