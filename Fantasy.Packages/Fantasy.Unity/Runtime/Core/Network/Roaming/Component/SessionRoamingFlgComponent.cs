#if FANTASY_NET
using Fantasy.Async;
using Fantasy.Entitas;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
namespace Fantasy.Network.Roaming;

/// <summary>
/// 挂载在 Session 上的生命周期标记，在 Session 释放时触发对应漫游上下文的延迟销毁。
/// </summary>
/// <remarks>
/// 漫游上下文本身不属于 Session；该标记只负责把 Session 的销毁事件转交给 <see cref="RoamingComponent"/>。
/// </remarks>
internal sealed class SessionRoamingFlgComponent : Entity
{
    /// <summary>Session 释放后保留漫游上下文的毫秒数。</summary>
    public int DelayRemove;
    /// <summary>创建本标记时 Session 的 RuntimeId，用于阻止旧 Session 的回调误删重连后的连接。</summary>
    public long OwnerSessionRuntimeId;

    /// <summary>主动换绑时跳过自动移除，避免移除标记产生递归清理。</summary>
    public bool DoNotRemove;
    private bool _isInnerDisposed;

    /// <summary>与当前 Session 绑定的漫游上下文。</summary>
    public EntityReference<SessionRoamingComponent> SessionRoamingComponent;

    /// <summary>
    /// 根据当前延迟策略请求移除漫游上下文，然后释放标记。
    /// </summary>
    public override void Dispose()
    {
        if (IsDisposed || _isInnerDisposed)
        {
            return;
        }

        _isInnerDisposed = true;
        DisposeAsync().Coroutine();
    }

    private async FTask DisposeAsync()
    {
        try
        {
            SessionRoamingComponent sessionRoamingComponent =  SessionRoamingComponent;

            if (sessionRoamingComponent == null || DoNotRemove)
            {
                return;
            }

            // owner 校验保证该异步清理只对创建本标记的 Session 代数生效。
            await Scene.RoamingComponent.Remove(Id, DelayRemove, OwnerSessionRuntimeId);
        }
        finally
        {
            DelayRemove = 0;
            OwnerSessionRuntimeId = 0;

            DoNotRemove = false;
            _isInnerDisposed = false;

            base.Dispose();
        }
    }
}
#endif
