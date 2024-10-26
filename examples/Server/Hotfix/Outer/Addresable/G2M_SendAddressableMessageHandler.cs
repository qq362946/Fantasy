using Fantasy.Async;
using Fantasy.Network.Interface;

namespace Fantasy;

public sealed class G2M_SendAddressableMessageHandler : Addressable<Unit, G2M_SendAddressableMessage>
{
    protected override async FTask Run(Unit unit, G2M_SendAddressableMessage message)
    {
        Log.Debug($"收到Gate发送来的Addressable消息 message:{message.Tag}");
        await FTask.CompletedTask;
    }
}