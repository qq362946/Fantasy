using System;
using System.Runtime.Serialization;
using Fantasy.Async;
using Fantasy.Pool;
using Fantasy.Serialize;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using ProtoBuf;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy.Network.Interface
{
    public abstract class AMessage : ASerialize, IPool
    {
#if FANTASY_NET || FANTASY_UNITY || FANTASY_CONSOLE
        [BsonIgnore] 
        [JsonIgnore] 
        [IgnoreDataMember] 
        [ProtoIgnore]
        private Scene _scene;
        protected Scene GetScene()
        {
            return _scene;
        }

        public void SetScene(Scene scene)
        {
            _scene = scene;
        }
#endif
#if FANTASY_NET
        [BsonIgnore] 
#endif
        [JsonIgnore] 
        [IgnoreDataMember] 
        [ProtoIgnore]
        private bool _isPool;

        public bool IsPool()
        {
            return _isPool;
        }

        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }
    }
    /// <summary>
    /// 表示通用消息接口。
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// 获取消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        uint OpCode();
    }

    /// <summary>
    /// 表示请求消息接口。
    /// </summary>
    public interface IRequest : IMessage
    {
        
    }

    /// <summary>
    /// 表示响应消息接口。
    /// </summary>
    public interface IResponse : IMessage
    {
        /// <summary>
        /// 获取或设置错误代码。
        /// </summary>
        uint ErrorCode { get; set; }
    }
    // 普通路由消息
    /// <summary>
    /// 表示普通路由消息的接口，继承自请求接口。
    /// </summary>
    public interface IRouteMessage : IRequest
    {
        
    }

    /// <summary>
    /// 普通路由请求接口，继承自普通路由消息接口。
    /// </summary>
    public interface IRouteRequest : IRouteMessage { }
    /// <summary>
    /// 普通路由响应接口，继承自响应接口。
    /// </summary>
    public interface IRouteResponse : IResponse { }
    // 可寻址协议
    /// <summary>
    /// 表示可寻址协议的普通路由消息接口，继承自普通路由消息接口。
    /// </summary>
    public interface IAddressableRouteMessage : IRouteMessage { }
    /// <summary>
    /// 可寻址协议的普通路由请求接口，继承自可寻址协议的普通路由消息接口。
    /// </summary>
    public interface IAddressableRouteRequest : IRouteRequest { }
    /// <summary>
    /// 可寻址协议的普通路由响应接口，继承自普通路由响应接口。
    /// </summary>
    public interface IAddressableRouteResponse : IRouteResponse { }
    // 自定义Route协议
    public interface ICustomRoute : IMessage
    {
        int RouteType { get; }
    }
    /// <summary>
    /// 表示自定义Route协议的普通路由消息接口，继承自普通路由消息接口。
    /// </summary>
    public interface ICustomRouteMessage : IRouteMessage, ICustomRoute { }
    /// <summary>
    /// 自定义Route协议的普通路由请求接口，继承自自定义Route协议的普通路由消息接口。
    /// </summary>
    public interface ICustomRouteRequest : IRouteRequest, ICustomRoute { }
    /// <summary>
    /// 自定义Route协议的普通路由响应接口，继承自普通路由响应接口。
    /// </summary>
    public interface ICustomRouteResponse : IRouteResponse { }
    /// <summary>
    /// 表示漫游协议的普通路由消息接口，继承自普通路由消息接口。
    /// </summary>
    public interface IRoamingMessage : IRouteMessage, ICustomRoute { }
    /// <summary>
    /// 漫游协议的普通路由请求接口，继承自自定义Route协议的普通路由消息接口。
    /// </summary>
    public interface IRoamingRequest : IRoamingMessage { }
    /// <summary>
    /// 漫游协议的普通路由响应接口，继承自普通路由响应接口。
    /// </summary>
    public interface IRoamingResponse : IRouteResponse { }
}