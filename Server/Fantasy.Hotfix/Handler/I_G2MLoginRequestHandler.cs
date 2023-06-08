using Fantasy.Core.Network;
using Fantasy.Hotfix.System;

namespace Fantasy.Hotfix;

public sealed class I_G2MLoginRequestHandler : RouteRPC<Scene, I_G2MLoginRequest, I_M2GLoginResponse>
{
    protected override async FTask Run(Scene scene, I_G2MLoginRequest request, I_M2GLoginResponse response, Action reply)
    {
        var unit = scene.GetComponent<AccountManageComponent>().Add(request.AccountId);
        await unit.AddComponent<AddressableMessageComponent>().Register();
        response.AddressRouteId = unit.RuntimeId;
        var responseAddressRouteId = (EntityIdStruct)response.AddressRouteId;
        Log.Debug($"I_G2MLoginRequest AddressRouteId:{response.AddressRouteId} AppId:{responseAddressRouteId.AppId}");
    }
}