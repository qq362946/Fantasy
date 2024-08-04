namespace Fantasy
{
    public enum NetworkType
    {
        None = 0,
        Client = 1,
#if FANTASY_NET
        Server = 2
#endif
    }
    public enum NetworkTarget
    {
        None = 0,
        Outer = 1,
#if FANTASY_NET
        Inner = 2
#endif
    }
    public enum NetworkProtocolType
    {
        None = 0,
        KCP = 1,
        TCP = 2,
        WebSocket = 3,
    }
}