using System;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy
{
    public sealed class WaitCoroutineLockPool : PoolCore<WaitCoroutineLock>
    {
        private readonly Scene _scene;
        private readonly CoroutineLockComponent _coroutineLockComponent;

        public WaitCoroutineLockPool(CoroutineLockComponent coroutineLockComponent) : base(2000)
        {
            _scene = coroutineLockComponent.Scene;
            _coroutineLockComponent = coroutineLockComponent;
        }

        public WaitCoroutineLock Rent(CoroutineLock coroutineLock, ref long coroutineLockQueueKey, string tag = null, int timeOut = 30000)
        {
            var timerId = 0L;
            var lockId = _coroutineLockComponent.LockId;
            var waitCoroutineLock = _coroutineLockComponent.WaitCoroutineLockPool.Rent();

            if (timeOut > 0)
            {
                timerId = _scene.TimerComponent.Net.OnceTimer(timeOut, new CoroutineLockTimeout(ref lockId, waitCoroutineLock));
            }

            waitCoroutineLock.Initialize(coroutineLock, this, ref coroutineLockQueueKey, ref timerId, ref lockId, tag);
            return waitCoroutineLock;
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
        public override void Handler(CoroutineLockTimeout self)
        {
            var selfWaitCoroutineLock = self.WaitCoroutineLock;
            
            if (self.LockId != selfWaitCoroutineLock.LockId)
            {
                return;
            }
            
            Log.Error($"coroutine lock timeout CoroutineLockQueueType:{selfWaitCoroutineLock.CoroutineLock.CoroutineLockType} Key:{selfWaitCoroutineLock.CoroutineLockQueueKey} Tag:{selfWaitCoroutineLock.Tag}");
        }
    }

    public sealed class WaitCoroutineLock : IPool, IDisposable
    {
        public bool IsPool { get; set; }
        internal string Tag { get; private set; }
        internal long LockId { get; private set; }
        internal long TimerId { get; private set; }
        internal long CoroutineLockQueueKey { get; private set; }
        internal CoroutineLock CoroutineLock { get; private set; }

        private bool _isSetResult;
        private FTask<WaitCoroutineLock> _tcs;
        private WaitCoroutineLockPool _waitCoroutineLockPool;
        internal void Initialize(CoroutineLock coroutineLock, WaitCoroutineLockPool waitCoroutineLockPool, ref long coroutineLockQueueKey, ref long timerId, ref long lockId, string tag)
        {
            Tag = tag;
            LockId = lockId;
            TimerId = timerId;
            CoroutineLock = coroutineLock;
            CoroutineLockQueueKey = coroutineLockQueueKey;
            _waitCoroutineLockPool = waitCoroutineLockPool;
        }

        public void Dispose()
        {
            if (LockId == 0)
            {
                Log.Error("WaitCoroutineLock is already disposed");
                return;
            }
            
            CoroutineLock.Release(CoroutineLockQueueKey);
            
            _tcs = null;
            Tag = null;
            LockId = 0;
            TimerId = 0;
            _isSetResult = false;
            CoroutineLockQueueKey = 0;
            _waitCoroutineLockPool.Return(this);
            CoroutineLock = null;
            _waitCoroutineLockPool = null;
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
            Tcs.SetResult(this);
        }
    }
}