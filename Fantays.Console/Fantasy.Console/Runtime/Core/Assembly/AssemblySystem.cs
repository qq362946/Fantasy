using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Fantasy.Async;
using Fantasy.Helper;

#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603
#pragma warning disable CS8618
namespace Fantasy.Assembly
{
    /// <summary>
    /// 管理程序集加载和卸载的帮助类。
    /// </summary>
    public static class AssemblySystem
    {
#if FANTASY_WEBGL
        private static readonly List<IAssembly> AssemblySystems = new List<IAssembly>();
        private static readonly Dictionary<long, AssemblyInfo> AssemblyList = new Dictionary<long, AssemblyInfo>();
#else
        private static readonly ConcurrentBag<IAssembly> AssemblySystems = new ConcurrentBag<IAssembly>();
        private static readonly ConcurrentDictionary<long, AssemblyInfo> AssemblyList = new ConcurrentDictionary<long, AssemblyInfo>();
#endif
        /// <summary>
        /// 初始化 AssemblySystem。
        /// </summary>
        public static void Initialize(params System.Reflection.Assembly[] assemblies)
        {
            LoadAssembly(typeof(AssemblySystem).Assembly);
            foreach (var assembly in assemblies)
            {
                LoadAssembly(assembly);
            }
        }

        /// <summary>
        /// 加载指定的程序集，并触发相应的事件。
        /// </summary>
        /// <param name="assembly">要加载的程序集。</param>
        public static void LoadAssembly(System.Reflection.Assembly assembly)
        {
            var assemblyIdentity = AssemblyIdentity(assembly);
                
            if (AssemblyList.TryGetValue(assemblyIdentity, out var assemblyInfo))
            {
                assemblyInfo.Unload();
                foreach (var assemblySystem in AssemblySystems)
                {
                    assemblySystem.ReLoad(assemblyIdentity);
                }
            }
            else
            {
                assemblyInfo = new AssemblyInfo(assemblyIdentity);
                AssemblyList.TryAdd(assemblyIdentity, assemblyInfo);
                foreach (var assemblySystem in AssemblySystems)
                {
                    assemblySystem.Load(assemblyIdentity);
                }
            }
            
            assemblyInfo.Load(assembly);
        }

        /// <summary>
        /// 卸载程序集
        /// </summary>
        /// <param name="assembly"></param>
        public static void UnLoadAssembly(System.Reflection.Assembly assembly)
        {
            var assemblyIdentity = AssemblyIdentity(assembly);
            
            if (!AssemblyList.Remove(assemblyIdentity, out var assemblyInfo))
            {
                return;
            }
            
            assemblyInfo.Unload();
            foreach (var assemblySystem in AssemblySystems)
            {
                assemblySystem.OnUnLoad(assemblyIdentity);
            }
        }
        
        /// <summary>
        /// 将AssemblySystem接口的object注册到程序集管理中心
        /// </summary>
        /// <param name="obj"></param>
        public static async FTask Register(object obj)
        {
            if (obj is not IAssembly assemblySystem)
            {
                return;
            }
            
            AssemblySystems.Add(assemblySystem);
            
            foreach (var (assemblyIdentity, _) in AssemblyList)
            {
                await assemblySystem.Load(assemblyIdentity);
            }
        }

        /// <summary>
        /// 程序集管理中心卸载注册的Load、ReLoad、UnLoad的接口
        /// </summary>
        /// <param name="obj"></param>
        public static void UnRegister(object obj)
        {
            if (obj is not IAssembly assemblySystem)
            {
                return;
            }
#if FANTASY_WEBGL
            AssemblySystems.Remove(assemblySystem);
#else
            while (AssemblySystems.TryTake(out var removeAssemblySystem))
            {
                if (removeAssemblySystem == assemblySystem)
                {
                    continue;
                }

                AssemblySystems.Add(removeAssemblySystem);
            }
#endif
        }

        /// <summary>
        /// 获取所有已加载程序集中的所有类型。
        /// </summary>
        /// <returns>所有已加载程序集中的类型。</returns>
        public static IEnumerable<Type> ForEach()
        {
            foreach (var (_, assemblyInfo) in AssemblyList)
            {
                foreach (var type in assemblyInfo.AssemblyTypeList)
                {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// 获取指定程序集中的所有类型。
        /// </summary>
        /// <param name="assemblyIdentity">程序集唯一标识。</param>
        /// <returns>指定程序集中的类型。</returns>
        public static IEnumerable<Type> ForEach(long assemblyIdentity)
        {
            if (!AssemblyList.TryGetValue(assemblyIdentity, out var assemblyInfo))
            {
                yield break;
            }

            foreach (var type in assemblyInfo.AssemblyTypeList)
            {
                yield return type;
            }
        }

        /// <summary>
        /// 获取所有已加载程序集中实现指定类型的所有类型。
        /// </summary>
        /// <param name="findType">要查找的基类或接口类型。</param>
        /// <returns>所有已加载程序集中实现指定类型的类型。</returns>
        public static IEnumerable<Type> ForEach(Type findType)
        {
            foreach (var (_, assemblyInfo) in AssemblyList)
            {
                if (!assemblyInfo.AssemblyTypeGroupList.TryGetValue(findType, out var assemblyLoad))
                {
                    continue;
                }
                
                foreach (var type in assemblyLoad)
                {
                    yield return type;
                }
            }
        }

        /// <summary>
        /// 获取指定程序集中实现指定类型的所有类型。
        /// </summary>
        /// <param name="assemblyIdentity">程序集唯一标识。</param>
        /// <param name="findType">要查找的基类或接口类型。</param>
        /// <returns>指定程序集中实现指定类型的类型。</returns>
        public static IEnumerable<Type> ForEach(long assemblyIdentity, Type findType)
        {
            if (!AssemblyList.TryGetValue(assemblyIdentity, out var assemblyInfo))
            {
                yield break;
            }

            if (!assemblyInfo.AssemblyTypeGroupList.TryGetValue(findType, out var assemblyLoad))
            {
                yield break;
            }
            
            foreach (var type in assemblyLoad)
            {
                yield return type;
            }
        }

        /// <summary>
        /// 获取指定程序集的实例。
        /// </summary>
        /// <param name="assemblyIdentity">程序集名称。</param>
        /// <returns>指定程序集的实例，如果未加载则返回 null。</returns>
        public static System.Reflection.Assembly GetAssembly(long assemblyIdentity)
        {
            return !AssemblyList.TryGetValue(assemblyIdentity, out var assemblyInfo) ? null : assemblyInfo.Assembly;
        }

        /// <summary>
        /// 获取当前框架注册的Assembly
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<System.Reflection.Assembly> ForEachAssembly
        {
            get
            {
                foreach (var (_, assemblyInfo) in AssemblyList)
                {
                    yield return assemblyInfo.Assembly;
                }
            }
        }
        
        /// <summary>
        /// 根据Assembly的强命名计算唯一标识。
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static long AssemblyIdentity(System.Reflection.Assembly assembly)
        {
            return HashCodeHelper.ComputeHash64(assembly.GetName().Name);
        }

        /// <summary>
        /// 释放资源，卸载所有加载的程序集。
        /// </summary>
        public static void Dispose()
        {
            foreach (var (_, assemblyInfo) in AssemblyList.ToArray())
            {
                UnLoadAssembly(assemblyInfo.Assembly);
            }
            
            AssemblyList.Clear();
            AssemblySystems.Clear();
        }
    }
}