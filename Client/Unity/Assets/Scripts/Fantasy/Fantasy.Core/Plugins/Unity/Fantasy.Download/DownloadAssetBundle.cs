#if FANTASY_UNITY
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Fantasy.Core
{
    public sealed class DownloadAssetBundle : AUnityDownload
    {
        public FTask<AssetBundle> StartDownload(string url, FCancellationToken cancellationToken = null)
        {
            var task = FTask<AssetBundle>.Create(false);
            var unityWebRequestAsyncOperation = Start(UnityWebRequestAssetBundle.GetAssetBundle(Uri.EscapeUriString(url)));
            
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
                        var assetBundle = DownloadHandlerAssetBundle.GetContent(UnityWebRequest);
                        task.SetResult(assetBundle);
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