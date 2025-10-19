using System;
using System.Collections.Generic;
using Fantasy.Entitas.Interface;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 实体系统注册器接口
    /// 由 Source Generator 自动生成实现类，用于在程序集加载时注册实体系统
    /// </summary>
    public interface IEntitySystemRegistrar : IDisposable
    {
#if FANTASY_NET
        /// <summary>
        /// 注册该程序集中的所有实体系统
        /// </summary>
        /// <param name="awakeSystems">Awake 系统容器</param>
        /// <param name="updateSystems">Update 系统容器</param>
        /// <param name="destroySystems">Destroy 系统容器</param>
        /// <param name="deserializeSystems">Deserialize 系统容器</param>
        void RegisterSystems(
            Dictionary<Type, IAwakeSystem> awakeSystems,
            Dictionary<Type, IUpdateSystem> updateSystems,
            Dictionary<Type, IDestroySystem> destroySystems,
            Dictionary<Type, IDeserializeSystem> deserializeSystems);

        /// <summary>
        /// 取消注册该程序集中的所有实体系统（热重载卸载时调用）
        /// </summary>
        /// <param name="awakeSystems">Awake 系统容器</param>
        /// <param name="updateSystems">Update 系统容器</param>
        /// <param name="destroySystems">Destroy 系统容器</param>
        /// <param name="deserializeSystems">Deserialize 系统容器</param>
        void UnRegisterSystems(
            Dictionary<Type, IAwakeSystem> awakeSystems,
            Dictionary<Type, IUpdateSystem> updateSystems,
            Dictionary<Type, IDestroySystem> destroySystems,
            Dictionary<Type, IDeserializeSystem> deserializeSystems);
#endif
#if FANTASY_UNITY
        /// <summary>
        /// 注册该程序集中的所有实体系统
        /// </summary>
        /// <param name="awakeSystems">Awake 系统容器</param>
        /// <param name="updateSystems">Update 系统容器</param>
        /// <param name="destroySystems">Destroy 系统容器</param>
        /// <param name="deserializeSystems">Deserialize 系统容器</param>
        /// <param name="lateUpdateSystems">LateUpdate 系统容器</param>
        void RegisterSystems(
            Dictionary<Type, IAwakeSystem> awakeSystems,
            Dictionary<Type, IUpdateSystem> updateSystems,
            Dictionary<Type, IDestroySystem> destroySystems,
            Dictionary<Type, IDeserializeSystem> deserializeSystems,
            Dictionary<Type, ILateUpdateSystem> lateUpdateSystems);

        /// <summary>
        /// 取消注册该程序集中的所有实体系统（热重载卸载时调用）
        /// </summary>
        /// <param name="awakeSystems">Awake 系统容器</param>
        /// <param name="updateSystems">Update 系统容器</param>
        /// <param name="destroySystems">Destroy 系统容器</param>
        /// <param name="deserializeSystems">Deserialize 系统容器</param>
        /// <param name="lateUpdateSystems">LateUpdate 系统容器</param>
        void UnRegisterSystems(
            Dictionary<Type, IAwakeSystem> awakeSystems,
            Dictionary<Type, IUpdateSystem> updateSystems,
            Dictionary<Type, IDestroySystem> destroySystems,
            Dictionary<Type, IDeserializeSystem> deserializeSystems,
            Dictionary<Type, ILateUpdateSystem> lateUpdateSystems);
#endif
    }
}