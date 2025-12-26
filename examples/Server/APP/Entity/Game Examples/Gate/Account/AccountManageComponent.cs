using Fantasy.Entitas;

namespace Fantasy;

public sealed class AccountManageComponent : Entity
{
    public readonly Dictionary<string, Account> Accounts = new Dictionary<string, Account>();
}