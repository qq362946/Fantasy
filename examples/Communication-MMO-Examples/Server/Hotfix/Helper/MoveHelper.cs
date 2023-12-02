using UnityEngine;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
using BestGame;

namespace Fantasy;

public static class MoveHelper
{
    /// <summary>
    /// 开始移动
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="speed"></param>
    /// <param name="target"></param>
    /// <param name="notice"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async FTask<int> MoveToAsync(Unit unit, float speed, Vector3 target, NoticeClientType notice, FCancellationToken cancellationToken = null)
    {
        if (target == Vector3.zero)
        {
            Log.Error($"{unit.UnitType} {unit.Id} 移动异常 {target}");
            return 100001;
        }

        if (unit.Position == target)
        {
            // 起始点和目标点一致、就不需要移动了
            return 0;
        }

        var moveComponent = unit.GetComponent<MoveComponent>();
        var moveTo = await moveComponent.MoveTo(speed, target, notice, cancellationToken);

        if (!moveTo)
        {
            // 移动中断要发送给客户端、客户端要提示一下
            return 100002;
        }

        return 0;
    }

    /// <summary>
    /// 停止移动
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="notice"></param>
    public static void Stop(Unit unit,NoticeClientType notice)
    {
        var moveComponent = unit.GetComponent<MoveComponent>();

        if (moveComponent == null)
        {
            return;
        }
        
        moveComponent.StopMove(notice);
    }
}