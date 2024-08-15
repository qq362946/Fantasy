using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
#if FANTASY_NET
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

            self.AddressableRouteMessageLock.Dispose();
            
            self.RequestCallback.Clear();
            self.TimeoutRouteMessageSenders.Clear();
            self.TimerComponent = null;
            self.MessageDispatcherComponent = null;
            self.AddressableRouteMessageLock = null;
        }
    }
#endif
    public sealed class NetworkMessagingComponent : Entity
    {
#if FANTASY_NET
        public long TimerId;
        private uint _rpcId;
        public CoroutineLock AddressableRouteMessageLock;
        public TimerComponent TimerComponent;
        public MessageDispatcherComponent MessageDispatcherComponent;
        public readonly SortedDictionary<uint, MessageSender> RequestCallback = new();
        public readonly Dictionary<uint, MessageSender> TimeoutRouteMessageSenders = new();
        
        public void SendInnerRoute(long entityId, IRouteMessage message)
        {
            if (entityId == 0)
            {
                Log.Error($"SendInnerRoute appId == 0");
                return;
            }

            Scene.GetSession(entityId).Send(message, 0, entityId);
        }

        internal async FTask SendInnerRoute(long routeId, Type messageType, APackInfo packInfo)
        {
            if (routeId == 0)
            {
                Log.Error($"SendInnerRoute routeId == 0");
                return;
            }

            await Scene.GetSession(routeId).Send(0, routeId, messageType, packInfo);
        }
        
        public async FTask SendInnerRoute(ICollection<long> routeIdCollection, IRouteMessage message)
        {
            if (routeIdCollection.Count <= 0)
            {
                Log.Error("SendInnerRoute routeIdCollection.Count <= 0");
                return;
            }

            var routeTypeOpCode = message.RouteTypeOpCode();
            using var processPackInfo = ProcessPackInfo.Create(Scene, message, routeIdCollection.Count);
            foreach (var routeId in routeIdCollection)
            {
                processPackInfo.Set(0, routeId);
                await Scene.GetSession(routeId).Send(processPackInfo, 0, routeTypeOpCode, routeId);
            }
        }
        
        public async FTask SendAddressable(long addressableId, IRouteMessage message)
        {
            await CallAddressable(addressableId, message);
        }

        internal async FTask<IResponse> CallInnerRoute(long routeId, Type requestType, APackInfo packInfo)
        {
            if (routeId == 0)
            {
                Log.Error($"CallInnerRoute routeId == 0");
                return null;
            }
            
            var rpcId = ++_rpcId;
            var session = Scene.GetSession(routeId);
            var requestCallback = FTask<IResponse>.Create(false);
            RequestCallback.Add(rpcId, MessageSender.Create(rpcId, requestType, requestCallback));
            await session.Send(rpcId, routeId, requestType, packInfo);
            return await requestCallback;
        }

        public async FTask<IResponse> CallInnerRouteBySession(Session session, long routeId, IRouteMessage request)
        {
            var rpcId = ++_rpcId;
            var requestCallback = FTask<IResponse>.Create(false);
            RequestCallback.Add(rpcId, MessageSender.Create(rpcId, request, requestCallback));
            session.Send(request, rpcId, routeId);
            return await requestCallback;
        }

        public async FTask<IResponse> CallInnerRoute(long routeId, IRouteMessage request)
        {
            if (routeId == 0)
            {
                Log.Error($"CallInnerRoute routeId == 0");
                return null;
            }
            
            var rpcId = ++_rpcId;
            var session = Scene.GetSession(routeId);
            var requestCallback = FTask<IResponse>.Create(false);
            RequestCallback.Add(rpcId, MessageSender.Create(rpcId, request, requestCallback));
            session.Send(request, rpcId, routeId);
            return await requestCallback;
        }
        
        public async FTask<IResponse> CallAddressable(long addressableId, IRouteMessage request)
        {
            var failCount = 0;
            
            using (await AddressableRouteMessageLock.Wait(addressableId, "CallAddressable"))
            {
                var addressableRouteId = await AddressableHelper.GetAddressableRouteId(Scene, addressableId);

                while (true)
                {
                    if (addressableRouteId == 0)
                    {
                        addressableRouteId = await AddressableHelper.GetAddressableRouteId(Scene, addressableId);
                    }
                    
                    if (addressableRouteId == 0)
                    {
                        return MessageDispatcherComponent.CreateResponse(request, InnerErrorCode.ErrNotFoundRoute);
                    }
                    
                    var iRouteResponse = await CallInnerRoute(addressableRouteId, request);
                    
                    switch (iRouteResponse.ErrorCode)
                    {
                        case InnerErrorCode.ErrNotFoundRoute:
                        {
                            if (++failCount > 20)
                            {
                                Log.Error($"AddressableComponent.Call failCount > 20 route send message fail, routeId: {addressableRouteId} AddressableMessageComponent:{addressableId}");
                                return iRouteResponse;
                            }
                            
                            await TimerComponent.Net.WaitAsync(500);
                            addressableRouteId = 0;
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
                throw new Exception($"not found rpc, response.RpcId:{rpcId} response message: {response.GetType().Name} Process:{Scene.Process.Id} Scene:{Scene.SceneConfigId}");
            }

            ResponseHandler(routeMessageSender, response);
        }
        
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
            try
            {
                switch (messageSender.Request)
                {
                    case IRouteMessage iRouteMessage:
                    {
                        // IRouteMessage是个特殊的RPC协议、这里不处理就可以了。
                        // var routeResponse = MessageDispatcherComponent.CreateResponse(iRouteMessage, InnerErrorCode.ErrRouteTimeout);
                        // responseRpcId = routeResponse.RpcId;
                        // routeResponse.RpcId = routeMessageSender.RpcId;
                        break;
                    }
                    case IRequest iRequest:
                    {
                        var response = MessageDispatcherComponent.CreateResponse(iRequest, InnerErrorCode.ErrRpcFail);
                        var responseRpcId = messageSender.RpcId;
                        ResponseHandler(responseRpcId, response);
                        Log.Warning($"timeout rpcId:{rpcId} responseRpcId:{responseRpcId} {iRequest.ToJson()}");
                        break;
                    }
                    default:
                    {
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
                Console.WriteLine(e);
                throw;
            }
        }
#endif
    }
}
