using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fantasy.DataStructure.Collection;

namespace Fantasy.Assembly
{
    /// <summary>
    /// 程序集生命周期管理类
    /// 管理所有注册的程序集生命周期回调，在程序集加载、卸载时触发相应的回调方法
    /// </summary>
    public static class AssemblyLifecycle
    {
#if FANTASY_WEBGL
        /// <summary>
        /// 程序集生命周期回调集合（WebGL 单线程版本）
        /// </summary>
        private static readonly HashSet<IAssemblyLifecycle> AssemblyLifecycles = new ();
#else
        /// <summary>
        /// 程序集生命周期回调集合（线程安全版本）
        /// 使用 ConcurrentDictionary 当作 Set 使用，Value 无实际意义
        /// </summary>
        private static readonly ConcurrentHashSet<IAssemblyLifecycle> AssemblyLifecycles = new();
#endif
        
        internal static Task RunOnContext(ThreadSynchronizationContext context, Action action)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            context.Post(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                    return;
                }

                taskCompletionSource.SetResult(true);
            });

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// 触发程序集加载事件
        /// 遍历所有已注册的生命周期回调，调用其 OnLoad 方法
        /// </summary>
        /// <param name="assemblyManifest">程序集清单对象</param>
        /// <returns>异步任务</returns>
        internal static async Task OnLoad(AssemblyManifest assemblyManifest)
        {
            List<Task> loadTasks = new();
            
            foreach (IAssemblyLifecycle assemblyLifecycle in AssemblyLifecycles)
            {
                loadTasks.Add(assemblyLifecycle.OnLoad(assemblyManifest));
            }
            
            await Task.WhenAll(loadTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// 触发程序集卸载事件
        /// 遍历所有已注册的生命周期回调，调用其 OnUnload 方法，并清理程序集清单
        /// </summary>
        /// <param name="assemblyManifest">程序集清单对象</param>
        /// <returns>异步任务</returns>
        internal static async Task OnUnLoad(AssemblyManifest assemblyManifest)
        {
            List<Task> unloadTasks = new();
            
            foreach (IAssemblyLifecycle assemblyLifecycle in AssemblyLifecycles)
            {
                unloadTasks.Add(RunOnUnload(assemblyLifecycle));
            }
            
            await Task.WhenAll(unloadTasks).ConfigureAwait(false);
            assemblyManifest.Clear();
            
            async Task RunOnUnload(IAssemblyLifecycle assemblyLifecycle)
            {
                try
                {
                    await assemblyLifecycle
                        .OnUnload(assemblyManifest)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 添加程序集生命周期回调
        /// 添加后会立即对所有已加载的程序集触发 Load 回调
        /// </summary>
        /// <param name="assemblyLifecycle">实现 IAssemblyLifecycle 接口的生命周期回调对象</param>
        public static async Task Add(IAssemblyLifecycle assemblyLifecycle)
        {
#if FANTASY_WEBGL
            AssemblyLifecycles.Add(assemblyLifecycle);
#else
            AssemblyLifecycles.TryAdd(assemblyLifecycle);
#endif
            List<Task> loadTasks = new();

            foreach (var (_, assemblyManifest) in AssemblyManifest.Manifests)
            {
                loadTasks.Add(assemblyLifecycle.OnLoad(assemblyManifest));
            }

            await Task.WhenAll(loadTasks).ConfigureAwait(false);
        }

        /// <summary>
        /// 移除程序集生命周期回调
        /// 移除后该回调将不再接收程序集的加载、卸载、重载事件
        /// </summary>
        /// <param name="assemblyLifecycle">要移除的生命周期回调对象</param>
        public static void Remove(IAssemblyLifecycle assemblyLifecycle)
        {
            AssemblyLifecycles.Remove(assemblyLifecycle);
        }

        /// <summary>
        /// 释放所有程序集生命周期回调
        /// 清空所有已注册的生命周期回调集合
        /// </summary>
        public static void Dispose()
        {
            AssemblyLifecycles.Clear();
        }
    }
}