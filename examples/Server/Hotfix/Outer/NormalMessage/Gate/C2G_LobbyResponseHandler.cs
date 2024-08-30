
namespace Fantasy.Gate;

public sealed class C2GLobbyResponseHandler : MessageRPC<C2G_LobbyRequest, G2C_LobbyResponse>
{
    protected override async FTask Run(Session session, C2G_LobbyRequest request, G2C_LobbyResponse response, Action reply)
    {
        var lobby = session.Scene.GetComponent<Lobby>();
        if (request.StatusCode == 1)
        {
            lobby.AddWaitingClient(session.RunTimeId,2);
        }else if(request.StatusCode == 2)
        {
            lobby.RemoveWaitingClient(session.RunTimeId);
        }

        await FTask.CompletedTask;
    }
}