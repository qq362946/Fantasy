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
        if(TimeHelper.Now - lastSendTime > 100)
        {
            CollectData();

            if(moveInfo.MoveState.ToMoveState() == MoveState.JUMP){
                Send();
            }
            
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

        // 角色由动到不动时，额外再发送一次（要把不动的状态发出去）
        var P1 = lastPosition.ToFixedVector3();
        var P2 = p.ToFixedVector3();
        var dislast = FixedVector3.Distance(P1,P2);
        if (dislast <0.05f && Quaternion.Equals(lastRotation, r))
        {
            // 有个小问题没解决，当角色遇障碍不动时，还输入键盘指令移动，如何发送消息
            if (!hasExtraSent)
            {
                hasExtraSent = true;
                SendStop(); 
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
        Log.Info($"==> Send: {msg.MoveInfo.Position.ToVector3()}");
        
        // 发送位置信息
        Sender.Ins.Send(msg);
    }
    private void SendStop(){
        CollectData(true);
        msg.MoveInfo = moveInfo;

        // 发送位置信息
        Sender.Ins.Send(msg);
    }

    private void CollectData(bool isStop = false){
        var movement = player.movement;
        moveInfo.RoleId = GameManager.Ins.RoleId;
        var p = transform.position.ToFixedVector3();
        moveInfo.Position = p.ToVector3().ToPosition();
        moveInfo.Rotation = transform.rotation.ToRotation();
        moveInfo.MoveState = isStop?0:(int)movement.state;
        moveInfo.Flag = IdGenerate.GenerateId();
    }
}
