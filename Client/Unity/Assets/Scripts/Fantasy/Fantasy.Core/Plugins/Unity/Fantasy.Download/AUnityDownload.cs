#if FANTASY_UNITY
using System;
using UnityEngine.Networking;

namespace Fantasy.Core
{
    public abstract class AUnityDownload : IDisposable
    {
        private long _timeId;
        public ulong DownloadSpeed;
        private ulong _totalDownloadedBytes;
        protected UnityWebRequest UnityWebRequest;
        private FCancellationToken _cancellationToken;

        protected AUnityDownload()
        {
            Download.Instance.Downloads.Add(this);
        }
        
        protected UnityWebRequestAsyncOperation Start(UnityWebRequest unityWebRequest)
        {
            UnityWebRequest = unityWebRequest;
            var unityWebRequestAsyncOperation = UnityWebRequest.SendWebRequest();

            _timeId = TimerScheduler.Instance.Unity.RepeatedTimer(1000, () =>
            {
                DownloadSpeed = UnityWebRequest.downloadedBytes - _totalDownloadedBytes;
                _totalDownloadedBytes = UnityWebRequest.downloadedBytes;
            });

            return unityWebRequestAsyncOperation;
        }

        public virtual void Dispose()
        {
            DownloadSpeed = 0;
            _totalDownloadedBytes = 0;
            UnityWebRequest?.Dispose();
            Download.Instance.Downloads.Remove(this);
            TimerScheduler.Instance.Unity.RemoveByRef(ref _timeId);
        }
    }
}
#endif