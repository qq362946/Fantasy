namespace Fantasy;

public sealed class C2G_CreateAddressableRequestHandler : MessageRPC<C2G_CreateAddressableRequest, G2C_CreateAddressableResponse>
{
    protected override async FTask Run(Session session, C2G_CreateAddressableRequest request, G2C_CreateAddressableResponse response, Action reply)
    {
        var scene = session.Scene;
        // 1、首先要通过SceneConfig配置文件拿到进行注册Addressable协议的服务器
        // 实际开发的时候，可能会根据一些规则来选择不同的Map服务器。
        // 演示的例子里只有一个MapScene，所以我就拿第一个Map服务器进行通讯了。
        // 我这里仅是演示功能，不是一定要这样拿Map
        var sceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[0];
        // 2、使用Scene.NetworkMessagingComponent.CallInnerRoute方法跟Gate服务器进行通讯。
        // CallInnerRoute方法跟Gate服务器进行通讯需要提供一个runTimeId，这个Id在sceneConfig.RouteId可以获取到。
        // 第二个参数是需要发送网络协议，这个协议在Fantasy/Examples/Config/ProtoBuf里的InnerBson或Inner文件定义。
        var responseAddressableId = (M2G_ResponseAddressableId)await scene.NetworkMessagingComponent.CallInnerRoute(sceneConfig.RouteId, new G2M_RequestAddressableId());
        // 3、给session添加一个AddressableRouteComponent组件，这个组件很重要、能否转发Addressable协议主要是通过这个。
        var addressableRouteComponent = session.AddComponent<AddressableRouteComponent>();
        // 4、拿到MapScene返回的AddressableId赋值给addressableRouteComponent.AddressableId。
        addressableRouteComponent.AddressableId = responseAddressableId.AddressableId;
    }
}