#if FANTASY_UNITY
using System;
using UnityEngine.Networking;

namespace Fantasy
{
    public abstract class AUnityDownload : IDisposable
    {
        private long _timeId;
        private ulong _totalDownloadedBytes;
        private Download _download;
        protected UnityWebRequest UnityWebRequest;
        private FCancellationToken _cancellationToken;

        protected AUnityDownload(Download download)
        {
            _download = download;
            _download.Tasks.Add(this);
        }

        protected UnityWebRequestAsyncOperation Start(UnityWebRequest unityWebRequest, bool monitor)
        {
            UnityWebRequest = unityWebRequest;
            _timeId = TimerScheduler.Instance.Unity.RepeatedTimer(33, Update);
            return UnityWebRequest.SendWebRequest();
        }

        private void Update()
        {
            var downloadSpeed = UnityWebRequest.downloadedBytes - _totalDownloadedBytes;
            _download.DownloadSpeed += downloadSpeed;
            _download.TotalDownloadedBytes += downloadSpeed;
            _totalDownloadedBytes = UnityWebRequest.downloadedBytes;
        }

        public virtual void Dispose()
        {
            Update();
            _totalDownloadedBytes = 0;
            UnityWebRequest?.Dispose();
            _download.Tasks.Remove(this);
            TimerScheduler.Instance?.Unity.RemoveByRef(ref _timeId);
            _download = null;
        }
    }
}
#endif