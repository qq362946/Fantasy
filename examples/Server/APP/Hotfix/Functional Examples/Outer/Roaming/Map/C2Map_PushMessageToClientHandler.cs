using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Roaming;

namespace Fantasy;

public sealed class C2Map_PushMessageToClientHandler : Roaming<Terminus, C2Map_PushMessageToClient>
{
    protected override async FTask Run(Terminus terminus, C2Map_PushMessageToClient message)
    {
        terminus.Send(new Map2C_PushMessageToClient()
        {
            Tag = message.Tag
        });
        await FTask.CompletedTask;
    }
}