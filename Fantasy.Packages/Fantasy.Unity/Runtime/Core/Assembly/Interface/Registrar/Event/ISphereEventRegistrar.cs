#if FANTASY_NET
using System;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Sphere;

namespace Fantasy.Assembly;

/// <summary>
/// Sphere事件注册器接口
/// 用于在程序集中注册和注销Sphere事件处理器
/// 由源代码生成器自动生成实现类
/// </summary>
public interface ISphereEventRegistrar
{
    /// <summary>
    /// 注册当前程序集中的所有Sphere事件处理器
    /// </summary>
    /// <param name="sphereEvents">Sphere事件集合，用于存储事件ID到事件处理器的映射关系</param>
    void Register(OneToManyHashSet<long, Func<Scene, SphereEventArgs, FTask>> sphereEvents);

    /// <summary>
    /// 注销当前程序集中的所有Sphere事件处理器
    /// </summary>
    /// <param name="sphereEvents">Sphere事件集合，用于移除事件ID到事件处理器的映射关系</param>
    void UnRegister(OneToManyHashSet<long, Func<Scene, SphereEventArgs, FTask>> sphereEvents);
}
#endif