using Fantasy;
using UnityEngine;

namespace BestGame;

// 最佳实践mmo项目中的移动同步，可以明白关键的状态同步道理吧，
// 因为没有地图寻路，所以没法计算服务器上的位置。
// 实用要加前后端寻路来让计算准确，加位置预测等来减轻同步压力。
public class MoveComponent : Entity 
{
    public Vector3 TargetPos;   // 要移动的目标点位置
    public float Speed;         // 移动的速度
    public long MoveStartTime;  // 移动开始的绝对时间
    public long MoveEndTime;    // 移动到目标的绝对时间
}

public static class MoveComponentSystem
{
    public static async FTask MoveTo(this MoveComponent self, float speed,MoveInfo moveInfo, NoticeClientType notice)
    {
        // 定义和设置变量
        self.Speed = speed;
        var unit = (Unit)self.Parent;
        // 计算移动到目标需要多少时间
        float distance = Vector3.Distance(unit.Position, self.TargetPos);

        // 计算移动开始和结束的时间
        self.MoveStartTime = TimeHelper.Now;
        self.MoveEndTime = TimeHelper.Now + (long)(distance / speed * 1000);
        moveInfo.MoveStartTime = self.MoveStartTime;
        moveInfo.MoveEndTime = self.MoveEndTime;

        // 发送开始移动的事件
        EventSystem.Instance.Publish(new StartMove()
        {
            Unit = unit,
            MoveInfo = moveInfo,
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
        unit.moveState = moveInfo.MoveState.ToMoveState();

        await FTask.CompletedTask;
    }
}
