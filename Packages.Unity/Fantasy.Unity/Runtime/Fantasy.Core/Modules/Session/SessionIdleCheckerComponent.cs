#if FANTASY_NET


namespace Fantasy;

/// <summary>
/// 负责检查会话空闲超时的组件。
/// </summary>
public class SessionIdleCheckerComponent : Entity
{
    private long _timeOut;  // 空闲超时时间（毫秒）
    private long _timerId;  // 检查计时器的 ID
    private long _selfRuntimeId;  // 用于确保组件完整性的自身运行时 ID
    private Session _session;  // 对会话对象的引用

    /// <summary>
    /// 重写 Dispose 方法以释放资源。
    /// </summary>
    public override void Dispose()
    {
        Stop(); // 停止检查计时器
        _timeOut = 0; // 重置空闲超时时间
        _selfRuntimeId = 0; // 重置自身运行时 ID
        _session = null; // 清除会话引用
        base.Dispose();
    }

    /// <summary>
    /// 使用指定的间隔和空闲超时时间启动空闲检查功能。
    /// </summary>
    /// <param name="interval">以毫秒为单位的检查间隔。</param>
    /// <param name="timeOut">以毫秒为单位的空闲超时时间。</param>
    public void Start(int interval, int timeOut)
    {
        _timeOut = timeOut;
        _session = (Session)Parent;
        _selfRuntimeId = RuntimeId;
        // 安排重复计时器，在指定的间隔内执行 Check 方法
        _timerId = TimerScheduler.Instance.Core.RepeatedTimer(interval, Check);
    }

    /// <summary>
    /// 停止空闲检查功能。
    /// </summary>
    public void Stop()
    {
        if (_timerId == 0)
        {
            return;
        }

        TimerScheduler.Instance.Core.RemoveByRef(ref _timerId);
    }

    /// <summary>
    /// 执行空闲检查操作。
    /// </summary>
    private void Check()
    {
        if (_selfRuntimeId != RuntimeId)
        {
            Stop();
        }
        
        var timeNow = TimeHelper.Now;
        
        if (timeNow - _session.LastReceiveTime < _timeOut)
        {
            return;
        }
        
        Log.Warning($"session timeout id:{Id} timeNow:{timeNow} _session.LastReceiveTime:{_session.LastReceiveTime} _timeOut:{_timeOut}");
        _session.Dispose();
    }
}
#endif