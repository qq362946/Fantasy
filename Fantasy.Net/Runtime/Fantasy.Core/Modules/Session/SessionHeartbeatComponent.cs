#if FANTASY_UNITY
using Fantasy

namespace Fantasy
{
    /// <summary>
    /// 负责管理会话心跳的组件。
    /// </summary>
    public class SessionHeartbeatComponent : Entity
    {
        private long _timerId;  // 心跳间隔计时器的 ID
        private long _selfRunTimeId;  // 用于确保组件完整性的自身运行时 ID
        private Session _session;  // 对会话对象的引用
        private readonly PingRequest _pingRequest = new PingRequest(); // 心跳的 Ping 请求对象
        
        /// <summary>
        /// 获取当前的 Ping 值。
        /// </summary>
        public int Ping { get; private set; }

        /// <summary>
        /// 重写 Dispose 方法以释放资源。
        /// </summary>
        public override void Dispose()
        {
            Stop();
            Ping = 0;
            _session = null;
            _selfRunTimeId = 0;
            base.Dispose();
        }

        /// <summary>
        /// 使用指定的间隔启动心跳功能。
        /// </summary>
        /// <param name="interval">以毫秒为单位的心跳请求发送间隔。</param>
        public void Start(int interval)
        {
            _session = (Session)Parent;
            _selfRunTimeId = RuntimeId;
            _timerId = TimerScheduler.Instance.Unity.RepeatedTimer(interval, () => RepeatedSend().Coroutine());
        }

        /// <summary>
        /// 停止心跳功能。
        /// </summary>
        public void Stop()
        {
            if (_timerId == 0)
            {
                return; // 如果计时器 ID 为 0，则计时器未激活，直接返回
            }
            
            TimerScheduler.Instance?.Unity.RemoveByRef(ref _timerId);
        }

        /// <summary>
        /// 异步发送心跳请求并处理响应。
        /// </summary>
        /// <returns>表示进行中操作的异步任务。</returns>
        private async FTask RepeatedSend()
        {
            if (_selfRunTimeId != RuntimeId)
            {
                Stop();
            }
            
            var requestTime = TimeHelper.Now;
            var pingResponse = (PingResponse)await _session.Call(_pingRequest);

            if (pingResponse.ErrorCode != 0)
            {
                return;
            }
            
            var responseTime = TimeHelper.Now; // 记录接收心跳响应的时间
            Ping = (int)(responseTime - requestTime) / 2;
            TimeHelper.TimeDiff = pingResponse.Now + Ping - responseTime;
        }
    }
}
#endif