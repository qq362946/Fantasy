using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fantasy.Attributes;
using Fantasy.DataStructure.Collection;

// ReSharper disable CollectionNeverQueried.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Fantasy.Assembly
{
    /// <summary>
    /// AssemblyInfo提供有关程序集和类型的信息
    /// </summary>
    public sealed class AssemblyInfo
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public readonly long AssemblyIdentity;
        /// <summary>
        /// 获取或设置与此程序集相关联的 <see cref="Assembly"/> 实例。
        /// </summary>
        public System.Reflection.Assembly Assembly { get; private set; }
        /// <summary>
        /// 程序集类型集合，获取一个列表，包含从程序集加载的所有类型。
        /// </summary>
        public readonly List<Type> AssemblyTypeList = new ();
        /// <summary>
        /// 程序集类型分组集合，获取一个分组列表，将接口类型映射到实现这些接口的类型。
        /// </summary>
        public readonly OneToManyList<Type, Type> AssemblyTypeGroupList = new ();
        /// <summary>
        /// 从Attributes中提取的开放泛型映射到闭合泛型标签类型
        /// </summary>
        public readonly OneToManyList<Type, Type> ClosedGenericsFromAttributesByDefinition = new ();
        /// <summary>
        /// 初始化 <see cref="AssemblyInfo"/> 类的新实例。
        /// </summary>
        /// <param name="assemblyIdentity"></param>
        public AssemblyInfo(long assemblyIdentity)
        {
            AssemblyIdentity = assemblyIdentity;
        }

        /// <summary>
        /// 从指定的程序集加载类型信息并进行分类。
        /// </summary>
        /// <param name="assembly">要加载信息的程序集。</param>
        public void Load(System.Reflection.Assembly assembly)
        {
            Assembly = assembly;
            var assemblyTypes = assembly.GetTypes().ToList();

            foreach (var type in assemblyTypes)
            {

                CollectClosedGenericsFromAttributes(type);

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
        /// 从闭合泛型标签中收集类型
        /// </summary>
        private void CollectClosedGenericsFromAttributes(Type type)
        {
            var closedAttrs = type.GetCustomAttributes<ClosedGenericsAttribute>(false);
            foreach (var attr in closedAttrs)
            {
                if (attr.theClosed != null)
                {
                    foreach (var closedType in attr.theClosed)
                    {                   
                        ClosedGenericsFromAttributesByDefinition.Add(closedType.GetGenericTypeDefinition(),closedType);
                    }
                }
            }
        }

        /// <summary>
        /// 重新加载程序集的类型信息。
        /// </summary>
        /// <param name="assembly"></param>
        public void ReLoad(System.Reflection.Assembly assembly)
        {
            Unload();
            Load(assembly);
        }

        /// <summary>
        /// 卸载程序集的类型信息。
        /// </summary>
        public void Unload()
        {
            AssemblyTypeList.Clear();
            AssemblyTypeGroupList.Clear();
            ClosedGenericsFromAttributesByDefinition.Clear();
        }
    }
}