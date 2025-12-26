using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;
using Fantasy.Roaming;

namespace Fantasy;

public sealed class C2Chat_TestSendMapMessageHandler : Roaming<Terminus, C2Chat_TestSendMapMessage>
{
    protected override async FTask Run(Terminus terminus, C2Chat_TestSendMapMessage message)
    {
        terminus.Send(RoamingType.MapRoamingType, new Chat2M_TestMessage()
        {
            Tag = "Hi Inner Roaming Message!"
        });
        await FTask.CompletedTask;
    }
}