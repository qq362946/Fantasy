using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fantasy.DataStructure;
// ReSharper disable CollectionNeverQueried.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Helper
{
    /// <summary>
    /// AssemblyInfo提供有关程序集和类型的信息
    /// </summary>
    public sealed class AssemblyInfo
    {
        /// <summary>
        /// 获取或设置与此程序集相关联的 <see cref="Assembly"/> 实例。
        /// </summary>
        public Assembly Assembly { get; private set; }
        /// <summary>
        /// 程序集类型集合，获取一个列表，包含从程序集加载的所有类型。
        /// </summary>
        public readonly List<Type> AssemblyTypeList = new List<Type>();
        /// <summary>
        /// 程序集类型分组集合，获取一个分组列表，将接口类型映射到实现这些接口的类型。
        /// </summary>
        public readonly OneToManyList<Type, Type> AssemblyTypeGroupList = new OneToManyList<Type, Type>();
        /// <summary>
        /// 构造函数
        /// </summary>
        public AssemblyInfo() { }
        /// <summary>
        /// 构造函数、可以传递程序集
        /// </summary>
        /// <param name="assembly"></param>
        public AssemblyInfo(Assembly assembly)
        {
            Load(assembly);
        }
        
        /// <summary>
        /// 从指定的程序集加载类型信息并进行分类。
        /// </summary>
        /// <param name="assembly">要加载信息的程序集。</param>
        public void Load(Assembly assembly)
        {
            Assembly = assembly;
            var assemblyTypes = assembly.GetTypes().ToList();

            foreach (var type in assemblyTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                var interfaces = type.GetInterfaces();

                foreach (var interfaceType in interfaces)
                {
                    AssemblyTypeGroupList.Add(interfaceType, type);
                }
            }

            AssemblyTypeList.AddRange(assemblyTypes);
        }

        /// <summary>
        /// 卸载程序集的类型信息。
        /// </summary>
        public void Unload()
        {
            AssemblyTypeList.Clear();
            AssemblyTypeGroupList.Clear();
        }
    }
}