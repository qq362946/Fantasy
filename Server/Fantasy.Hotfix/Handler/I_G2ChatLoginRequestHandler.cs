using Fantasy.Core.Network;
using Fantasy.Hotfix.System;

namespace Fantasy.Hotfix.Handler;

public sealed class I_G2ChatLoginRequestHandler : RouteRPC<Scene, I_G2ChatLoginRequest, I_M2ChatLoginResponse>
{
    protected override async FTask Run(Scene scene, I_G2ChatLoginRequest request, I_M2ChatLoginResponse response, Action reply)
    {
        var unit = scene.GetComponent<AccountManageComponent>().Add(request.AccountId);
        response.AddressRouteId = unit.RuntimeId;
        var responseAddressRouteId = (EntityIdStruct)response.AddressRouteId;
        Log.Debug($"I_G2ChatLoginRequest AddressRouteId:{response.AddressRouteId} AppId:{responseAddressRouteId.AppId}");
        await FTask.CompletedTask;
    }
}