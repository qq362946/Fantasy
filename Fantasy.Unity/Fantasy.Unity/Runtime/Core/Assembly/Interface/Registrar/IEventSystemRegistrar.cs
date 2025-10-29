using System;
using Fantasy.DataStructure.Collection;
using Fantasy.Event;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 事件系统注册器接口
    /// 由 Source Generator 自动生成实现类，用于在程序集加载时注册事件系统
    /// </summary>
    public interface IEventSystemRegistrar
    {
        /// <summary>
        /// 注册该程序集中的所有事件系统
        /// </summary>
        /// <param name="events">同步事件容器</param>
        /// <param name="asyncEvents">异步事件容器</param>
        void Register(
            OneToManyList<RuntimeTypeHandle, IEvent> events,
            OneToManyList<RuntimeTypeHandle, IEvent> asyncEvents);

        /// <summary>
        /// 取消注册该程序集中的所有事件系统（热重载卸载时调用）
        /// </summary>
        /// <param name="events">同步事件容器</param>
        /// <param name="asyncEvents">异步事件容器</param>
        void UnRegister(
            OneToManyList<RuntimeTypeHandle, IEvent> events,
            OneToManyList<RuntimeTypeHandle, IEvent> asyncEvents);
    }
}