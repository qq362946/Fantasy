using Fantasy.Helper;
#if FANTASY_UNITY
using UnityEngine;
#endif
namespace Fantasy
{
    public sealed class TimerScheduler : Singleton<TimerScheduler>, IUpdateSingleton
    {
#if FANTASY_SERVER
        public readonly TimerSchedulerCore Core = new TimerSchedulerCore(() => TimeHelper.Now);
#elif FANTASY_UNITY
        public readonly TimerSchedulerCore Core = new TimerSchedulerCore(() => TimeHelper.Now);
        public readonly TimerSchedulerCore Unity = new TimerSchedulerCore(() => (long) (Time.time * 1000));
#endif
        public void Update()
        {
            Core.Update();
#if FANTASY_UNITY
            Unity.Update();
#endif
        }
    }
}