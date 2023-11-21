#if FANTASY_UNITY
using UnityEngine.Networking;

namespace Fantasy
{
    public sealed class DownloadByte : AUnityDownload
    {
        public DownloadByte(Download download) : base(download)
        {
        }

        public FTask<byte[]> StartDownload(string url, bool monitor, FCancellationToken cancellationToken = null)
        {
            var task = FTask<byte[]>.Create(false);
            var unityWebRequestAsyncOperation = Start(UnityWebRequest.Get(url), monitor);

            if (cancellationToken != null)
            {
                cancellationToken.Add(() =>
                {
                    Dispose();
                    task.SetResult(null);
                });
            }
            
            unityWebRequestAsyncOperation.completed += operation =>
            {
                try
                {
                    if (UnityWebRequest.result == UnityWebRequest.Result.Success)
                    {
                        var bytes = UnityWebRequest.downloadHandler.data;
                        task.SetResult(bytes);
                    }
                    else
                    {
                        Log.Error(UnityWebRequest.error);
                        task.SetResult(null);
                    }
                }
                finally
                {
                    Dispose();
                }
            };

            return task;
        }
    }
}
#endif