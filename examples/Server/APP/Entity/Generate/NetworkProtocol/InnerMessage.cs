using LightProto;
using MemoryPack;
using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

// ReSharper disable InconsistentNaming
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable RedundantTypeArgumentsOfMethod
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CheckNamespace
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable RedundantUsingDirective
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
namespace Fantasy
{
    [Serializable]
    [ProtoContract]
    public partial class G2A_TestMessage : AMessage, IAddressMessage
    {
        public static G2A_TestMessage Create(bool autoReturn = true)
        {
            var g2A_TestMessage = MessageObjectPool<G2A_TestMessage>.Rent();
            g2A_TestMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2A_TestMessage.SetIsPool(false);
            }
            
            return g2A_TestMessage;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Tag = default;
            MessageObjectPool<G2A_TestMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2A_TestRequest : AMessage, IAddressRequest
    {
        public static G2A_TestRequest Create(bool autoReturn = true)
        {
            var g2A_TestRequest = MessageObjectPool<G2A_TestRequest>.Rent();
            g2A_TestRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2A_TestRequest.SetIsPool(false);
            }
            
            return g2A_TestRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            MessageObjectPool<G2A_TestRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_TestRequest; } 
        [ProtoIgnore]
        public G2A_TestResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2A_TestResponse : AMessage, IAddressResponse
    {
        public static G2A_TestResponse Create(bool autoReturn = true)
        {
            var g2A_TestResponse = MessageObjectPool<G2A_TestResponse>.Rent();
            g2A_TestResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2A_TestResponse.SetIsPool(false);
            }
            
            return g2A_TestResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<G2A_TestResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2A_TestResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2M_RequestAddressableId : AMessage, IAddressRequest
    {
        public static G2M_RequestAddressableId Create(bool autoReturn = true)
        {
            var g2M_RequestAddressableId = MessageObjectPool<G2M_RequestAddressableId>.Rent();
            g2M_RequestAddressableId.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2M_RequestAddressableId.SetIsPool(false);
            }
            
            return g2M_RequestAddressableId;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            MessageObjectPool<G2M_RequestAddressableId>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2M_RequestAddressableId; } 
        [ProtoIgnore]
        public M2G_ResponseAddressableId ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class M2G_ResponseAddressableId : AMessage, IAddressResponse
    {
        public static M2G_ResponseAddressableId Create(bool autoReturn = true)
        {
            var m2G_ResponseAddressableId = MessageObjectPool<M2G_ResponseAddressableId>.Rent();
            m2G_ResponseAddressableId.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2G_ResponseAddressableId.SetIsPool(false);
            }
            
            return m2G_ResponseAddressableId;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            AddressableId = default;
            MessageObjectPool<M2G_ResponseAddressableId>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2G_ResponseAddressableId; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long AddressableId { get; set; }
    }
    /// <summary>
    /// 通知Chat服务器创建一个RouteId
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Chat_CreateRouteRequest : AMessage, IAddressRequest
    {
        public static G2Chat_CreateRouteRequest Create(bool autoReturn = true)
        {
            var g2Chat_CreateRouteRequest = MessageObjectPool<G2Chat_CreateRouteRequest>.Rent();
            g2Chat_CreateRouteRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Chat_CreateRouteRequest.SetIsPool(false);
            }
            
            return g2Chat_CreateRouteRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            GateAddress = default;
            MessageObjectPool<G2Chat_CreateRouteRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Chat_CreateRouteRequest; } 
        [ProtoIgnore]
        public Chat2G_CreateRouteResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public long GateAddress { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Chat2G_CreateRouteResponse : AMessage, IAddressResponse
    {
        public static Chat2G_CreateRouteResponse Create(bool autoReturn = true)
        {
            var chat2G_CreateRouteResponse = MessageObjectPool<Chat2G_CreateRouteResponse>.Rent();
            chat2G_CreateRouteResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                chat2G_CreateRouteResponse.SetIsPool(false);
            }
            
            return chat2G_CreateRouteResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            ChatAddress = default;
            MessageObjectPool<Chat2G_CreateRouteResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Chat2G_CreateRouteResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long ChatAddress { get; set; }
    }
    /// <summary>
    /// Map给另外一个Map发送Unit数据
    /// </summary>
    [Serializable]
    [MemoryPackable]
    public partial class M2M_SendUnitRequest : AMessage, IAddressRequest
    {
        public static M2M_SendUnitRequest Create(bool autoReturn = true)
        {
            var m2M_SendUnitRequest = MessageObjectPool<M2M_SendUnitRequest>.Rent();
            m2M_SendUnitRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2M_SendUnitRequest.SetIsPool(false);
            }
            
            return m2M_SendUnitRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Unit = default;
            MessageObjectPool<M2M_SendUnitRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2M_SendUnitRequest; } 
        [MemoryPackIgnore]
        public M2M_SendUnitResponse ResponseType { get; set; }
        [MemoryPackOrder(1)]
        public Unit Unit { get; set; }
    }
    [Serializable]
    [MemoryPackable]
    public partial class M2M_SendUnitResponse : AMessage, IAddressResponse
    {
        public static M2M_SendUnitResponse Create(bool autoReturn = true)
        {
            var m2M_SendUnitResponse = MessageObjectPool<M2M_SendUnitResponse>.Rent();
            m2M_SendUnitResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2M_SendUnitResponse.SetIsPool(false);
            }
            
            return m2M_SendUnitResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<M2M_SendUnitResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2M_SendUnitResponse; } 
        [MemoryPackOrder(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// Gate发送Addressable消息给MAP
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2M_SendAddressableMessage : AMessage, IAddressableMessage
    {
        public static G2M_SendAddressableMessage Create(bool autoReturn = true)
        {
            var g2M_SendAddressableMessage = MessageObjectPool<G2M_SendAddressableMessage>.Rent();
            g2M_SendAddressableMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2M_SendAddressableMessage.SetIsPool(false);
            }
            
            return g2M_SendAddressableMessage;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Tag = default;
            MessageObjectPool<G2M_SendAddressableMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2M_SendAddressableMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2M_CreateSubSceneRequest : AMessage, IAddressRequest
    {
        public static G2M_CreateSubSceneRequest Create(bool autoReturn = true)
        {
            var g2M_CreateSubSceneRequest = MessageObjectPool<G2M_CreateSubSceneRequest>.Rent();
            g2M_CreateSubSceneRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2M_CreateSubSceneRequest.SetIsPool(false);
            }
            
            return g2M_CreateSubSceneRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            MessageObjectPool<G2M_CreateSubSceneRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2M_CreateSubSceneRequest; } 
        [ProtoIgnore]
        public M2G_CreateSubSceneResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class M2G_CreateSubSceneResponse : AMessage, IAddressResponse
    {
        public static M2G_CreateSubSceneResponse Create(bool autoReturn = true)
        {
            var m2G_CreateSubSceneResponse = MessageObjectPool<M2G_CreateSubSceneResponse>.Rent();
            m2G_CreateSubSceneResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2G_CreateSubSceneResponse.SetIsPool(false);
            }
            
            return m2G_CreateSubSceneResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            SubSceneAddress = default;
            MessageObjectPool<M2G_CreateSubSceneResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2G_CreateSubSceneResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long SubSceneAddress { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2SubScene_SentMessage : AMessage, IAddressMessage
    {
        public static G2SubScene_SentMessage Create(bool autoReturn = true)
        {
            var g2SubScene_SentMessage = MessageObjectPool<G2SubScene_SentMessage>.Rent();
            g2SubScene_SentMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2SubScene_SentMessage.SetIsPool(false);
            }
            
            return g2SubScene_SentMessage;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Tag = default;
            MessageObjectPool<G2SubScene_SentMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2SubScene_SentMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// Gate通知SubScene创建一个Addressable消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2SubScene_AddressableIdRequest : AMessage, IAddressRequest
    {
        public static G2SubScene_AddressableIdRequest Create(bool autoReturn = true)
        {
            var g2SubScene_AddressableIdRequest = MessageObjectPool<G2SubScene_AddressableIdRequest>.Rent();
            g2SubScene_AddressableIdRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2SubScene_AddressableIdRequest.SetIsPool(false);
            }
            
            return g2SubScene_AddressableIdRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            MessageObjectPool<G2SubScene_AddressableIdRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2SubScene_AddressableIdRequest; } 
        [ProtoIgnore]
        public SubScene2G_AddressableIdResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class SubScene2G_AddressableIdResponse : AMessage, IAddressResponse
    {
        public static SubScene2G_AddressableIdResponse Create(bool autoReturn = true)
        {
            var subScene2G_AddressableIdResponse = MessageObjectPool<SubScene2G_AddressableIdResponse>.Rent();
            subScene2G_AddressableIdResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                subScene2G_AddressableIdResponse.SetIsPool(false);
            }
            
            return subScene2G_AddressableIdResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            AddressableId = default;
            MessageObjectPool<SubScene2G_AddressableIdResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.SubScene2G_AddressableIdResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public long AddressableId { get; set; }
    }
    /// <summary>
    /// Chat发送一个漫游消息给Map
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Chat2M_TestMessage : AMessage, IRoamingMessage
    {
        public static Chat2M_TestMessage Create(bool autoReturn = true)
        {
            var chat2M_TestMessage = MessageObjectPool<Chat2M_TestMessage>.Rent();
            chat2M_TestMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                chat2M_TestMessage.SetIsPool(false);
            }
            
            return chat2M_TestMessage;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Tag = default;
            MessageObjectPool<Chat2M_TestMessage>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Chat2M_TestMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 测试一个Gate服务器发送一个Route消息给某个漫游终端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Map_TestRouteMessageRequest : AMessage, IAddressRequest
    {
        public static G2Map_TestRouteMessageRequest Create(bool autoReturn = true)
        {
            var g2Map_TestRouteMessageRequest = MessageObjectPool<G2Map_TestRouteMessageRequest>.Rent();
            g2Map_TestRouteMessageRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Map_TestRouteMessageRequest.SetIsPool(false);
            }
            
            return g2Map_TestRouteMessageRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Tag = default;
            MessageObjectPool<G2Map_TestRouteMessageRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_TestRouteMessageRequest; } 
        [ProtoIgnore]
        public Map2G_TestRouteMessageResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Map2G_TestRouteMessageResponse : AMessage, IAddressResponse
    {
        public static Map2G_TestRouteMessageResponse Create(bool autoReturn = true)
        {
            var map2G_TestRouteMessageResponse = MessageObjectPool<Map2G_TestRouteMessageResponse>.Rent();
            map2G_TestRouteMessageResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                map2G_TestRouteMessageResponse.SetIsPool(false);
            }
            
            return map2G_TestRouteMessageResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<Map2G_TestRouteMessageResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Map2G_TestRouteMessageResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 测试一个Gate服务器发送一个漫游协议给某个漫游终端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Map_TestRoamingMessageRequest : AMessage, IRoamingRequest
    {
        public static G2Map_TestRoamingMessageRequest Create(bool autoReturn = true)
        {
            var g2Map_TestRoamingMessageRequest = MessageObjectPool<G2Map_TestRoamingMessageRequest>.Rent();
            g2Map_TestRoamingMessageRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Map_TestRoamingMessageRequest.SetIsPool(false);
            }
            
            return g2Map_TestRoamingMessageRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            Tag = default;
            MessageObjectPool<G2Map_TestRoamingMessageRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_TestRoamingMessageRequest; } 
        [ProtoIgnore]
        public Map2G_TestRoamingMessageResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Map2G_TestRoamingMessageResponse : AMessage, IRoamingResponse
    {
        public static Map2G_TestRoamingMessageResponse Create(bool autoReturn = true)
        {
            var map2G_TestRoamingMessageResponse = MessageObjectPool<Map2G_TestRoamingMessageResponse>.Rent();
            map2G_TestRoamingMessageResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                map2G_TestRoamingMessageResponse.SetIsPool(false);
            }
            
            return map2G_TestRoamingMessageResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<Map2G_TestRoamingMessageResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Map2G_TestRoamingMessageResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// Gate服务器通知Map订阅一个领域事件到Gate上
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Map_SubscribeSphereEventRequest : AMessage, IAddressRequest
    {
        public static G2Map_SubscribeSphereEventRequest Create(bool autoReturn = true)
        {
            var g2Map_SubscribeSphereEventRequest = MessageObjectPool<G2Map_SubscribeSphereEventRequest>.Rent();
            g2Map_SubscribeSphereEventRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Map_SubscribeSphereEventRequest.SetIsPool(false);
            }
            
            return g2Map_SubscribeSphereEventRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            GateAddress = default;
            MessageObjectPool<G2Map_SubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_SubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public G2Map_SubscribeSphereEventResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public long GateAddress { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2Map_SubscribeSphereEventResponse : AMessage, IAddressResponse
    {
        public static G2Map_SubscribeSphereEventResponse Create(bool autoReturn = true)
        {
            var g2Map_SubscribeSphereEventResponse = MessageObjectPool<G2Map_SubscribeSphereEventResponse>.Rent();
            g2Map_SubscribeSphereEventResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Map_SubscribeSphereEventResponse.SetIsPool(false);
            }
            
            return g2Map_SubscribeSphereEventResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<G2Map_SubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_SubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// Gate服务器通知Map取消订阅一个领域事件到Gate上
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2Map_UnsubscribeSphereEventRequest : AMessage, IAddressRequest
    {
        public static G2Map_UnsubscribeSphereEventRequest Create(bool autoReturn = true)
        {
            var g2Map_UnsubscribeSphereEventRequest = MessageObjectPool<G2Map_UnsubscribeSphereEventRequest>.Rent();
            g2Map_UnsubscribeSphereEventRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2Map_UnsubscribeSphereEventRequest.SetIsPool(false);
            }
            
            return g2Map_UnsubscribeSphereEventRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            GateAddress = default;
            MessageObjectPool<G2Map_UnsubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2Map_UnsubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public Map2G_UnsubscribeSphereEventResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public long GateAddress { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Map2G_UnsubscribeSphereEventResponse : AMessage, IAddressResponse
    {
        public static Map2G_UnsubscribeSphereEventResponse Create(bool autoReturn = true)
        {
            var map2G_UnsubscribeSphereEventResponse = MessageObjectPool<Map2G_UnsubscribeSphereEventResponse>.Rent();
            map2G_UnsubscribeSphereEventResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                map2G_UnsubscribeSphereEventResponse.SetIsPool(false);
            }
            
            return map2G_UnsubscribeSphereEventResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<Map2G_UnsubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.Map2G_UnsubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 通知Map进行下线操作
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2M_OfflineRequest : AMessage, IRoamingRequest
    {
        public static G2M_OfflineRequest Create(bool autoReturn = true)
        {
            var g2M_OfflineRequest = MessageObjectPool<G2M_OfflineRequest>.Rent();
            g2M_OfflineRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2M_OfflineRequest.SetIsPool(false);
            }
            
            return g2M_OfflineRequest;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            OfflineTime = default;
            MessageObjectPool<G2M_OfflineRequest>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.G2M_OfflineRequest; } 
        [ProtoIgnore]
        public M2G_OfflineResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public int OfflineTime { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class M2G_OfflineResponse : AMessage, IRoamingResponse
    {
        public static M2G_OfflineResponse Create(bool autoReturn = true)
        {
            var m2G_OfflineResponse = MessageObjectPool<M2G_OfflineResponse>.Rent();
            m2G_OfflineResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2G_OfflineResponse.SetIsPool(false);
            }
            
            return m2G_OfflineResponse;
        }
        
        public void Return()
        {
            if (!AutoReturn)
            {
                SetIsPool(true);
                AutoReturn = true;
            }
            else if (!IsPool())
            {
                return;
            }
            Dispose();
        }

        public void Dispose()
        {
            if (!IsPool()) return; 
            ErrorCode = 0;
            MessageObjectPool<M2G_OfflineResponse>.Return(this);
        }
        public uint OpCode() { return InnerOpcode.M2G_OfflineResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
}