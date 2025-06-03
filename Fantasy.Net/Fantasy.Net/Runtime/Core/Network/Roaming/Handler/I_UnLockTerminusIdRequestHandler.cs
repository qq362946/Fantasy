#if FANTASY_NET
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 内部网络漫游解锁的请求处理。
/// </summary>
internal sealed class I_UnLockTerminusIdRequestHandler : RouteRPC<Scene, I_UnLockTerminusIdRequest, I_UnLockTerminusIdResponse>
{
    protected override async FTask Run(Scene scene, I_UnLockTerminusIdRequest request, I_UnLockTerminusIdResponse response, Action reply)
    {
        if (!scene.TryGetEntity(request.SessionRuntimeId, out var sessionEntity))
        {
            response.ErrorCode = InnerErrorCode.ErrUnLockTerminusIdNotFoundSession;
            return;
        }
        
        var session = (Session)sessionEntity;
        
        if (!scene.RoamingComponent.TryGet(session, out var sessionRoamingComponent) ||  !sessionRoamingComponent.TryGetRoaming(request.RoamingType, out var sessionRoaming))
        {
            response.ErrorCode = InnerErrorCode.ErrLockTerminusIdNotFoundRoamingType;
            return;
        }

        sessionRoaming.UnLockTerminusId(request.TerminusId, request.TargetSceneRouteId);
        await FTask.CompletedTask;
    }
}
#endif