namespace SRDebugger.UI.Other
{
    using Controls;
    using Internal;
    using Services;
    using SRF;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class DockConsoleController : SRMonoBehaviourEx, IPointerEnterHandler, IPointerExitHandler
    {
        public const float NonFocusOpacity = 0.65f;
        private bool _isDirty;
        private bool _isDragging;
        private int _pointersOver;

        [Import]
        public IConsoleFilterState FilterState;

        [RequiredField] public GameObject BottomHandle;

        [RequiredField] public CanvasGroup CanvasGroup;

        [RequiredField] public ConsoleLogControl Console;

        [RequiredField] public GameObject Dropdown;

        [RequiredField] public Image DropdownToggleSprite;

        [RequiredField] public Text TextErrors;

        [RequiredField] public Text TextInfo;

        [RequiredField] public Text TextWarnings;

        [RequiredField] public Toggle ToggleErrors;

        [RequiredField] public Toggle ToggleInfo;

        [RequiredField] public Toggle ToggleWarnings;

        [RequiredField] public GameObject TopBar;

        [RequiredField] public GameObject TopHandle;

        [RequiredField] public GameObject TopSafeAreaSpacer;
        [RequiredField] public GameObject BottomSafeAreaSpacer;

        public bool IsVisible
        {
            get { return CachedGameObject.activeSelf; }
            set { CachedGameObject.SetActive(value); }
        }

        protected override void Start()
        {
            base.Start();

            //_canvasScaler = Canvas.GetComponent<CanvasScaler>();
            Service.Console.Updated += ConsoleOnUpdated;

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

            FilterState.FilterStateChange += OnFilterStateChange;

            Refresh();
            RefreshAlpha();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (Service.Console != null)
            {
                Service.Console.Updated -= ConsoleOnUpdated;
            }

            FilterState.FilterStateChange -= OnFilterStateChange;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _pointersOver = 0;
            _isDragging = false;
            RefreshAlpha();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _pointersOver = 0;
        }

        protected override void Update()
        {
            base.Update();

            if (_isDirty)
            {
                Refresh();
            }
        }

        private void OnFilterStateChange(LogType logType, bool newState)
        {
            switch (logType)
            {
                case LogType.Error:
                    ToggleErrors.isOn = newState;
                    break;
                case LogType.Warning:
                    ToggleWarnings.isOn = newState;
                    break;
                case LogType.Log:
                    ToggleInfo.isOn = newState;
                    break;
            }
        }

        private void ConsoleOnUpdated(IConsoleService console)
        {
            _isDirty = true;
        }

        public void SetDropdownVisibility(bool visible)
        {
            Dropdown.SetActive(visible);
            DropdownToggleSprite.rectTransform.localRotation = Quaternion.Euler(0, 0, visible ? 0f : 180f);
        }

        public void SetAlignmentMode(ConsoleAlignment alignment)
        {
            switch (alignment)
            {
                case ConsoleAlignment.Top:
                {
                    TopBar.transform.SetSiblingIndex(0);
                    Dropdown.transform.SetSiblingIndex(2);
                    TopHandle.SetActive(false);
                    BottomHandle.SetActive(true);
                    transform.SetSiblingIndex(0);
                    DropdownToggleSprite.rectTransform.parent.localRotation = Quaternion.Euler(0, 0, 0f);
                    TopSafeAreaSpacer.SetActive(true);
                    BottomSafeAreaSpacer.SetActive(false);
                    break;
                }

                case ConsoleAlignment.Bottom:
                {
                    Dropdown.transform.SetSiblingIndex(0);
                    TopBar.transform.SetSiblingIndex(2);
                    TopHandle.SetActive(true);
                    BottomHandle.SetActive(false);
                    transform.SetSiblingIndex(1);
                    DropdownToggleSprite.rectTransform.parent.localRotation = Quaternion.Euler(0, 0, 180f);
                    TopSafeAreaSpacer.SetActive(false);
                    BottomSafeAreaSpacer.SetActive(true);
                    break;
                }
            }
        }

        private void Refresh()
        {
            // Update total counts labels
            TextInfo.text = SRDebuggerUtil.GetNumberString(Service.Console.InfoCount, 999, "999+");
            TextWarnings.text = SRDebuggerUtil.GetNumberString(Service.Console.WarningCount, 999, "999+");
            TextErrors.text = SRDebuggerUtil.GetNumberString(Service.Console.ErrorCount, 999, "999+");

            ToggleErrors.isOn = FilterState.GetConsoleFilterState(LogType.Error);
            ToggleWarnings.isOn = FilterState.GetConsoleFilterState(LogType.Warning);
            ToggleInfo.isOn = FilterState.GetConsoleFilterState(LogType.Log);

            _isDirty = false;
        }

        private void RefreshAlpha()
        {
            if (_isDragging || _pointersOver > 0)
            {
                CanvasGroup.alpha = 1.0f;
            }
            else
            {
                CanvasGroup.alpha = NonFocusOpacity;
            }
        }

        #region Event Callbacks

        public void ToggleDropdownVisible()
        {
            SetDropdownVisibility(!Dropdown.activeSelf);
        }

        public void MenuButtonPressed()
        {
            SRDebug.Instance.ShowDebugPanel(DefaultTabs.Console);
        }

        public void ClearButtonPressed()
        {
            Service.Console.Clear();
        }

        public void TogglesUpdated()
        {
            Console.ShowErrors = ToggleErrors.isOn;
            Console.ShowWarnings = ToggleWarnings.isOn;
            Console.ShowInfo = ToggleInfo.isOn;

            SetDropdownVisibility(true);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _pointersOver = 1;
            RefreshAlpha();
        }

        public void OnPointerExit(PointerEventData e)
        {
            _pointersOver = 0; //Mathf.Max(0, _pointersOver - 1);
            RefreshAlpha();
        }

        public void OnBeginDrag()
        {
            _isDragging = true;
            RefreshAlpha();
        }

        public void OnEndDrag()
        {
            _isDragging = false;
            _pointersOver = 0;
            RefreshAlpha();
        }

        #endregion
    }
}
