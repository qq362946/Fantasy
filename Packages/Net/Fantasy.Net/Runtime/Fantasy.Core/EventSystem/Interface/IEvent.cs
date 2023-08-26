using System;

namespace Fantasy
{
    /// <summary>
    /// 定义事件的接口。
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// 获取事件的类型。
        /// </summary>
        /// <returns>事件的类型。</returns>
        Type EventType();
        /// <summary>
        /// 调用事件处理方法。
        /// </summary>
        /// <param name="self">事件的实例。</param>
        void Invoke(object self);
    }

    /// <summary>
    /// 定义异步事件的接口。
    /// </summary>
    public interface IAsyncEvent
    {
        /// <summary>
        /// 获取事件的类型。
        /// </summary>
        /// <returns>事件的类型。</returns>
        Type EventType();
        /// <summary>
        /// 异步调用事件处理方法。
        /// </summary>
        /// <param name="self">事件的实例。</param>
        /// <returns>表示异步操作的任务。</returns>
        FTask InvokeAsync(object self);
    }

    /// <summary>
    /// 事件系统的抽象基类。
    /// </summary>
    /// <typeparam name="T">事件的类型。</typeparam>
    public abstract class EventSystem<T> : IEvent
    {
        private readonly Type _selfType = typeof(T);

        /// <summary>
        /// 获取事件的类型。
        /// </summary>
        /// <returns>事件的类型。</returns>
        public Type EventType()
        {
            return _selfType;
        }

        /// <summary>
        /// 同步处理事件的方法。
        /// </summary>
        /// <param name="self">事件的实例。</param>
        public abstract void Handler(T self);

        /// <summary>
        /// 调用事件处理方法。
        /// </summary>
        /// <param name="self">事件的实例。</param>
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
    /// 异步事件系统的抽象基类。
    /// </summary>
    /// <typeparam name="T">事件的类型。</typeparam>
    public abstract class AsyncEventSystem<T> : IAsyncEvent
    {
        private readonly Type _selfType = typeof(T);

        /// <summary>
        /// 获取事件的类型。
        /// </summary>
        /// <returns>事件的类型。</returns>
        public Type EventType()
        {
            return _selfType;
        }

        /// <summary>
        /// 异步处理事件的方法。
        /// </summary>
        /// <param name="self">事件的实例。</param>
        /// <returns>表示异步操作的任务。</returns>
        public abstract FTask Handler(T self);

        /// <summary>
        /// 异步调用事件处理方法。
        /// </summary>
        /// <param name="self">事件的实例。</param>
        /// <returns>表示异步操作的任务。</returns>
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