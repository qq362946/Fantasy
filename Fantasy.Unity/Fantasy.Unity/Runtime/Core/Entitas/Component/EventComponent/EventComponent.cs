using System;
using System.Reflection;
// ReSharper disable PossibleMultipleEnumeration
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
// ReSharper disable MethodOverloadWithOptionalParameter

namespace Fantasy
{
    internal sealed class EventCache
    {
        public readonly Type EnventType;
        public readonly object Obj;
        public EventCache(Type enventType, object obj)
        {
            EnventType = enventType;
            Obj = obj;
        }
    }

    public sealed class EventComponent : Entity, IAssembly
    {
        private readonly OneToManyList<Type, IEvent> _events = new();
        private readonly OneToManyList<Type, IAsyncEvent> _asyncEvents = new();
        private readonly OneToManyList<long, EventCache> _assemblyEvents = new();
        private readonly OneToManyList<long, EventCache> _assemblyAsyncEvents = new();

        public async FTask<EventComponent> Initialize()
        {
            await AssemblySystem.Register(this);
            return this;
        }

        #region Assembly

        public async FTask Load(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                LoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        public async FTask ReLoad(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                LoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        public async FTask OnUnLoad(long assemblyIdentity)
        {
            var tcs = FTask.Create(false);
            Scene.ThreadSynchronizationContext.Post(() =>
            {
                OnUnLoadInner(assemblyIdentity);
                tcs.SetResult();
            });
            await tcs;
        }

        private void LoadInner(long assemblyIdentity)
        {
            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(IEvent)))
            {
                var @event = (IEvent)Activator.CreateInstance(type);

                if (@event == null)
                {
                    continue;
                }
                
                var eventType = @event.EventType();
                _events.Add(eventType, @event);
                _assemblyEvents.Add(assemblyIdentity, new EventCache(eventType, @event));
            }

            foreach (var type in AssemblySystem.ForEach(assemblyIdentity, typeof(IAsyncEvent)))
            {
                var @event = (IAsyncEvent)Activator.CreateInstance(type);

                if (@event == null)
                {
                    continue;
                }
                
                var eventType = @event.EventType();
                _asyncEvents.Add(eventType, @event);
                _assemblyAsyncEvents.Add(assemblyIdentity, new EventCache(eventType, @event));
            }
        }

        private void OnUnLoadInner(long assemblyIdentity)
        {
            if (_assemblyEvents.TryGetValue(assemblyIdentity, out var events))
            {
                foreach (var @event in events)
                {
                    _events.RemoveValue(@event.EnventType, (IEvent)@event.Obj);
                }

                _assemblyEvents.RemoveByKey(assemblyIdentity);
            }

            if (_assemblyAsyncEvents.TryGetValue(assemblyIdentity, out var asyncEvents))
            {
                foreach (var @event in asyncEvents)
                {
                    _asyncEvents.RemoveValue(@event.EnventType, (IAsyncEvent)@event.Obj);
                }

                _assemblyAsyncEvents.RemoveByKey(assemblyIdentity);
            }
        }

        #endregion

        #region Publish

        /// <summary>
        /// 发布一个值类型的事件数据。
        /// </summary>
        /// <typeparam name="TEventData">事件数据类型（值类型）。</typeparam>
        /// <param name="eventData">事件数据实例。</param>
        public void Publish<TEventData>(TEventData eventData) where TEventData : struct
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
            if (!_asyncEvents.TryGetValue(typeof(TEventData), out var list))
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
        
        #endregion

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