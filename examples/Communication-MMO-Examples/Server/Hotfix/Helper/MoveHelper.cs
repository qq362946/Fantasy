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
    /// <returns></returns>
    public static async FTask<int> MoveToAsync(Unit unit, float speed, MoveInfo moveInfo,NoticeClientType notice)
    {
        var target = moveInfo.Position.ToVector3();
        if (target == Vector3.zero)
        {
            Log.Error($"{unit.UnitType} {unit.Id} 移动异常 {target}");
            return 100001;
        }

        var moveComponent = unit.GetComponent<MoveComponent>();
        await moveComponent.MoveTo(speed, moveInfo, notice);

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
        
        // moveComponent.StopMove(notice);
    }
}