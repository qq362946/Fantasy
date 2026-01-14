using Fantasy.Entitas;

namespace Fantasy;

public sealed class GateAccountFlagComponent : Entity
{
    public EntityReference<Account> Account;
}