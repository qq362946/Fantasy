using System;
using System.Collections.Generic;

#pragma warning disable CS8625
namespace Fantasy
{
    /// <summary>
    /// 表示一个自定义的取消标记，允许添加和移除取消动作，并可用于取消一组注册的动作。
    /// </summary>
    public sealed class FCancellationToken
    {
        private HashSet<Action> _actions = new HashSet<Action>();
        /// <summary>
        /// 获取一个值，指示取消标记是否已被取消。
        /// </summary>
        public bool IsCancel => _actions == null;

        /// <summary>
        /// 将一个动作添加到在取消时执行的动作列表中。
        /// </summary>
        /// <param name="action">要添加的动作。</param>
        public void Add(Action action)
        {
            _actions.Add(action);
        }

        /// <summary>
        /// 从取消标记中移除之前添加的动作。
        /// </summary>
        /// <param name="action">要移除的动作。</param>
        public void Remove(Action action)
        {
            _actions.Remove(action);
        }

        /// <summary>
        /// 取消标记并执行所有已注册的取消动作。
        /// </summary>
        public void Cancel()
        {
            if (_actions == null)
            {
                return;
            }
            
            var runActions = _actions;
            _actions = null;
            
            foreach (var action in runActions)
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
        }
    }
}

