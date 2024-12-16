using Fantasy.Network.Interface;

#if FANTASY_NET
namespace Fantasy.Network;

internal sealed class ProcessSessionInfo(Session session, AClientNetwork aClientNetwork) : IDisposable
{
    public readonly Session Session = session;
    public readonly AClientNetwork AClientNetwork = aClientNetwork;

    public void Dispose()
    {
        Session.Dispose();
        AClientNetwork?.Dispose();
    }
}
#endif
