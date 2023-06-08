using Fantasy.Core.Network;

namespace Fantasy.Hotfix.Handler;

public sealed class H_C2M_AddressableRequestHandler : AddressableRPC<Unit, H_C2M_AddressableRequest, H_M2C_AddressableResponse>
{
    protected override async FTask Run(Unit unit, H_C2M_AddressableRequest request, H_M2C_AddressableResponse response,
        Action reply)
    {
        Log.Debug($"H_C2M_AddressableRequest unitId:{unit.Id} name:{unit.Name} messageName:{request.Name}");
        response.ResultName = "Fantasy 123123123123";
        await FTask.CompletedTask;
    }
}