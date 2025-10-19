using System;
using System.Collections.Generic;
using Fantasy.DataStructure.Dictionary;
using Fantasy.Network.Interface;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 消息分发器注册器接口
    /// 由 Source Generator 自动生成实现类，用于在程序集加载时注册网络消息处理器
    /// </summary>
    public interface IMessageDispatcherRegistrar : IDisposable
    {
#if FANTASY_NET
        /// <summary>
        /// 注册该程序集中的所有消息分发系统（服务器端版本）
        /// </summary>
        /// <param name="networkProtocols">网络协议映射表（OpCode ↔ 消息类型）</param>
        /// <param name="responseTypes">响应类型映射表（请求类型 → 响应类型）</param>
        /// <param name="messageHandlers">消息处理器集合</param>
        /// <param name="customRouteMap">自定义路由映射表</param>
        /// <param name="routeMessageHandlers">路由消息处理器集合</param>
        void RegisterSystems(
            DoubleMapDictionary<uint, Type> networkProtocols,
            Dictionary<Type, Type> responseTypes,
            Dictionary<Type, IMessageHandler> messageHandlers,
            Dictionary<long, int> customRouteMap,
            Dictionary<Type, IRouteMessageHandler> routeMessageHandlers);
        /// <summary>
        /// 取消注册该程序集中的所有消息分发系统（热重载卸载时调用）
        /// </summary>
        /// <param name="networkProtocols">网络协议映射表（OpCode ↔ 消息类型）</param>
        /// <param name="responseTypes">响应类型映射表（请求类型 → 响应类型）</param>
        /// <param name="messageHandlers">消息处理器集合</param>
        /// <param name="customRouteMap">自定义路由映射表</param>
        /// <param name="routeMessageHandlers">路由消息处理器集合</param>
        void UnRegisterSystems(
            DoubleMapDictionary<uint, Type> networkProtocols,
            Dictionary<Type, Type> responseTypes,
            Dictionary<Type, IMessageHandler> messageHandlers,
            Dictionary<long, int> customRouteMap,
            Dictionary<Type, IRouteMessageHandler> routeMessageHandlers);
#endif
#if FANTASY_UNITY
        /// <summary>
        /// 注册该程序集中的所有消息分发系统（Unity 客户端版本）
        /// </summary>
        /// <param name="networkProtocols">网络协议映射表（OpCode ↔ 消息类型）</param>
        /// <param name="responseTypes">响应类型映射表（请求类型 → 响应类型）</param>
        /// <param name="messageHandlers">消息处理器集合</param>
        void RegisterSystems(
            DoubleMapDictionary<uint, Type> networkProtocols,
            Dictionary<Type, Type> responseTypes,
            Dictionary<Type, IMessageHandler> messageHandlers);
        /// <summary>
        /// 取消注册该程序集中的所有消息分发系统（热重载卸载时调用）
        /// </summary>
        /// <param name="networkProtocols">网络协议映射表（OpCode ↔ 消息类型）</param>
        /// <param name="responseTypes">响应类型映射表（请求类型 → 响应类型）</param>
        /// <param name="messageHandlers">消息处理器集合</param>
        void UnRegisterSystems(
            DoubleMapDictionary<uint, Type> networkProtocols,
            Dictionary<Type, Type> responseTypes,
            Dictionary<Type, IMessageHandler> messageHandlers);
#endif
    }
}