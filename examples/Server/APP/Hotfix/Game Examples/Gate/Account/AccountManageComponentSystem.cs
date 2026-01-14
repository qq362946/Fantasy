using Fantasy.Entitas.Interface;

namespace Fantasy;

public sealed class AccountManageComponentDestroySystem : DestroySystem<AccountManageComponent>
{
    protected override void Destroy(AccountManageComponent self)
    {
        foreach (var (_, account) in self.Accounts)
        {
            account.Dispose();
        }

        self.Accounts.Clear();
    }
}

public static class AccountManageComponentSystem
{
    public static bool Add(this AccountManageComponent self, string accountName, out Account account)
    {
        if (self.Accounts.ContainsKey(accountName))
        {
            account = null!;
            return false;
        }

        account = AccountFactory.Create(self.Scene, accountName);
        self.Accounts.Add(accountName, account);
        return true;
    }

    public static bool Remove(this AccountManageComponent self, string accountName, bool isDispose = true)
    {
        if (!self.Accounts.Remove(accountName, out var account))
        {
            return false;
        }

        if (isDispose)
        {
            account.Dispose();
        }
        
        return true;
    }

    public static bool TryGetAccount(this AccountManageComponent self, string accountName, out Account account)
    {
        return self.Accounts.TryGetValue(accountName, out account!);
    }
}