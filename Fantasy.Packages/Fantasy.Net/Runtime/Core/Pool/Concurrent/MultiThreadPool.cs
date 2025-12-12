#if !FANTASY_WEBGL
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
#pragma warning disable CS8603 // Possible null reference return.

namespace Fantasy.Pool
{
    /// <summary>
    /// 线程安全的静态通用对象池。
    /// </summary>
    internal static class MultiThreadPool
    {
        private static readonly ConcurrentDictionary<Type, MultiThreadPoolQueue> ObjectPools = new ConcurrentDictionary<Type, MultiThreadPoolQueue>();

        public static T Rent<T>() where T : IPool, new()
        {
            return ObjectPools.GetOrAdd(typeof(T), t => new MultiThreadPoolQueue(2000, () => new T())).Rent<T>();
        }

        public static IPool Rent(Scene scene, Type type)
        {

            return ObjectPools.GetOrAdd(type, t =>
            {
                return new MultiThreadPoolQueue(2000,
                    () => scene.PoolGeneratorComponent.Create(type));
            }).Rent();
        }

        public static void Return<T>(T obj) where T : IPool, new()
        {
            if (!obj.IsPool())
            {
                return;
            }

            ObjectPools.GetOrAdd(typeof(T), t => new MultiThreadPoolQueue(2000, () => new T())).Return(obj);
        }
    }

    /// <summary>
    /// 基于<see cref="MultiThreadPoolStack"/>的线程安全静态通用对象池。
    /// (比基于<see cref="MultiThreadPoolQueue"/>的实现<see cref="MultiThreadPool"/>略快)。
    /// 缓存友好, 且能更好地在开发时暴露池对象未清理干净的问题, 但是不具备公平性(池中对象不会被平等地利用)。
    /// </summary>
    internal static class MultiThreadPoolStacks
    {
        private static readonly ConcurrentDictionary<Type, MultiThreadPoolStack> ObjectPools =
            new ConcurrentDictionary<Type, MultiThreadPoolStack>();

        private const int DefaultMaxCapacity = 2000;

        /// <summary>
        /// 从池中租用指定类型 T 的对象。如果池不存在，则创建并使用默认构造函数初始化。
        /// </summary>
        public static T Rent<T>() where T : IPool, new()
        {
            MultiThreadPoolStack poolStack = ObjectPools.GetOrAdd(
                typeof(T),
                t => new MultiThreadPoolStack(DefaultMaxCapacity, () => new T())
            );
            return poolStack.Rent<T>();
        }

        /// <summary>
        /// 从池中租用指定 Type 的对象。如果池不存在，则创建并使用 Scene 的组件来创建实例。
        /// </summary>
        public static IPool Rent(Scene scene, Type type)
        {
            MultiThreadPoolStack poolStack = ObjectPools.GetOrAdd(
                type,
                t => new MultiThreadPoolStack(
                    DefaultMaxCapacity,
                    () => scene.PoolGeneratorComponent.Create(type)
                )
            );

            return poolStack.Rent();
        }

        /// <summary>
        /// 将对象归还给池子。
        /// </summary>
        public static void Return<T>(T obj) where T : IPool, new()
        {
            if (!obj.IsPool())
            {
                return;
            }

            MultiThreadPoolStack poolStack = ObjectPools.GetOrAdd(
                typeof(T),
                t => new MultiThreadPoolStack(DefaultMaxCapacity, () => new T())
            );

            poolStack.Return(obj);
        }
    }
}
#endif