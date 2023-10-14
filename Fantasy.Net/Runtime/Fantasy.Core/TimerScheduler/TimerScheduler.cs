using Fantasy.Helper;
#if FANTASY_UNITY
using UnityEngine;
#endif
namespace Fantasy
{
    /// <summary>
    /// 计时器调度器类，用于管理计时器任务的调度。
    /// </summary>
    public sealed class TimerScheduler : Singleton<TimerScheduler>, IUpdateSingleton
    {
        /// <summary>
        /// 使用系统时间创建的计时器核心。
        /// </summary>
        public readonly TimerSchedulerCore Core = new TimerSchedulerCore(() => TimeHelper.Now);
#if FANTASY_UNITY
        /// <summary>
        /// 使用 Unity 时间创建的计时器核心。
        /// </summary>
        public readonly TimerSchedulerCore Unity = new TimerSchedulerCore(() => (long) (Time.time * 1000));
#endif
        /// <summary>
        /// 更新计时器任务。
        /// </summary>
        public void Update()
        {
            Core.Update();
#if FANTASY_UNITY
            Unity.Update();
#endif
        }
    }
}