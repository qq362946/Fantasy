using System;
using Fantasy.Async;
using Fantasy.Network.Interface;
using Fantasy.Network.Route;

namespace Fantasy;

public class M2M_SendUnitRequestHandler : RouteRPC<Scene, M2M_SendUnitRequest, M2M_SendUnitResponse>
{
    protected override async FTask Run(Scene scene, M2M_SendUnitRequest request, M2M_SendUnitResponse response, Action reply)
    {
        var requestUnit = request.Unit;
        // 反序列化Unit，把Unit注册到框架中
        requestUnit.Deserialize(scene);
        // 解锁这个Unit的Addressable消息，解锁后，Gate上缓存的消息会发送到这里。
        // 由于AddressableMessageComponent不支持存数据库，所以在发送Unit的时候，会自动把这个给忽略掉。
        // 所以需要再次手动的添加下才可以。
        await requestUnit.AddComponent<AddressableMessageComponent>().UnLock("M2M_SendUnitRequestHandler");
        Log.Debug($"传送完成 {scene.SceneConfigId}");
    }
}