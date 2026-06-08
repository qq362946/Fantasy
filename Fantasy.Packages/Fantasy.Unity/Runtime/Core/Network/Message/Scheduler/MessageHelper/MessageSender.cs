using System;
using System.Runtime.CompilerServices;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network.Interface;

#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy.Scheduler
{
    /// <summary>
    /// 网络消息发送者的类。
    /// </summary>
    public struct MessageSender : IDisposable
    {
        /// <summary>
        /// 获取或设置协议编号
        /// </summary>
        public uint ProtocolCode { get; private set; }
        
        /// <summary>
        /// 获取或设置 RPC ID。
        /// </summary>
        public uint RpcId { get; private set; }
        /// <summary>
        /// 获取或设置创建时间。
        /// </summary>
        public long CreateTime { get; private set; }
        /// <summary>
        /// 获取或设置消息类型。
        /// </summary>
        public Type MessageType { get; private set; }
       
        /// <summary>
        /// 获取或设置任务。
        /// </summary>
        public FTask<IResponse> Tcs { get; private set; }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            RpcId = 0;
            CreateTime = 0;
            ProtocolCode = 0;
            Tcs = null;
            MessageType = null;
        }

        /// <summary>
        /// 创建一个 <see cref="MessageSender"/> 实例。
        /// </summary>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="protocolCode">协议编号。</param>
        /// <param name="requestType">请求消息类型。</param>
        /// <param name="tcs">任务。</param>
        /// <returns>创建的 <see cref="MessageSender"/> 实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageSender Create(uint rpcId, uint protocolCode, Type requestType, FTask<IResponse> tcs)
        {
            var routeMessageSender = new MessageSender();
            routeMessageSender.Tcs = tcs;
            routeMessageSender.RpcId = rpcId;
            routeMessageSender.ProtocolCode = protocolCode;
            routeMessageSender.MessageType = requestType;
            routeMessageSender.CreateTime = TimeHelper.Now;
            return routeMessageSender;
        }

        /// <summary>
        /// 创建一个 <see cref="MessageSender"/> 实例。
        /// </summary>
        /// <param name="rpcId">RPC ID。</param>
        /// <param name="protocolCode">协议编号。</param>
        /// <param name="tcs">任务。</param>
        /// <returns>创建的 <see cref="MessageSender"/> 实例。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MessageSender Create<T>(uint rpcId, uint protocolCode, FTask<IResponse> tcs) where T : IAddressMessage
        {
            var routeMessageSender = new MessageSender();
            routeMessageSender.Tcs = tcs;
            routeMessageSender.RpcId = rpcId;
            routeMessageSender.MessageType = typeof(T);
            routeMessageSender.ProtocolCode = protocolCode;
            routeMessageSender.CreateTime = TimeHelper.Now;
            return routeMessageSender;
        }

        // /// <summary>
        // /// 创建一个 <see cref="MessageSender"/> 实例。
        // /// </summary>
        // /// <param name="rpcId">RPC ID。</param>
        // /// <param name="address">Address。</param>
        // /// <param name="protocolCode">协议编号。</param>
        // /// <param name="request">路由消息请求。</param>
        // /// <param name="tcs">任务。</param>
        // /// <returns>创建的 <see cref="MessageSender"/> 实例。</returns>
        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static MessageSender Create(uint rpcId, long address, uint protocolCode, IAddressMessage request, FTask<IResponse> tcs)
        // {
        //     var routeMessageSender = new MessageSender();
        //     routeMessageSender.Tcs = tcs;
        //     routeMessageSender.RpcId = rpcId;
        //     routeMessageSender.Address = address;
        //     routeMessageSender.ProtocolCode = protocolCode;
        //     routeMessageSender.CreateTime = TimeHelper.Now;
        //     return routeMessageSender;
        // }
    }
}