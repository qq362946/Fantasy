using System.Runtime.CompilerServices;
using Fantasy.Async;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace Fantasy.EventAwaiter
{
    /// <summary>
    /// 事件等待器超时处理器（值类型，避免堆分配）
    /// 用于在指定时间后自动触发超时，通过 RuntimeId 验证回调有效性
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    internal struct EventAwaiterTimeoutHandler<T> where T : struct
    {
        private int _timeoutMs;
        private bool _isCancelled;
        private long _capturedRuntimeId;
        private EventAwaiterCallback<T> _callback;

        /// <summary>
        /// 启动超时处理器（静态方法，避免闭包）
        /// </summary>
        /// <param name="scene">Scene 实例</param>
        /// <param name="callback">回调对象</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <param name="cancellationToken">取消令牌</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EventAwaiterTimeoutHandler<T> StartTimeout( Scene scene, EventAwaiterCallback<T> callback, int timeoutMs, FCancellationToken? cancellationToken)
        {
            var handler = new EventAwaiterTimeoutHandler<T>
            {
                _callback = callback,
                _capturedRuntimeId = callback.RuntimeId,
                _timeoutMs = timeoutMs,
                _isCancelled = false
            };

            handler.WaitTimeout(scene, cancellationToken).Coroutine();
            return handler;
        }

        /// <summary>
        /// 取消超时处理
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cancel()
        {
            _isCancelled = true;
        }

        /// <summary>
        /// 异步等待超时
        /// </summary>
        private async FTask WaitTimeout(Scene scene, FCancellationToken? cancellationToken)
        {
            // 等待超时
#if FANTASY_UNITY
            await scene.TimerComponent.Unity.WaitAsync(_timeoutMs, cancellationToken!);
#endif
#if FANTASY_NET
            await scene.TimerComponent.Net.WaitAsync(_timeoutMs, cancellationToken!);
#endif

            // 检查是否已取消
            if (_isCancelled)
            {
                return;
            }

            // 检查 CancellationToken 是否已取消
            if (cancellationToken !=null && cancellationToken.IsCancel)
            {
                return;
            }

            // ✅ 关键验证：检查 RuntimeId 是否匹配
            // 如果不匹配，说明 callback 已经被复用，不应该再处理
            if (_callback.RuntimeId != _capturedRuntimeId)
            {
                return;
            }

            // ✅ 验证通过，设置超时结果
            _callback.SetResult(default);  // 发送默认值表示超时
        }
    }
}
