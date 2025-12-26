using Fantasy.Entitas;
using Fantasy.Network;

namespace Fantasy;

public sealed class Account : Entity
{
    public string Name;

    public EntityReference<Session> Session;
}