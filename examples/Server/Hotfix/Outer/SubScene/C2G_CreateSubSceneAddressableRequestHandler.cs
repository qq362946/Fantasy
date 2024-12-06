using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Route;

namespace Fantasy;

public sealed class C2G_CreateSubSceneAddressableRequestHandler : MessageRPC<C2G_CreateSubSceneAddressableRequest, G2C_CreateSubSceneAddressableResponse>
{
    protected override async FTask Run(Session session, C2G_CreateSubSceneAddressableRequest request, G2C_CreateSubSceneAddressableResponse response, Action reply)
    {
        var scene = session.Scene;
        var subSceneRouteId = session.GetComponent<GateSubSceneFlagComponent>().SubSceneRouteId;
        // 1、向SubScene请求AddressableId
        var responseAddressableId = (SubScene2G_AddressableIdResponse)await scene.NetworkMessagingComponent.CallInnerRoute(subSceneRouteId, new G2SubScene_AddressableIdRequest());
        // 2、给session添加一个AddressableRouteComponent组件，这个组件很重要、能否转发Addressable协议主要是通过这个。
        var addressableRouteComponent = session.AddComponent<AddressableRouteComponent>();
        // 3、拿到SubScene返回的AddressableId赋值给addressableRouteComponent.AddressableId。
        addressableRouteComponent.AddressableId = responseAddressableId.AddressableId;
    }
}