using System;
using Fantasy.Event;
using Fantasy.Pool;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy.Async
{
    internal sealed class WaitCoroutineLockPool : PoolCore<WaitCoroutineLock>
    {
        private readonly Scene _scene;
        private readonly CoroutineLockComponent _coroutineLockComponent;

        public WaitCoroutineLockPool(CoroutineLockComponent coroutineLockComponent) : base(2000)
        {
            _scene = coroutineLockComponent.Scene;
            _coroutineLockComponent = coroutineLockComponent;
        }

        public WaitCoroutineLock Rent(ICoroutineLock coroutineLock, ref long coroutineLockQueueKey, string tag = null, int timeOut = 30000)
        {
            var lockId = _coroutineLockComponent.LockId;
            var waitCoroutineLock = _coroutineLockComponent.WaitCoroutineLockPool.Rent();
            waitCoroutineLock.Initialize(coroutineLock, this, ref coroutineLockQueueKey, ref lockId, tag, timeOut);
            return waitCoroutineLock;
        }

        public long StartTimeoutTimer(int timeOut, ref long lockId, WaitCoroutineLock waitCoroutineLock)
        {
            return _scene.TimerComponent.Net.OnceTimer(timeOut, new CoroutineLockTimeout(ref lockId, waitCoroutineLock));
        }

        public void RemoveTimeoutTimer(long timerId)
        {
            _scene.TimerComponent.Net.Remove(timerId);
        }
    }

    internal struct CoroutineLockTimeout
    {
        public readonly long LockId;
        public readonly WaitCoroutineLock WaitCoroutineLock;

        public CoroutineLockTimeout(ref long lockId, WaitCoroutineLock waitCoroutineLock)
        {
            LockId = lockId;
            WaitCoroutineLock = waitCoroutineLock;
        }
    }

    internal sealed class OnCoroutineLockTimeout : EventSystem<CoroutineLockTimeout>
    {
        protected override void Handler(CoroutineLockTimeout self)
        {
            var selfWaitCoroutineLock = self.WaitCoroutineLock;

            if (self.LockId != selfWaitCoroutineLock.LockId)
            {
                return;
            }

            selfWaitCoroutineLock.Timeout();
        }
    }

    /// <summary>
    /// 一个协程锁的等待器, 用户通过这里释放锁。(通常使用 using 语句)
    /// </summary>
    public sealed class WaitCoroutineLock : IPool, IDisposable
    {
        private bool _isPool;
        internal string Tag { get; private set; }
        internal long LockId { get; private set; }
        internal long TimerId { get; private set; }
        internal long CoroutineLockQueueKey { get; private set; }
        internal ICoroutineLock CoroutineLock { get; private set; }

        private bool _isSetResult;
        private bool _isAcquired;
        private bool _isReleased;
        private int _timeOut;
        private FTask<WaitCoroutineLock> _tcs;
        private WaitCoroutineLockPool _waitCoroutineLockPool;
        internal void Initialize(ICoroutineLock coroutineLock, WaitCoroutineLockPool waitCoroutineLockPool, ref long coroutineLockQueueKey, ref long lockId, string tag, int timeOut)
        {
            Tag = tag;
            LockId = lockId;
            CoroutineLock = coroutineLock;
            CoroutineLockQueueKey = coroutineLockQueueKey;
            _waitCoroutineLockPool = waitCoroutineLockPool;
            _timeOut = timeOut;
        }
        /// <summary>
        /// 释放协程锁
        /// </summary>
        public void Dispose()
        {
            if (LockId == 0)
            {
                Log.Error("WaitCoroutineLock is already disposed");
                return;
            }
            
            if (TimerId != 0)
            {
                _waitCoroutineLockPool.RemoveTimeoutTimer(TimerId);
                TimerId = 0;
            }

            if (!_isReleased)
            {
                _isReleased = true;

                if (_isAcquired)
                {
                    CoroutineLock.Release(CoroutineLockQueueKey);
                }
            }
            
            var waitCoroutineLockPool = _waitCoroutineLockPool;
            _tcs = null;
            Tag = null;
            LockId = 0;
            TimerId = 0;
            _isSetResult = false;
            _isAcquired = false;
            _isReleased = false;
            _timeOut = 0;
            CoroutineLockQueueKey = 0;
            CoroutineLock = null;
            _waitCoroutineLockPool = null;
            waitCoroutineLockPool.Return(this);
        }

        internal void SetAcquired()
        {
            if (_isAcquired || _isReleased || LockId == 0)
            {
                return;
            }

            _isAcquired = true;

            if (_timeOut > 0)
            {
                var lockId = LockId;
                TimerId = _waitCoroutineLockPool.StartTimeoutTimer(_timeOut, ref lockId, this);
            }
        }

        internal void Timeout()
        {
            if (!_isAcquired || _isReleased || LockId == 0)
            {
                return;
            }

            _isReleased = true;
            TimerId = 0;
            Log.Error($"coroutine lock timeout and auto release LockDuty:{CoroutineLock.LockDuty} Key:{CoroutineLockQueueKey} Tag:{Tag}");
            CoroutineLock.Release(CoroutineLockQueueKey);
        }
        
        internal FTask<WaitCoroutineLock> Tcs
        {
            get { return _tcs ??= FTask<WaitCoroutineLock>.Create(); }
        }

        internal void SetResult()
        {
            if (_isSetResult)
            {
                Log.Error("WaitCoroutineLock is already SetResult");
                return;
            }
            
            _isSetResult = true;
            SetAcquired();
            Tcs.SetResult(this);
        }

        /// <summary>
        /// 获取一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <returns></returns>
        public bool IsPool()
        {
            return _isPool;
        }

        /// <summary>
        /// 设置一个值，该值指示当前实例是否为对象池中的实例。
        /// </summary>
        /// <param name="isPool"></param>
        public void SetIsPool(bool isPool)
        {
            _isPool = isPool;
        }
    }
}