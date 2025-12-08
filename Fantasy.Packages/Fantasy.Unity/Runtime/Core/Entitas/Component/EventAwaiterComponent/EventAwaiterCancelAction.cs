using System;
using System.Runtime.CompilerServices;
using Fantasy.Pool;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy.EventAwaiter
{
    /// <summary>
    /// 事件等待器取消动作（池化对象）
    /// 用于在 FCancellationToken 触发时通知 EventAwaiterComponent
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    internal sealed class EventAwaiterCancelAction<T> : IPool where T : struct
    {
        private bool _isPool;
        private EventAwaiterComponent _component;

        /// <summary>
        /// 缓存的取消委托，避免每次创建新委托对象
        /// </summary>
        public Action Action { get; }

        /// <summary>
        /// 构造函数，创建并缓存 Action 委托
        /// </summary>
        public EventAwaiterCancelAction()
        {
            Action = Cancel;  // 只在构造时创建一次委托，后续复用
        }

        /// <summary>
        /// 初始化取消动作，设置关联的组件
        /// </summary>
        /// <param name="component">关联的 EventAwaiterComponent</param>
        /// <returns>返回自身以支持链式调用</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventAwaiterCancelAction<T> Initialize(EventAwaiterComponent component)
        {
            _component = component;
            return this;
        }

        /// <summary>
        /// 取消回调方法，通知组件发送默认事件
        /// 由 FCancellationToken.Cancel() 触发
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Cancel()
        {
            _component?.Notify(new T());  // 使用 ?. 防止 _component 为 null
        }

        /// <summary>
        /// 获取当前对象是否在对象池中
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPool() => _isPool;

        /// <summary>
        /// 设置对象池状态，归还到池时清理引用
        /// </summary>
        /// <param name="isPool">是否在池中</param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;

            if (!isPool)  // 归还到池时清理引用，避免内存泄漏
            {
                _component = null;
            }
        }
    }
}