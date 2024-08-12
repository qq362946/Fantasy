using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fantasy
{
    public sealed class FCancellationToken : IDisposable
    {
        private bool _isDispose;
        private bool _isCancel;
        private readonly HashSet<Action> _actions = new HashSet<Action>();
        public bool IsCancel => _isDispose || _isCancel;
        
        public void Add(Action action)
        {
            if (_isDispose)
            {
                return;
            }
            
            _actions.Add(action);
        }

        public void Remove(Action action)
        {
            if (_isDispose)
            {
                return;
            }
            
            _actions.Remove(action);
        }

        public void Cancel()
        {
            if (IsCancel)
            {
                return;
            }
            
            _isCancel = true;
            
            foreach (var action in _actions)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            
            _actions.Clear();
        }

        public void Dispose()
        {
            if (_isDispose)
            {
                return;
            }

            if (!IsCancel)
            {
                Cancel();
                _isCancel = true;
            }
            
            _isDispose = true;

            if (Caches.Count > 2000)
            {
                return;
            }

            Caches.Enqueue(this);
        }
        
        #region Static

        private static readonly ConcurrentQueue<FCancellationToken> Caches = new ConcurrentQueue<FCancellationToken>();

        public static FCancellationToken ToKen
        {
            get
            {
                if (!Caches.TryDequeue(out var fCancellationToken))
                {
                    fCancellationToken = new FCancellationToken();
                }

                fCancellationToken._isCancel = false;
                fCancellationToken._isDispose = false;
                return fCancellationToken;
            }
        }

        #endregion
    }
}