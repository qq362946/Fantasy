#if FANTASY_UNITY
using Fantasy.Async;
using UnityEngine.Networking;

namespace Fantasy.Unity.Download
{
    public sealed class DownloadByte : AUnityDownload
    {
        public DownloadByte(Scene scene, Download download) : base(scene, download)
        {
        }

        public void StartDownload(string url, DownloadHandler downloadHandler, bool monitor)
        {
            UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = Start(UnityWebRequest.Get(url), downloadHandler, monitor);
           
            unityWebRequestAsyncOperation.completed += operation =>
            {
                try
                {
                    if (UnityWebRequest.result != UnityWebRequest.Result.Success)
                    {
                        Log.Error(UnityWebRequest.error);
                    } 
                }
                finally
                {
                    Dispose();
                }
            };
        }
    }
}
#endif