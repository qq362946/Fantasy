namespace SRDebugger.UI.Controls
{
    using Internal;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;
#if UNITY_5_5_OR_NEWER
    using UnityEngine.Profiling;
#endif

    public class ProfilerEnableControl : SRMonoBehaviourEx
    {
        private bool _previousState;
        [RequiredField] public Text ButtonText;
        [RequiredField] public UnityEngine.UI.Button EnableButton;
        [RequiredField] public Text Text;

        protected override void Start()
        {
            base.Start();

            if (!Profiler.supported)
            {
                Text.text = SRDebugStrings.Current.Profiler_NotSupported;
                EnableButton.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            if (!Application.HasProLicense())
            {
                Text.text = SRDebugStrings.Current.Profiler_NoProInfo;
                EnableButton.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            UpdateLabels();
        }

        protected void UpdateLabels()
        {
            if (!Profiler.enabled)
            {
                Text.text = SRDebugStrings.Current.Profiler_EnableProfilerInfo;
                ButtonText.text = "Enable";
            }
            else
            {
                Text.text = SRDebugStrings.Current.Profiler_DisableProfilerInfo;
                ButtonText.text = "Disable";
            }

            _previousState = Profiler.enabled;
        }

        protected override void Update()
        {
            base.Update();

            if (Profiler.enabled != _previousState)
            {
                UpdateLabels();
            }
        }

        public void ToggleProfiler()
        {
            Debug.Log("Toggle Profiler");
            Profiler.enabled = !Profiler.enabled;
        }
    }
}
