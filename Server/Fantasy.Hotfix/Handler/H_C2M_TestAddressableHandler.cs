using Fantasy.Core.Network;

namespace Fantasy.Hotfix.Handler;

public sealed class H_C2M_TestAddressableHandler : Addressable<Unit, H_C2M_TestAddressable>
{
    protected override async FTask Run(Unit unit, H_C2M_TestAddressable message)
    {
        Log.Debug($"H_C2M_TestAddressable unitId:{unit.Id} name:{unit.Name} messageName:{message.Name}");
        await FTask.CompletedTask;
    }
}