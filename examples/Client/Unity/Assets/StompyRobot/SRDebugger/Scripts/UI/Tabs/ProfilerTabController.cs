//#define SR_CONSOLE_DEBUG

namespace SRDebugger.UI.Tabs
{
    using SRF;
    using UnityEngine.UI;

    public class ProfilerTabController : SRMonoBehaviourEx
    {
        private bool _isDirty;

        [RequiredField] public Toggle PinToggle;

        protected override void Start()
        {
            base.Start();

            PinToggle.onValueChanged.AddListener(PinToggleValueChanged);
            Refresh();
        }

        private void PinToggleValueChanged(bool isOn)
        {
            SRDebug.Instance.IsProfilerDocked = isOn;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _isDirty = true;
        }

        protected override void Update()
        {
            base.Update();

            if (_isDirty)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            PinToggle.isOn = SRDebug.Instance.IsProfilerDocked;
            _isDirty = false;
        }
    }
}
