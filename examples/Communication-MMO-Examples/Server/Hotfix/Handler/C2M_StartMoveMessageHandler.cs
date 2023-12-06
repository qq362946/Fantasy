using Fantasy;

namespace BestGame;

public class C2M_StartMoveMessageHandler : Addressable<Unit,C2M_StartMoveMessage>
{
    protected override async FTask Run(Unit unit, C2M_StartMoveMessage message)
    {
        // 路径点合法判断略...
        // 移动停止检测略...
  
        // 调用MoveComponent
        var targetPos = message.MoveInfo.Position.ToVector3();
        MoveHelper.MoveToAsync(unit,5, message.MoveInfo, NoticeClientType.Aoi).Coroutine();
        // Log.Info($"-->移动到{targetPos}");

        await FTask.CompletedTask;
    }
}