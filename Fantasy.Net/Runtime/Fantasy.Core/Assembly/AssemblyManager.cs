using System;
using System.Collections.Generic;
using System.Reflection;
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
    public static class AssemblyManager
    {
        /// <summary>
        /// 当程序集加载时触发的事件。
        /// </summary>
        public static event Action<int> OnLoadAssemblyEvent;
        /// <summary>
        /// 当程序集卸载时触发的事件。
        /// </summary>
        public static event Action<int> OnUnLoadAssemblyEvent;
        /// <summary>
        /// 当重新加载程序集时触发的事件。
        /// </summary>
        public static event Action<int> OnReLoadAssemblyEvent;
        /// <summary>
        /// 存储已加载的程序集信息的字典。
        /// </summary>
        private static readonly Dictionary<int, AssemblyInfo> AssemblyList = new Dictionary<int, AssemblyInfo>();

        /// <summary>
        /// 初始化 AssemblyManager，加载当前程序集。
        /// </summary>
        public static void Initialize()
        {
            LoadAssembly(int.MaxValue, typeof(AssemblyManager).Assembly);
        }

        /// <summary>
        /// 加载指定的程序集，并触发相应的事件。
        /// 无MaxValue判断。
        /// </summary>
        /// <param name="assemblyName">程序集名称。</param>
        /// <param name="assembly">要加载的程序集。</param>
        public static void LoadAssembly(int assemblyName, Assembly assembly)
        {
            var isReLoad = false;

            // 检查是否已经存在相同名称的程序集
            if (!AssemblyList.TryGetValue(assemblyName, out var assemblyInfo))
            {
                assemblyInfo = new AssemblyInfo();
                AssemblyList.Add(assemblyName, assemblyInfo);
            }
            else
            {
                // 若已存在，则表示重新加载
                isReLoad = true;
                // 卸载之前的程序集
                assemblyInfo.Unload();

                // 触发 OnUnLoadAssemblyEvent 事件，通知程序集被卸载
                if (OnUnLoadAssemblyEvent != null)
                {
                    OnUnLoadAssemblyEvent(assemblyName);
                }
            }

            // 加载新的程序集
            assemblyInfo.Load(assembly);

            // 触发 OnLoadAssemblyEvent 事件，通知程序集已加载
            if (OnLoadAssemblyEvent != null)
            {
                OnLoadAssemblyEvent(assemblyName);
            }

            // 若为重新加载且存在 OnReLoadAssemblyEvent 事件，则触发此事件，通知程序集已重新加载
            if (isReLoad && OnReLoadAssemblyEvent != null)
            {
                OnReLoadAssemblyEvent(assemblyName);
            }
        }

        /// <summary>
        /// 加载指定的程序集。有MaxValue判断
        /// </summary>
        /// <param name="assemblyName">程序集名称。</param>
        /// <param name="assembly">要加载的程序集。</param>
        /// <exception cref="NotSupportedException">当程序集名称为 <see cref="int.MaxValue"/> 时，抛出异常。</exception>
        public static void Load(int assemblyName, Assembly assembly)
        {
            if (int.MaxValue == assemblyName)
            {
                throw new NotSupportedException("AssemblyName cannot be 2147483647");
            }

            LoadAssembly(assemblyName, assembly);
        }

        /// <summary>
        /// 获取所有已加载程序集的名称。
        /// </summary>
        /// <returns>所有已加载程序集的名称。</returns>
        public static IEnumerable<int> ForEachAssemblyName()
        {
            foreach (var (key, _) in AssemblyList)
            {
                yield return key;
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
        /// <param name="assemblyName">程序集名称。</param>
        /// <returns>指定程序集中的类型。</returns>
        public static IEnumerable<Type> ForEach(int assemblyName)
        {
            if (!AssemblyList.TryGetValue(assemblyName, out var assemblyInfo))
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
        /// <param name="assemblyName">程序集名称。</param>
        /// <param name="findType">要查找的基类或接口类型。</param>
        /// <returns>指定程序集中实现指定类型的类型。</returns>
        public static IEnumerable<Type> ForEach(int assemblyName, Type findType)
        {
            if (!AssemblyList.TryGetValue(assemblyName, out var assemblyInfo))
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
        /// <param name="assemblyName">程序集名称。</param>
        /// <returns>指定程序集的实例，如果未加载则返回 null。</returns>
        public static Assembly GetAssembly(int assemblyName)
        {
            return !AssemblyList.TryGetValue(assemblyName, out var assemblyInfo) ? null : assemblyInfo.Assembly;
        }

        /// <summary>
        /// 释放资源，卸载所有加载的程序集。
        /// </summary>
        public static void Dispose()
        {
            // 卸载所有已加载的程序集
            foreach (var (_, assemblyInfo) in AssemblyList)
            {
                assemblyInfo.Unload();
            }

            // 清空已加载的程序集列表
            AssemblyList.Clear();

            // 移除所有事件处理程序，以避免事件泄漏和内存泄漏
            if (OnLoadAssemblyEvent != null)
            {
                foreach (var @delegate in OnLoadAssemblyEvent.GetInvocationList())
                {
                    OnLoadAssemblyEvent -= @delegate as Action<int>;
                }
            }
            
            if (OnUnLoadAssemblyEvent != null)
            {
                foreach (var @delegate in OnUnLoadAssemblyEvent.GetInvocationList())
                {
                    OnUnLoadAssemblyEvent -= @delegate as Action<int>;
                }
            }
            
            if (OnReLoadAssemblyEvent != null)
            {
                foreach (var @delegate in OnReLoadAssemblyEvent.GetInvocationList())
                {
                    OnReLoadAssemblyEvent -= @delegate as Action<int>;
                }
            }
        }
    }
}