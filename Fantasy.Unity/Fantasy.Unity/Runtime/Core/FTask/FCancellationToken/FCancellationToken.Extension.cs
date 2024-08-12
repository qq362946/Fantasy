// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
using System;
namespace Fantasy
{
    public static class FCancellationTokenExtension
    {
        /// <summary>
        /// 在当前的FCancellationToken中再创建一个FCancellationToken
        /// 父FCancellationToken.Cancel()会执行子FCancellationToken。
        /// </summary>
        /// <param name="fTask"></param>
        /// <param name="fCancellationToken"></param>
        /// <exception cref="Exception"></exception>
        public static async FTask AddToCancellationToken(this FTask fTask, FCancellationToken fCancellationToken)
        {
            if (fCancellationToken == null)
            {
                throw new Exception("fCancellationToken is null");
            }

            var userTokenAsync = await FTask.GetUserTokenAsync<FCancellationToken>();
            
            if (userTokenAsync != null)
            {
                userTokenAsync.Add(fCancellationToken.Cancel);
            }

            await fTask.SetUserToKen(fCancellationToken);
        }

        /// <summary>
        /// 在当前的FCancellationToken中再创建一个FCancellationToken
        /// 父FCancellationToken.Cancel()会执行子FCancellationToken。
        /// </summary>
        /// <param name="fTask"></param>
        /// <param name="fCancellationToken"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async FTask<T> AddToCancellationToken<T>(this FTask<T> fTask, FCancellationToken fCancellationToken)
        {
            if (fCancellationToken == null)
            {
                throw new Exception("fCancellationToken is null");
            }
            
            var userTokenAsync = await FTask.GetUserTokenAsync<FCancellationToken>();

            if (userTokenAsync != null)
            {
                userTokenAsync.Add(fCancellationToken.Cancel);
            }

            return await fTask.SetUserToKen(fCancellationToken);
        }
    }
}