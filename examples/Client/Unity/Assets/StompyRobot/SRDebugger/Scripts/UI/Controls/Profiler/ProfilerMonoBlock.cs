namespace SRDebugger.UI.Controls
{
    using System;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;
#if UNITY_5_5_OR_NEWER
    using UnityEngine.Profiling;
#endif

    public class ProfilerMonoBlock : SRMonoBehaviourEx
    {
        private float _lastRefresh;

        [RequiredField]
        public Text CurrentUsedText;

        [RequiredField]
        public GameObject NotSupportedMessage;

        [RequiredField]
        public Slider Slider;

        [RequiredField]
        public Text TotalAllocatedText;
        private bool _isSupported;

        protected override void OnEnable()
        {
            base.OnEnable();

#if UNITY_5_6_OR_NEWER
            _isSupported = Profiler.GetMonoUsedSizeLong() > 0;
#else
            _isSupported = Profiler.GetMonoUsedSize() > 0;
#endif

            NotSupportedMessage.SetActive(!_isSupported);
            CurrentUsedText.gameObject.SetActive(_isSupported);

            TriggerRefresh();
        }

        protected override void Update()
        {
            base.Update();

            if (SRDebug.Instance.IsDebugPanelVisible && (Time.realtimeSinceStartup - _lastRefresh > 1f))
            {
                TriggerRefresh();
                _lastRefresh = Time.realtimeSinceStartup;
            }
        }

        public void TriggerRefresh()
        {
            long max;
            long current;

#if UNITY_5_6_OR_NEWER
            max = _isSupported ? Profiler.GetMonoHeapSizeLong() : GC.GetTotalMemory(false);
            current = Profiler.GetMonoUsedSizeLong();
#else
            max = _isSupported ? Profiler.GetMonoHeapSize() : GC.GetTotalMemory(false);
            current = Profiler.GetMonoUsedSize();
#endif

            var maxMb = (max >> 10);
            maxMb /= 1024; // On new line to workaround IL2CPP bug

            var currentMb = (current >> 10);
            currentMb /= 1024;

            Slider.maxValue = maxMb;
            Slider.value = currentMb;

            TotalAllocatedText.text = "Total: <color=#FFFFFF>{0}</color>MB".Fmt(maxMb);

            if (currentMb > 0)
            {
                CurrentUsedText.text = "<color=#FFFFFF>{0}</color>MB".Fmt(currentMb);
            }
        }

        public void TriggerCollection()
        {
            GC.Collect();
            TriggerRefresh();
        }
    }
}
