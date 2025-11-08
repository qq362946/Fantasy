#if FANTASY_NET
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy.Sphere;

/// <summary>
/// 处理领域事件发布请求的路由消息处理器。
/// 用于接收远程节点发布的领域事件,并触发本地订阅者的事件处理。
/// </summary>
internal sealed class I_PublishSphereEventRequestHandler : RouteRPC<Scene, I_PublishSphereEventRequest, I_PublishSphereEventResponse>
{
    protected override async FTask Run(Scene scene, I_PublishSphereEventRequest request, I_PublishSphereEventResponse response, Action reply)
    {
        // 验证 RouteId 是否有效
        // RouteId 用于标识发布者,不能为 0
        if (request.RouteId == 0)
        {
            response.ErrorCode = InnerErrorCode.ErrPublishSphereEventInvalidRouteId;
            return;
        }

        // 验证 SphereEventArgs 是否为空
        // SphereEventArgs 包含事件的具体数据,不能为 null
        if (request.SphereEventArgs == null)
        {
            response.ErrorCode = InnerErrorCode.ErrPublishSphereEventNullEventArgs;
            return;
        }

        // 处理远程发布的事件并调用本地订阅者
        // 将事件分发给所有订阅了该事件类型的本地处理器
        response.ErrorCode = await scene.SphereEventComponent.HandleRemotePublication(request.RouteId, request.SphereEventArgs);
    }
}
#endif