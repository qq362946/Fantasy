using System;
using System.Runtime.CompilerServices;
using Fantasy.Async;
using Fantasy.DataStructure.Collection;
using Fantasy.Entitas;
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace Fantasy.EventAwaiter
{
    /// <summary>
    /// 事件等待器组件，用于实现类型化的异步等待和通知机制
    /// </summary>
    public sealed class EventAwaiterComponent : Entity
    {
        /// <summary>
        /// 存储不同类型事件的等待回调队列
        /// </summary>
        private OneToManyList<RuntimeTypeHandle, object> WaitCallbacks { get; } = new();

        /// <summary>
        /// 等待指定类型的事件
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async FTask<T> Wait<T>(FCancellationToken? cancellationToken = null) where T : struct
        {
            var typeHandle = typeof(T).TypeHandle;
            var sceneEventAwaiterPool = Scene.EventAwaiterPool;
            var eventAwaiterCallback = sceneEventAwaiterPool.Rent<EventAwaiterCallback<T>>().Initialize(Scene);

            WaitCallbacks.Add(typeHandle, eventAwaiterCallback);

            T result;

            if (cancellationToken != null)
            {
                var cancelAction = sceneEventAwaiterPool.Rent<EventAwaiterCancelAction<T>>().Initialize(this);

                try
                {
                    cancellationToken.Add(cancelAction.Action);
                    result = await eventAwaiterCallback.Task;
                }
                finally
                {
                    cancellationToken.Remove(cancelAction.Action);
                    sceneEventAwaiterPool.Return(typeof(EventAwaiterCancelAction<T>), cancelAction);
                }
            }
            else
            {
                result = await eventAwaiterCallback.Task;
            }

            sceneEventAwaiterPool.Return(typeof(EventAwaiterCallback<T>), eventAwaiterCallback);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async FTask<T> Wait<T>(int timeout, FCancellationToken? cancellationToken = null) where T : struct
        {
            var typeHandle = typeof(T).TypeHandle;
            var sceneEventAwaiterPool = Scene.EventAwaiterPool;
            var eventAwaiterCallback = sceneEventAwaiterPool.Rent<EventAwaiterCallback<T>>().Initialize(Scene);
            var timeoutHandler = EventAwaiterTimeoutHandler<T>.StartTimeout(Scene, eventAwaiterCallback, timeout, cancellationToken);

            WaitCallbacks.Add(typeHandle, eventAwaiterCallback);

            T result;

            if (cancellationToken != null)
            {
                var cancelAction = sceneEventAwaiterPool.Rent<EventAwaiterCancelAction<T>>().Initialize(this);

                try
                {
                    cancellationToken.Add(cancelAction.Action);
                    result = await eventAwaiterCallback.Task;
                }
                finally
                {
                    cancellationToken.Remove(cancelAction.Action);
                    timeoutHandler.Cancel(); 
                    sceneEventAwaiterPool.Return(typeof(EventAwaiterCancelAction<T>), cancelAction);
                }
            }
            else
            {
                try
                {
                    result = await eventAwaiterCallback.Task;
                }
                finally
                {
                    timeoutHandler.Cancel(); 
                }
            }

            sceneEventAwaiterPool.Return(typeof(EventAwaiterCallback<T>), eventAwaiterCallback);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        public void Notify<T>(T obj) where T : struct
        {
            var typeHandle = typeof(T).TypeHandle;

            if (!WaitCallbacks.TryGetValue(typeHandle, out var wailtCallbackList))
            {
                return;
            }

            foreach (var wailtCallback in wailtCallbackList)
            {
                ((EventAwaiterCallback<T>)wailtCallback).SetResult(obj);
            }

            WaitCallbacks.RemoveKey(typeHandle);
        }
    }
}