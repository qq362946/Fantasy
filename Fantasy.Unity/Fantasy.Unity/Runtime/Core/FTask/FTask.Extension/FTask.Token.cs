// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
namespace Fantasy
{
    public partial class FTask
    {
        public static async FTask<T> GetUserTokenAsync<T>() where T : class
        {
            var tcs = FTask<object>.Create();
            tcs.FTaskType = FTaskType.UserToKenTask;
            var result = await tcs;
            if (result == null)
            {
                return null;
            }
            return result as T;
        }
    }
    
    internal static class FTaskExtension
    {
        internal static void InnerSetUserToken(this IFTask task, object userToken)
        {
            while (task != null)
            {
                switch (task.FTaskType)
                {
                    case FTaskType.ContagionUserToKen:
                    {
                        // 只有在非常极端的情况下才会执行到这里，不是非常理解FTask的做不出来这个问题。
                        // 只有在重复设置Task的UserToken才会执行到这里。
                        // 所以不允许一个Task重复设置UserToken。
                        Log.Error("UserToken is set repeatedly");
                        return;
                    }
                    case FTaskType.UserToKenTask:
                    {
                        (task as FTask<object>).SetResult(userToken);
                        return;
                    }
                }

                task.FTaskType = FTaskType.ContagionUserToKen;
                var child = task.UserToKen;
                task.UserToKen = userToken;
                task = child as IFTask;
            }
        }
    }
}