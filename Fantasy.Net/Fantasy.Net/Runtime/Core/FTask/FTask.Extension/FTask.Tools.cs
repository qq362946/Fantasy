// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System.Collections.Generic;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace Fantasy.Async
{
    public partial class FTask
    {
        /// <summary>
        /// 等待所有任务完成
        /// </summary>
        /// <param name="tasks"></param>
        public static async FTask WaitAll(List<FTask> tasks)
        {
            if (tasks.Count <= 0)
            {
                return;
            }
            
            var count = tasks.Count;
            var sTaskCompletionSource = Create();
            
            foreach (var task in tasks)
            {
                RunSTask(task).Coroutine();
            }
            
            await sTaskCompletionSource;
            
            async FVoid RunSTask(FTask task)
            {
                await task;
                count--;
                if (count <= 0)
                {
                    sTaskCompletionSource.SetResult();
                }
            }
        }

        /// <summary>
        /// 等待其中一个任务完成
        /// </summary>
        /// <param name="tasks"></param>
        public static async FTask WaitAny(List<FTask> tasks)
        {
            if (tasks.Count <= 0)
            {
                return;
            }
            
            var count = 1;
            var sTaskCompletionSource = Create();
            
            foreach (var task in tasks)
            {
                RunSTask(task).Coroutine();
            }
            
            await sTaskCompletionSource;
            
            async FVoid RunSTask(FTask task)
            {
                await task;
                count--;
                if (count == 0)
                {
                    sTaskCompletionSource.SetResult();
                }
            }
        }
    }
}