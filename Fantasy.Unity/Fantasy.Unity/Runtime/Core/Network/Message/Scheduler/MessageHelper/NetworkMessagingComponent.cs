using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
#if FANTASY_NET
    /// <summary>
    /// 定时检查过期的Call消息事件。
    /// </summary>
    public struct NetworkMessageUpdate
    {
        public NetworkMessagingComponent NetworkMessagingComponent;
    }

    public class NetworkMessagingComponentAwakeSystem : AwakeSystem<NetworkMessagingComponent>
    {
        protected override void Awake(NetworkMessagingComponent self)
        {
            var selfScene = self.Scene;
            self.Process = selfScene.Process;
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

            self.AddressableRouteMessageLock.Dispose();
            
            self.RequestCallback.Clear();
            self.TimeoutRouteMessageSenders.Clear();
            self.Process = null;
            self.TimerComponent = null;
            self.MessageDispatcherComponent = null;
            self.AddressableRouteMessageLock = null;
        }
    }
#endif
    /// <summary>
    /// 网络消息组件。
    /// </summary>
    public class NetworkMessagingComponent : Entity
    {
#if FANTASY_NET
        /// <summary>
        /// TimerId。
        /// </summary>
        public long TimerId;
        /// <summary>
        /// RPC ID。
        /// </summary>
        private uint _rpcId;
        /// <summary>
        /// Address消息所需的锁队列。
        /// </summary>
        public CoroutineLock AddressableRouteMessageLock;
        /// <summary>
        /// 缓存一下方便后面使用。
        /// </summary>
        public Process Process;
        public TimerComponent TimerComponent;
        public MessageDispatcherComponent MessageDispatcherComponent;
        /// <summary>
        /// 存储请求回调的字典。
        /// </summary>
        public readonly SortedDictionary<uint, MessageSender> RequestCallback = new();
        /// <summary>
        /// 存储超时路由消息发送者的字典。
        /// </summary>
        public readonly Dictionary<uint, MessageSender> TimeoutRouteMessageSenders = new();

        /// <summary>
        /// 将消息发送给内部路由。
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="message">消息</param>
        public void SendInnerRoute(long entityId, IRouteMessage message)
        {
            if (entityId == 0)
            {
                Log.Error($"SendInnerRoute appId == 0");
                return;
            }

            Scene.GetSession(entityId).Send(message, 0, entityId);
        }

        /// <summary>
        /// 将消息发送给内部路由，并指定路由类型操作码和消息数据流。
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="routeTypeOpCode">路由类型操作码</param>
        /// <param name="memoryStream">消息数据流</param>
        public void SendInnerRoute(long entityId, long routeTypeOpCode, MemoryStream memoryStream)
        {
            if (entityId == 0)
            {
                Log.Error($"SendInnerRoute appId == 0");
                return;
            }

            Scene.GetSession(entityId).Send(memoryStream, 0, routeTypeOpCode, entityId);
        }

        /// <summary>
        /// 将消息发送给一组内部路由。
        /// </summary>
        /// <param name="routeIdCollection">路由ID集合</param>
        /// <param name="message">消息</param>
        public void SendInnerRoute(ICollection<long> routeIdCollection, IRouteMessage message)
        {
            if (routeIdCollection.Count <= 0)
            {
                Log.Error($"SendInnerRoute routeId.Count <= 0");
                return;
            }

            var routeTypeOpCode = message.RouteTypeOpCode();
            using var memoryStream = InnerPacketParser.MessagePack(Scene, 0, 0, message);
            foreach (var routeId in routeIdCollection)
            {
                SendInnerRoute(routeId, routeTypeOpCode, InnerPacketParser.MessagePack(0, routeId, memoryStream));
            }
        }

        /// <summary>
        /// 将消息发送给可寻址对象，并在协程中执行。
        /// </summary>
        /// <param name="addressableId">可寻址对象ID</param>
        /// <param name="message">消息</param>
        public void SendAddressable(long addressableId, IRouteMessage message)
        {
            // 调用可寻址组件发送消息并在协程中执行
            CallAddressable(addressableId, message).Coroutine();
        }

        /// <summary>
        /// 异步调用内部路由，并指定路由类型操作码、请求类型和请求数据流。
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="routeTypeOpCode">路由类型操作码</param>
        /// <param name="requestType">请求类型</param>
        /// <param name="request">请求数据流</param>
        /// <returns>异步任务，返回响应</returns>
        public async FTask<IResponse> CallInnerRoute(long entityId, long routeTypeOpCode, Type requestType, MemoryStream request)
        {
            if (entityId == 0)
            {
                Log.Error($"CallInnerRoute appId == 0");
                return null;
            }
            
            var rpcId = ++_rpcId;
            var session = Scene.GetSession(entityId);
            var requestCallback = FTask<IResponse>.Create(false);
            RequestCallback.Add(rpcId, MessageSender.Create(rpcId, requestType, requestCallback));
            session.Send(request, rpcId, routeTypeOpCode, entityId);
            return await requestCallback;
        }

        public async FTask<IResponse> CallInnerRouteBySession(Session session, long entityId,IRouteMessage request)
        {
            var rpcId = ++_rpcId;
            var requestCallback = FTask<IResponse>.Create(false);
            RequestCallback.Add(rpcId, MessageSender.Create(rpcId, request, requestCallback));
            session.Send(request, rpcId, entityId);
            return await requestCallback;
        }

        /// <summary>
        /// 异步调用内部路由，并传递路由消息。
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="request">路由消息</param>
        /// <returns></returns>
        public async FTask<IResponse> CallInnerRoute(long entityId, IRouteMessage request)
        {
            if (entityId == 0)
            {
                Log.Error($"CallInnerRoute entityId == 0");
                return null;
            }
            
            var rpcId = ++_rpcId;
            var session = Scene.GetSession(entityId);
            var requestCallback = FTask<IResponse>.Create(false);
            RequestCallback.Add(rpcId, MessageSender.Create(rpcId, request, requestCallback));
            session.Send(request, rpcId, entityId);
            return await requestCallback;
        }

        /// <summary>
        /// 异步调用可寻址对象，并传递路由消息。
        /// </summary>
        /// <param name="addressableId">可寻址对象ID</param>
        /// <param name="request">路由消息</param>
        /// <returns>异步任务，返回响应</returns>
        public async FTask<IResponse> CallAddressable(long addressableId, IRouteMessage request)
        {
            var failCount = 0;

            // 使用可寻址消息锁，确保并发请求的同步
            using (await AddressableRouteMessageLock.Wait(addressableId, "CallAddressable"))
            {
                var addressableRouteId = await AddressableHelper.GetAddressableRouteId(Scene, addressableId);

                while (true)
                {
                    // 如果找不到可寻址的路由 ID，尝试获取一次
                    if (addressableRouteId == 0)
                    {
                        addressableRouteId = await AddressableHelper.GetAddressableRouteId(Scene, addressableId);
                    }

                    // 如果找不到可寻址的路由 ID，则返回错误响应
                    if (addressableRouteId == 0)
                    {
                        return MessageDispatcherComponent.CreateResponse(request, InnerErrorCode.ErrNotFoundRoute);
                    }

                    // 调用内部路由方法，发送消息并等待响应
                    var iRouteResponse = await CallInnerRoute(addressableRouteId, request);

                    // 根据响应中的错误码进行处理
                    switch (iRouteResponse.ErrorCode)
                    {
                        case InnerErrorCode.ErrNotFoundRoute:
                        {
                            // 如果连续失败次数超过阈值，记录错误并返回响应
                            if (++failCount > 20)
                            {
                                Log.Error(
                                    $"AddressableComponent.Call failCount > 20 route send message fail, routeId: {addressableRouteId} AddressableMessageComponent:{addressableId}");
                                return iRouteResponse;
                            }

                            // 等待一段时间后重试
                            await TimerComponent.Net.WaitAsync(500);
                            addressableRouteId = 0;
                            continue;
                        }
                        case InnerErrorCode.ErrRouteTimeout:
                        {
                            // 如果响应为路由超时错误，记录错误并返回响应
                            Log.Error(
                                $"CallAddressableRoute ErrorCode.ErrRouteTimeout Error:{iRouteResponse.ErrorCode} Message:{request}");
                            return iRouteResponse;
                        }
                        default:
                        {
                            // 其他错误码或正常响应，直接返回响应
                            return iRouteResponse;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 处理响应消息的方法。
        /// </summary>
        /// <param name="rpcId">RPC ID</param>
        /// <param name="response">响应消息</param>
        public void ResponseHandler(uint rpcId, IResponse response)
        {
            if (!RequestCallback.Remove(rpcId, out var routeMessageSender))
            {
                throw new Exception($"not found rpc, response.RpcId:{rpcId} response message: {response.GetType().Name} Process:{Scene.Process.Id} Scene:{Scene.SceneConfigId}");
            }

            ResponseHandler(routeMessageSender, response);
        }

        /// <summary>
        /// 处理响应消息的私有方法。
        /// </summary>
        /// <param name="messageSender">消息发送者</param>
        /// <param name="response">响应消息</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ResponseHandler(MessageSender messageSender, IResponse response)
        {
            if (response.ErrorCode == InnerErrorCode.ErrRouteTimeout)
            {
#if FANTASY_DEVELOP
                messageSender.Tcs.SetException(new Exception($"Rpc error: request, 注意RouteId消息超时，请注意查看是否死锁或者没有reply: RouteId: {messageSender.RouteId} {messageSender.Request.ToJson()}, response: {response}"));
#else
                messageSender.Tcs.SetException(new Exception($"Rpc error: request, 注意RouteId消息超时，请注意查看是否死锁或者没有reply: RouteId: {messageSender.RouteId} {messageSender.Request}, response: {response}"));
#endif
                messageSender.Dispose();
                return;
            }

            messageSender.Tcs.SetResult(response);
            messageSender.Dispose();
        }

        public void ReturnMessageSender(uint rpcId, MessageSender messageSender)
        {
            uint responseRpcId = 0;

            try
            {
                switch (messageSender.Request)
                {
                    // case IRouteMessage iRouteMessage:
                    // {
                    //     Log.Error("NetworkMessagingComponent ReturnMessageSender 根据路由消息生成响应，并进行处理,没有实现呢！");
                    //     // // TODO: 根据路由消息生成响应，并进行处理。
                    //     // var routeResponse = MessageDispatcherComponent.CreateResponse(iRouteMessage, InnerErrorCode.ErrRouteTimeout);
                    //     // responseRpcId = routeResponse.RpcId;
                    //     // routeResponse.RpcId = routeMessageSender.RpcId;
                    //     // MessageHelper.ResponseHandler(routeResponse);
                    //     break;
                    // }
                    case IRequest iRequest:
                    {
                        // 根据普通请求生成响应，并进行处理。
                        var response = MessageDispatcherComponent.CreateResponse(iRequest, InnerErrorCode.ErrRpcFail);
                        responseRpcId = messageSender.RpcId;
                        ResponseHandler(responseRpcId, response);
                        Log.Warning($"timeout rpcId:{rpcId} responseRpcId:{responseRpcId} {iRequest.ToJson()}");
                        break;
                    }
                    default:
                    {
                        // 处理不支持的请求类型。
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        Log.Error(messageSender.Request != null
                            ? $"Unsupported protocol type {messageSender.Request.GetType()} rpcId:{rpcId}"
                            : $"Unsupported protocol type:{messageSender.MessageType.FullName} rpcId:{rpcId}");
                        RequestCallback.Remove(rpcId);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"responseRpcId:{responseRpcId} routeMessageSender.RpcId:{messageSender.RpcId} {e}");
            }
        }
#endif
    }
}