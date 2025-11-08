#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.InnerMessage;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Fantasy.Network.Roaming;

/// <summary>
/// 漫游实体
/// </summary>
public sealed class Roaming : Entity
{
    /// <summary>
    /// 连接到漫游TerminusId。
    /// 也可以理解为目标实体的RouteId。
    /// </summary>
    internal long TerminusId;
    /// <summary>
    /// 漫游目标Scene的RouteId。
    /// </summary>
    public long TargetSceneRouteId { get; internal set; }
    /// <summary>
    /// 漫游转发Session的RouteId。
    /// </summary>
    public long ForwardSessionRouteId { get; internal set; }
    /// <summary>
    /// 当前漫游类型。
    /// </summary>
    public int RoamingType { get; internal set; }
    /// <summary>
    /// 协程锁。
    /// </summary>
    internal CoroutineLock RoamingLock;
    /// <summary>
    /// 当前正在执行的协程锁。
    /// </summary>
    private WaitCoroutineLock? _waitCoroutineLock;
    /// <summary>
    /// 关联Session的漫游组件。
    /// </summary>
    internal SessionRoamingComponent SessionRoamingComponent;
    /// <summary>
    /// 获得当前漫游对应终端的TerminusId。
    /// </summary>
    /// <returns></returns>
    internal async FTask<long> GetTerminusId()
    {
        using (await RoamingLock.Wait(RoamingType,"Roaming.cs GetTerminusId"))
        {
            return TerminusId;
        }
    }
    /// <summary>
    /// 设置当前漫游对应的终端的TerminusId。
    /// </summary>
    /// <param name="terminusId"></param>
    internal async FTask SetTerminusId(long terminusId)
    {
        using (await RoamingLock.Wait(RoamingType,"Roaming.cs SetTerminusId"))
        {
            TerminusId = terminusId;
        }
    }
    /// <summary>
    /// 锁定TerminusId。
    /// </summary>
    internal async FTask LockTerminusId()
    {
        _waitCoroutineLock = await RoamingLock.Wait(RoamingType,"Roaming.cs LockTerminusId");
    }

    /// <summary>
    /// 解锁TerminusId。
    /// </summary>
    /// <param name="terminusId"></param>
    /// <param name="targetSceneRouteId"></param>
    internal void UnLockTerminusId(long terminusId, long targetSceneRouteId)
    {
        if (_waitCoroutineLock == null)
        {
            Log.Error("terminusId unlock waitCoroutineLock is null");
            return;
        }

        TerminusId = terminusId;
        TargetSceneRouteId = targetSceneRouteId;
        _waitCoroutineLock.Dispose();
        _waitCoroutineLock = null;
    }

    /// <summary>
    /// 断开当前漫游的连接。
    /// </summary>
    /// <returns></returns>
    public async FTask<uint> Disconnect()
    {
        var response =
            await Scene.NetworkMessagingComponent.CallInnerRoute(TargetSceneRouteId, new I_UnLinkRoamingRequest()
            {
                RoamingId = SessionRoamingComponent.Id
            });
        return response.ErrorCode;
    }
    /// <summary>
    /// 销毁方法
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
        
        TerminusId = 0;
        TargetSceneRouteId = 0;
        ForwardSessionRouteId = 0;
        
        RoamingLock = null;
        SessionRoamingComponent = null;
        base.Dispose();
    }
}
#endif