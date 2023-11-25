using System.Threading.Tasks;
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
        /// <summary>
        /// 使用帧运行总时间创建的计时器核心。
        /// </summary>
        public readonly TimerSchedulerCore Frame = new TimerSchedulerCore(() => TimeHelper.TotalFrameTime);
#if FANTASY_UNITY
        /// <summary>
        /// 使用 Unity 时间创建的计时器核心。
        /// </summary>
        public readonly TimerSchedulerCore Unity = new TimerSchedulerCore(() => (long) (Time.time * 1000));
#endif
        /// <summary>
        /// 初始化计时器调度器。
        /// </summary>
        /// <returns></returns>
        public override async Task Initialize()
        {
            TimeHelper.PreviousFrameTime = TimeHelper.Now;
            await FTask.CompletedTask;
        }

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
        /// <summary>
        /// 更新帧计时器任务。自动根据TimeHelper.Now计算帧间隔时间。
        /// </summary>
        public void FrameUpdate()
        {
            var now = TimeHelper.Now;
            var frameDeltaTime = now - TimeHelper.PreviousFrameTime;
            TimeHelper.PreviousFrameTime = now;
            FrameUpdate((int)frameDeltaTime);
        }
        /// <summary>
        /// 手动更新帧计时器任务。需要自己计算帧间隔时间。
        /// </summary>
        /// <param name="frameDeltaTime"></param>
        public void FrameUpdate(int frameDeltaTime)
        {
            TimeHelper.FrameDeltaTime = frameDeltaTime;
            TimeHelper.TotalFrameTime += frameDeltaTime;
            Frame.Update();
        }
    }
}