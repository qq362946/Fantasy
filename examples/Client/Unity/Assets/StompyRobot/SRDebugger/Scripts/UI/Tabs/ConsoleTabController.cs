//#define SR_CONSOLE_DEBUG

using System.Collections;

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

        private static bool _hasWarnedAboutLogHandler;
        private static bool _hasWarnedAboutLoggingDisabled;

        [Import]
        public IConsoleFilterState FilterState;

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
        public GameObject CopyToClipboardContainer;

        [RequiredField]
        public GameObject CopyToClipboardButton;

        [RequiredField]
        public GameObject CopyToClipboardMessage;

        [RequiredField]
        public CanvasGroup CopyToClipboardMessageCanvasGroup;

        [RequiredField]
        public GameObject LoggingIsDisabledCanvasGroup;

        [RequiredField]
        public GameObject LogHandlerHasBeenOverridenGroup;

        [RequiredField]
        public Toggle FilterToggle;
        [RequiredField]
        public InputField FilterField;
        [RequiredField]
        public GameObject FilterBarContainer;

        private ConsoleEntry _selectedItem;

        private Coroutine _fadeButtonCoroutine;

        protected override void Start()
        {
            base.Start();

            _consoleCanvas = GetComponent<Canvas>();
            
            ToggleErrors.isOn = FilterState.GetConsoleFilterState(LogType.Error);
            ToggleWarnings.isOn = FilterState.GetConsoleFilterState(LogType.Warning);
            ToggleInfo.isOn = FilterState.GetConsoleFilterState(LogType.Log);
            
            ToggleErrors.onValueChanged.AddListener(isOn =>
            {
                FilterState.SetConsoleFilterState(LogType.Error, isOn);
                _isDirty = true;
            });

            ToggleWarnings.onValueChanged.AddListener(isOn =>
            {
                FilterState.SetConsoleFilterState(LogType.Warning, isOn);
                _isDirty = true;
            });

            ToggleInfo.onValueChanged.AddListener(isOn =>
            {
                FilterState.SetConsoleFilterState(LogType.Log, isOn);
                _isDirty = true;
            });

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
            FilterState.FilterStateChange += OnFilterStateChange;

            StackTraceText.supportRichText = Settings.Instance.RichTextInConsole;
            PopulateStackTraceArea(null);

            Refresh();
        }

        private void OnFilterStateChange(LogType logtype, bool newstate)
        {
            switch (logtype)
            {
                case LogType.Error:
                    ToggleErrors.isOn = newstate;
                    break;
                case LogType.Warning:
                    ToggleWarnings.isOn = newstate;
                    break;
                case LogType.Log:
                    ToggleInfo.isOn = newstate;
                    break;
            }
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
                StopAnimations();
            }
        }

        private void PinToggleValueChanged(bool isOn)
        {
            Service.DockConsole.IsVisible = isOn;
        }

        protected override void OnDestroy()
        {
            StopAnimations();

            if (Service.Console != null)
            {
                Service.Console.Updated -= ConsoleOnUpdated;
            }

            FilterState.FilterStateChange -= OnFilterStateChange;

            
            base.OnDestroy();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _isDirty = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAnimations();
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
                SetCopyToClipboardButtonState(CopyToClipboardStates.Hidden);
                StackTraceText.text = "";
            }
            else
            {
                if (SRDebug.CopyConsoleItemCallback != null)
                {
                    SetCopyToClipboardButtonState(CopyToClipboardStates.Visible);
                }

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
            _selectedItem = entry;
        }

        public void CopyToClipboard()
        {
            if (_selectedItem != null)
            {
                SetCopyToClipboardButtonState(CopyToClipboardStates.Activated);
                if (SRDebug.CopyConsoleItemCallback != null)
                {
                    SRDebug.CopyConsoleItemCallback(_selectedItem);
                }
                else
                {
                    Debug.LogError("[SRDebugger] Copy to clipboard is not available.");
                }
            }
        }

        public enum CopyToClipboardStates
        {
            Hidden,
            Visible,
            Activated
        }

        void SetCopyToClipboardButtonState(CopyToClipboardStates state)
        {
            StopAnimations();

            switch (state)
            {
                case CopyToClipboardStates.Hidden:
                    CopyToClipboardContainer.SetActive(false);
                    CopyToClipboardButton.SetActive(false);
                    CopyToClipboardMessage.SetActive(false);
                    break;
                case CopyToClipboardStates.Visible:
                    CopyToClipboardContainer.SetActive(true);
                    CopyToClipboardButton.SetActive(true);
                    CopyToClipboardMessage.SetActive(false);
                    break;
                case CopyToClipboardStates.Activated:
                    CopyToClipboardMessageCanvasGroup.alpha = 1;
                    CopyToClipboardContainer.SetActive(true);
                    CopyToClipboardButton.SetActive(false);
                    CopyToClipboardMessage.SetActive(true);

                    _fadeButtonCoroutine = StartCoroutine(FadeCopyButton());
                    break;
                default:
                    throw new ArgumentOutOfRangeException("state", state, null);
            }
        }

        IEnumerator FadeCopyButton()
        {
            yield return new WaitForSecondsRealtime(2f);

            float startTime = Time.realtimeSinceStartup;
            float endTime = Time.realtimeSinceStartup + 1f;

            while (Time.realtimeSinceStartup < endTime)
            {
                float currentAlpha = Mathf.InverseLerp(endTime, startTime, Time.realtimeSinceStartup);
                CopyToClipboardMessageCanvasGroup.alpha = currentAlpha;
                yield return new WaitForEndOfFrame();
            }

            CopyToClipboardMessageCanvasGroup.alpha = 0;
            _fadeButtonCoroutine = null;
        }

        void StopAnimations()
        {
            if (_fadeButtonCoroutine != null)
            {
                StopCoroutine(_fadeButtonCoroutine);
                _fadeButtonCoroutine = null;
                CopyToClipboardMessageCanvasGroup.alpha = 0;
            }
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

            if (!_hasWarnedAboutLogHandler && Service.Console.LogHandlerIsOverriden)
            {
                LogHandlerHasBeenOverridenGroup.SetActive(true);
                _hasWarnedAboutLogHandler = true;
            }

            if (!_hasWarnedAboutLoggingDisabled && !Service.Console.LoggingEnabled)
            {
                LoggingIsDisabledCanvasGroup.SetActive(true);
            }
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

        public void LogHandlerHasBeenOverridenOkayButtonPress()
        {
            _hasWarnedAboutLogHandler = true;
            LogHandlerHasBeenOverridenGroup.SetActive(false);
        }

        public void LoggingDisableCloseAndIgnorePressed()
        {
            LoggingIsDisabledCanvasGroup.SetActive(false);
            _hasWarnedAboutLoggingDisabled = true;
        }       
        
        public void LoggingDisableReenablePressed()
        {
            Service.Console.LoggingEnabled = true;
            LoggingIsDisabledCanvasGroup.SetActive(false);

            Debug.Log("[SRDebugger] Re-enabled logging.");
        }
    }
}
