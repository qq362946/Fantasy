using UnityEngine;

namespace Fantasy
{
    public sealed class AudioComponent : Entity
    {
        /// <summary>
        /// 音层
        /// </summary>
        public int AudioLayer;
        
        /// <summary>
        /// 关键的GameObject
        /// </summary>
        public GameObject GameObject;

        /// <summary>
        /// 音频源控件
        /// </summary>
        public AudioSource AudioSource;

        /// <summary>
        /// 是否播放中
        /// </summary>
        public bool Playing
        {
            get => AudioSource.isPlaying;
            set
            {
                if (value)
                {
                    AudioSource.Play();
                    return;
                }
                
                AudioSource.Stop();
            }
        }

        /// <summary>
        /// 是否静音
        /// </summary>
        public bool Mute
        {
            get => AudioSource.mute;
            set => AudioSource.mute = value;
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            AudioSource.Pause();
        }
        
        /// <summary>
        /// 音量
        /// </summary>
        public float Volume
        {
            get => AudioSource.volume;
            set => AudioSource.volume = value;
        }

        /// <summary>
        /// 是否循环播放
        /// </summary>
        public bool Loop
        {
            get => AudioSource.loop;
            set => AudioSource.loop = value;
        }
        
        /// <summary>
        /// 重新播放
        /// </summary>
        public void Replay()
        {
            Playing = false;
            Playing = true;
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            var audioSource = AudioSource;
            
            if (audioSource != null)
            {
                Object.Destroy(audioSource);
            }

            AudioLayer = 0;
            GameObject = null;
            AudioManageComponent.Instance?.Remove(this, false);
            base.Dispose();
        }
    }
}