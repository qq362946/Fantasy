#if !FANTASY_WEBGL && !UNITY_WEBGL
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy.Async
{
    /// <summary>
    /// 一个可跨线程完成的异步任务
    /// </summary>
    public sealed partial class FThreadTask
    {
        private const int MaxPoolSize = 2000;

        private bool _isPool;

        [ThreadStatic]
        private static Stack<FThreadTask>? _caches;

        /// <summary>
        /// 创建一个已完成的任务
        /// </summary>
        public static FTaskCompleted CompletedTask => new FTaskCompleted();

        private FThreadTask() { }

        /// <summary>
        /// 创建一个可跨线程完成的任务
        /// </summary>
        /// <param name="isPool">是否从对象池中创建</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FThreadTask Create(bool isPool = true)
        {
            if (!isPool)
            {
                return new FThreadTask();
            }

            var caches = _caches;
            FThreadTask task;

            if (caches != null && caches.Count > 0)
            {
                task = caches.Pop();
            }
            else
            {
                task = new FThreadTask();
            }

            task._isPool = true;
            return task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Return()
        {
            if (!_isPool)
            {
                return;
            }

            var caches = _caches;
            if (caches == null)
            {
                _caches = caches = new Stack<FThreadTask>(16);
            }

            if (caches.Count >= MaxPoolSize)
            {
                return;
            }

            _exception = null;
            _callBack = null;
            _status = (int)STaskStatus.Pending;
            caches.Push(this);
        }
    }

    /// <summary>
    /// 一个可跨线程完成的异步任务
    /// </summary>
    /// <typeparam name="T">任务结果类型</typeparam>
    public sealed partial class FThreadTask<T>
    {
        private const int MaxPoolSize = 2000;

        private bool _isPool;

        [ThreadStatic]
        private static Stack<FThreadTask<T>>? _caches;

        private FThreadTask() { }

        /// <summary>
        /// 创建一个可跨线程完成的任务
        /// </summary>
        /// <param name="isPool">是否从对象池中创建</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FThreadTask<T> Create(bool isPool = true)
        {
            if (!isPool)
            {
                return new FThreadTask<T>();
            }

            var caches = _caches;
            FThreadTask<T> task;

            if (caches != null && caches.Count > 0)
            {
                task = caches.Pop();
            }
            else
            {
                task = new FThreadTask<T>();
            }

            task._isPool = true;
            return task;
        }

        /// <summary>
        /// 创建一个已完成的任务
        /// </summary>
        /// <param name="result">任务结果</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FThreadTask<T> FromResult(T result)
        {
            var task = Create();
            task.SetResult(result);
            return task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Return()
        {
            if (!_isPool)
            {
                return;
            }

            var caches = _caches;
            if (caches == null)
            {
                _caches = caches = new Stack<FThreadTask<T>>(16);
            }

            if (caches.Count >= MaxPoolSize)
            {
                return;
            }

            _value = default!;
            _exception = null;
            _callBack = null;
            _status = (int)STaskStatus.Pending;
            caches.Push(this);
        }
    }
}
#endif
