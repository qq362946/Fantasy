// ReSharper disable ForCanBeConvertedToForeach
#if FANTASY_UNITY
using UnityEngine;
#endif
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Fantasy
{
    /// <summary>
    /// 时间调度组件的UpdateSystem
    /// </summary>
    public sealed class TimerComponentUpdateSystem : UpdateSystem<TimerComponent>
    {
        /// <summary>
        /// Update方法
        /// </summary>
        /// <param name="self"></param>
        protected override void Update(TimerComponent self)
        {
            self.Update();
        }
    }

    /// <summary>
    /// 时间调度组件
    /// </summary>
    public sealed class TimerComponent : Entity
    {
        /// <summary>
        /// 使用系统时间创建的计时器核心。
        /// </summary>
        public readonly TimerScheduler Core = new TimerScheduler(() => TimeHelper.Now);
#if FANTASY_UNITY
        /// <summary>
        /// 使用 Unity 时间创建的计时器核心。
        /// </summary>
        public readonly TimerScheduler Unity = new TimerScheduler(() => (long) (Time.time * 1000));
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
