#if FANTASY_UNITY
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fantasy
{
    public sealed class Download
    {
        public ulong DownloadSpeed;
        public ulong TotalDownloadedBytes;
        public readonly HashSet<AUnityDownload> Tasks = new();

        public static Download Create => new Download();

        public void Clear()
        {
            DownloadSpeed = 0;
            TotalDownloadedBytes = 0;
            
            if (Tasks.Count <= 0)
            {
                return;
            }
            
            foreach (var aUnityDownload in Tasks.ToArray())
            {
                aUnityDownload.Dispose();
            }
            
            Tasks.Clear();
        }

        public FTask<AssetBundle> DownloadAssetBundle(string url, bool monitor = false, FCancellationToken cancellationToken = null)
        {
            return new DownloadAssetBundle(this).StartDownload(url, monitor, cancellationToken);
        }

        public FTask<AudioClip> DownloadAudioClip(string url, AudioType audioType, bool monitor = false, FCancellationToken cancellationToken = null)
        {
            return new DownloadAudioClip(this).StartDownload(url, audioType, monitor, cancellationToken);
        }

        public FTask<Sprite> DownloadSprite(string url, bool monitor = false, FCancellationToken cancellationToken = null)
        {
            return new DownloadSprite(this).StartDownload(url, monitor, cancellationToken);
        }
        
        public FTask<Texture> DownloadTexture(string url, bool monitor = false, FCancellationToken cancellationToken = null)
        {
            return new DownloadTexture(this).StartDownload(url, monitor, cancellationToken);
        }
        
        public FTask<string> DownloadText(string url, bool monitor = false, FCancellationToken cancellationToken = null)
        {
            return new DownloadText(this).StartDownload(url, monitor, cancellationToken);
        }

        public FTask<byte[]> DownloadByte(string url, bool monitor = false, FCancellationToken cancellationToken = null)
        {
            return new DownloadByte(this).StartDownload(url, monitor, cancellationToken);
        }
    }
}
#endif
