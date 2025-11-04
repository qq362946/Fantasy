using System;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 网络协议 OpCode 解析器接口
    /// </summary>
    /// <remarks>
    /// 此接口由 SourceGenerator 自动生成的类实现。
    /// 每个包含网络协议的程序集都会生成自己的解析器实现。
    /// </remarks>
    public interface IOpCodeRegistrar
    {
        /// <summary>
        /// 返回OpCode的总数
        /// </summary>
        /// <returns></returns>
        int GetOpCodeCount();
        /// <summary>
        /// 返回CustomRouteType的总数
        /// </summary>
        /// <returns></returns>
        int GetCustomRouteTypeCount();
        /// <summary>
        /// 填充OpCodeType到指定的数组中
        /// </summary>
        void FillOpCodeType(uint[] opCodes, Type[] types);
        /// <summary>
        /// 填充CustomRouteType到指定的数组中
        /// </summary>
        void FillCustomRouteType(uint[] opCodes, int[] routeTypes);
        
    }
}