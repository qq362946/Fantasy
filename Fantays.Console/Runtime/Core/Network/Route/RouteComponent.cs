using Fantasy.Entitas;

#if FANTASY_NET
namespace Fantasy.Network;

/// <summary>
/// 自定义Route组件、如果要自定义Route协议必须使用这个组件
/// </summary>
public sealed class RouteComponent : Entity
{
    /// <summary>
    /// 存储路由类型和路由ID的映射关系。
    /// </summary>
    public readonly Dictionary<long, long> RouteAddress = new Dictionary<long, long>();

    /// <summary>
    /// 添加路由类型和路由ID的映射关系。
    /// </summary>
    /// <param name="routeType">路由类型。</param>
    /// <param name="routeId">路由ID。</param>
    public void AddAddress(long routeType, long routeId)
    {
        RouteAddress.Add(routeType, routeId);
    }

    /// <summary>
    /// 移除指定路由类型的映射关系。
    /// </summary>
    /// <param name="routeType">路由类型。</param>
    public void RemoveAddress(long routeType)
    {
        RouteAddress.Remove(routeType);
    }

    /// <summary>
    /// 获取指定路由类型的路由ID。
    /// </summary>
    /// <param name="routeType">路由类型。</param>
    /// <returns>路由ID。</returns>
    public long GetRouteId(long routeType)
    {
        return RouteAddress.GetValueOrDefault(routeType, 0);
    }

    /// <summary>
    /// 尝试获取指定路由类型的路由ID。
    /// </summary>
    /// <param name="routeType">路由类型。</param>
    /// <param name="routeId">输出的路由ID。</param>
    /// <returns>如果获取成功返回true，否则返回false。</returns>
    public bool TryGetRouteId(long routeType, out long routeId)
    {
        return RouteAddress.TryGetValue(routeType, out routeId);
    }

    /// <summary>
    /// 释放组件资源，清空映射关系。
    /// </summary>
    public override void Dispose()
    {
        RouteAddress.Clear();
        base.Dispose();
    }
}
#endif