#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.InnerMessage;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy.Network.Roaming;

/// <summary>
/// 表示源端一个 roamingType 到目标 Terminus 的连接和路由状态。
/// </summary>
public sealed class Roaming : Entity
{
    /// <summary>
    /// 当前目标实体地址；传送期间会暂时置为 0，并在锁内更新。
    /// </summary>
    internal long TerminusId;
    /// <summary>
    /// Terminus 当前所在的目标 Scene 地址。
    /// </summary>
    public long TargetSceneAddress { get; internal set; }
    /// <summary>
    /// 接收 Terminus 转发消息的 Session 地址。
    /// </summary>
    public long ForwardSessionAddress { get; internal set; }
    /// <summary>
    /// 当前连接对应的漫游类型。
    /// </summary>
    public int RoamingType { get; internal set; }
    /// <summary>
    /// 保护当前 roamingType 的 TerminusId 迁移过程。
    /// </summary>
    internal CoroutineLock RoamingLock;
    /// <summary>
    /// 跨远程 Lock/UnLock 请求持有的锁句柄。
    /// </summary>
    private WaitCoroutineLock? _waitCoroutineLock;
    /// <summary>
    /// 所属的源端漫游上下文。
    /// </summary>
    internal SessionRoamingComponent? SessionRoamingComponent;
    /// <summary>
    /// 在迁移锁内读取当前 TerminusId。
    /// </summary>
    /// <returns>当前可路由的 TerminusId；迁移中可能为 0。</returns>
    internal async FTask<long> GetTerminusId()
    {
        using (await RoamingLock.Wait(RoamingType,"Roaming.cs GetTerminusId"))
        {
            return TerminusId;
        }
    }
    /// <summary>
    /// 在迁移锁内更新当前 TerminusId。
    /// </summary>
    /// <param name="terminusId">新的目标实体地址。</param>
    internal async FTask SetTerminusId(long terminusId)
    {
        using (await RoamingLock.Wait(RoamingType,"Roaming.cs SetTerminusId"))
        {
            TerminusId = terminusId;
        }
    }
    /// <summary>
    /// 锁定 TerminusId，直到目标端通过 <see cref="UnLockTerminusId"/> 提交新地址。
    /// </summary>
    internal async FTask LockTerminusId()
    {
        _waitCoroutineLock = await RoamingLock.Wait(RoamingType,"Roaming.cs LockTerminusId");
    }

    /// <summary>
    /// 原子更新 TerminusId 和目标 Scene 地址后释放迁移锁。
    /// </summary>
    /// <param name="terminusId">迁移完成后的目标实体地址。</param>
    /// <param name="targetSceneAddress">迁移完成后的目标 Scene 地址。</param>
    internal void UnLockTerminusId(long terminusId, long targetSceneAddress)
    {
        if (_waitCoroutineLock == null)
        {
            Log.Error("terminusId unlock waitCoroutineLock is null");
            return;
        }

        TerminusId = terminusId;
        TargetSceneAddress = targetSceneAddress;
        _waitCoroutineLock.Dispose();
        _waitCoroutineLock = null;
    }

    /// <summary>
    /// 通知目标 Terminus 切换到新的转发 Session，并恢复转发。
    /// </summary>
    /// <param name="forwardSessionAddress">重连后的 Session 地址。</param>
    internal async FTask SetForwardSessionAddress(long forwardSessionAddress)
    {
        using var response = await Scene.NetworkMessagingComponent.Call(
            TargetSceneAddress,
            new I_SetForwardSessionAddressRequest()
            {
                RoamingId = SessionRoamingComponent!.Id,
                ForwardSessionAddress = forwardSessionAddress,
                OwnerRoamingRuntimeId = SessionRoamingComponent.RuntimeId
            });

        if (response.ErrorCode == 0)
        {
            ForwardSessionAddress = forwardSessionAddress;
            return;
        }

        Log.Warning($"SetForwardSessionAddress failed with ErrorCode: {response.ErrorCode}, RoamingId: {SessionRoamingComponent!.Id}, TargetSceneAddress: {TargetSceneAddress}");
    }

    /// <summary>
    /// 通知目标 Terminus 暂停向已断开的 Session 转发消息。
    /// </summary>
    /// <remarks>只暂停转发，不销毁目标 Terminus，便于客户端在保活期限内重连。</remarks>
    internal async FTask StopForwarding()
    {
        using var response = await Scene.NetworkMessagingComponent.Call(
            TargetSceneAddress,
            new I_StopForwardingRequest()
            {
                RoamingId = SessionRoamingComponent!.Id,
                OwnerRoamingRuntimeId = SessionRoamingComponent.RuntimeId
            });

        if (response.ErrorCode == 0)
        {
            return;
        }

        Log.Warning($"StopForwarding failed with ErrorCode: {response.ErrorCode}, RoamingId: {SessionRoamingComponent!.Id}, TargetSceneAddress: {TargetSceneAddress}");
    }

    /// <summary>
    /// 请求目标 Scene 断开并销毁当前 Terminus。
    /// </summary>
    /// <returns>目标端返回的错误码，0 表示成功。</returns>
    internal async FTask<uint> Disconnect()
    {
        var sessionRoamingComponent = SessionRoamingComponent!;

        using var response =
            await Scene.NetworkMessagingComponent.Call(
                TargetSceneAddress,
                new I_UnLinkRoamingRequest()
                {
                    RoamingId = SessionRoamingComponent!.Id,
                    DisposeRoaming = true,
                    OwnerRoamingRuntimeId = sessionRoamingComponent.RuntimeId
                });
        
        return response.ErrorCode;
    }

    /// <summary>
    /// 释放迁移锁并从所属漫游上下文中摘除当前连接。
    /// </summary>
    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_waitCoroutineLock != null)
        {
            _waitCoroutineLock.Dispose();
            _waitCoroutineLock = null;
        }

        if (SessionRoamingComponent != null)
        {
            SessionRoamingComponent.Remove(RoamingType, false);
            SessionRoamingComponent = null;
        }

        TerminusId = 0;
        RoamingType = 0;
        TargetSceneAddress = 0;
        ForwardSessionAddress = 0;

        RoamingLock = null;
        base.Dispose();
    }
}
#endif
