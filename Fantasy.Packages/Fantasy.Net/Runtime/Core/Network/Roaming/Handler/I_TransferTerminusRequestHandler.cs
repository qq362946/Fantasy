#if FANTASY_NET
using System;
using Fantasy.Async;
using Fantasy.InnerMessage;
using Fantasy.Network;
using Fantasy.Network.Interface;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Roaming.Handler;

/// <summary>
/// 传送漫游Terminus的请求处理
/// </summary>
internal sealed class I_TransferTerminusRequestHandler : AddressRPC<Scene, I_TransferTerminusRequest, I_TransferTerminusResponse>
{
    protected override async FTask Run(Scene scene, I_TransferTerminusRequest request, I_TransferTerminusResponse response, Action reply)
    {
        // 目标 Scene 已经有该 Terminus 时拒绝重复注册。
        if (scene.TerminusComponent.TryGetTerminus(request.Terminus.Id, out _))
        {
            Log.Warning($"Transfer Terminus already exists. Scene:{scene.Address} TerminusId:{request.Terminus.Id}");
            response.ErrorCode = InnerErrorCode.ErrAddRoamingTerminalAlreadyExists;
            return;
        }
        
        // 添加Terminus到当前Scene下。
        if (!scene.TerminusComponent.AddTerminus(request.Terminus))
        {
            response.ErrorCode = InnerErrorCode.ErrRoamingDisposed;
            return;
        }
        
        try
        {
            // 执行Terminus的传送完成逻辑。
            response.ErrorCode = await request.Terminus.TransferComplete(scene);

            if (response.ErrorCode != 0)
            {
                // 解锁失败时回滚目标端，源端收到错误后会恢复原来的路由。
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