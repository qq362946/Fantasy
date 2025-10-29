using System;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 网络协议响应类型解析器接口
    /// 用于根据 OpCode 解析对应的响应消息类型
    /// 此接口通常由 NetworkProtocol SourceGenerator 自动生成实现
    /// </summary>
    public interface INetworkProtocolResponseTypeResolver
    {
        /// <summary>
        /// 获取已注册的 Response 总数
        /// </summary>
        /// <returns>协议系统中可用的 OpCode 数量</returns>
        int GetRequestCount();

        /// <summary>
        /// 获取指定 OpCode 对应的响应消息类型
        /// 用于在处理网络消息时确定期望的响应类型
        /// </summary>
        /// <param name="opCode">要解析的 OpCode</param>
        /// <returns>响应消息的类型，如果该 OpCode 没有关联响应类型则返回 null</returns>
        Type GetResponseType(uint opCode);
    }
}