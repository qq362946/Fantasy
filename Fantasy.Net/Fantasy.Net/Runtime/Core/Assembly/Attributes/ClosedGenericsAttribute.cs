using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Fantasy.Helper;

namespace Fantasy.Attributes
{
    /// <summary>
    /// 预注册闭合泛型类的Attribute。
    /// 在任意类或枚举类打上这个标签, 框架在初始化阶段将闭合的泛型提前注册实例。 
    /// 否则, 首次Awake时会触发自动闭合, 将产生反射构造和实例化开销。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum , Inherited = false, AllowMultiple = true)]
    public class ClosedGenericsAttribute : Attribute
    {
        /// <summary>
        /// 闭合泛型
        /// </summary>
        public Type[]? theClosed { get; }
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="theClosedGenerics"></param>
        public ClosedGenericsAttribute(Type[] theClosedGenerics)
        {
            theClosed = theClosedGenerics;
        }
    }
}
