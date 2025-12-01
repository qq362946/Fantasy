#if FANTASY_NET
using System.Collections.Generic;
using Fantasy.Async;
using Fantasy.Entitas;
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy.Network.Roaming;

/// <summary>
/// 当Terminus创建完成后发送的事件参数
/// </summary>
public struct OnCreateTerminus
{
    /// <summary>
    /// 获取与事件关联的场景实体。
    /// </summary>
    public readonly Scene Scene;
    /// <summary>
    /// 获取与事件关联的Terminus。
    /// </summary>
    public readonly Terminus Terminus;
    /// <summary>
    /// 初始化一个新的 OnCreateTerminus 实例。
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="terminus"></param>
    public OnCreateTerminus(Scene scene, Terminus terminus)
    {
        Scene = scene;
        Terminus = terminus;
    }
}

/// <summary>
/// 漫游终端管理组件。
/// 这个组件不需要手动挂载，会在Scene启动的时候自动挂载这个组件。
/// </summary>
public sealed class TerminusComponent : Entity
{
    /// <summary>
    /// 漫游终端的实体集合。
    /// </summary>
    private readonly Dictionary<long, Terminus> _terminals = new();

    /// <summary>
    /// Dispose
    /// </summary>
    public override void Dispose()
    {
        foreach (var (_, terminus) in _terminals)
        {
            terminus.Dispose();
        }

        _terminals.Clear();
        base.Dispose();
    }

    /// <summary>
    /// 创建一个新的漫游终端。
    /// </summary>
    /// <param name="roamingId"></param>
    /// <param name="roamingType"></param>
    /// <param name="forwardSessionAddress"></param>
    /// <param name="forwardSceneAddress"></param>
    /// <returns></returns>
    internal (uint, Terminus) Create(long roamingId, int roamingType, long forwardSessionAddress, long forwardSceneAddress)
    {
        if (_terminals.ContainsKey(roamingId))
        {
            return (InnerErrorCode.ErrAddRoamingTerminalAlreadyExists, null);
        }

        var terminus = roamingId == 0
            ? Entity.Create<Terminus>(Scene, false, true)
            : Entity.Create<Terminus>(Scene, roamingId, false, true);
        terminus.IsDisposeTerminus = false;
        terminus.RoamingType = roamingType;
        terminus.TerminusId = terminus.RuntimeId;
        terminus.ForwardSceneAddress = forwardSceneAddress;
        terminus.ForwardSessionAddress = forwardSessionAddress;
        terminus.RoamingMessageLock = Scene.CoroutineLockComponent.Create(terminus.Type.TypeHandle.Value.ToInt64());
        _terminals.Add(terminus.Id, terminus);
        return (0U, terminus);
    }

    /// <summary>
    /// 添加一个漫游终端。
    /// </summary>
    /// <param name="terminus"></param>
    internal void AddTerminus(Terminus terminus)
    {
        _terminals.Add(terminus.Id, terminus);
    }

    /// <summary>
    /// 根据roamingId获取一个漫游终端。
    /// </summary>
    /// <param name="roamingId"></param>
    /// <param name="terminus"></param>
    /// <returns></returns>
    internal bool TryGetTerminus(long roamingId, out Terminus terminus)
    {
        return _terminals.TryGetValue(roamingId, out terminus);
    }
    
    /// <summary>
    /// 根据roamingId获取一个漫游终端。
    /// </summary>
    /// <param name="roamingId"></param>
    /// <returns></returns>
    internal Terminus GetTerminus(long roamingId)
    {
        return _terminals[roamingId];
    }

    /// <summary>
    /// 根据roamingId移除一个漫游终端。
    /// </summary>
    /// <param name="roamingId"></param>
    /// <param name="isDispose"></param>
    internal void RemoveTerminus(long roamingId, bool isDispose)
    {
        if (!_terminals.Remove(roamingId, out var terminus))
        {
            return;
        }

        if (isDispose)
        {
            terminus.Dispose();
        }
    }
}
#endif
