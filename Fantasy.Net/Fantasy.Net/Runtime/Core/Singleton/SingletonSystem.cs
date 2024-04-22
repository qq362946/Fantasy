// ReSharper disable StaticMemberInGenericType
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Fantasy
{
    /// <summary>
    /// 程序集单例信息。
    /// </summary>
    internal class AssemblySingletonInfo : IDisposable
    {
        public readonly Dictionary<Type, ISingleton> Singletons = new Dictionary<Type, ISingleton>();

        public void Dispose()
        {
            foreach (var singleton in Singletons.Values.ToArray())
            {
                singleton.Dispose();
            }
        }
    }
    
    /// <summary>
    /// 单例管理系统，负责管理和调度实现 <see cref="ISingleton"/> 接口的单例对象。
    /// </summary>
    internal sealed class SingletonSystem : IAssembly
    {
        public static SingletonSystem Instance { get; private set; }
        private readonly ConcurrentDictionary<long, AssemblySingletonInfo> _singletonsByAssembly = new ConcurrentDictionary<long, AssemblySingletonInfo>();
        
        internal static Task Initialize()
        {
            Instance = new SingletonSystem();
            return AssemblySystem.Register(Instance);
        }
        
        #region Assembly
        
        public Task Load(long assemblyIdentity)
        {
            return Task.Run(() => { LoadInner(assemblyIdentity); });
        }
        
        public Task ReLoad(long assemblyIdentity)
        {
            return Task.Run(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                LoadInner(assemblyIdentity);
            });
        }
        
        public Task OnUnLoad(long assemblyIdentity)
        {
            return Task.Run(() => { OnUnLoadInner(assemblyIdentity); });
        }

        private ISingleton CreateInstance(Type singletonType, List<Task> initTasks, List<Task> regTasks)
        {
            var instance = (ISingleton)Activator.CreateInstance(singletonType);
            var registerMethodInfo = singletonType.BaseType?.GetMethod("Register", BindingFlags.Instance | BindingFlags.NonPublic);
            var initializeMethodInfo = singletonType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public);

            if (initializeMethodInfo != null)
            {
                initTasks.Add((Task)initializeMethodInfo.Invoke(instance, null));
            }

            if (registerMethodInfo != null)
            {
                regTasks.Add((Task)registerMethodInfo.Invoke(instance, new object[] { instance }));
            }

            return instance;
        }

        private void LoadInner(long assemblyIdentity)
        {
            var initTasks = new List<Task>();
            var regTasks = new List<Task>();
            
            foreach (var singletonType in AssemblySystem.ForEach(assemblyIdentity, typeof(ISingleton)))
            {
                if (!_singletonsByAssembly.TryGetValue(assemblyIdentity, out var assemblySingletonInfo))
                {
                    assemblySingletonInfo = new AssemblySingletonInfo();
                    _singletonsByAssembly.TryAdd(assemblyIdentity, assemblySingletonInfo);
                }
                
                var singleton = CreateInstance(singletonType, initTasks, regTasks);
                assemblySingletonInfo.Singletons.Add(singletonType, singleton);
            }
            
            if (initTasks.Count <= 0)
            {
                return;
            }
            
            Task.WaitAll(initTasks.ToArray());
            Task.WaitAll(regTasks.ToArray());
        }
        
        private void OnUnLoadInner(long assemblyIdentity)
        {
            if (!_singletonsByAssembly.TryRemove(assemblyIdentity, out var assemblySingletonInfo))
            {
                return;
            }
            
            assemblySingletonInfo.Dispose();
        }

        #endregion
        
        public void Dispose()
        {
            foreach (var (_, assemblySingletonInfo) in _singletonsByAssembly)
            {
                assemblySingletonInfo.Dispose();
            }
            
            _singletonsByAssembly.Clear();
            AssemblySystem.UnRegister(Instance);
        }
    }
}