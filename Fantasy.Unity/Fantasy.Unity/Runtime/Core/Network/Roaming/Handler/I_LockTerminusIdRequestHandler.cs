#if FANTASY_NET
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 内部网络漫游锁定的请求处理。
/// </summary>
internal sealed class I_LockTerminusIdRequestHandler : RouteRPC<Scene,  I_LockTerminusIdRequest, I_LockTerminusIdResponse>
{
    protected override async FTask Run(Scene scene, I_LockTerminusIdRequest request, I_LockTerminusIdResponse response, Action reply)
    {
        if (!scene.TryGetEntity(request.SessionRuntimeId, out var sessionEntity))
        {
            response.ErrorCode = InnerErrorCode.ErrLockTerminusIdNotFoundSession;
            return;
        }
        
        var session = (Session)sessionEntity;
        
        if (!scene.RoamingComponent.TryGet(session, out var sessionRoamingComponent) ||  !sessionRoamingComponent.TryGetRoaming(request.RoamingType, out var sessionRoaming))
        {
            response.ErrorCode = InnerErrorCode.ErrLockTerminusIdNotFoundRoamingType;
            return;
        }

        await sessionRoaming.LockTerminusId();
    }
}
#endif