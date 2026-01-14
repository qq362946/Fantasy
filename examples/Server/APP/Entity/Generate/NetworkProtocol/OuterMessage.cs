using LightProto;
using System;
using MemoryPack;
using System.Collections.Generic;
using Fantasy;
using Fantasy.Pool;
using Fantasy.Network.Interface;
using Fantasy.Serialize;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618
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
namespace Fantasy
{
    /// <summary>
    /// 测试使用ErrorCode枚举的消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestEnumMessage : AMessage, IMessage
    {
        public static C2G_TestEnumMessage Create(bool autoReturn = true)
        {
            var c2G_TestEnumMessage = MessageObjectPool<C2G_TestEnumMessage>.Rent();
            c2G_TestEnumMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestEnumMessage.SetIsPool(false);
            }
            
            return c2G_TestEnumMessage;
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
            Code = default;
            Message = default;
            State = default;
            MessageObjectPool<C2G_TestEnumMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestEnumMessage; } 
        /// <summary>
        /// 错误码
        /// </summary>
        [ProtoMember(1)]
        public ErrorCodeEnum Code { get; set; }
        /// <summary>
        /// 消息内容
        /// </summary>
        [ProtoMember(2)]
        public string Message { get; set; }
        /// <summary>
        /// 玩家状态
        /// </summary>
        [ProtoMember(3)]
        public PlayerState State { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestEmptyMessage : AMessage, IMessage
    {
        public static C2G_TestEmptyMessage Create(bool autoReturn = true)
        {
            var c2G_TestEmptyMessage = MessageObjectPool<C2G_TestEmptyMessage>.Rent();
            c2G_TestEmptyMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestEmptyMessage.SetIsPool(false);
            }
            
            return c2G_TestEmptyMessage;
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
            MessageObjectPool<C2G_TestEmptyMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestEmptyMessage; } 
    }
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestMessage : AMessage, IMessage
    {
        public static C2G_TestMessage Create(bool autoReturn = true)
        {
            var c2G_TestMessage = MessageObjectPool<C2G_TestMessage>.Rent();
            c2G_TestMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestMessage.SetIsPool(false);
            }
            
            return c2G_TestMessage;
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
            MessageObjectPool<C2G_TestMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRequest : AMessage, IRequest
    {
        public static C2G_TestRequest Create(bool autoReturn = true)
        {
            var c2G_TestRequest = MessageObjectPool<C2G_TestRequest>.Rent();
            c2G_TestRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestRequest.SetIsPool(false);
            }
            
            return c2G_TestRequest;
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
            Data = default;
            MessageObjectPool<C2G_TestRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestRequest; } 
        [ProtoIgnore]
        public G2C_TestResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Tag { get; set; }
        [ProtoMember(2)]
        public byte Data { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_TestResponse : AMessage, IResponse
    {
        public static G2C_TestResponse Create(bool autoReturn = true)
        {
            var g2C_TestResponse = MessageObjectPool<G2C_TestResponse>.Rent();
            g2C_TestResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_TestResponse.SetIsPool(false);
            }
            
            return g2C_TestResponse;
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
            Tag = default;
            Data = null;
            MessageObjectPool<G2C_TestResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_TestResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public string Tag { get; set; }
        [ProtoMember(3)]
        public byte[] Data { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRequestPushMessage : AMessage, IMessage
    {
        public static C2G_TestRequestPushMessage Create(bool autoReturn = true)
        {
            var c2G_TestRequestPushMessage = MessageObjectPool<C2G_TestRequestPushMessage>.Rent();
            c2G_TestRequestPushMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestRequestPushMessage.SetIsPool(false);
            }
            
            return c2G_TestRequestPushMessage;
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
            MessageObjectPool<C2G_TestRequestPushMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestRequestPushMessage; } 
    }
    /// <summary>
    /// Gate服务器推送一个消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class G2C_PushMessage : AMessage, IMessage
    {
        public static G2C_PushMessage Create(bool autoReturn = true)
        {
            var g2C_PushMessage = MessageObjectPool<G2C_PushMessage>.Rent();
            g2C_PushMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_PushMessage.SetIsPool(false);
            }
            
            return g2C_PushMessage;
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
            MessageObjectPool<G2C_PushMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_PushMessage; } 
        /// <summary>
        /// 标记
        /// </summary>
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateAddressableRequest : AMessage, IRequest
    {
        public static C2G_CreateAddressableRequest Create(bool autoReturn = true)
        {
            var c2G_CreateAddressableRequest = MessageObjectPool<C2G_CreateAddressableRequest>.Rent();
            c2G_CreateAddressableRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_CreateAddressableRequest.SetIsPool(false);
            }
            
            return c2G_CreateAddressableRequest;
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
            MessageObjectPool<C2G_CreateAddressableRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateAddressableRequest; } 
        [ProtoIgnore]
        public G2C_CreateAddressableResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateAddressableResponse : AMessage, IResponse
    {
        public static G2C_CreateAddressableResponse Create(bool autoReturn = true)
        {
            var g2C_CreateAddressableResponse = MessageObjectPool<G2C_CreateAddressableResponse>.Rent();
            g2C_CreateAddressableResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_CreateAddressableResponse.SetIsPool(false);
            }
            
            return g2C_CreateAddressableResponse;
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
            MessageObjectPool<G2C_CreateAddressableResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateAddressableResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class C2M_TestMessage : AMessage, IAddressableMessage
    {
        public static C2M_TestMessage Create(bool autoReturn = true)
        {
            var c2M_TestMessage = MessageObjectPool<C2M_TestMessage>.Rent();
            c2M_TestMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2M_TestMessage.SetIsPool(false);
            }
            
            return c2M_TestMessage;
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
            MessageObjectPool<C2M_TestMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class C2M_TestRequest : AMessage, IAddressableRequest
    {
        public static C2M_TestRequest Create(bool autoReturn = true)
        {
            var c2M_TestRequest = MessageObjectPool<C2M_TestRequest>.Rent();
            c2M_TestRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2M_TestRequest.SetIsPool(false);
            }
            
            return c2M_TestRequest;
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
            MessageObjectPool<C2M_TestRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_TestRequest; } 
        [ProtoIgnore]
        public M2C_TestResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class M2C_TestResponse : AMessage, IAddressableResponse
    {
        public static M2C_TestResponse Create(bool autoReturn = true)
        {
            var m2C_TestResponse = MessageObjectPool<M2C_TestResponse>.Rent();
            m2C_TestResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2C_TestResponse.SetIsPool(false);
            }
            
            return m2C_TestResponse;
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
            Tag = default;
            MessageObjectPool<M2C_TestResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.M2C_TestResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 通知Gate服务器创建一个Chat的Route连接
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateChatRouteRequest : AMessage, IRequest
    {
        public static C2G_CreateChatRouteRequest Create(bool autoReturn = true)
        {
            var c2G_CreateChatRouteRequest = MessageObjectPool<C2G_CreateChatRouteRequest>.Rent();
            c2G_CreateChatRouteRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_CreateChatRouteRequest.SetIsPool(false);
            }
            
            return c2G_CreateChatRouteRequest;
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
            MessageObjectPool<C2G_CreateChatRouteRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateChatRouteRequest; } 
        [ProtoIgnore]
        public G2C_CreateChatRouteResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateChatRouteResponse : AMessage, IResponse
    {
        public static G2C_CreateChatRouteResponse Create(bool autoReturn = true)
        {
            var g2C_CreateChatRouteResponse = MessageObjectPool<G2C_CreateChatRouteResponse>.Rent();
            g2C_CreateChatRouteResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_CreateChatRouteResponse.SetIsPool(false);
            }
            
            return g2C_CreateChatRouteResponse;
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
            MessageObjectPool<G2C_CreateChatRouteResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateChatRouteResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 发送一个Route消息给Chat
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestMessage : AMessage, ICustomRouteMessage
    {
        public static C2Chat_TestMessage Create(bool autoReturn = true)
        {
            var c2Chat_TestMessage = MessageObjectPool<C2Chat_TestMessage>.Rent();
            c2Chat_TestMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Chat_TestMessage.SetIsPool(false);
            }
            
            return c2Chat_TestMessage;
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
            MessageObjectPool<C2Chat_TestMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RouteType.ChatRoute;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 发送一个RPCRoute消息给Chat
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestMessageRequest : AMessage, ICustomRouteRequest
    {
        public static C2Chat_TestMessageRequest Create(bool autoReturn = true)
        {
            var c2Chat_TestMessageRequest = MessageObjectPool<C2Chat_TestMessageRequest>.Rent();
            c2Chat_TestMessageRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Chat_TestMessageRequest.SetIsPool(false);
            }
            
            return c2Chat_TestMessageRequest;
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
            MessageObjectPool<C2Chat_TestMessageRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestMessageRequest; } 
        [ProtoIgnore]
        public Chat2C_TestMessageResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RouteType.ChatRoute;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Chat2C_TestMessageResponse : AMessage, ICustomRouteResponse
    {
        public static Chat2C_TestMessageResponse Create(bool autoReturn = true)
        {
            var chat2C_TestMessageResponse = MessageObjectPool<Chat2C_TestMessageResponse>.Rent();
            chat2C_TestMessageResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                chat2C_TestMessageResponse.SetIsPool(false);
            }
            
            return chat2C_TestMessageResponse;
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
            Tag = default;
            MessageObjectPool<Chat2C_TestMessageResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Chat2C_TestMessageResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 发送一个RPC消息给Map，让Map里的Entity转移到另外一个Map上
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2M_MoveToMapRequest : AMessage, IAddressableRequest
    {
        public static C2M_MoveToMapRequest Create(bool autoReturn = true)
        {
            var c2M_MoveToMapRequest = MessageObjectPool<C2M_MoveToMapRequest>.Rent();
            c2M_MoveToMapRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2M_MoveToMapRequest.SetIsPool(false);
            }
            
            return c2M_MoveToMapRequest;
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
            MessageObjectPool<C2M_MoveToMapRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_MoveToMapRequest; } 
        [ProtoIgnore]
        public M2C_MoveToMapResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class M2C_MoveToMapResponse : AMessage, IAddressableResponse
    {
        public static M2C_MoveToMapResponse Create(bool autoReturn = true)
        {
            var m2C_MoveToMapResponse = MessageObjectPool<M2C_MoveToMapResponse>.Rent();
            m2C_MoveToMapResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2C_MoveToMapResponse.SetIsPool(false);
            }
            
            return m2C_MoveToMapResponse;
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
            MessageObjectPool<M2C_MoveToMapResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.M2C_MoveToMapResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 发送一个消息给Gate，让Gate发送一个Addressable消息给MAP
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_SendAddressableToMap : AMessage, IMessage
    {
        public static C2G_SendAddressableToMap Create(bool autoReturn = true)
        {
            var c2G_SendAddressableToMap = MessageObjectPool<C2G_SendAddressableToMap>.Rent();
            c2G_SendAddressableToMap.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_SendAddressableToMap.SetIsPool(false);
            }
            
            return c2G_SendAddressableToMap;
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
            MessageObjectPool<C2G_SendAddressableToMap>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_SendAddressableToMap; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 发送一个消息给Chat，让Chat服务器主动推送一个RouteMessage消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestRequestPushMessage : AMessage, ICustomRouteMessage
    {
        public static C2Chat_TestRequestPushMessage Create(bool autoReturn = true)
        {
            var c2Chat_TestRequestPushMessage = MessageObjectPool<C2Chat_TestRequestPushMessage>.Rent();
            c2Chat_TestRequestPushMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Chat_TestRequestPushMessage.SetIsPool(false);
            }
            
            return c2Chat_TestRequestPushMessage;
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
            MessageObjectPool<C2Chat_TestRequestPushMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestRequestPushMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RouteType.ChatRoute;
    }
    /// <summary>
    /// Chat服务器主动推送一个消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Chat2C_PushMessage : AMessage, ICustomRouteMessage
    {
        public static Chat2C_PushMessage Create(bool autoReturn = true)
        {
            var chat2C_PushMessage = MessageObjectPool<Chat2C_PushMessage>.Rent();
            chat2C_PushMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                chat2C_PushMessage.SetIsPool(false);
            }
            
            return chat2C_PushMessage;
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
            MessageObjectPool<Chat2C_PushMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Chat2C_PushMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RouteType.ChatRoute;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 客户端发送给Gate服务器通知map服务器创建一个SubScene
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateSubSceneRequest : AMessage, IRequest
    {
        public static C2G_CreateSubSceneRequest Create(bool autoReturn = true)
        {
            var c2G_CreateSubSceneRequest = MessageObjectPool<C2G_CreateSubSceneRequest>.Rent();
            c2G_CreateSubSceneRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_CreateSubSceneRequest.SetIsPool(false);
            }
            
            return c2G_CreateSubSceneRequest;
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
            MessageObjectPool<C2G_CreateSubSceneRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateSubSceneRequest; } 
        [ProtoIgnore]
        public G2C_CreateSubSceneResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateSubSceneResponse : AMessage, IResponse
    {
        public static G2C_CreateSubSceneResponse Create(bool autoReturn = true)
        {
            var g2C_CreateSubSceneResponse = MessageObjectPool<G2C_CreateSubSceneResponse>.Rent();
            g2C_CreateSubSceneResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_CreateSubSceneResponse.SetIsPool(false);
            }
            
            return g2C_CreateSubSceneResponse;
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
            MessageObjectPool<G2C_CreateSubSceneResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateSubSceneResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 客户端通知Gate服务器给SubScene发送一个消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_SendToSubSceneMessage : AMessage, IMessage
    {
        public static C2G_SendToSubSceneMessage Create(bool autoReturn = true)
        {
            var c2G_SendToSubSceneMessage = MessageObjectPool<C2G_SendToSubSceneMessage>.Rent();
            c2G_SendToSubSceneMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_SendToSubSceneMessage.SetIsPool(false);
            }
            
            return c2G_SendToSubSceneMessage;
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
            MessageObjectPool<C2G_SendToSubSceneMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_SendToSubSceneMessage; } 
    }
    /// <summary>
    /// 客户端通知Gate服务器创建一个SubScene的Address消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_CreateSubSceneAddressableRequest : AMessage, IRequest
    {
        public static C2G_CreateSubSceneAddressableRequest Create(bool autoReturn = true)
        {
            var c2G_CreateSubSceneAddressableRequest = MessageObjectPool<C2G_CreateSubSceneAddressableRequest>.Rent();
            c2G_CreateSubSceneAddressableRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_CreateSubSceneAddressableRequest.SetIsPool(false);
            }
            
            return c2G_CreateSubSceneAddressableRequest;
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
            MessageObjectPool<C2G_CreateSubSceneAddressableRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_CreateSubSceneAddressableRequest; } 
        [ProtoIgnore]
        public G2C_CreateSubSceneAddressableResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_CreateSubSceneAddressableResponse : AMessage, IResponse
    {
        public static G2C_CreateSubSceneAddressableResponse Create(bool autoReturn = true)
        {
            var g2C_CreateSubSceneAddressableResponse = MessageObjectPool<G2C_CreateSubSceneAddressableResponse>.Rent();
            g2C_CreateSubSceneAddressableResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_CreateSubSceneAddressableResponse.SetIsPool(false);
            }
            
            return g2C_CreateSubSceneAddressableResponse;
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
            MessageObjectPool<G2C_CreateSubSceneAddressableResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_CreateSubSceneAddressableResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 客户端向SubScene发送一个测试消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2SubScene_TestMessage : AMessage, IAddressableMessage
    {
        public static C2SubScene_TestMessage Create(bool autoReturn = true)
        {
            var c2SubScene_TestMessage = MessageObjectPool<C2SubScene_TestMessage>.Rent();
            c2SubScene_TestMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2SubScene_TestMessage.SetIsPool(false);
            }
            
            return c2SubScene_TestMessage;
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
            MessageObjectPool<C2SubScene_TestMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2SubScene_TestMessage; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 客户端向SubScene发送一个销毁测试消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2SubScene_TestDisposeMessage : AMessage, IAddressableMessage
    {
        public static C2SubScene_TestDisposeMessage Create(bool autoReturn = true)
        {
            var c2SubScene_TestDisposeMessage = MessageObjectPool<C2SubScene_TestDisposeMessage>.Rent();
            c2SubScene_TestDisposeMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2SubScene_TestDisposeMessage.SetIsPool(false);
            }
            
            return c2SubScene_TestDisposeMessage;
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
            MessageObjectPool<C2SubScene_TestDisposeMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2SubScene_TestDisposeMessage; } 
    }
    /// <summary>
    /// 客户端向服务器发送连接消息（Roaming）
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_ConnectRoamingRequest : AMessage, IRequest
    {
        public static C2G_ConnectRoamingRequest Create(bool autoReturn = true)
        {
            var c2G_ConnectRoamingRequest = MessageObjectPool<C2G_ConnectRoamingRequest>.Rent();
            c2G_ConnectRoamingRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_ConnectRoamingRequest.SetIsPool(false);
            }
            
            return c2G_ConnectRoamingRequest;
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
            MessageObjectPool<C2G_ConnectRoamingRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_ConnectRoamingRequest; } 
        [ProtoIgnore]
        public G2C_ConnectRoamingResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_ConnectRoamingResponse : AMessage, IResponse
    {
        public static G2C_ConnectRoamingResponse Create(bool autoReturn = true)
        {
            var g2C_ConnectRoamingResponse = MessageObjectPool<G2C_ConnectRoamingResponse>.Rent();
            g2C_ConnectRoamingResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_ConnectRoamingResponse.SetIsPool(false);
            }
            
            return g2C_ConnectRoamingResponse;
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
            MessageObjectPool<G2C_ConnectRoamingResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_ConnectRoamingResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 测试一个Chat漫游普通消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestRoamingMessage : AMessage, IRoamingMessage
    {
        public static C2Chat_TestRoamingMessage Create(bool autoReturn = true)
        {
            var c2Chat_TestRoamingMessage = MessageObjectPool<C2Chat_TestRoamingMessage>.Rent();
            c2Chat_TestRoamingMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Chat_TestRoamingMessage.SetIsPool(false);
            }
            
            return c2Chat_TestRoamingMessage;
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
            MessageObjectPool<C2Chat_TestRoamingMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestRoamingMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.ChatRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 测试一个Map漫游普通消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Map_TestRoamingMessage : AMessage, IRoamingMessage
    {
        public static C2Map_TestRoamingMessage Create(bool autoReturn = true)
        {
            var c2Map_TestRoamingMessage = MessageObjectPool<C2Map_TestRoamingMessage>.Rent();
            c2Map_TestRoamingMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Map_TestRoamingMessage.SetIsPool(false);
            }
            
            return c2Map_TestRoamingMessage;
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
            MessageObjectPool<C2Map_TestRoamingMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Map_TestRoamingMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 测试一个Chat漫游RPC消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestRPCRoamingRequest : AMessage, IRoamingRequest
    {
        public static C2Chat_TestRPCRoamingRequest Create(bool autoReturn = true)
        {
            var c2Chat_TestRPCRoamingRequest = MessageObjectPool<C2Chat_TestRPCRoamingRequest>.Rent();
            c2Chat_TestRPCRoamingRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Chat_TestRPCRoamingRequest.SetIsPool(false);
            }
            
            return c2Chat_TestRPCRoamingRequest;
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
            MessageObjectPool<C2Chat_TestRPCRoamingRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestRPCRoamingRequest; } 
        [ProtoIgnore]
        public Chat2C_TestRPCRoamingResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.ChatRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class Chat2C_TestRPCRoamingResponse : AMessage, IRoamingResponse
    {
        public static Chat2C_TestRPCRoamingResponse Create(bool autoReturn = true)
        {
            var chat2C_TestRPCRoamingResponse = MessageObjectPool<Chat2C_TestRPCRoamingResponse>.Rent();
            chat2C_TestRPCRoamingResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                chat2C_TestRPCRoamingResponse.SetIsPool(false);
            }
            
            return chat2C_TestRPCRoamingResponse;
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
            MessageObjectPool<Chat2C_TestRPCRoamingResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Chat2C_TestRPCRoamingResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 客户端发送一个漫游消息给Map通知Map主动推送一个消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Map_PushMessageToClient : AMessage, IRoamingMessage
    {
        public static C2Map_PushMessageToClient Create(bool autoReturn = true)
        {
            var c2Map_PushMessageToClient = MessageObjectPool<C2Map_PushMessageToClient>.Rent();
            c2Map_PushMessageToClient.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Map_PushMessageToClient.SetIsPool(false);
            }
            
            return c2Map_PushMessageToClient;
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
            MessageObjectPool<C2Map_PushMessageToClient>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Map_PushMessageToClient; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 漫游端发送一个消息给客户端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Map2C_PushMessageToClient : AMessage, IRoamingMessage
    {
        public static Map2C_PushMessageToClient Create(bool autoReturn = true)
        {
            var map2C_PushMessageToClient = MessageObjectPool<Map2C_PushMessageToClient>.Rent();
            map2C_PushMessageToClient.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                map2C_PushMessageToClient.SetIsPool(false);
            }
            
            return map2C_PushMessageToClient;
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
            MessageObjectPool<Map2C_PushMessageToClient>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Map2C_PushMessageToClient; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 测试传送漫游的触发协议
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Map_TestTransferRequest : AMessage, IRoamingRequest
    {
        public static C2Map_TestTransferRequest Create(bool autoReturn = true)
        {
            var c2Map_TestTransferRequest = MessageObjectPool<C2Map_TestTransferRequest>.Rent();
            c2Map_TestTransferRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Map_TestTransferRequest.SetIsPool(false);
            }
            
            return c2Map_TestTransferRequest;
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
            MessageObjectPool<C2Map_TestTransferRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Map_TestTransferRequest; } 
        [ProtoIgnore]
        public Map2C_TestTransferResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
    }
    [Serializable]
    [ProtoContract]
    public partial class Map2C_TestTransferResponse : AMessage, IRoamingResponse
    {
        public static Map2C_TestTransferResponse Create(bool autoReturn = true)
        {
            var map2C_TestTransferResponse = MessageObjectPool<Map2C_TestTransferResponse>.Rent();
            map2C_TestTransferResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                map2C_TestTransferResponse.SetIsPool(false);
            }
            
            return map2C_TestTransferResponse;
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
            MessageObjectPool<Map2C_TestTransferResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.Map2C_TestTransferResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 测试一个Chat发送到Map之间漫游协议
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2Chat_TestSendMapMessage : AMessage, IRoamingMessage
    {
        public static C2Chat_TestSendMapMessage Create(bool autoReturn = true)
        {
            var c2Chat_TestSendMapMessage = MessageObjectPool<C2Chat_TestSendMapMessage>.Rent();
            c2Chat_TestSendMapMessage.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2Chat_TestSendMapMessage.SetIsPool(false);
            }
            
            return c2Chat_TestSendMapMessage;
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
            MessageObjectPool<C2Chat_TestSendMapMessage>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2Chat_TestSendMapMessage; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.ChatRoamingType;
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 通知Gate服务器发送一个Route消息给Map的漫游终端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRouteToRoaming : AMessage, IMessage
    {
        public static C2G_TestRouteToRoaming Create(bool autoReturn = true)
        {
            var c2G_TestRouteToRoaming = MessageObjectPool<C2G_TestRouteToRoaming>.Rent();
            c2G_TestRouteToRoaming.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestRouteToRoaming.SetIsPool(false);
            }
            
            return c2G_TestRouteToRoaming;
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
            MessageObjectPool<C2G_TestRouteToRoaming>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestRouteToRoaming; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 通知Gate服务器发送一个漫游消息给Map的漫游终端
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_TestRoamingToRoaming : AMessage, IMessage
    {
        public static C2G_TestRoamingToRoaming Create(bool autoReturn = true)
        {
            var c2G_TestRoamingToRoaming = MessageObjectPool<C2G_TestRoamingToRoaming>.Rent();
            c2G_TestRoamingToRoaming.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_TestRoamingToRoaming.SetIsPool(false);
            }
            
            return c2G_TestRoamingToRoaming;
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
            MessageObjectPool<C2G_TestRoamingToRoaming>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_TestRoamingToRoaming; } 
        [ProtoMember(1)]
        public string Tag { get; set; }
    }
    /// <summary>
    /// 通知Gate服务器发送一个内网消息通知Map服务器向Gate服务器注册一个领域事件
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_SubscribeSphereEventRequest : AMessage, IRequest
    {
        public static C2G_SubscribeSphereEventRequest Create(bool autoReturn = true)
        {
            var c2G_SubscribeSphereEventRequest = MessageObjectPool<C2G_SubscribeSphereEventRequest>.Rent();
            c2G_SubscribeSphereEventRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_SubscribeSphereEventRequest.SetIsPool(false);
            }
            
            return c2G_SubscribeSphereEventRequest;
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
            MessageObjectPool<C2G_SubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_SubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public G2C_SubscribeSphereEventResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_SubscribeSphereEventResponse : AMessage, IResponse
    {
        public static G2C_SubscribeSphereEventResponse Create(bool autoReturn = true)
        {
            var g2C_SubscribeSphereEventResponse = MessageObjectPool<G2C_SubscribeSphereEventResponse>.Rent();
            g2C_SubscribeSphereEventResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_SubscribeSphereEventResponse.SetIsPool(false);
            }
            
            return g2C_SubscribeSphereEventResponse;
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
            MessageObjectPool<G2C_SubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_SubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 通知Gate发送一个订阅领域事件
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_PublishSphereEventRequest : AMessage, IRequest
    {
        public static C2G_PublishSphereEventRequest Create(bool autoReturn = true)
        {
            var c2G_PublishSphereEventRequest = MessageObjectPool<C2G_PublishSphereEventRequest>.Rent();
            c2G_PublishSphereEventRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_PublishSphereEventRequest.SetIsPool(false);
            }
            
            return c2G_PublishSphereEventRequest;
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
            MessageObjectPool<C2G_PublishSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_PublishSphereEventRequest; } 
        [ProtoIgnore]
        public G2C_PublishSphereEventResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_PublishSphereEventResponse : AMessage, IResponse
    {
        public static G2C_PublishSphereEventResponse Create(bool autoReturn = true)
        {
            var g2C_PublishSphereEventResponse = MessageObjectPool<G2C_PublishSphereEventResponse>.Rent();
            g2C_PublishSphereEventResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_PublishSphereEventResponse.SetIsPool(false);
            }
            
            return g2C_PublishSphereEventResponse;
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
            MessageObjectPool<G2C_PublishSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_PublishSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 通知Gate取消一个订阅领域事件
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_UnsubscribeSphereEventRequest : AMessage, IRequest
    {
        public static C2G_UnsubscribeSphereEventRequest Create(bool autoReturn = true)
        {
            var c2G_UnsubscribeSphereEventRequest = MessageObjectPool<C2G_UnsubscribeSphereEventRequest>.Rent();
            c2G_UnsubscribeSphereEventRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_UnsubscribeSphereEventRequest.SetIsPool(false);
            }
            
            return c2G_UnsubscribeSphereEventRequest;
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
            MessageObjectPool<C2G_UnsubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_UnsubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public G2C_UnsubscribeSphereEventResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_UnsubscribeSphereEventResponse : AMessage, IResponse
    {
        public static G2C_UnsubscribeSphereEventResponse Create(bool autoReturn = true)
        {
            var g2C_UnsubscribeSphereEventResponse = MessageObjectPool<G2C_UnsubscribeSphereEventResponse>.Rent();
            g2C_UnsubscribeSphereEventResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_UnsubscribeSphereEventResponse.SetIsPool(false);
            }
            
            return g2C_UnsubscribeSphereEventResponse;
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
            MessageObjectPool<G2C_UnsubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_UnsubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 通知Map取消一个订阅领域事件
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_MapUnsubscribeSphereEventRequest : AMessage, IRequest
    {
        public static C2G_MapUnsubscribeSphereEventRequest Create(bool autoReturn = true)
        {
            var c2G_MapUnsubscribeSphereEventRequest = MessageObjectPool<C2G_MapUnsubscribeSphereEventRequest>.Rent();
            c2G_MapUnsubscribeSphereEventRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_MapUnsubscribeSphereEventRequest.SetIsPool(false);
            }
            
            return c2G_MapUnsubscribeSphereEventRequest;
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
            MessageObjectPool<C2G_MapUnsubscribeSphereEventRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_MapUnsubscribeSphereEventRequest; } 
        [ProtoIgnore]
        public G2C_MapUnsubscribeSphereEventResponse ResponseType { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_MapUnsubscribeSphereEventResponse : AMessage, IResponse
    {
        public static G2C_MapUnsubscribeSphereEventResponse Create(bool autoReturn = true)
        {
            var g2C_MapUnsubscribeSphereEventResponse = MessageObjectPool<G2C_MapUnsubscribeSphereEventResponse>.Rent();
            g2C_MapUnsubscribeSphereEventResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_MapUnsubscribeSphereEventResponse.SetIsPool(false);
            }
            
            return g2C_MapUnsubscribeSphereEventResponse;
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
            MessageObjectPool<G2C_MapUnsubscribeSphereEventResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_MapUnsubscribeSphereEventResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 客户端登陆到Gate服务器
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2G_LoginGameRequest : AMessage, IRequest
    {
        public static C2G_LoginGameRequest Create(bool autoReturn = true)
        {
            var c2G_LoginGameRequest = MessageObjectPool<C2G_LoginGameRequest>.Rent();
            c2G_LoginGameRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2G_LoginGameRequest.SetIsPool(false);
            }
            
            return c2G_LoginGameRequest;
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
            AccountName = default;
            MessageObjectPool<C2G_LoginGameRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2G_LoginGameRequest; } 
        [ProtoIgnore]
        public G2C_LoginGameResponse ResponseType { get; set; }
        [ProtoMember(1)]
        public string AccountName { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class G2C_LoginGameResponse : AMessage, IResponse
    {
        public static G2C_LoginGameResponse Create(bool autoReturn = true)
        {
            var g2C_LoginGameResponse = MessageObjectPool<G2C_LoginGameResponse>.Rent();
            g2C_LoginGameResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                g2C_LoginGameResponse.SetIsPool(false);
            }
            
            return g2C_LoginGameResponse;
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
            MessageObjectPool<G2C_LoginGameResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.G2C_LoginGameResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
    }
    /// <summary>
    /// 客户端通知服务器可以接收服务器推送的消息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2M_InitComplete : AMessage, IRoamingMessage
    {
        public static C2M_InitComplete Create(bool autoReturn = true)
        {
            var c2M_InitComplete = MessageObjectPool<C2M_InitComplete>.Rent();
            c2M_InitComplete.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2M_InitComplete.SetIsPool(false);
            }
            
            return c2M_InitComplete;
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
            MessageObjectPool<C2M_InitComplete>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_InitComplete; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
    }
    /// <summary>
    /// Map服务器通知客户端创建新的Unit
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class M2C_UnitCreate : AMessage, IRoamingMessage
    {
        public static M2C_UnitCreate Create(bool autoReturn = true)
        {
            var m2C_UnitCreate = MessageObjectPool<M2C_UnitCreate>.Rent();
            m2C_UnitCreate.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2C_UnitCreate.SetIsPool(false);
            }
            
            return m2C_UnitCreate;
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
            if (Unit != null)
            {
                Unit.Dispose();
                Unit = null;
            }
            IsSelf = default;
            MessageObjectPool<M2C_UnitCreate>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.M2C_UnitCreate; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public UnitInfo Unit { get; set; }
        [ProtoMember(2)]
        public bool IsSelf { get; set; }
    }
    /// <summary>
    /// Map通知客户端有Unit离开
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class M2C_UnitLeave : AMessage, IRoamingMessage
    {
        public static M2C_UnitLeave Create(bool autoReturn = true)
        {
            var m2C_UnitLeave = MessageObjectPool<M2C_UnitLeave>.Rent();
            m2C_UnitLeave.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2C_UnitLeave.SetIsPool(false);
            }
            
            return m2C_UnitLeave;
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
            UnitId = default;
            MessageObjectPool<M2C_UnitLeave>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.M2C_UnitLeave; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public long UnitId { get; set; }
    }
    /// <summary>
    /// Unit信息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class UnitInfo : AMessage, IDisposable
    {
        public static UnitInfo Create(bool autoReturn = true)
        {
            var unitInfo = MessageObjectPool<UnitInfo>.Rent();
            unitInfo.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                unitInfo.SetIsPool(false);
            }
            
            return unitInfo;
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
            UnitId = default;
            Name = default;
            if (Pos != null)
            {
                Pos.Dispose();
                Pos = null;
            }
            UnitType = default;
            MessageObjectPool<UnitInfo>.Return(this);
        }
        [ProtoMember(1)]
        public long UnitId { get; set; }
        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public Position Pos { get; set; }
        [ProtoMember(4)]
        public int UnitType { get; set; }
    }
    /// <summary>
    /// 客户端发送请求移动
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class C2M_MoveRequest : AMessage, IRoamingRequest
    {
        public static C2M_MoveRequest Create(bool autoReturn = true)
        {
            var c2M_MoveRequest = MessageObjectPool<C2M_MoveRequest>.Rent();
            c2M_MoveRequest.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                c2M_MoveRequest.SetIsPool(false);
            }
            
            return c2M_MoveRequest;
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
            if (TargetPos != null)
            {
                TargetPos.Dispose();
                TargetPos = null;
            }
            MessageObjectPool<C2M_MoveRequest>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.C2M_MoveRequest; } 
        [ProtoIgnore]
        public M2C_MoveResponse ResponseType { get; set; }
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public Position TargetPos { get; set; }
    }
    [Serializable]
    [ProtoContract]
    public partial class M2C_MoveResponse : AMessage, IRoamingResponse
    {
        public static M2C_MoveResponse Create(bool autoReturn = true)
        {
            var m2C_MoveResponse = MessageObjectPool<M2C_MoveResponse>.Rent();
            m2C_MoveResponse.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2C_MoveResponse.SetIsPool(false);
            }
            
            return m2C_MoveResponse;
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
            Data.Clear();
            MessageObjectPool<M2C_MoveResponse>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.M2C_MoveResponse; } 
        [ProtoMember(1)]
        public uint ErrorCode { get; set; }
        [ProtoMember(2)]
        public List<Position> Data { get; set; } = new List<Position>();
    }
    /// <summary>
    /// 坐标信息
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class Position : AMessage, IDisposable
    {
        public static Position Create(bool autoReturn = true)
        {
            var position = MessageObjectPool<Position>.Rent();
            position.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                position.SetIsPool(false);
            }
            
            return position;
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
            X = default;
            Y = default;
            Z = default;
            MessageObjectPool<Position>.Return(this);
        }
        [ProtoMember(1)]
        public float X { get; set; }
        [ProtoMember(2)]
        public float Y { get; set; }
        [ProtoMember(3)]
        public float Z { get; set; }
    }
    /// <summary>
    /// 通知客户端Unit移动状态改变
    /// </summary>
    [Serializable]
    [ProtoContract]
    public partial class M2C_UnitMoveState : AMessage, IRoamingMessage
    {
        public static M2C_UnitMoveState Create(bool autoReturn = true)
        {
            var m2C_UnitMoveState = MessageObjectPool<M2C_UnitMoveState>.Rent();
            m2C_UnitMoveState.AutoReturn = autoReturn;
            
            if (!autoReturn)
            {
                m2C_UnitMoveState.SetIsPool(false);
            }
            
            return m2C_UnitMoveState;
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
            State = default;
            UnitId = default;
            if (Pos != null)
            {
                Pos.Dispose();
                Pos = null;
            }
            MessageObjectPool<M2C_UnitMoveState>.Return(this);
        }
        public uint OpCode() { return OuterOpcode.M2C_UnitMoveState; } 
        [ProtoIgnore]
        public int RouteType => Fantasy.RoamingType.MapRoamingType;
        [ProtoMember(1)]
        public int State { get; set; }
        [ProtoMember(2)]
        public long UnitId { get; set; }
        [ProtoMember(3)]
        public Position Pos { get; set; }
    }
}