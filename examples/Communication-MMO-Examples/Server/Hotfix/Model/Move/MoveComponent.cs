using Fantasy;
using UnityEngine;

namespace BestGame;

public class MoveComponent : Entity 
{
    public long StartTime;
    public Vector3 StartPos;    // 移动起始点的位置
    public Vector3 TargetPos;   // 要移动的目标点位置
    public float Speed;         // 移动的速度
    public long NeedTime;       // 移动到目标所需要的时间
    public long MoveEndTime;    // 移动到目标的绝对时间

    public MoveInfo startInfo;
    public MoveInfo stopInfo;
    
}

public static class MoveComponentSystem
{
    public static async FTask MoveTo(this MoveComponent self, float speed,MoveInfo moveInfo, NoticeClientType notice)
    {
        // 定义和设置变量
        self.Speed = speed;
        var unit = (Unit)self.Parent;
        var runtimeId = self.RuntimeId;
        // 计算移动到目标需要多少时间
        var faceVector = self.TargetPos - unit.Position;
        var distance = faceVector.magnitude;
        self.MoveEndTime = TimeHelper.Now + (long)(distance / speed * 1000);

        self.startInfo = new MoveInfo()
        {
            RoleId = unit.RoleInfo.RoleId,
            Position = unit.Position.ToPosition(),
            Rotation = unit.Rotation.ToRotation(),
            MoveEndTime = self.MoveEndTime
        };
        // 发送开始移动的事件
        EventSystem.Instance.Publish(new StartMove()
        {
            Unit = unit,
            MoveInfo = self.startInfo,
            Notice = notice,
            MoveEndTime = self.MoveEndTime
        });

        // 服务器上unit的位置移动计算
        self.InnerMoveToAsync(unit,moveInfo,speed).Coroutine();
        await FTask.CompletedTask;
    }

    public static async FTask InnerMoveToAsync(this MoveComponent self,Unit unit, MoveInfo moveInfo, float speed)
    {
        // 测试练习，略插值计算过程...
        unit.Position = moveInfo.Position.ToVector3();
        unit.Rotation = moveInfo.Rotation.ToQuaternion();

        await FTask.CompletedTask;
    }
}
