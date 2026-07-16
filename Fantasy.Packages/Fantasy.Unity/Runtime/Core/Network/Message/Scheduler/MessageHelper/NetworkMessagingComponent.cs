#if FANTASY_NET
using System;
using System.Collections.Generic;
using System.Linq;
using Fantasy.Entitas;
using System.Runtime.CompilerServices;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.IdFactory;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Network.Route;
using Fantasy.PacketParser;
using Fantasy.PacketParser.Interface;
using Fantasy.Serialize;
using Fantasy.Timer;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy.Scheduler
{
    public struct NetworkMessageUpdate
    {
        public NetworkMessagingComponent NetworkMessagingComponent;
    }
    
    public class NetworkMessagingComponentAwakeSystem : AwakeSystem<NetworkMessagingComponent>
    {
        protected override void Awake(NetworkMessagingComponent self)
        {
            var selfScene = self.Scene;
            self.TimerComponent = selfScene.TimerComponent;
            self.MessageDispatcherComponent = selfScene.MessageDispatcherComponent;
            self.AddressableRouteMessageLock = selfScene.CoroutineLockComponent.Create(self.GetType().TypeHandle.Value.ToInt64());

            self.TimerId = self.TimerComponent.Net.RepeatedTimer(10000, new NetworkMessageUpdate()
            {
                NetworkMessagingComponent = self
            });
        }
    }
    
    public class NetworkMessagingComponentDestroySystem : DestroySystem<NetworkMessagingComponent>
    {
        protected override void Destroy(NetworkMessagingComponent self)
        {
            if (self.TimerId != 0)
            {
                self.TimerComponent.Net.Remove(ref self.TimerId);
            }
            
            foreach (var (rpcId, messageSender) in self.RequestCallback.ToDictionary())
            {
                self.ReturnMessageSender(rpcId, messageSender);
            }

            // 组件销毁时释放所有尚未发送的序列化缓冲区。
            self.ClearPendingRouteMessages();
            // 必须先释放队列消息，再销毁队列专用缓冲区池。
            self.MemoryStreamBufferPool.Dispose();
            
            self.AddressableRouteMessageLock.Dispose();
            
            self.RequestCallback.Clear();
            self.TimeoutRouteMessageSenders.Clear();
            self.TimerComponent = null;
            self.MessageDispatcherComponent = null;
            self.AddressableRouteMessageLock = null;
        }
    }
    public sealed class NetworkMessagingComponent : Entity
    {
        public long TimerId;
        private uint _rpcId;
        public CoroutineLock AddressableRouteMessageLock;
        public TimerComponent TimerComponent;
        public MessageDispatcherComponent MessageDispatcherComponent;
        public readonly SortedDictionary<uint, MessageSender> RequestCallback = new();
        public readonly Dictionary<uint, MessageSender> TimeoutRouteMessageSenders = new();
        public readonly MemoryStreamBufferPool MemoryStreamBufferPool = new MemoryStreamBufferPool(4096);
        private static readonly APacketParser PacketParser = PacketParserFactory.CreatePacketParser(NetworkTarget.Inner);

        /// <summary>
        /// 单个目标 Scene 最多允许缓存的待发送消息数量。
        /// 防止 Control Center 不可用时消息无限堆积。
        /// </summary>
        private const int MaxPendingMessagesPerScene = 1024;
        
        /// <summary>
        /// 按目标 SceneId 保存路由缺失期间的待发送消息。
        /// NetworkMessagingComponent 运行于 Scene 线程，因此不需要并发容器。
        /// </summary>
        private readonly Dictionary<uint, PendingRoute> _pendingRoutes = new();

        /// <summary>
        /// 一个目标 Scene 对应的待发送队列。
        /// </summary>
        private sealed class PendingRoute
        {
            public readonly Queue<PendingSend> Messages = new();
        }
        
        /// <summary>
        /// 批量发送的共享序列化缓冲区。
        /// 初始引用表示 Send 方法自身正在使用；
        /// 每个进入等待队列的 Address 增加一个引用。
        /// </summary>
        private sealed class PendingBatchBuffer(MemoryStreamBuffer buffer)
        {
            public readonly MemoryStreamBuffer Buffer = buffer;

            private int _referenceCount = 1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Retain()
            {
                ++_referenceCount;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Release()
            {
                return --_referenceCount == 0;
            }
        }
        
        /// <summary>
        /// 等待路由解析后发送的消息。
        /// </summary>
        private readonly struct PendingSend
        {
            public readonly long Address;
            public readonly uint ProtocolCode;
            public readonly Type MessageType;
            public readonly MemoryStreamBuffer Buffer;
            public readonly APackInfo PackInfo;
            public readonly PendingBatchBuffer BatchBuffer;

            /// <summary>
            /// 单地址普通序列化消息。
            /// Buffer 由该 PendingSend 独占。
            /// </summary>
            public PendingSend(long address, uint protocolCode, Type messageType, MemoryStreamBuffer buffer)
            {
                Address = address;
                ProtocolCode = protocolCode;
                MessageType = messageType;
                Buffer = buffer;
                PackInfo = null;
                BatchBuffer = null;
            }

            /// <summary>
            /// 批量发送中的一个目标地址。
            /// 多个 PendingSend 共享同一个 BatchBuffer。
            /// </summary>
            public PendingSend(long address, uint protocolCode, Type messageType, PendingBatchBuffer batchBuffer)
            {
                Address = address;
                ProtocolCode = protocolCode;
                MessageType = messageType;
                Buffer = batchBuffer.Buffer;
                PackInfo = null;
                BatchBuffer = batchBuffer;
            }

            /// <summary>
            /// 已经接收到的原始消息包。
            /// 不复制 MemoryStreamBuffer。
            /// </summary>
            public PendingSend(long address, Type messageType, APackInfo packInfo)
            {
                Address = address;
                ProtocolCode = packInfo.ProtocolCode;
                MessageType = messageType;
                Buffer = null;
                PackInfo = packInfo;
                BatchBuffer = null;
            }
        }
        
        public void Send<T>(long address, T message) where T : IAddressMessage
        {
            if (address == 0)
            {
                Log.Error($"Send appId == 0");
                return;
            }
            
            // 高频路径：Session 已存在时与原来的发送流程一致。
            if (Scene.TryGetSession(address, out var session))
            {
                session.Send(message, 0, address);
                return;
            }
            
            // 服务发现未启用，并且本地连接及配置路由均不存在。
            // 调用 GetSession 是为了保持旧模式统一的异常信息。
            if (!ServiceDiscovery.IsEnabled)
            {
                Scene.GetSession(address).Send(message, 0, address);
                return;
            }
            
            var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(address);
            var protocolCode = message.OpCode();
            var rpcId = 0U;
            var routeAddress = 0L;
            var buffer = MemoryStreamBufferPool.RentMemoryStream(MemoryStreamBufferSource.MultiPack);

            try
            {
                PacketParser.PackMemoryStream(ref rpcId, ref routeAddress, message, typeof(T), buffer);
            }
            catch
            {
                MemoryStreamBufferPool.ReturnMemoryStream(buffer);
                throw;
            }

            EnqueuePending(sceneId, new PendingSend(address, protocolCode, typeof(T), buffer));
        }

        internal void Send(long address, Type messageType, APackInfo packInfo)
        {
            if (address == 0)
            {
                Log.Error($"Send address == 0");
                return;
            }
            
            // 高频路径完全保持原样。
            if (Scene.TryGetSession(address, out var session))
            {
                session.Send(0, address, messageType, packInfo);
                return;
            }

            // 服务发现未启用，并且本地连接及配置路由均不存在。
            // 调用 GetSession 是为了保持旧模式统一的异常信息。
            if (!ServiceDiscovery.IsEnabled)
            {
                Scene.GetSession(address).Send(0, address, messageType, packInfo);
                return;
            }
            
            var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(address);
            
            /*
             * 将 APackInfo 的生命周期转交给待发送队列。
             *
             * OuterMessageScheduler 返回后还会调用 packInfo.Dispose()，
             * 设置 IsDisposed=true 可以让那次 Dispose() 直接返回，
             * 从而避免原始 MemoryStreamBuffer 被提前归还到对象池。
             *
             * 这里不复制、不重新序列化，也不创建 MemoryStreamBuffer。
             */
            
            packInfo.IsDisposed = true;
            EnqueuePending(sceneId, new PendingSend(address, messageType, packInfo));
        }
        
        public void Send<T>(ICollection<long> addressCollection, T message, int capacity = 4096) where T : IAddressMessage
        {
            if (addressCollection.Count <= 0)
            {
                Log.Warning("Send addressCollection.Count <= 0");
                return;
            }
            
            var rpcId = 0U;
            var address = 0L;
            var protocolCode = message.OpCode();
            var buffer = MemoryStreamBufferPool.RentMemoryStream(MemoryStreamBufferSource.MultiPack, capacity);

            try
            {
                // 无论多少个 Address，消息只序列化一次。
                PacketParser.PackMemoryStream( ref rpcId, ref address, message, typeof(T), buffer);
            }
            catch
            {
                MemoryStreamBufferPool.ReturnMemoryStream(buffer);
                throw;
            }
            
            PendingBatchBuffer batchBuffer = null;

            try
            {
                var serviceDiscoveryEnabled = ServiceDiscovery.IsEnabled;
                
                foreach (var sendAddress in addressCollection)
                {
                    if (sendAddress == 0)
                    {
                        Log.Error("Send address == 0");
                        continue;
                    }
                    
                    if (Scene.TryGetSession(sendAddress, out var session))
                    {
                        session.Send(buffer, typeof(T), protocolCode, rpcId, sendAddress);
                        continue;
                    }
                    
                    if (!serviceDiscoveryEnabled)
                    {
                        Scene.GetSession(sendAddress).Send(buffer, typeof(T), protocolCode, rpcId, sendAddress);
                        continue;
                    }

                    var sceneId = IdFactoryHelper.RuntimeIdTool.GetSceneId(sendAddress);
                    
                    batchBuffer ??= new PendingBatchBuffer(buffer);
                    batchBuffer.Retain();

                    EnqueuePending(sceneId, new PendingSend(sendAddress, protocolCode, typeof(T), batchBuffer));
                }
            }
            finally
            {
                if (batchBuffer == null)
                {
                    // 没有未知路由，所有发送已经同步提交。
                    MemoryStreamBufferPool.ReturnMemoryStream(buffer);
                }
                else
                {
                    // 释放 Send 方法自身持有的初始引用。
                    ReleasePendingBatchBuffer(batchBuffer);
                }
            }
        }
        
        /// <summary>
        /// 将消息加入目标 Scene 的路由等待队列。
        /// </summary>
        private void EnqueuePending(uint sceneId, PendingSend pendingSend)
        {
            if (!_pendingRoutes.TryGetValue(sceneId, out var pendingRoute))
            {
                pendingRoute = new PendingRoute();
                _pendingRoutes.Add(sceneId, pendingRoute);
                pendingRoute.Messages.Enqueue(pendingSend);
                // 每个 SceneId 只启动一次异步解析。
                ResolvePendingRouteAsync(sceneId).Coroutine();
                return;
            }

            if (pendingRoute.Messages.Count >= MaxPendingMessagesPerScene)
            {
                ReleasePendingSend(pendingSend);

                Log.Warning(
                    $"Pending route queue is full. " +
                    $"sceneId:{sceneId} " +
                    $"count:{pendingRoute.Messages.Count}");

                return;
            }

            pendingRoute.Messages.Enqueue(pendingSend);
        }
        
        /// <summary>
        /// 解析目标 Scene 路由，并按入队顺序发送消息。
        /// </summary>
        private async FTask ResolvePendingRouteAsync(uint sceneId)
        {
            try
            {
                await ServiceDiscovery.ResolveAddressAsync(sceneId);

                if (IsDisposed || !_pendingRoutes.TryGetValue(sceneId, out var pendingRoute))
                {
                    return;
                }

                while (pendingRoute.Messages.Count > 0)
                {
                    var pendingSend = pendingRoute.Messages.Dequeue();

                    try
                    {
                        var session = Scene.GetSession(pendingSend.Address);

                        if (pendingSend.PackInfo != null)
                        {
                            // 原始包直接转发，没有复制和重新序列化。
                            session.Send(
                                0,
                                pendingSend.Address,
                                pendingSend.MessageType,
                                pendingSend.PackInfo);
                        }
                        else
                        {
                            // 普通 Send<T> 已经提前序列化。
                            session.Send(
                                pendingSend.Buffer,
                                pendingSend.MessageType,
                                pendingSend.ProtocolCode,
                                0,
                                pendingSend.Address);
                        }
                    }
                    finally
                    {
                        ReleasePendingSend(pendingSend);
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(
                    $"Resolve pending route failed. " +
                    $"sceneId:{sceneId}\n{exception}");
            }
            finally
            {
                ClearPendingRoute(sceneId);
            }
        }
        
        /// <summary>
        /// 释放一条待发送消息持有的资源。
        /// </summary>
        private void ReleasePendingSend(in PendingSend pendingSend)
        {
            if (pendingSend.PackInfo != null)
            {
                pendingSend.PackInfo.IsDisposed = false;
                pendingSend.PackInfo.Dispose();
                return;
            }

            if (pendingSend.BatchBuffer != null)
            {
                ReleasePendingBatchBuffer(pendingSend.BatchBuffer);
                return;
            }

            // 普通单地址消息独占自己的缓冲区。
            MemoryStreamBufferPool.ReturnMemoryStream(pendingSend.Buffer);
        }
        
        /// <summary>
        /// 释放批量发送缓冲区的一个引用。
        /// 最后一个引用释放时归还缓冲区对象池。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleasePendingBatchBuffer( PendingBatchBuffer batchBuffer)
        {
            if (!batchBuffer.Release())
            {
                return;
            }

            MemoryStreamBufferPool.ReturnMemoryStream(batchBuffer.Buffer);
        }
        
        /// <summary>
        /// 清理指定 Scene 的待发送队列。
        /// </summary>
        private void ClearPendingRoute(uint sceneId)
        {
            if (!_pendingRoutes.Remove(sceneId, out var pendingRoute))
            {
                return;
            }

            while (pendingRoute.Messages.Count > 0)
            {
                var pendingSend =
                    pendingRoute.Messages.Dequeue();

                ReleasePendingSend(pendingSend);
            }
        }

        /// <summary>
        /// 组件销毁时清理所有待发送消息。
        /// </summary>
        internal void ClearPendingRouteMessages()
        {
            foreach (var pendingRoute in _pendingRoutes.Values)
            {
                while (pendingRoute.Messages.Count > 0)
                {
                    var pendingSend = pendingRoute.Messages.Dequeue();

                    ReleasePendingSend(pendingSend);
                }
            }

            _pendingRoutes.Clear();
        }

        internal async FTask<IResponse> Call(long address, Type requestType, APackInfo packInfo)
        {
            if (address == 0)
            {
                Log.Error($"Call address == 0");
                return null;
            }

            var rpcId = ++_rpcId;
            var session = await Scene.GetSessionAsync(address);
            var requestCallback = FTask<IResponse>.Create(false);
            RequestCallback.Add(rpcId, MessageSender.Create(rpcId, packInfo.ProtocolCode, requestType, requestCallback));
            session.Send(rpcId, address, requestType, packInfo);
            return await requestCallback;
        }

        public async FTask<IResponse> Call<T>(long address, T request) where T : IAddressMessage
        {
            if (address == 0)
            {
                Log.Error($"Call address == 0");
                return null;
            }
            
            var rpcId = ++_rpcId;
            var session = await Scene.GetSessionAsync(address);
            var requestCallback = FTask<IResponse>.Create(false);
            RequestCallback.Add(rpcId, MessageSender.Create<T>(rpcId, request.OpCode(), requestCallback));
            session.Send<T>(request, rpcId, address);
            return await requestCallback;
        }
        
        public async FTask SendAddressable<T>(long addressableId, T message) where T : IAddressableMessage
        {
            await CallAddressable(addressableId, message);
        }
        
        public async FTask<IResponse> CallAddressable<T>(long addressableId, T request) where T : IAddressableMessage
        {
            var failCount = 0;
            
            using (await AddressableRouteMessageLock.Wait(addressableId, "CallAddressable"))
            {
                var addressableAddress = await AddressableHelper.GetAddressableAddress(Scene, addressableId);

                while (true)
                {
                    if (addressableAddress == 0)
                    {
                        addressableAddress = await AddressableHelper.GetAddressableAddress(Scene, addressableId);
                    }
                    
                    if (addressableAddress == 0)
                    {
                        return MessageDispatcherComponent.CreateResponse(request.OpCode(), InnerErrorCode.ErrNotFoundRoute);
                    }
                    
                    var iRouteResponse = await Call(addressableAddress, request);
                    
                    switch (iRouteResponse.ErrorCode)
                    {
                        case InnerErrorCode.ErrNotFoundRoute:
                        {
                            if (++failCount > 20)
                            {
                                Log.Error($"AddressableComponent.Call failCount > 20 route send message fail, address: {addressableAddress} AddressableMessageComponent:{addressableId}");
                                return iRouteResponse;
                            }
                            
                            await TimerComponent.Net.WaitAsync(500);
                            addressableAddress = 0;
                            continue;
                        }
                        case InnerErrorCode.ErrRouteTimeout:
                        {
                            Log.Error($"CallAddressableRoute ErrorCode.ErrRouteTimeout Error:{iRouteResponse.ErrorCode} Message:{request}");
                            return iRouteResponse;
                        }
                        default:
                        {
                            return iRouteResponse;
                        }
                    }
                }
            }
        }
        
        public void ResponseHandler(uint rpcId, IResponse response)
        {
            if (!RequestCallback.Remove(rpcId, out var routeMessageSender))
            {
                Log.Error($"not found rpc, response.RpcId:{rpcId} response message: {response.GetType().Name} Process:{Scene.Process.Id} Scene:{Scene.SceneConfigId}");
            }

            ResponseHandler(routeMessageSender, response);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ResponseHandler(MessageSender messageSender, IResponse response)
        {
            if (response.ErrorCode == InnerErrorCode.ErrRouteTimeout)
            {
#if FANTASY_DEVELOP
                messageSender.Tcs.SetException(new Exception($"Rpc error: request, 注意Address消息超时，请注意查看是否死锁或者没有reply: {messageSender.MessageType}, response: {response}"));
#else
                messageSender.Tcs.SetException(new Exception($"Rpc error: request, 注意Address消息超时，请注意查看是否死锁或者没有reply: {messageSender.MessageType}, response: {response}"));
#endif
                messageSender.Dispose();
                return;
            }

            messageSender.Tcs.SetResult(response);
            messageSender.Dispose();
        }

        public void ReturnMessageSender(uint rpcId, MessageSender messageSender)
        {
            try
            {
                if (!RequestCallback.Remove(rpcId))
                {
                    messageSender.Dispose();
                    return;
                }

                var requestOpCode = messageSender.ProtocolCode;

                if (requestOpCode == 0)
                {
                    messageSender.Tcs.SetException(new Exception($"Timeout rpcId:{rpcId}, unsupported protocol type:{messageSender.MessageType}"));
                    messageSender.Dispose();
                    return;
                }

                var response = MessageDispatcherComponent.CreateResponse(requestOpCode, InnerErrorCode.ErrRpcFail);
                ResponseHandler(messageSender, response);
                Log.Warning($"timeout rpcId:{rpcId} messageType:{messageSender.MessageType} protocolCode:{messageSender.ProtocolCode}");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
#endif