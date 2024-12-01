using System;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Network.Interface;
using Fantasy.Network.Route;
using Fantasy.Platform.Net;
using Fantasy.Serialize;

namespace Fantasy;

public class C2M_MoveToMapRequestHandler : AddressableRPC<Unit, C2M_MoveToMapRequest, M2C_MoveToMapResponse>
{
    protected override async FTask Run(Unit unit, C2M_MoveToMapRequest request, M2C_MoveToMapResponse response, Action reply)
    {
        // 1、首先要通过SceneConfig配置文件拿到MapScene的配置文件。
        // 这里Map[1]就是要发送的服务器,因为Map[0]是当前的Scene。
        var scene = unit.Scene;
        var mapSceneConfig = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Map)[1];
        // 2、锁定Addressable防止在转移期间有消息发送过来。
        // LockAndRelease方法是先锁定Addressable消息后再销毁这个组件。
        // 注意:只有这个方法的销毁组件不会去Addressable里删除自己的位置信息。
        // 其他的任何销毁这个AddressableMessageComponent方法都会去Addressable里删除自己的位置信息。
        await unit.GetComponent<AddressableMessageComponent>().LockAndRelease();
        // 3、通过NetworkMessagingComponent发送内部消息给Map的Scene。
        var sendResponse = await scene.NetworkMessagingComponent.CallInnerRoute(mapSceneConfig.RouteId,
            new M2M_SendUnitRequest()
            {
                Unit = unit
            });
        if (sendResponse.ErrorCode != 0)
        {
            Log.Error($"转移Unit到目标Map服务器失败 ErrorCode={sendResponse.ErrorCode}");
            return;
        }
        // 这个Unit已经转移到另外的Map服务器了，所以就不需要这个组件了。
        unit.Dispose();
        Log.Debug("转移Unit到目标Map服务器成功");
    }
}