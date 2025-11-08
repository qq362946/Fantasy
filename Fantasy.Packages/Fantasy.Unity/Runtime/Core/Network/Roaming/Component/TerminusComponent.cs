#if FANTASY_NET
using Fantasy.Async;
using Fantasy.DataStructure.Dictionary;
using Fantasy.Entitas;
using Fantasy.Network;
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
    /// <param name="forwardSessionRouteId"></param>
    /// <param name="forwardSceneRouteId"></param>
    /// <returns></returns>
    public async FTask<(uint, Terminus)> Create(long roamingId, int roamingType, long forwardSessionRouteId, long forwardSceneRouteId)
    {
        if (_terminals.ContainsKey(roamingId))
        {
            return (InnerErrorCode.ErrAddRoamingTerminalAlreadyExists, null);
        }

        var terminus = roamingId == 0
            ? Entity.Create<Terminus>(Scene, false, true)
            : Entity.Create<Terminus>(Scene, roamingId, false, true);
        terminus.RoamingType = roamingType;
        terminus.TerminusId = terminus.RuntimeId;
        terminus.ForwardSceneRouteId = forwardSceneRouteId;
        terminus.ForwardSessionRouteId = forwardSessionRouteId;
        terminus.RoamingMessageLock = Scene.CoroutineLockComponent.Create(terminus.Type.TypeHandle.Value.ToInt64());
        await Scene.EventComponent.PublishAsync(new OnCreateTerminus(Scene, terminus));
        _terminals.Add(terminus.Id, terminus);
        return (0U, terminus);
    }

    /// <summary>
    /// 添加一个漫游终端。
    /// </summary>
    /// <param name="terminus"></param>
    public void AddTerminus(Terminus terminus)
    {
        _terminals.Add(terminus.Id, terminus);
    }

    /// <summary>
    /// 根据roamingId获取一个漫游终端。
    /// </summary>
    /// <param name="roamingId"></param>
    /// <param name="terminus"></param>
    /// <returns></returns>
    public bool TryGetTerminus(long roamingId, out Terminus terminus)
    {
        return _terminals.TryGetValue(roamingId, out terminus);
    }
    
    /// <summary>
    /// 根据roamingId获取一个漫游终端。
    /// </summary>
    /// <param name="roamingId"></param>
    /// <returns></returns>
    public Terminus GetTerminus(long roamingId)
    {
        return _terminals[roamingId];
    }

    /// <summary>
    /// 根据roamingId移除一个漫游终端。
    /// </summary>
    /// <param name="roamingId"></param>
    /// <param name="isDispose"></param>
    public void RemoveTerminus(long roamingId, bool isDispose = true)
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
