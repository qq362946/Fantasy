using System.Runtime.CompilerServices;
using Fantasy.Async;
using Fantasy.Pool;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Fantasy.EventAwaiter
{
    internal struct EventAwaiterTimeoutHandler<T> where T : struct
    {  
        private EventAwaiterComponent _component;
        private EventAwaiterCallback<T> _callback;
        private RuntimeTypeHandle _typeHandle;
        private int _timeoutMs;
        private bool _isCancelled;

        /// <summary>
        /// 初始化取消动作，设置关联的组件
        /// </summary>
        /// <param name="component">关联的 EventAwaiterComponent</param>
        /// <param name="callback"></param>
        /// <param name="typeHandle"></param>
        /// <param name="timeoutMs"></param>
        /// <returns>返回自身以支持链式调用</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EventAwaiterTimeoutHandler<T> Initialize( EventAwaiterComponent component, EventAwaiterCallback<T> callback, RuntimeTypeHandle typeHandle, int timeoutMs)
        {
            _component = component;
            _callback = callback;
            _typeHandle = typeHandle;
            _timeoutMs = timeoutMs;
            _isCancelled = false;
            return this;
        }

        public async FTask StartTimeout(Scene scene, FCancellationToken cancellationToken)
        {
#if FANTASY_UNITY
            await scene.TimerComponent.Unity.WaitAsync(_timeoutMs, cancellationToken);
#endif
#if FANTASY_NET
            await scene.TimerComponent.Net.WaitAsync(_timeoutMs, cancellationToken);
#endif
            if (!_isCancelled)
            {
                return;
            }

            if (cancellationToken != null && cancellationToken.IsCancel)
            {
                return;
            }
            

            // if (_callback.IsDisposed)
            // {
            //     return;
            // }
            //
            // _callback.SetResult(new T { Error = EventAwaiterResult.Timeout });
        }
        
    }
}