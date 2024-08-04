#if FANTASY_NET
namespace Fantasy;

public sealed class InnerSessionInfo : IDisposable
{
    public readonly Session Session;
    public readonly AClientNetwork AClientNetwork;
    public InnerSessionInfo(Session session, AClientNetwork aClientNetwork)
    {
        Session = session;
        AClientNetwork = aClientNetwork;
    }

    public void Dispose()
    {
        Session.Dispose();
        AClientNetwork?.Dispose();
    }
}
#endif
