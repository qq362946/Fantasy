// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy
{
    public static class ThreadScheduler
    {
        public static MainScheduler MainScheduler { get; private set; }
        public static ISceneScheduler MultiThreadScheduler { get; private set; }
        public static ISceneScheduler ThreadPoolScheduler { get; private set; }

        static ThreadScheduler()
        {
            MainScheduler = new MainScheduler();
        }
        
        public static void Update()
        {
            MainScheduler.Update();
        }

        public static void AddMainScheduler(Scene scene)
        {
            MainScheduler.Add(scene);
        }

        public static void AddToMultiThreadScheduler(Scene scene)
        {
            if (MultiThreadScheduler == null)
            {
#if FANTASY_SINGLETHREAD || FANTASY_WEBGL
                MultiThreadScheduler = MainScheduler;
#else
                MultiThreadScheduler = new MultiThreadScheduler();
#endif
            }

            MultiThreadScheduler.Add(scene);
        }

        public static void AddToThreadPoolScheduler(Scene scene)
        {
            if (ThreadPoolScheduler == null)
            {
#if FANTASY_SINGLETHREAD || FANTASY_WEBGL
                ThreadPoolScheduler = MainScheduler;
#else
                ThreadPoolScheduler = new ThreadPoolScheduler();
#endif
            }
            
            ThreadPoolScheduler.Add(scene);
        }
    }
}