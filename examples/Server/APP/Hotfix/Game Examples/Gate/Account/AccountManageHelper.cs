// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

using System.Runtime.CompilerServices;

namespace Fantasy;

public static class AccountManageHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Add(Scene scene, string accountName, out Account account)
    {
        var accountManageComponent = scene.GetComponent<AccountManageComponent>();
        if (accountManageComponent != null)
        {
            return accountManageComponent.Add(accountName, out account);
        }

        account = null!;
        Log.Error("当前Scene下没有找到AccountManageComponent组件");
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Remove(Scene scene, string accountName)
    {
        var accountManageComponent = scene.GetComponent<AccountManageComponent>();
        if (accountManageComponent != null)
        {
            return accountManageComponent.Remove(accountName);
        }
        Log.Error("当前Scene下没有找到AccountManageComponent组件");
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetAccount(Scene scene, string accountName, out Account account)
    {
        var accountManageComponent = scene.GetComponent<AccountManageComponent>();
        if (accountManageComponent != null)
        {
            return accountManageComponent.TryGetAccount(accountName, out account);
        }

        account = null!;
        Log.Error("当前Scene下没有找到AccountManageComponent组件");
        return false;
    }
}