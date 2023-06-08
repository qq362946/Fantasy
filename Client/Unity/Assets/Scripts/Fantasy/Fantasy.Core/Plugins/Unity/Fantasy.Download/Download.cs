#if FANTASY_UNITY
using System.Collections.Generic;
using Fantasy.Helper;
using UnityEngine;

namespace Fantasy.Core
{
    public sealed class Download : Singleton<Download>
    {
        public readonly HashSet<AUnityDownload> Downloads = new();
        
        public ulong GetDownloadSpeed()
        {
            ulong speed = 0;
            
            foreach (var aUnityDownload in Downloads)
            {
                speed += aUnityDownload.DownloadSpeed;
            }

            return speed;
        }

        public FTask<AssetBundle> DownloadAssetBundle(string url, FCancellationToken cancellationToken = null)
        {
            return new DownloadAssetBundle().StartDownload(url, cancellationToken);
        }

        public FTask<AudioClip> DownloadAudioClip(string url, AudioType audioType, FCancellationToken cancellationToken = null)
        {
            return new DownloadAudioClip().StartDownload(url, audioType, cancellationToken);
        }

        public FTask<Sprite> DownloadSprite(string url, FCancellationToken cancellationToken = null)
        {
            return new DownloadSprite().StartDownload(url, cancellationToken);
        }
        
        public FTask<Texture> DownloadTexture(string url, FCancellationToken cancellationToken = null)
        {
            return new DownloadTexture().StartDownload(url, cancellationToken);
        }
        
        public FTask<string> DownloadText(string url, FCancellationToken cancellationToken = null)
        {
            return new DownloadText().StartDownload(url, cancellationToken);
        }

        public FTask<byte[]> DownloadByte(string url, FCancellationToken cancellationToken = null)
        {
            return new DownloadByte().StartDownload(url, cancellationToken);
        }
    }
}
#endif
