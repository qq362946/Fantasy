// using System;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using Fantasy.Async;
// using Fantasy.DataStructure.Collection;
// using Fantasy.Entitas;
// using Fantasy.Entitas.Interface;
// using Fantasy.Timer;
// // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
//
// namespace Fantasy.Event
// {
//     /// <summary>
//     /// 事件等待结果类型
//     /// </summary>
//     public static class EventAwaiterResult
//     {
//         /// <summary>
//         /// 成功完成
//         /// </summary>
//         public const int Success = 0;
//
//         /// <summary>
//         /// 实体被销毁
//         /// </summary>
//         public const int Destroy = 1;
//
//         /// <summary>
//         /// 操作被取消
//         /// </summary>
//         public const int Cancel = 2;
//
//         /// <summary>
//         /// 等待超时
//         /// </summary>
//         public const int Timeout = 3;
//     }
//
//     /// <summary>
//     /// 事件类型接口，所有等待的事件类型都必须实现此接口
//     /// </summary>
//     public interface IEventType
//     {
//         /// <summary>
//         /// 错误码，使用 EventAwaiterResult 中的常量
//         /// </summary>
//         int Error { get; set; }
//     }
//
//     /// <summary>
//     /// ResultCallback 对象池
//     /// </summary>
//     internal sealed class EventAwaiterCallbackPool<T> where T : struct, IEventType
//     {
//         private const int MaxCapacity = 1000;
//         private readonly Queue<EventAwaiterCallback<T>> _pool = new Queue<EventAwaiterCallback<T>>();
//
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public EventAwaiterCallback<T> Rent()
//         {
//             if (_pool.Count == 0)
//             {
//                 return new EventAwaiterCallback<T>();
//             }
//
//             return _pool.Dequeue();
//         }
//
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public void Return(EventAwaiterCallback<T> callback)
//         {
//             if (_pool.Count >= MaxCapacity)
//             {
//                 return;
//             }
//
//             callback.Reset();
//             _pool.Enqueue(callback);
//         }
//
//         public void Clear()
//         {
//             _pool.Clear();
//         }
//     }
//
//     /// <summary>
//     /// 事件等待回调包装器（池化对象）
//     /// </summary>
//     internal sealed class EventAwaiterCallback<T> where T : struct, IEventType
//     {
//         private FTask<T> _task;
//         public FTask<T> Task => _task;
//         public bool IsDisposed => _task == null;
//
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public void Initialize()
//         {
//             _task = FTask<T>.Create();
//         }
//
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public void SetResult(T result)
//         {
//             var task = _task;
//             _task = null;
//             task?.SetResult(result);
//         }
//
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public void Reset()
//         {
//             _task = null;
//         }
//     }
//
//     /// <summary>
//     /// 事件等待器组件，用于实现类型化的异步等待和通知机制
//     /// </summary>
//     public sealed class EventAwaiterComponent : Entity
//     {
//         private static readonly Dictionary<RuntimeTypeHandle, object> _poolCache = new Dictionary<RuntimeTypeHandle, object>();
//
//         /// <summary>
//         /// 存储不同类型事件的等待回调队列
//         /// </summary>
//         internal OneToManyList<RuntimeTypeHandle, object> WaitCallbacks { get; } = new();
//
//         /// <summary>
//         /// 获取或创建对象池
//         /// </summary>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         internal static EventAwaiterCallbackPool<T> GetOrCreatePool<T>(RuntimeTypeHandle typeHandle)
//             where T : struct, IEventType
//         {
//             if (!_poolCache.TryGetValue(typeHandle, out var pool))
//             {
//                 pool = new EventAwaiterCallbackPool<T>();
//                 _poolCache[typeHandle] = pool;
//             }
//
//             return (EventAwaiterCallbackPool<T>)pool;
//         }
//     }
//
//     /// <summary>
//     /// 事件等待器组件系统
//     /// </summary>
//     [EntitySystemOf(typeof(EventAwaiterComponent))]
//     [FriendOf(typeof(EventAwaiterComponent))]
//     public static class EventAwaiterComponentSystem
//     {
//         /// <summary>
//         /// 实体唤醒时初始化
//         /// </summary>
//         [EntitySystem]
//         private static void Awake(this EventAwaiterComponent self)
//         {
//             self.WaitCallbacks.Clear();
//         }
//
//         /// <summary>
//         /// 实体销毁时通知所有等待者
//         /// </summary>
//         [EntitySystem]
//         private static void Destroy(this EventAwaiterComponent self)
//         {
//             // 通知所有等待者组件已被销毁
//             foreach (var pair in self.WaitCallbacks)
//             {
//                 var callbackList = pair.Value;
//
//                 // 由于是泛型队列，需要通过接口通知销毁
//                 if (callbackList is INotifyDestroy notifyDestroy)
//                 {
//                     notifyDestroy.NotifyAllDestroy();
//                 }
//             }
//
//             self.WaitCallbacks.Clear();
//         }
//
//         /// <summary>
//         /// 等待指定类型的事件
//         /// </summary>
//         /// <typeparam name="T">事件类型（必须为值类型并实现 IEventType）</typeparam>
//         /// <param name="self">EventAwaiterComponent 实例</param>
//         /// <param name="cancellationToken">取消令牌</param>
//         /// <returns>事件结果</returns>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static async FTask<T> Wait<T>(this EventAwaiterComponent self, FCancellationToken? cancellationToken = null)
//             where T : struct, IEventType
//         {
//             var typeHandle = typeof(T).TypeHandle;
//             var pool = EventAwaiterComponent.GetOrCreatePool<T>(typeHandle);
//             var callback = pool.Rent();
//             callback.Initialize();
//
//             self.AddCallback(typeHandle, callback);
//
//             T result;
//
//             if (cancellationToken != null)
//             {
//                 var cancelAction = new CancelAction<T>(self);
//                 try
//                 {
//                     cancellationToken.Add(cancelAction.Action);
//                     result = await callback.Task;
//                 }
//                 finally
//                 {
//                     cancellationToken.Remove(cancelAction.Action);
//                 }
//             }
//             else
//             {
//                 result = await callback.Task;
//             }
//
//             pool.Return(callback);
//             return result;
//         }
//
//         /// <summary>
//         /// 等待指定类型的事件，支持超时
//         /// </summary>
//         /// <typeparam name="T">事件类型（必须为值类型并实现 IEventType）</typeparam>
//         /// <param name="self">EventAwaiterComponent 实例</param>
//         /// <param name="timeoutMs">超时时间（毫秒）</param>
//         /// <param name="cancellationToken">取消令牌</param>
//         /// <returns>事件结果</returns>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static async FTask<T> Wait<T>(this EventAwaiterComponent self, int timeoutMs, FCancellationToken? cancellationToken = null)
//             where T : struct, IEventType
//         {
//             var typeHandle = typeof(T).TypeHandle;
//             var pool = EventAwaiterComponent.GetOrCreatePool<T>(typeHandle);
//             var callback = pool.Rent();
//             callback.Initialize();
//             
//             // 超时处理
//             var timeoutHandler = new TimeoutHandler<T>(self, callback, typeHandle, timeoutMs);
//             timeoutHandler.StartTimeout(self.Scene, cancellationToken).Coroutine();
//
//             self.AddCallback(typeHandle, callback);
//
//             T result;
//
//             if (cancellationToken != null)
//             {
//                 var cancelAction = new CancelAction<T>(self);
//                 try
//                 {
//                     cancellationToken.Add(cancelAction.Action);
//                     result = await callback.Task;
//                 }
//                 finally
//                 {
//                     cancellationToken.Remove(cancelAction.Action);
//                     timeoutHandler.Cancel();
//                 }
//             }
//             else
//             {
//                 try
//                 {
//                     result = await callback.Task;
//                 }
//                 finally
//                 {
//                     timeoutHandler.Cancel();
//                 }
//             }
//
//             pool.Return(callback);
//             return result;
//         }
//
//         /// <summary>
//         /// 通知所有等待指定类型事件的协程
//         /// </summary>
//         /// <typeparam name="T">事件类型（必须为值类型并实现 IEventType）</typeparam>
//         /// <param name="self">EventAwaiterComponent 实例</param>
//         /// <param name="eventData">事件数据</param>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         public static void Notify<T>(this EventAwaiterComponent self, T eventData) where T : struct, IEventType
//         {
//             var typeHandle = typeof(T).TypeHandle;
//             if (!self.WaitCallbacks.TryGetValue(typeHandle, out var callbackList))
//             {
//                 return;
//             }
//
//             var queue = (NotifyQueue<T>)callbackList;
//             queue.NotifyAll(eventData);
//         }
//
//         /// <summary>
//         /// 添加回调到等待队列
//         /// </summary>
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private static void AddCallback<T>(this EventAwaiterComponent self, RuntimeTypeHandle typeHandle, EventAwaiterCallback<T> callback)
//             where T : struct, IEventType
//         {
//             if (!self.WaitCallbacks.TryGetValue(typeHandle, out var callbackList))
//             {
//                 var queue = new NotifyQueue<T>();
//                 queue.Enqueue(callback);
//                 self.WaitCallbacks.Add(typeHandle, queue);
//             }
//             else
//             {
//                 ((NotifyQueue<T>)callbackList).Enqueue(callback);
//             }
//         }
//
//         /// <summary>
//         /// 取消动作包装（避免闭包分配）
//         /// </summary>
//         private struct CancelAction<T> where T : struct, IEventType
//         {
//             private readonly EventAwaiterComponent _component;
//             public readonly Action Action;
//
//             public CancelAction(EventAwaiterComponent component)
//             {
//                 _component = component;
//                 Action = Cancel;
//             }
//
//             private void Cancel()
//             {
//                 _component.Notify(new T { Error = EventAwaiterResult.Cancel });
//             }
//         }
//
//         /// <summary>
//         /// 超时处理器（避免闭包分配）
//         /// </summary>
//         private struct TimeoutHandler<T> where T : struct, IEventType
//         {
//             private readonly EventAwaiterComponent _component;
//             private readonly EventAwaiterCallback<T> _callback;
//             private readonly RuntimeTypeHandle _typeHandle;
//             private readonly int _timeoutMs;
//             private bool _isCancelled;
//
//             public TimeoutHandler(EventAwaiterComponent component, EventAwaiterCallback<T> callback,
//                 RuntimeTypeHandle typeHandle, int timeoutMs)
//             {
//                 _component = component;
//                 _callback = callback;
//                 _typeHandle = typeHandle;
//                 _timeoutMs = timeoutMs;
//                 _isCancelled = false;
//             }
//
//             public async FTask StartTimeout(Scene scene, FCancellationToken? cancellationToken)
//             {
//                 await scene.TimerComponent.Core.WaitAsync(_timeoutMs, () => _isCancelled, cancellationToken);
//
//                 if (_isCancelled || (cancellationToken != null && cancellationToken.IsCancel()))
//                 {
//                     return;
//                 }
//
//                 if (_callback.IsDisposed)
//                 {
//                     return;
//                 }
//
//                 _callback.SetResult(new T { Error = EventAwaiterResult.Timeout });
//             }
//
//             public void Cancel()
//             {
//                 _isCancelled = true;
//             }
//         }
//     }
//
//     /// <summary>
//     /// 通知队列接口，用于销毁时通知
//     /// </summary>
//     internal interface INotifyDestroy
//     {
//         void NotifyAllDestroy();
//     }
//
//     /// <summary>
//     /// 带通知功能的队列（避免装箱）
//     /// </summary>
//     internal sealed class NotifyQueue<T> : INotifyDestroy where T : struct, IEventType
//     {
//         private readonly Queue<EventAwaiterCallback<T>> _queue = new Queue<EventAwaiterCallback<T>>();
//
//         public void Enqueue(EventAwaiterCallback<T> callback)
//         {
//             _queue.Enqueue(callback);
//         }
//
//         public void NotifyAll(T eventData)
//         {
//             while (_queue.Count > 0)
//             {
//                 var callback = _queue.Dequeue();
//                 callback.SetResult(eventData);
//             }
//         }
//
//         public void NotifyAllDestroy()
//         {
//             while (_queue.Count > 0)
//             {
//                 var callback = _queue.Dequeue();
//                 callback.SetResult(new T { Error = EventAwaiterResult.Destroy });
//             }
//         }
//     }
// }
