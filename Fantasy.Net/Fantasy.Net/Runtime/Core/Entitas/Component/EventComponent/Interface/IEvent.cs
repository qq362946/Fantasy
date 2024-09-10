using System;
using Fantasy.Async;

namespace Fantasy.Event
{
    /// <summary>
    /// 事件的接口
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// 用于指定事件的Type
        /// </summary>
        /// <returns></returns>
        Type EventType();
        /// <summary>
        /// 时间内部使用的入口
        /// </summary>
        /// <param name="self"></param>
        void Invoke(object self);
    }
    
    /// <summary>
    /// 异步事件的接口
    /// </summary>
    public interface IAsyncEvent
    {
        /// <summary>
        /// <see cref="IEvent.EventType"/>
        /// </summary>
        /// <returns></returns>
        Type EventType();
        /// <summary>
        /// <see cref="IEvent.Invoke"/>
        /// </summary>
        /// <returns></returns>
        FTask InvokeAsync(object self);
    }
    
    /// <summary>
    /// 事件的抽象类，要使用事件必须要继承这个抽象接口。
    /// </summary>
    /// <typeparam name="T">要监听的事件泛型类型</typeparam>
    public abstract class EventSystem<T> : IEvent
    {
        private readonly Type _selfType = typeof(T);
        /// <summary>
        /// <see cref="IEvent.EventType"/>
        /// </summary>
        /// <returns></returns>
        public Type EventType()
        {
            return _selfType;
        }
        /// <summary>
        /// 事件调用的方法，要在这个方法里编写事件发生的逻辑
        /// </summary>
        /// <param name="self"></param>
        protected abstract void Handler(T self);
        /// <summary>
        /// <see cref="IEvent.Invoke"/>
        /// </summary>
        /// <returns></returns>
        public void Invoke(object self)
        {
            try
            {
                Handler((T) self);
            }
            catch (Exception e)
            {
                Log.Error($"{_selfType.Name} Error {e}");
            }
        }
    }
    /// <summary>
    /// 异步事件的抽象类，要使用事件必须要继承这个抽象接口。
    /// </summary>
    /// <typeparam name="T">要监听的事件泛型类型</typeparam>
    public abstract class AsyncEventSystem<T> : IAsyncEvent
    {
        private readonly Type _selfType = typeof(T);
        /// <summary>
        /// <see cref="IEvent.EventType"/>
        /// </summary>
        /// <returns></returns>
        public Type EventType()
        {
            return _selfType;
        }
        /// <summary>
        /// 事件调用的方法，要在这个方法里编写事件发生的逻辑
        /// </summary>
        /// <param name="self"></param>
        protected abstract FTask Handler(T self);
        /// <summary>
        /// <see cref="IEvent.Invoke"/>
        /// </summary>
        /// <returns></returns>
        public async FTask InvokeAsync(object self)
        {
            try
            {
                await Handler((T) self);
            }
            catch (Exception e)
            {
                Log.Error($"{_selfType.Name} Error {e}");
            }
        }
    }
}