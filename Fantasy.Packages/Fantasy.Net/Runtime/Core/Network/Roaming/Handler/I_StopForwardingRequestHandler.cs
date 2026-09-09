#if FANTASY_NET
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 在源端 Session 断开后的保活期内暂停 Terminus 消息转发。
/// </summary>
internal sealed class I_StopForwardingRequestHandler : AddressRPC<Scene, I_StopForwardingRequest, I_StopForwardingResponse>
{
    protected override async FTask Run(Scene scene, I_StopForwardingRequest request, I_StopForwardingResponse response, Action reply)
    {
        if (!scene.TerminusComponent.TryGetTerminus(request.RoamingId, out var terminus))
        {
            // Terminus 已不存在时，目标已经处于无需转发的状态。
            return;
        }
        
        if (terminus.OwnerRoamingRuntimeId != request.OwnerRoamingRuntimeId)
        {
            // 旧 Gate 的延迟暂停请求已经失效。
            // 按成功处理，避免旧 Gate 记录无意义的错误日志。
            return;
        }

        terminus.StopForwarding = true;
        await FTask.CompletedTask;
    }
}
#endif
