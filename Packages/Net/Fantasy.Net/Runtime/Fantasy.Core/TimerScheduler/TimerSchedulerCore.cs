using System;
using System.Collections.Generic;
using Fantasy.DataStructure;
using Fantasy.Helper;
#pragma warning disable CS8625

namespace Fantasy
{
    /// <summary>
    /// 计时器调度核心类，提供计时器的核心功能。
    /// </summary>
    public class TimerSchedulerCore
    {
        private long _minTime; // 最小时间
        private readonly Func<long> _now; // 获取当前时间的委托
        private readonly Queue<long> _timeOutTime = new(); // 超时时间队列
        private readonly Queue<long> _timeOutTimerIds = new(); // 超时计时器ID队列
        private readonly Dictionary<long, TimerAction> _timers = new(); // 计时器字典，按ID存储计时器对象
        private readonly SortedOneToManyList<long, long> _timeId = new(); // 时间与计时器ID的有序一对多列表

        /// <summary>
        /// 构造函数，初始化计时器核心。
        /// </summary>
        /// <param name="now">获取当前时间的委托。</param>
        public TimerSchedulerCore(Func<long> now)
        {
            _now = now;
        }

        /// <summary>
        /// 更新计时器，检查并执行超时的计时器任务。
        /// </summary>
        public void Update()
        {
            try
            {
                var currentTime = _now(); // 获取当前时间

                if (_timeId.Count == 0)
                {
                    return;
                }

                if (currentTime < _minTime)
                {
                    return;
                }

                _timeOutTime.Clear();
                _timeOutTimerIds.Clear();

                // 遍历时间ID列表，查找超时的计时器任务
                foreach (var (key, _) in _timeId)
                {
                    if (key > currentTime)
                    {
                        _minTime = key;
                        break;
                    }

                    _timeOutTime.Enqueue(key);
                }

                // 处理超时的计时器任务
                while (_timeOutTime.TryDequeue(out var time))
                {
                    foreach (var timerId in _timeId[time])
                    {
                        _timeOutTimerIds.Enqueue(timerId);
                    }

                    _timeId.RemoveKey(time);
                }

                // 执行超时的计时器任务的回调操作
                while (_timeOutTimerIds.TryDequeue(out var timerId))
                {
                    if (!_timers.TryGetValue(timerId, out var timer))
                    {
                        continue;
                    }

                    _timers.Remove(timer.Id);

                    // 根据计时器类型执行不同的操作
                    switch (timer.TimerType)
                    {
                        case TimerType.OnceWaitTimer:
                        {
                            var tcs = (FTask<bool>) timer.Callback;
                            timer.Dispose();
                            tcs.SetResult(true);
                            break;
                        }
                        case TimerType.OnceTimer:
                        {
                            var action = (Action) timer.Callback;
                            timer.Dispose();

                            if (action == null)
                            {
                                Log.Error($"timer {timer.ToJson()}");
                                break;
                            }

                            action();
                            break;
                        }
                        case TimerType.RepeatedTimer:
                        {
                            var action = (Action) timer.Callback;
                            AddTimer(_now() + timer.Time, timer);

                            if (action == null)
                            {
                                Log.Error($"timer {timer.ToJson()}");
                                break;
                            }

                            action();
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        private void AddTimer(long tillTime, TimerAction timer)
        {
            _timers.Add(timer.Id, timer);
            _timeId.Add(tillTime, timer.Id);

            if (tillTime < _minTime)
            {
                _minTime = tillTime;
            }
        }

        /// <summary>
        /// 异步等待一帧时间。
        /// </summary>
        /// <returns>等待是否成功。</returns>
        public async FTask<bool> WaitFrameAsync()
        {
            return await WaitAsync(1);
        }

        /// <summary>
        /// 异步等待指定时间。
        /// </summary>
        /// <param name="time">等待的时间长度。</param>
        /// <param name="cancellationToken">可选的取消令牌。</param>
        /// <returns>等待是否成功。</returns>
        public async FTask<bool> WaitAsync(long time, FCancellationToken cancellationToken = null)
        {
            return await WaitTillAsync(_now() + time, cancellationToken);
        }

        /// <summary>
        /// 异步等待直到指定时间。
        /// </summary>
        /// <param name="tillTime">等待的目标时间。</param>
        /// <param name="cancellationToken">可选的取消令牌。</param>
        /// <returns>等待是否成功。</returns>
        public async FTask<bool> WaitTillAsync(long tillTime, FCancellationToken cancellationToken = null)
        {
            if (_now() > tillTime)
            {
                return true;
            }

            var tcs = FTask<bool>.Create();
            var timerAction = TimerAction.Create();
            var timerId = timerAction.Id;
            timerAction.Callback = tcs;
            timerAction.TimerType = TimerType.OnceWaitTimer;

            // 定义取消操作的方法
            void CancelActionVoid()
            {
                if (!_timers.ContainsKey(timerId))
                {
                    return;
                }

                Remove(timerId);
                tcs.SetResult(false);
            }

            bool b;
            try
            {
                cancellationToken?.Add(CancelActionVoid);
                AddTimer(tillTime, timerAction);
                b = await tcs;
            }
            finally
            {
                cancellationToken?.Remove(CancelActionVoid);
            }

            return b;
        }

        /// <summary>
        /// 创建一个在下一帧执行的计时器。
        /// </summary>
        /// <param name="action">计时器回调方法。</param>
        /// <returns>计时器的 ID。</returns>
        public long NewFrameTimer(Action action)
        {
            return RepeatedTimer(100, action);
        }

        /// <summary>
        /// 创建一个重复执行的计时器。
        /// </summary>
        /// <param name="time">计时器重复间隔的时间。</param>
        /// <param name="action">计时器回调方法。</param>
        /// <returns>计时器的 ID。</returns>
        public long RepeatedTimer(long time, Action action)
        {
            if (time <= 0)
            {
                throw new Exception("repeated time <= 0");
            }

            var tillTime = _now() + time;
            var timer = TimerAction.Create();
            timer.TimerType = TimerType.RepeatedTimer;
            timer.Time = time;
            timer.Callback = action;
            AddTimer(tillTime, timer);
            return timer.Id;
        }

        /// <summary>
        /// 创建一个重复执行的计时器，用于发布指定类型的事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="time">计时器重复间隔的时间。</param>
        /// <param name="timerHandlerType">事件处理器类型。</param>
        /// <returns>计时器的 ID。</returns>
        public long RepeatedTimer<T>(long time, T timerHandlerType) where T : struct
        {
            void RepeatedTimerVoid()
            {
                EventSystem.Instance.Publish(timerHandlerType);
            }

            return RepeatedTimer(time, RepeatedTimerVoid);
        }

        /// <summary>
        /// 创建一个只执行一次的计时器。
        /// </summary>
        /// <param name="time">计时器执行的延迟时间。</param>
        /// <param name="action">计时器回调方法。</param>
        /// <returns>计时器的 ID。</returns>
        public long OnceTimer(long time, Action action)
        {
            return OnceTillTimer(_now() + time, action);
        }

        /// <summary>
        /// 创建一个只执行一次的计时器，用于发布指定类型的事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="time">计时器执行的延迟时间。</param>
        /// <param name="timerHandlerType">事件处理器类型。</param>
        /// <returns>计时器的 ID。</returns>
        public long OnceTimer<T>(long time, T timerHandlerType) where T : struct
        {
            void OnceTimerVoid()
            {
                EventSystem.Instance.Publish(timerHandlerType);
            }

            return OnceTimer(time, OnceTimerVoid);
        }

        /// <summary>
        /// 创建一个只执行一次的计时器，直到指定时间。
        /// </summary>
        /// <param name="tillTime">计时器执行的目标时间。</param>
        /// <param name="action">计时器回调方法。</param>
        /// <returns>计时器的 ID。</returns>
        public long OnceTillTimer(long tillTime, Action action)
        {
            if (tillTime < _now())
            {
                Log.Error($"new once time too small tillTime:{tillTime} Now:{_now()}");
            }

            var timer = TimerAction.Create();
            timer.TimerType = TimerType.OnceTimer;
            timer.Callback = action;
            AddTimer(tillTime, timer);
            return timer.Id;
        }

        /// <summary>
        /// 创建一个只执行一次的计时器，直到指定时间，用于发布指定类型的事件。
        /// </summary>
        /// <typeparam name="T">事件类型。</typeparam>
        /// <param name="tillTime">计时器执行的目标时间。</param>
        /// <param name="timerHandlerType">事件处理器类型。</param>
        /// <returns>计时器的 ID。</returns>
        public long OnceTillTimer<T>(long tillTime, T timerHandlerType) where T : struct
        {
            void OnceTillTimerVoid()
            {
                EventSystem.Instance.Publish(timerHandlerType);
            }

            return OnceTillTimer(tillTime, OnceTillTimerVoid);
        }

        /// <summary>
        /// 通过引用移除计时器。
        /// </summary>
        /// <param name="id">计时器的 ID。</param>
        public void RemoveByRef(ref long id)
        {
            Remove(id);
            id = 0;
        }

        /// <summary>
        /// 移除指定 ID 的计时器。
        /// </summary>
        /// <param name="id">计时器的 ID。</param>
        public void Remove(long id)
        {
            if (id == 0 || !_timers.Remove(id, out var timer))
            {
                return;
            }

            timer?.Dispose();
        }
    }
}