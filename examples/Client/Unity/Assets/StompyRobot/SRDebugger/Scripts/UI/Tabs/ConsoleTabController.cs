//#define SR_CONSOLE_DEBUG

namespace SRDebugger.UI.Tabs
{
    using System;
    using Controls;
    using Internal;
    using Services;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class ConsoleTabController : SRMonoBehaviourEx
    {
        private const int MaxLength = 2600;

        private Canvas _consoleCanvas;
        private bool _isDirty;

        [RequiredField]
        public ConsoleLogControl ConsoleLogControl;

        [RequiredField]
        public Toggle PinToggle;
        //public bool IsListening = true;

        [RequiredField]
        public ScrollRect StackTraceScrollRect;
        [RequiredField]
        public Text StackTraceText;
        [RequiredField]
        public Toggle ToggleErrors;
        [RequiredField]
        public Text ToggleErrorsText;
        [RequiredField]
        public Toggle ToggleInfo;
        [RequiredField]
        public Text ToggleInfoText;
        [RequiredField]
        public Toggle ToggleWarnings;
        [RequiredField]
        public Text ToggleWarningsText;

        [RequiredField]
        public Toggle FilterToggle;
        [RequiredField]
        public InputField FilterField;
        [RequiredField]
        public GameObject FilterBarContainer;

        protected override void Start()
        {
            base.Start();

            _consoleCanvas = GetComponent<Canvas>();

            ToggleErrors.onValueChanged.AddListener(isOn => _isDirty = true);
            ToggleWarnings.onValueChanged.AddListener(isOn => _isDirty = true);
            ToggleInfo.onValueChanged.AddListener(isOn => _isDirty = true);

            PinToggle.onValueChanged.AddListener(PinToggleValueChanged);

            FilterToggle.onValueChanged.AddListener(FilterToggleValueChanged);
            FilterBarContainer.SetActive(FilterToggle.isOn);

#if UNITY_5_3_OR_NEWER
            FilterField.onValueChanged.AddListener(FilterValueChanged);
#else
            FilterField.onValueChange.AddListener(FilterValueChanged);
#endif

            ConsoleLogControl.SelectedItemChanged = ConsoleLogSelectedItemChanged;

            Service.Console.Updated += ConsoleOnUpdated;
            Service.Panel.VisibilityChanged += PanelOnVisibilityChanged;

            StackTraceText.supportRichText = Settings.Instance.RichTextInConsole;
            PopulateStackTraceArea(null);

            Refresh();
        }


        private void FilterToggleValueChanged(bool isOn)
        {
            if (isOn)
            {
                FilterBarContainer.SetActive(true);
                ConsoleLogControl.Filter = FilterField.text;
            }
            else
            {
                ConsoleLogControl.Filter = null;
                FilterBarContainer.SetActive(false);
            }
        }
        private void FilterValueChanged(string filterText)
        {
            if (FilterToggle.isOn && !string.IsNullOrEmpty(filterText) && filterText.Trim().Length != 0)
            {
                ConsoleLogControl.Filter = filterText;
            }
            else
            {
                ConsoleLogControl.Filter = null;
            }
        }

        private void PanelOnVisibilityChanged(IDebugPanelService debugPanelService, bool b)
        {
            if (_consoleCanvas == null)
            {
                return;
            }

            if (b)
            {
                _consoleCanvas.enabled = true;
            }
            else
            {
                _consoleCanvas.enabled = false;
            }
        }

        private void PinToggleValueChanged(bool isOn)
        {
            Service.DockConsole.IsVisible = isOn;
        }

        protected override void OnDestroy()
        {
            if (Service.Console != null)
            {
                Service.Console.Updated -= ConsoleOnUpdated;
            }

            base.OnDestroy();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _isDirty = true;
        }

        private void ConsoleLogSelectedItemChanged(object item)
        {
            var log = item as ConsoleEntry;
            PopulateStackTraceArea(log);
        }

        protected override void Update()
        {
            base.Update();

            if (_isDirty)
            {
                Refresh();
            }
        }

        private void PopulateStackTraceArea(ConsoleEntry entry)
        {
            if (entry == null)
            {
                StackTraceText.text = "";
            }
            else
            {
                var text = entry.Message + Environment.NewLine +
                           (!string.IsNullOrEmpty(entry.StackTrace)
                               ? entry.StackTrace
                               : SRDebugStrings.Current.Console_NoStackTrace);

                if (text.Length > MaxLength)
                {
                    text = text.Substring(0, MaxLength);
                    text += "\n" + SRDebugStrings.Current.Console_MessageTruncated;
                }

                StackTraceText.text = text;
            }

            StackTraceScrollRect.normalizedPosition = new Vector2(0, 1);
        }

        private void Refresh()
        {
            // Update total counts labels
            ToggleInfoText.text = SRDebuggerUtil.GetNumberString(Service.Console.InfoCount, 999, "999+");
            ToggleWarningsText.text = SRDebuggerUtil.GetNumberString(Service.Console.WarningCount, 999, "999+");
            ToggleErrorsText.text = SRDebuggerUtil.GetNumberString(Service.Console.ErrorCount, 999, "999+");

            ConsoleLogControl.ShowErrors = ToggleErrors.isOn;
            ConsoleLogControl.ShowWarnings = ToggleWarnings.isOn;
            ConsoleLogControl.ShowInfo = ToggleInfo.isOn;

            PinToggle.isOn = Service.DockConsole.IsVisible;

            _isDirty = false;
        }

        private void ConsoleOnUpdated(IConsoleService console)
        {
            _isDirty = true;
        }

        public void Clear()
        {
            Service.Console.Clear();
            _isDirty = true;
        }
    }
}
