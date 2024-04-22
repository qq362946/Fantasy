// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
namespace Fantasy
{
    /// <summary>
    /// 线程调度器。
    /// </summary>
    public static class ThreadScheduler
    {
        /// <summary>
        /// 主线程调度器。
        /// </summary>
        private static readonly IThreadScheduler MainThreadScheduler = new MainThreadScheduler();
        /// <summary>
        /// 多线程调度器。
        /// </summary>
        private static IThreadScheduler MultiThreadScheduler { get; set; }
        /// <summary>
        /// 线程池调度器。
        /// </summary>
        private static IThreadScheduler ThreadPoolScheduler { get; set; }
        /// <summary>
        /// 更新调度器。
        /// </summary>
        public static void Update()
        {
            MainThreadScheduler.Update();
        }
        /// <summary>
        /// 添加到主线程调度器。
        /// </summary>
        /// <param name="sceneSchedulerId"></param>
        public static void AddToMainThreadScheduler(long sceneSchedulerId)
        {
            MainThreadScheduler.Add(sceneSchedulerId);
        }
        /// <summary>
        /// 添加到多线程调度器。
        /// </summary>
        /// <param name="sceneSchedulerId"></param>
        public static void AddToMultiThreadScheduler(long sceneSchedulerId)
        {
            if (MultiThreadScheduler == null)
            {
#if FANTASY_SINGLETHREAD || FANTASY_WEBGL
                MultiThreadScheduler = MainThreadScheduler;
#else
                MultiThreadScheduler = new MultiThreadScheduler();
#endif
            }

            MultiThreadScheduler.Add(sceneSchedulerId);
        }

        /// <summary>
        /// 添加到线程池调度器。
        /// </summary>
        /// <param name="sceneSchedulerId"></param>
        public static void AddToThreadPoolScheduler(long sceneSchedulerId)
        {
            if (ThreadPoolScheduler == null)
            {
#if FANTASY_SINGLETHREAD || FANTASY_WEBGL
                ThreadPoolScheduler = MainThreadScheduler;
#else
                ThreadPoolScheduler = new ThreadPoolScheduler();
#endif
            }

            ThreadPoolScheduler.Add(sceneSchedulerId);
        }
    }
}