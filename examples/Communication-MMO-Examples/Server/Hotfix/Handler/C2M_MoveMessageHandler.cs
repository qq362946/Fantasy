using Fantasy;

namespace BestGame;

public class C2M_MoveMessageHandler : Addressable<Unit,C2M_MoveMessage>
{
    protected override async FTask Run(Unit unit, C2M_MoveMessage message)
    {
        // 路径点合法判断略...
        // 移动停止检测略...
  
        // 调用MoveComponent
        unit.GetComponent<MoveComponent>().MoveToAsync(message.MoveInfo).Coroutine();

        await FTask.CompletedTask;
    }
}