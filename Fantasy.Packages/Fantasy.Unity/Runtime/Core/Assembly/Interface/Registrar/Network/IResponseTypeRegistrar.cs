using System;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 网络协议响应类型注册器接口
    /// 用于根据 OpCode 解析对应的响应消息类型
    /// 此接口通常由 NetworkProtocol SourceGenerator 自动生成实现
    /// </summary>
    public interface IResponseTypeRegistrar
    {
        /// <summary>
        /// 获取已注册的 Response 总数
        /// </summary>
        /// <returns>协议系统中可用的 OpCode 数量</returns>
        int GetRequestCount();
        /// <summary>
        /// FillResponseType
        /// </summary>
        void FillResponseType(uint[] opCodes, Type[] types);
    }
}