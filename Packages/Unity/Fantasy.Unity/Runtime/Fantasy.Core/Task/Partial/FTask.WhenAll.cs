using System.Collections.Generic;

namespace Fantasy
{
    /// <summary>
    /// 提供用于异步任务操作的静态方法。
    /// </summary>
    public partial class FTask
    {
        /// <summary>
        /// 等待所有任务完成的异步方法。
        /// </summary>
        /// <param name="tasks">要等待的任务列表。</param>
        public static async FTask WhenAll(List<FTask> tasks)
        {
            if (tasks.Count <= 0)
            {
                return;
            }
            
            var count = tasks.Count;
            var sTaskCompletionSource = Create();
     
            foreach (var task in tasks)
            {
                RunSTask(sTaskCompletionSource, task).Coroutine();
            }

            await sTaskCompletionSource;

            async FVoid RunSTask(FTask tcs, FTask task)
            {
                await task;
                count--;
                
                if (count <= 0)
                {
                    tcs.SetResult();
                }
            }
        }

        /// <summary>
        /// 等待任意一个任务完成的异步方法。
        /// </summary>
        /// <param name="tasks">要等待的任务数组。</param>
        public static async FTask Any(params FTask[] tasks)
        {
            if (tasks == null || tasks.Length <= 0)
            {
                return;
            }

            var tcs = FTask.Create();

            int count = 1;
            
            foreach (FTask task in tasks)
            {
                RunSTask(task).Coroutine();
            }
            
            await tcs;

            async FVoid RunSTask(FTask task)
            {
                await task;

                count--;

                if (count == 0)
                {
                    tcs.SetResult();
                }
            }
        }
    }

    /// <summary>
    /// 提供用于异步任务操作的静态方法，支持泛型参数。
    /// </summary>
    public partial class FTask<T>
    {
        /// <summary>
        /// 等待所有任务完成的异步方法。
        /// </summary>
        /// <param name="tasks">要等待的任务列表。</param>
        public static async FTask WhenAll(List<FTask<T>> tasks)
        {
            if (tasks.Count <= 0)
            {
                return;
            }
            
            var count = tasks.Count;
            var sTaskCompletionSource = FTask.Create();

            foreach (var task in tasks)
            {
                RunSTask(sTaskCompletionSource, task).Coroutine();
            }

            await sTaskCompletionSource;

            async FVoid RunSTask(FTask tcs, FTask<T> task)
            {
                await task;
                count--;
                if (count == 0)
                {
                    tcs.SetResult();
                }
            }
        }

        /// <summary>
        /// 等待所有任务完成的异步方法。
        /// </summary>
        /// <param name="tasks">要等待的任务数组。</param>
        public static async FTask WhenAll(params FTask<T>[] tasks)
        {
            if (tasks == null || tasks.Length <= 0)
            {
                return;
            }
            
            var count = tasks.Length;
            var tcs = FTask.Create();

            foreach (var task in tasks)
            {
                RunSTask(task).Coroutine();
            }

            await tcs;

            async FVoid RunSTask(FTask<T> task)
            {
                await task;
                count--;
                if (count == 0)
                {
                    tcs.SetResult();
                }
            }
        }

        /// <summary>
        /// 等待任意一个任务完成的异步方法。
        /// </summary>
        /// <param name="tasks">要等待的任务数组。</param>
        public static async FTask WaitAny(params FTask<T>[] tasks)
        {
            if (tasks == null || tasks.Length <= 0)
            {
                return;
            }

            var tcs = FTask.Create();

            int count = 1;
            
            foreach (FTask<T> task in tasks)
            {
                RunSTask(task).Coroutine();
            }
            
            await tcs;

            async FVoid RunSTask(FTask<T> task)
            {
                await task;

                count--;

                if (count == 0)
                {
                    tcs.SetResult();
                }
            }
        }
    }
}