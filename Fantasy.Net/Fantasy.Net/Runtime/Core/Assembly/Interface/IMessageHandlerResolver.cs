using System;
using System.Collections.Generic;
using Fantasy.DataStructure.Dictionary;
using Fantasy.Entitas;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Async;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 消息分发器注册器接口
    /// 由 Source Generator 自动生成实现类，用于在程序集加载时注册网络消息处理器
    /// </summary>
    public interface IMessageHandlerResolver
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int GetMessageHandlerCount();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="rpcId"></param>
        /// <param name="protocolCode"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool MessageHandler(Session session, uint rpcId, uint protocolCode, object message);
#if FANTASY_NET
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int GetRouteMessageHandlerCount();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="entity"></param>
        /// <param name="rpcId"></param>
        /// <param name="protocolCode"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        FTask<bool> RouteMessageHandler(Session session, Entity entity, uint rpcId, uint protocolCode, object message);
#endif
    }
}