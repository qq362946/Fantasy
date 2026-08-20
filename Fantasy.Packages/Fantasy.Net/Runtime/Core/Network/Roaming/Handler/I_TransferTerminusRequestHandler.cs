#if FANTASY_NET
using System;
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 在目标 Scene 接收 Terminus，并完成路由地址切换。
/// </summary>
internal sealed class I_TransferTerminusRequestHandler : AddressRPC<Scene, I_TransferTerminusRequest, I_TransferTerminusResponse>
{
    protected override async FTask Run(Scene scene, I_TransferTerminusRequest request, I_TransferTerminusResponse response, Action reply)
    {
        // 同一 roamingId 只能有一个目标端实例，重复注册会破坏路由唯一性。
        if (scene.TerminusComponent.TryGetTerminus(request.Terminus.Id, out _))
        {
            Log.Warning($"Transfer Terminus already exists. Scene:{scene.Address} TerminusId:{request.Terminus.Id}");
            response.ErrorCode = InnerErrorCode.ErrAddRoamingTerminalAlreadyExists;
            return;
        }

        // 先占用目标 Scene 索引，防止异步恢复期间收到重复传送。
        if (!scene.TerminusComponent.AddTerminus(request.Terminus))
        {
            response.ErrorCode = InnerErrorCode.ErrRoamingDisposed;
            return;
        }

        try
        {
            // 恢复实体并向源端提交新的 TerminusId；提交完成前源端消息仍在锁中等待。
            response.ErrorCode = await request.Terminus.TransferComplete(scene);

            if (response.ErrorCode != 0)
            {
                // 提交新地址失败时回滚目标端，源端收到错误后会恢复旧路由。
                scene.TerminusComponent.Remove(request.Terminus.Id, true);
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
            response.ErrorCode = InnerErrorCode.ErrTransfer;
            scene.TerminusComponent.Remove(request.Terminus.Id, true);
        }
    }
}
#endif
