using Fantasy.Entitas.Interface;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy;

public sealed class GateAccountFlagComponentDestroySystem : DestroySystem<GateAccountFlagComponent>
{
    protected override void Destroy(GateAccountFlagComponent self)
    {
        Account account = self.Account;

        if (account == null)
        {
            // 这里也可以打印一个日志来表示下线没有找到关联的Account
            return;
        }
        
        // 因为是异步的，所以这个不要清除组件的字段/属性，要在Offline里处理。
        AccountHelper.Offline(account).Coroutine();
    }
}