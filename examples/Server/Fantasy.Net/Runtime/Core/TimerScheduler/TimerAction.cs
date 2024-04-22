using System;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS8625
#pragma warning disable CS8618

namespace Fantasy
{
    /// <summary>
    /// 计时器操作类，用于管理定时器相关信息。
    /// </summary>
    public struct TimerAction
    {
        /// <summary>
        /// 开始的事件
        /// </summary>
        public long StartTime;
        /// <summary>
        /// 触发时间。
        /// </summary>
        public long TriggerTime;
        /// <summary>
        /// 回调对象。
        /// </summary>
        public readonly object Callback;
        /// <summary>
        /// 计时器类型。
        /// </summary>
        public readonly TimerType TimerType;

        public TimerAction(TimerType timerType, long startTime, long triggerTime, object callback)
        {
            Callback = callback;
            TimerType = timerType;
            StartTime = startTime;
            TriggerTime = triggerTime;
        }
    }
}