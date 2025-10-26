using System;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 网络协议 OpCode 解析器接口
    /// 用于通过生成的 switch 表达式实现高性能的 OpCode 到 Type 的解析
    /// </summary>
    /// <remarks>
    /// 此接口由 SourceGenerator 自动生成的类实现。
    /// 每个包含网络协议的程序集都会生成自己的解析器实现。
    /// 生成的实现使用 switch 表达式而不是字典查找，以获得更好的性能。
    /// </remarks>
    public interface INetworkProtocolOpCodeResolver
    {
        /// <summary>
        /// 获取当前OpCode数量
        /// </summary>
        /// <returns>返回对应的OpCode数量</returns>
        int GetOpCodeCount();
        /// <summary>
        /// 获取当前RouteType数量
        /// </summary>
        /// <returns>返回对应的RouteType数量</returns>
        int GetCustomRouteTypeCount();
        /// <summary>
        /// 根据指定的 OpCode 获取对应的 Type
        /// </summary>
        /// <param name="opCode">网络协议操作码</param>
        /// <returns>OpCode 对应的类型；如果未找到则返回 null</returns>
        Type GetOpCodeType(uint opCode);
        /// <summary>
        /// 根据指定的 OpCode 获取对应的 CustomRouteType
        /// </summary>
        /// <param name="opCode">网络协议操作码</param>
        /// <returns>OpCode 对应的CustomRouteType；如果未找到则返回 null</returns>
        int? GetCustomRouteType(uint opCode);
        
    }
}