#if FANTASY_UNITY
using UnityEngine.Networking;

namespace Fantasy
{
    public sealed class DownloadText : AUnityDownload
    {
        public DownloadText(Download download) : base(download)
        {
        }
        
        public FTask<string> StartDownload(string url, bool monitor, FCancellationToken cancellationToken = null)
        {
            var task = FTask<string>.Create(false);
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
                        var text = UnityWebRequest.downloadHandler.text;
                        task.SetResult(text);
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