using System;
using UnityEngine;

namespace Fantasy.Core
{
    public enum AudioLayer
    {
        None = 0,
        /// <summary>
        /// 背景音效
        /// </summary>
        Background = 1, 
        /// <summary>
        /// UI音效
        /// </summary>
        UI = 2,
        /// <summary>
        /// 单位音效（玩家、怪物、等）
        /// </summary>
        Unit = 3,
        /// <summary>
        /// 游戏中NPC
        /// </summary>
        NPC = 4,
        /// <summary>
        /// 音效层的最大
        /// </summary>
        Max
    }
    
    public sealed class AudioManageComponentAwakeSystem : AwakeSystem<AudioManageComponent>
    {
        protected override void Awake(AudioManageComponent self)
        {
            AudioManageComponent.Instance = self;
        }
    }

    /// <summary>
    /// 音效管理组件
    /// </summary>
    public sealed class AudioManageComponent : Entity
    {
        public static AudioManageComponent Instance;
        private readonly OneToManyList<int, AudioComponent> _audioComponents = new OneToManyList<int, AudioComponent>();

        /// <summary>
        /// 用自定义audioLayer添加一个音效组件
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="gameObject"></param>
        /// <param name="audioLayer">audioLayer是自己定义的层并且不能小于等于AudioLayer枚举的MAX</param>
        /// <param name="playOnAwake"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public AudioComponent Add(Entity entity, GameObject gameObject, int audioLayer, bool playOnAwake = true, bool loop = true)
        { 
            if (audioLayer <= (int)AudioLayer.Max)
            {
                Log.Error($"AudioLayer:{audioLayer} cannot be less than or equal to AudioLayer.Max:{(int)AudioLayer.Max}");
                return null;
            }

            var audioComponent = entity.AddComponent<AudioComponent>();

            try
            {
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = loop;
                audioSource.playOnAwake = playOnAwake;
                audioComponent.GameObject = gameObject;
                audioComponent.AudioLayer = audioLayer;
                _audioComponents.Add(audioLayer, audioComponent);
            }
            catch (Exception e)
            {
                audioComponent.Dispose();
                audioComponent = null;
                Log.Error(e);
            }
            
            return audioComponent;
        }

        /// <summary>
        /// 用自带的AudioLayer枚举创建一个音效组件
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="gameObject"></param>
        /// <param name="audioLayer">使用框架自带的AudioLayer枚举</param>
        /// <param name="playOnAwake"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public AudioComponent Add(Entity entity, GameObject gameObject, AudioLayer audioLayer, bool playOnAwake = true, bool loop = true)
        {
            var audioComponent = entity.AddComponent<AudioComponent>();
            
            try
            {
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = loop;
                audioSource.playOnAwake = playOnAwake;
                audioComponent.GameObject = gameObject;
                audioComponent.AudioLayer = (int)audioLayer;
                _audioComponents.Add((int)audioLayer, audioComponent);
            }
            catch (Exception e)
            {
                audioComponent.Dispose();
                audioComponent = null;
                Log.Error(e);
            }
            
            return audioComponent;
        }

        /// <summary>
        /// 移除一个音效组件
        /// </summary>
        /// <param name="audioComponent"></param>
        /// <param name="isDisposed"></param>
        public void Remove(AudioComponent audioComponent, bool isDisposed = true)
        {
            _audioComponents.RemoveValue(audioComponent.AudioLayer, audioComponent);

            if (isDisposed)
            {
                audioComponent.Dispose();
            }
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            
            foreach (var (_, audioComponents) in _audioComponents)
            {
                foreach (var audioComponent in audioComponents)
                {
                    audioComponent.Dispose();
                }
            }
            
            Instance = null;
            _audioComponents.Clear();
            base.Dispose();
        }
    }
}