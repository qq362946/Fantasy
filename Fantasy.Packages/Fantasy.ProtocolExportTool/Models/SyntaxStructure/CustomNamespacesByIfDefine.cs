using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fantasy.ProtocolExportTool.Models
{
    /// <summary>
    /// 自定义命名空间字典, 根据条件编译符分组。实现了<see cref="IfDefineDictionary{T}"/>。
    /// </summary>
    public sealed class CustomNamespacesByIfDefine : IfDefineDictionary<string>
    {
        /// <summary>
        /// 将列表元素写出字符串
        /// </summary>
        /// <param name="customNamespace"></param>
        /// <returns></returns>
        public override string WriteCSharpLine(string listElement, object? arg = null)
        {
            return $"using {listElement.Replace(";","")};"; // 写出命名空间
        }
    }
}
