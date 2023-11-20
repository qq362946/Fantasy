using Fantasy;
using Fantasy.Core.Network;
using Fantasy.Helper;

public class M2C_MoveBroadcastHandler : Message<M2C_MoveBroadcast>
{
    protected override async FTask Run(Session session, M2C_MoveBroadcast message)
    {
        Log.Info("---->"+message.Moves.ToJson());
        
        await FTask.CompletedTask;
    }
}