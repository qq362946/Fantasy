using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Network.Interface;
using Fantasy.Network.Route;

namespace Fantasy;

public sealed class G2SubScene_AddressableIdRequestHandler : RouteRPC<SubScene, G2SubScene_AddressableIdRequest, SubScene2G_AddressableIdResponse>
{
    protected override async FTask Run(SubScene subScene, G2SubScene_AddressableIdRequest request, SubScene2G_AddressableIdResponse response, Action reply)
    {
        // 1、因为是测试代码，所以默认每次请求这个协议我都创建一个新的Unit来做Addressable。
        var unit = Entity.Create<Unit>(subScene, false, true);
        // 2、给Unit添加AddressableMessageComponent组件，并执行Register()，向AddressableScene注册自己当前的位置。
        await unit.AddComponent<AddressableMessageComponent>().Register();
        // 3、返回给Gate服务器AddressableId
        response.AddressableId = unit.Id;
        await FTask.CompletedTask;
    }
}