using System;
using System.Threading;

namespace Cysharp.Threading.Tasks
{
    public static partial class UniTaskExtension
    {
        public static void AttachCancellation(this AutoResetUniTaskCompletionSourcePlus tcs, CancellationToken token)
        {
            if (!token.CanBeCanceled)
                return;

            void TrySetCanceled()
            {
                tcs.TrySetCanceled(token);
            }

            CancellationTokenRegistration ctr = token.RegisterWithoutCaptureExecutionContext(TrySetCanceled);

            void Dispose()
            {
                ctr.Dispose();
            }

            tcs.AddOnCancelAction(Dispose);
            tcs.AddOnExceptionAction(Dispose);
            tcs.AddOnResultAction(Dispose);
        }

        public static void AttachCancellation<T>(this AutoResetUniTaskCompletionSourcePlus<T> tcs, CancellationToken token)
        {
            if (!token.CanBeCanceled)
                return;

            void TrySetCanceled()
            {
                tcs.TrySetCanceled(token);
            }

            CancellationTokenRegistration ctr = token.RegisterWithoutCaptureExecutionContext(TrySetCanceled);

            void Dispose()
            {
                ctr.Dispose();
            }

            tcs.AddOnCancelAction(Dispose);
            tcs.AddOnExceptionAction(Dispose);
            tcs.AddOnResultAction(Dispose);
        }

        public static UniTask AttachCancellation(this UniTask task, CancellationToken token, Action cancelAction = null)
        {
            if (token.IsCancellationRequested)
            {
                throw new Exception("Can't attach canceled CancellationToken!");
            }
            if (token.CanBeCanceled)
            {
                var tcs = AutoResetUniTaskCompletionSource.Create();
                
                void CancelAction()
                {
                    cancelAction?.Invoke();
                    tcs.TrySetCanceled(token);
                }
                
                async UniTaskVoid RunTask()
                {
                    var ctr = token.RegisterWithoutCaptureExecutionContext(CancelAction);
                    try
                    {
                        await task;
                        tcs.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        ctr.Dispose();
                    }
                }
                
                RunTask().Forget();
                return tcs.Task;
            }
            return task;
        }
        
        public static UniTask<T> AttachCancellation<T>(this UniTask<T> task, CancellationToken token, Action cancelAction = null)
        {
            if (token.IsCancellationRequested)
            {
                throw new Exception("Can't attach canceled CancellationToken!");
            }
            if (token.CanBeCanceled)
            {
                var tcs = AutoResetUniTaskCompletionSource<T>.Create();
                
                void CancelAction()
                {
                    cancelAction?.Invoke();
                    tcs.TrySetCanceled(token);
                }
                
                async UniTaskVoid RunTask()
                {
                    var ctr = token.RegisterWithoutCaptureExecutionContext(CancelAction);
                    try
                    {
                        T result = await task;
                        tcs.TrySetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                    finally
                    {
                        ctr.Dispose();
                    }
                }
                
                RunTask().Forget();
                return tcs.Task;
            }
            return task;
        }

        public static async UniTask RunCancelAsync(this UniTask task, Action cancelAction)
        {
            bool canceled = await task.SuppressCancellationThrow();
            if (canceled)
            {
                cancelAction.Invoke();
            }
        }
        
        public static async UniTask RunCancelAsync<T>(this UniTask<T> task, Action cancelAction)
        {
            (bool canceled, _) = await task.SuppressCancellationThrow();
            if (canceled)
            {
                cancelAction.Invoke();
            }
        }
    }
}