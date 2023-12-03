using Fantasy;
using UnityEngine;
using BestGame;

public class PlayerMoveSender : MonoBehaviour
{
    private Player player;

    void Awake()
    {
        player = GetComponent<Player>();
    }

    private MoveInfo moveInfo = new MoveInfo();
    void Update()
    {
        var movement = player.movement;
        moveInfo.Position = movement.transform.position.ToPosition();
        moveInfo.Rotation = movement.transform.rotation.ToRotation();
        moveInfo.MoveState = (int)movement.state;
        moveInfo.RoleId = GameManager.Ins.RoleId;

        if(TimeHelper.Now - lastSendTime > 100)
        {
            lastSendTime = TimeHelper.Now;

            RepeatedSend();
        }
    }

    private long lastSendTime;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
	private bool hasExtraSent = false;
    private void RepeatedSend()
    {
        var p = transform.position;
        var r = transform.rotation;

        // 角色由动到不动时，再发送一次（要把不动的状态发出去），就不再发送
        if (Vector3.Distance(lastPosition, p) <0.05f && Quaternion.Equals(lastRotation, r))
        {
            if (!hasExtraSent)
            {
                hasExtraSent = true;
                Send();
            }
            return;
        }
            
        hasExtraSent = false;
        lastPosition = p;
        lastRotation = r;
        Send();
    }

    private C2M_StartMoveMessage msg = new C2M_StartMoveMessage();
    private void Send(){
        msg.MoveInfo = moveInfo;
        // 发送位置信息
        Sender.Ins.Send(msg);
    }
}
