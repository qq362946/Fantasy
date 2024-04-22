using System;
using System.Threading.Tasks;

using Type = System.Type;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// ReSharper disable MethodOverloadWithOptionalParameter

namespace Fantasy
{
    internal sealed class EventInfo
    {
        public readonly Type Type;
        public readonly object Obj;

        public EventInfo(Type type, object obj)
        {
            Type = type;
            Obj = obj;
        }
    }

    /// <summary>
    /// 事件系统类，用于加卸载程序集，发布和订阅事件。
    /// </summary>
    public sealed class EventSystem : Singleton<EventSystem>
    {
        private readonly OneToManyList<Type, IEvent> _events = new();
        private readonly OneToManyList<Type, IAsyncEvent> _asyncEvents = new();
        
        private readonly OneToManyList<long, EventInfo> _assemblyEvents = new();
        private readonly OneToManyList<long, EventInfo> _assemblyAsyncEvents = new();

        #region Assembly

        public override Task Load(long assemblyIdentity)
        {
            return Task.Run(() =>
            {
                LoadInner(assemblyIdentity);
            });
        }
        
        public override Task OnUnLoad(long assemblyIdentity)
        {
            return Task.Run(() => { OnUnLoadInner(assemblyIdentity); });
        }
        
        public override Task ReLoad(long assemblyIdentity)
        {
            return Task.Run(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                LoadInner(assemblyIdentity);
            });
        }

        private void LoadInner(long assemblyIdentity)
        {
            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(IEvent)))
            {
                var obj = (IEvent)Activator.CreateInstance(type);

                if (obj == null)
                {
                    continue;
                }

                var eventType = obj.EventType();
                _events.Add(eventType, obj);
                _assemblyEvents.Add(assemblyIdentity, new EventInfo(eventType, obj));
            }

            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(IAsyncEvent)))
            {
                var obj = (IAsyncEvent)Activator.CreateInstance(type);

                if (obj == null)
                {
                    continue;
                }

                var eventType = obj.EventType();
                _asyncEvents.Add(eventType, obj);
                _assemblyAsyncEvents.Add(assemblyIdentity, new EventInfo(eventType, obj));
            }
        }

        private void OnUnLoadInner(long assemblyIdentity)
        {
            if (_assemblyEvents.TryGetValue(assemblyIdentity, out var events))
            {
                foreach (var @event in events)
                {
                    _events.RemoveValue(@event.Type, (IEvent)@event.Obj);
                }

                _assemblyEvents.RemoveByKey(assemblyIdentity);
            }

            if (_assemblyAsyncEvents.TryGetValue(assemblyIdentity, out var asyncEvents))
            {
                foreach (var @event in asyncEvents)
                {
                    _asyncEvents.RemoveValue(@event.Type, (IAsyncEvent)@event.Obj);
                }

                _assemblyAsyncEvents.RemoveByKey(assemblyIdentity);
            }
        }

        #endregion

        /// <summary>
        /// 发布一个值类型的事件数据。
        /// </summary>
        /// <typeparam name="TEventData">事件数据类型（值类型）。</typeparam>
        /// <param name="eventData">事件数据实例。</param>
        public void Publish<TEventData>(TEventData eventData) where TEventData : struct
        {
            if (!_events.TryGetValue(eventData.GetType(), out var list))
            {
                return;
            }
            
            foreach (var @event in list)
            {
                try
                {
                    @event.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 发布一个继承自 Entity 的事件数据。
        /// </summary>
        /// <typeparam name="TEventData">事件数据类型（继承自 Entity）。</typeparam>
        /// <param name="eventData">事件数据实例。</param>
        /// <param name="isDisposed">是否释放事件数据。</param>
        public void Publish<TEventData>(TEventData eventData, bool isDisposed = true) where TEventData : Entity
        {
            if (!_events.TryGetValue(typeof(TEventData), out var list))
            {
                return;
            }
            
            foreach (var @event in list)
            {
                try
                {
                    @event.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            if (isDisposed)
            {
                eventData.Dispose();
            }
        }

        /// <summary>
        /// 异步发布一个值类型的事件数据。
        /// </summary>
        /// <typeparam name="TEventData">事件数据类型（值类型）。</typeparam>
        /// <param name="eventData">事件数据实例。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async FTask PublishAsync<TEventData>(TEventData eventData) where TEventData : struct
        {
            if (!_asyncEvents.TryGetValue(eventData.GetType(), out var list))
            {
                return;
            }
            
            using var tasks = ListPool<FTask>.Create();

            foreach (var @event in list)
            {
                tasks.Add(@event.InvokeAsync(eventData));
            }

            await FTask.WhenAll(tasks);
        }

        /// <summary>
        /// 异步发布一个继承自 Entity 的事件数据。
        /// </summary>
        /// <typeparam name="TEventData">事件数据类型（继承自 Entity）。</typeparam>
        /// <param name="eventData">事件数据实例。</param>
        /// <param name="isDisposed">是否释放事件数据。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async FTask PublishAsync<TEventData>(TEventData eventData, bool isDisposed = true) where TEventData : Entity
        {
            if (!_asyncEvents.TryGetValue(eventData.GetType(), out var list))
            {
                return;
            }
            
            using var tasks = ListPool<FTask>.Create();

            foreach (var @event in list)
            {
                tasks.Add(@event.InvokeAsync(eventData));
            }

            await FTask.WhenAll(tasks);

            if (isDisposed)
            {
                eventData.Dispose();
            }
        }

        /// <summary>
        /// 清理资源和事件订阅
        /// </summary>
        public override void Dispose()
        {
            _events.Clear();
            _asyncEvents.Clear();
            _assemblyEvents.Clear();
            _assemblyAsyncEvents.Clear();
            base.Dispose();
        }
    }
}