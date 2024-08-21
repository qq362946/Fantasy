// ReSharper disable MemberCanBePrivate.Global
#if FANTASY_UNITY

namespace Fantasy
{
    public class SessionHeartbeatComponentAwakeSystem : AwakeSystem<SessionHeartbeatComponent>
    {
        protected override void Awake(SessionHeartbeatComponent self)
        {
            self.TimerComponent = self.Scene.TimerComponent;
        }
    }

    /// <summary>
    /// 负责管理会话心跳的组件。
    /// </summary>
    public class SessionHeartbeatComponent : Entity
    {
        public int TimeOut;
        public long TimerId;  
        public long LastTime;
        public long SelfRunTimeId;
        public long TimeOutTimerId;
        public Session Session;
        public TimerComponent TimerComponent; 
        private readonly PingRequest _pingRequest = new PingRequest(); 
        
        public int Ping { get; private set; }
        
        public override void Dispose()
        {
            Stop();
            Ping = 0;
            Session = null;
            TimeOut = 0;
            SelfRunTimeId = 0;
            base.Dispose();
        }

        /// <summary>
        /// 使用指定的间隔启动心跳功能。
        /// </summary>
        /// <param name="interval">以毫秒为单位的心跳请求发送间隔。</param>
        /// <param name="timeOut">设置与服务器的通信超时时间，如果超过这个时间限制，将自动断开会话(Session)。</param>
        /// <param name="timeOutInterval">用于检测与服务器连接超时频率。</param>
        public void Start(int interval, int timeOut = 2000, int timeOutInterval = 3000)
        {
            TimeOut = timeOut;
            Session = (Session)Parent;
            SelfRunTimeId = RunTimeId;
            LastTime = TimeHelper.Now;
            TimerId = TimerComponent.Unity.RepeatedTimer(interval, () => RepeatedSend().Coroutine());
            TimeOutTimerId = TimerComponent.Unity.RepeatedTimer(timeOutInterval, CheckTimeOut);
        }

        private void CheckTimeOut()
        {
            if (TimeHelper.Now - LastTime < TimeOut)
            {
                return;
            }

            if (SelfRunTimeId != Session.RunTimeId)
            {
                return;
            }
            
            Session.Dispose();
        }

        /// <summary>
        /// 停止心跳功能。
        /// </summary>
        public void Stop()
        {
            if (TimerId != 0)
            {
                TimerComponent?.Unity.Remove(ref TimerId);
            }
            
            if (TimeOutTimerId != 0)
            {
                TimerComponent?.Unity.Remove(ref TimeOutTimerId);
            }
        }

        /// <summary>
        /// 异步发送心跳请求并处理响应。
        /// </summary>
        /// <returns>表示进行中操作的异步任务。</returns>
        private async FTask RepeatedSend()
        {
            if (SelfRunTimeId != RunTimeId)
            {
                Stop();
            }
            
            var requestTime = TimeHelper.Now;
            
            var pingResponse = (PingResponse)await Session.Call(_pingRequest);

            if (pingResponse.ErrorCode != 0)
            {
                return;
            }
            
            var responseTime = TimeHelper.Now;
            LastTime = responseTime;
            Ping = (int)(responseTime - requestTime) / 2;
            TimeHelper.TimeDiff = pingResponse.Now + Ping - responseTime;
        }
    }
}
#endif