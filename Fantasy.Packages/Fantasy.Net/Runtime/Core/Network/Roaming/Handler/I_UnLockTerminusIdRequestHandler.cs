#if FANTASY_NET
using System;
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 提交 Terminus 迁移后的新地址并解除源端路由锁。
/// </summary>
internal sealed class I_UnLockTerminusIdRequestHandler : AddressRPC<Scene, I_UnLockTerminusIdRequest, I_UnLockTerminusIdResponse>
{
    protected override async FTask Run(Scene scene, I_UnLockTerminusIdRequest request, I_UnLockTerminusIdResponse response, Action reply)
    {
        if (!scene.RoamingComponent.TryGet(request.RoamingId, out var sessionRoamingComponent) ||
            !sessionRoamingComponent.TryGetRoaming(request.RoamingType, out var sessionRoaming))
        {
            response.ErrorCode = InnerErrorCode.ErrLockTerminusIdNotFoundRoamingType;
            return;
        }

        // 先更新地址再释放锁，等待中的消息恢复后只会看到新路由。
        sessionRoaming.UnLockTerminusId(request.TerminusId, request.TargetSceneAddress);
        await FTask.CompletedTask;
    }
}
#endif
