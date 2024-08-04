using System;

namespace Fantasy
{
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
    
    /// <summary>
    /// 表示可以序列化为 BSON 格式的消息接口。
    /// </summary>
    public interface IBsonMessage : IMessage
    {
    
    }

    /// <summary>
    /// 表示可以序列化为 BSON 格式的请求消息接口。
    /// </summary>
    public interface IBsonRequest : IBsonMessage, IRequest
    {
        
    }

    /// <summary>
    /// 表示可以序列化为 BSON 格式的响应消息接口。
    /// </summary>
    public interface IBsonResponse : IBsonMessage, IResponse
    {
        
    }
    
    // 普通路由消息
    /// <summary>
    /// 表示普通路由消息的接口，继承自请求接口。
    /// </summary>
    public interface IRouteMessage : IRequest
    {
        /// <summary>
        /// 获取路由消息的操作代码。
        /// </summary>
        /// <returns>操作代码。</returns>
        long RouteTypeOpCode();
    }

    /// <summary>
    /// 普通路由请求接口，继承自普通路由消息接口。
    /// </summary>
    public interface IRouteRequest : IRouteMessage { }
    /// <summary>
    /// 普通路由响应接口，继承自响应接口。
    /// </summary>
    public interface IRouteResponse : IResponse { }

    // 普通路由Bson消息
    /// <summary>
    /// 表示普通路由Bson消息的接口，继承自Bson消息和普通路由消息接口。
    /// </summary>
    public interface IBsonRouteMessage : IBsonMessage, IRouteMessage { }
    /// <summary>
    /// 普通路由Bson请求接口，继承自普通路由Bson消息接口。
    /// </summary>
    public interface IBsonRouteRequest : IBsonRouteMessage, IRouteRequest { }
    /// <summary>
    /// 普通路由Bson响应接口，继承自Bson响应接口。
    /// </summary>
    public interface IBsonRouteResponse : IBsonResponse, IRouteResponse { }

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

    // 可寻址Bson协议
    /// <summary>
    /// 表示可寻址Bson协议的普通路由消息接口，继承自Bson消息和可寻址协议的普通路由消息接口。
    /// </summary>
    public interface IBsonAddressableRouteMessage : IBsonMessage, IAddressableRouteMessage { }
    /// <summary>
    /// 可寻址Bson协议的普通路由请求接口，继承自可寻址Bson协议的普通路由消息接口。
    /// </summary>
    public interface IBsonAddressableRouteRequest : IBsonRouteMessage, IAddressableRouteRequest { }
    /// <summary>
    /// 可寻址Bson协议的普通路由响应接口，继承自Bson响应接口。
    /// </summary>
    public interface IBsonAddressableRouteResponse : IBsonResponse, IAddressableRouteResponse { }

    // 自定义Route协议
    /// <summary>
    /// 表示自定义Route协议的普通路由消息接口，继承自普通路由消息接口。
    /// </summary>
    public interface ICustomRouteMessage : IRouteMessage { }
    /// <summary>
    /// 自定义Route协议的普通路由请求接口，继承自自定义Route协议的普通路由消息接口。
    /// </summary>
    public interface ICustomRouteRequest : IRouteRequest { }
    /// <summary>
    /// 自定义Route协议的普通路由响应接口，继承自普通路由响应接口。
    /// </summary>
    public interface ICustomRouteResponse : IRouteResponse { }
}