using System;
using Fantasy.DataStructure.Collection;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICustomInterface
    {
        
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ICustomInterfaceRegistrar
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="customRegistrar"></param>
        void Register(OneToManyList<RuntimeTypeHandle, CustomInterfaceInfo> customRegistrar);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="customRegistrar"></param>
        void UnRegister(OneToManyList<RuntimeTypeHandle, CustomInterfaceInfo> customRegistrar);
    }

    public sealed class CustomInterfaceInfo
    {
        public readonly Type Type;
        public readonly Func<ICustomInterface> CustomInterfaceFunc;
        
        public CustomInterfaceInfo(Type type, Func<ICustomInterface> customInterfaceFunc)
        {
            Type = type;
            CustomInterfaceFunc = customInterfaceFunc;
        }
    }
}