using Fantasy.Async;
using Fantasy.Network.Interface;
namespace Fantasy;

public sealed class C2M_MoveRequestHandler : RoamingRPC<PlayerUnit, C2M_MoveRequest, M2C_MoveResponse>
{
    protected override async FTask Run(PlayerUnit playerUnit, C2M_MoveRequest request, M2C_MoveResponse response, Action reply)
    {
        // 这里仅是做了位置状态，实际的同步是需要特殊处理的。
        // 因为本项目仅是为了展示框架的功能，所以同步功能就暂时不做了。
        playerUnit.MoveTo(request.TargetPos);
        await FTask.CompletedTask;
    }
}