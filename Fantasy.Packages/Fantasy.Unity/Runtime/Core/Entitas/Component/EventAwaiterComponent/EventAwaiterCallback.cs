using Fantasy.Async;
using System.Runtime.CompilerServices;
using Fantasy.Pool;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

// ReSharper disable CheckNamespace
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

namespace Fantasy.EventAwaiter
{
    /// <summary>
    /// EventAwaiter 专用对象池
    /// 用于管理 EventAwaiterCallback 和 EventAwaiterCancelAction 的对象池化
    /// </summary>
    internal sealed class EventAwaiterPool : PoolCore
    {
        /// <summary>
        /// 构造函数，初始化对象池
        /// </summary>
        /// <param name="maxCapacity">最大容量，默认 1024</param>
        public EventAwaiterPool(int maxCapacity = 1024) : base(maxCapacity) { }
    }

    /// <summary>
    /// 事件等待回调包装器（池化对象）
    /// 封装 FTask 用于异步等待事件通知，通过 RuntimeId 解决对象池复用时的竞态条件问题
    /// </summary>
    /// <typeparam name="T">事件类型，必须为值类型</typeparam>
    internal sealed class EventAwaiterCallback<T> : IPool where T : struct
    {
        private bool _isPool;

        /// <summary>
        /// 封装的异步任务，等待事件结果
        /// 当事件通知到达时通过 SetResult 完成任务
        /// </summary>
        public FTask<T> Task { get; private set; }

        /// <summary>
        /// 运行时唯一 ID，用于验证超时处理器的有效性
        /// 每次 Initialize 时生成新 ID，防止对象池复用时超时处理器访问错误的实例
        /// </summary>
        public long RuntimeId { get; private set; }

        /// <summary>
        /// 初始化回调包装器，创建新的 FTask 和唯一 RuntimeId
        /// 从对象池租借后必须调用此方法进行初始化
        /// </summary>
        /// <param name="scene">Scene 实例，用于生成 RuntimeId</param>
        /// <returns>返回自身以支持链式调用</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventAwaiterCallback<T> Initialize(Scene scene)
        {
            // 生成全局唯一的 RuntimeId，用于超时处理器验证
            // 即使对象从池中复用，每次 Initialize 都会生成新的 ID
            RuntimeId = scene.RuntimeIdFactory.Create(true);
            // 创建新的 FTask 用于异步等待
            Task = FTask<T>.Create();
            return this;
        }

        /// <summary>
        /// 设置任务结果并完成等待
        /// 由 EventAwaiterComponent.Notify() 或超时处理器调用
        /// </summary>
        /// <param name="result">事件结果</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(T result)
        {
            // 早期返回：如果 Task 已经为 null，说明已经处理过或未初始化
            if (Task == null)
            {
                return;
            }

            // 先保存 Task 引用，再清空 Task 字段
            // 防止多次调用 SetResult（幂等性）
            var task = Task;
            Task = null;

            // 设置任务结果，唤醒等待的协程
            task.SetResult(result);
        }

        /// <summary>
        /// 获取当前对象是否在对象池中
        /// </summary>
        /// <returns>true 表示在池中，false 表示已租出</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPool() => _isPool;

        /// <summary>
        /// 设置对象池状态，归还到池时清理引用
        /// 对象池框架会自动调用此方法管理对象状态
        /// </summary>
        /// <param name="isPool">是否在池中</param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;

            // 归还到池时清理 Task 引用，避免内存泄漏
            // 即使 SetResult 未调用（异常情况），也能确保 FTask 被释放
            if (!isPool)
            {
                Task = null;
                RuntimeId = 0;
            }
        }
    }
}
