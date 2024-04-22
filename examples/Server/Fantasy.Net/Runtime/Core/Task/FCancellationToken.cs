using System;
using System.Collections.Generic;

#pragma warning disable CS8625
namespace Fantasy
{
    /// <summary>
    /// 表示一个自定义的取消标记，允许添加和移除取消动作，并可用于取消一组注册的动作。
    /// </summary>
    public sealed class FCancellationToken : IDisposable, IPool
    {
        /// <summary>
        /// 是否是池
        /// </summary>
        public bool IsPool { get; set; }
        private bool _isDispose;
        /// <summary>
        /// 获取一个值，指示取消标记是否已被取消。
        /// </summary>
        public bool IsCancel => _isDispose;
        private readonly HashSet<Action> _actions = new HashSet<Action>();
        
        /// <summary>
        /// 创建一个取消标记。
        /// </summary>
        /// <returns></returns>
        public static FCancellationToken Create()
        {
            var fCancellationToken = MultiThreadPool.Rent<FCancellationToken>();
            fCancellationToken._isDispose = false;
            fCancellationToken.IsPool = true;
            return fCancellationToken;
        }

        /// <summary>
        /// 将一个动作添加到在取消时执行的动作列表中。
        /// </summary>
        /// <param name="action">要添加的动作。</param>
        public void Add(Action action)
        {
            if (_isDispose)
            {
                return;
            }
            
            _actions.Add(action);
        }

        /// <summary>
        /// 从取消标记中移除之前添加的动作。
        /// </summary>
        /// <param name="action">要移除的动作。</param>
        public void Remove(Action action)
        {
            if (_isDispose)
            {
                return;
            }
            
            _actions.Remove(action);
        }

        /// <summary>
        /// 取消标记并执行所有已注册的取消动作。
        /// </summary>
        public void Dispose()
        {
            if (_isDispose)
            {
                return;
            }

            _isDispose = true;
            
            foreach (var action in _actions)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
            
            _actions.Clear();
            MultiThreadPool.Return(this);
        }
    }
}