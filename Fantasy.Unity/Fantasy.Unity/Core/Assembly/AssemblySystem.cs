using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
#if FANTASY_NET
using System.Runtime.Loader;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#endif
#pragma warning disable CS8603
#pragma warning disable CS8618
namespace Fantasy
{
    /// <summary>
    /// 管理程序集加载和卸载的帮助类。
    /// </summary>
    public static class AssemblySystem
    {
        private static readonly ConcurrentBag<IAssembly> AssemblySystems = new ConcurrentBag<IAssembly>();
        private static readonly ConcurrentDictionary<long, AssemblyInfo> AssemblyList = new ConcurrentDictionary<long, AssemblyInfo>();
        
        /// <summary>
        /// 初始化 AssemblySystem。
        /// </summary>
        public static void Initialize(params Assembly[] assemblies)
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
        public static void LoadAssembly(Assembly assembly)
        {
            var tasks = new List<Task>();
            var assemblyIdentity = AssemblyIdentity(assembly);
                
            if (AssemblyList.TryGetValue(assemblyIdentity, out var assemblyInfo))
            {
                assemblyInfo.Unload();
                foreach (var assemblySystem in AssemblySystems)
                {
                    tasks.Add(assemblySystem.ReLoad(assemblyIdentity));
                }
            }
            else
            {
                assemblyInfo = new AssemblyInfo(assemblyIdentity, assembly);
                AssemblyList.TryAdd(assemblyIdentity, assemblyInfo);
                foreach (var assemblySystem in AssemblySystems)
                {
                    tasks.Add(assemblySystem.Load(assemblyIdentity));
                }
            }
            
            // 加载新的程序集
            assemblyInfo.Load(assembly);

            if (tasks.Count <= 0)
            {
                return;
            }
                    
            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// 卸载程序集
        /// </summary>
        /// <param name="assembly"></param>
        public static void UnLoadAssembly(Assembly assembly)
        {
            var assemblyIdentity = AssemblyIdentity(assembly);
            
            if (!AssemblyList.Remove(assemblyIdentity, out var assemblyInfo))
            {
                return;
            }
            
            assemblyInfo.Unload();
            var tasks = new List<Task>();
            foreach (var assemblySystem in AssemblySystems)
            {
                tasks.Add(assemblySystem.OnUnLoad(assemblyIdentity));
            }
            Task.WaitAll(tasks.ToArray());
        }
        
        /// <summary>
        /// 将AssemblySystem接口的object注册到程序集管理中心
        /// </summary>
        /// <param name="obj"></param>
        public static async Task Register(object obj)
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
            
            while (AssemblySystems.TryTake(out var removeAssemblySystem))
            {
                if (removeAssemblySystem == assemblySystem)
                {
                    continue;
                }
                
                AssemblySystems.Add(removeAssemblySystem);
            }
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
        public static Assembly GetAssembly(long assemblyIdentity)
        {
            return !AssemblyList.TryGetValue(assemblyIdentity, out var assemblyInfo) ? null : assemblyInfo.Assembly;
        }
        
        /// <summary>
        /// 根据Assembly的强命名计算唯一标识。
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private static long AssemblyIdentity(Assembly assembly)
        {
            var assemblyName = assembly.GetName();
            var name = assemblyName.Name;
            var version = assemblyName.Version.ToString();
            var culture = assemblyName.CultureInfo?.Name ?? "neutral";
            var publicKeyToken = BitConverter.ToString(assemblyName.GetPublicKeyToken()).Replace("-", string.Empty);
            var assemblyIdentity = $"{name}|{version}|{culture}|{publicKeyToken}";
            var hashBytes = EncryptHelper.ComputeSha256Hash(Encoding.UTF8.GetBytes(assemblyIdentity));
            return BitConverter.ToInt64(hashBytes, 0);
        }

        /// <summary>
        /// 释放资源，卸载所有加载的程序集。
        /// </summary>
        public static void Dispose()
        {
            foreach (var (_, assemblyInfo) in AssemblyList)
            {
                assemblyInfo.Unload();
            }
            
            AssemblyList.Clear();
            AssemblySystems.Clear();
        }
    }
}