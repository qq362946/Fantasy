using Fantasy.Entitas;

namespace Fantasy;

public static class AccountFactory
{
    public static Account Create(Scene scene, string accountName)
    {
        var account = Entity.Create<Account>(scene);
        account.Name = accountName;
        return account;
    }
}