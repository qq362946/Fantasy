using Fantasy.Core.Network;
using Fantasy.Helper;

namespace Fantasy.Hotfix;

public class H_C2M_MessageHandler : Addressable<Unit,H_C2M_Message>
{
    protected override async FTask Run(Unit unit, H_C2M_Message message)
    {
        Log.Debug($"接收到一个Address消息 Unit:{unit.Id} message:{message.ToJson()}");
        await FTask.CompletedTask;
    }
}