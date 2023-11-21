using System;

#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy
{
    /// <summary>
    /// 计时器操作类，用于管理定时器相关信息。
    /// </summary>
    public sealed class TimerAction : IDisposable
    {
        /// <summary>
        /// 唯一标识符。
        /// </summary>
        public long Id;
        /// <summary>
        /// 触发时间。
        /// </summary>
        public long Time;
        /// <summary>
        /// 回调对象。
        /// </summary>
        public object Callback;
        /// <summary>
        /// 计时器类型。
        /// </summary>
        public TimerType TimerType;

        /// <summary>
        /// 创建一个 <see cref="TimerAction"/> 实例。
        /// </summary>
        /// <returns>新创建的 <see cref="TimerAction"/> 实例。</returns>
        public static TimerAction Create()
        {
            var timerAction = Pool<TimerAction>.Rent();
            timerAction.Id = IdFactory.NextRunTimeId();
            return timerAction;
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            Id = 0;
            Time = 0;
            Callback = null;
            TimerType = TimerType.None;
            Pool<TimerAction>.Return(this);
        }
    }
}