using System;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Route;

namespace Fantasy;

public sealed class G2M_RequestAddressableIdHandler : RouteRPC<Scene, G2M_RequestAddressableId, M2G_ResponseAddressableId>
{
    protected override async FTask Run(Scene scene, G2M_RequestAddressableId request, M2G_ResponseAddressableId response, Action reply)
    {
        // 1、因为是测试代码，所以默认每次请求这个协议我都创建一个新的Unit来做Addressable。
        var unit = Entity.Create<Unit>(scene, false, true);
        // 2、给Unit添加AddressableMessageComponent组件，并执行Register()，向AddressableScene注册自己当前的位置。
        await unit.AddComponent<AddressableMessageComponent>().Register();
        // 3、返回给Gate服务器AddressableId
        response.AddressableId = unit.Id;
        await FTask.CompletedTask;
    }
}