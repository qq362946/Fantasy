namespace SRDebugger.UI.Tabs
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Controls;
    using Controls.Data;
    using Internal;
    using Other;
    using Services;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class OptionsTabController : SRMonoBehaviourEx
    {
        private class CategoryInstance
        {
            public CategoryGroup CategoryGroup { get; private set; }
            public readonly List<OptionsControlBase> Options = new List<OptionsControlBase>();

            public CategoryInstance(CategoryGroup group)
            {
                CategoryGroup = group;
            }
        }

        private readonly List<OptionsControlBase> _controls = new List<OptionsControlBase>();
        private readonly List<CategoryInstance> _categories = new List<CategoryInstance>();

        private readonly Dictionary<OptionDefinition, OptionsControlBase> _options =
            new Dictionary<OptionDefinition, OptionsControlBase>();

        private bool _queueRefresh;
        private bool _selectionModeEnabled;
        private Canvas _optionCanvas;

        [RequiredField] public ActionControl ActionControlPrefab;

        [RequiredField] public CategoryGroup CategoryGroupPrefab;

        [RequiredField] public RectTransform ContentContainer;

        [RequiredField] public GameObject NoOptionsNotice;

        [RequiredField] public Toggle PinButton;

        [RequiredField] public GameObject PinPromptSpacer;

        [RequiredField] public GameObject PinPromptText;


        protected override void Start()
        {
            base.Start();

            PinButton.onValueChanged.AddListener(SetSelectionModeEnabled);

            PinPromptText.SetActive(false);
            //PinPromptSpacer.SetActive(false);

            Populate();

            _optionCanvas = GetComponent<Canvas>();

            Service.Options.OptionsUpdated += OnOptionsUpdated;
            Service.Options.OptionsValueUpdated += OnOptionsValueChanged;
            Service.PinnedUI.OptionPinStateChanged += OnOptionPinnedStateChanged;
        }

        private void OnOptionPinnedStateChanged(OptionDefinition optionDefinition, bool isPinned)
        {
            if (_options.ContainsKey(optionDefinition))
            {
                _options[optionDefinition].IsSelected = isPinned;
            }
        }

        private void OnOptionsUpdated(object sender, EventArgs eventArgs)
        {
            Clear();
            Populate();
        }

        private void OnOptionsValueChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            _queueRefresh = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Service.Panel.VisibilityChanged += PanelOnVisibilityChanged;
        }

        protected override void OnDisable()
        {
            // Always end pinning mode when tabbing away
            SetSelectionModeEnabled(false);

            if (Service.Panel != null)
            {
                Service.Panel.VisibilityChanged -= PanelOnVisibilityChanged;
            }

            base.OnDisable();
        }

        protected override void Update()
        {
            base.Update();

            if (_queueRefresh)
            {
                _queueRefresh = false;
                Refresh();
            }
        }

        private void PanelOnVisibilityChanged(IDebugPanelService debugPanelService, bool b)
        {
            // Always end pinning mode when panel is closed
            if (!b)
            {
                SetSelectionModeEnabled(false);

                // Refresh bindings for all pinned controls
                Refresh();
            }
            else if (b && CachedGameObject.activeInHierarchy)
            {
                // If the panel is visible, and this tab is active (selected), refresh all the data bindings
                Refresh();
            }

            if (_optionCanvas != null)
            {
                _optionCanvas.enabled = b;
            }
        }

        public void SetSelectionModeEnabled(bool isEnabled)
        {
            if (_selectionModeEnabled == isEnabled)
            {
                return;
            }

            _selectionModeEnabled = isEnabled;

            PinButton.isOn = isEnabled;
            PinPromptText.SetActive(isEnabled);
            //PinPromptSpacer.SetActive(isEnabled);

            foreach (var kv in _options)
            {
                kv.Value.SelectionModeEnabled = isEnabled;

                // Set IsSelected if entering selection mode.
                if (isEnabled)
                {
                    kv.Value.IsSelected = Service.PinnedUI.HasPinned(kv.Key);
                }
            }

            foreach (var cat in _categories)
            {
                cat.CategoryGroup.SelectionModeEnabled = isEnabled;
            }

            RefreshCategorySelection();

            // Return if entering selection mode
            if (isEnabled)
            {
                return;
            }
        }

        private void Refresh()
        {
            for (var i = 0; i < _options.Count; i++)
            {
                _controls[i].Refresh();
                _controls[i].IsSelected = Service.PinnedUI.HasPinned(_controls[i].Option);
            }
        }

        private void CommitPinnedOptions()
        {
            foreach (var kv in _options)
            {
                var control = kv.Value;

                if (control.IsSelected && !Service.PinnedUI.HasPinned(kv.Key))
                {
                    Service.PinnedUI.Pin(kv.Key);
                }
                else if (!control.IsSelected && Service.PinnedUI.HasPinned(kv.Key))
                {
                    Service.PinnedUI.Unpin(kv.Key);
                }
            }
        }

        private bool _isTogglingCategory;

        private void RefreshCategorySelection()
        {
            _isTogglingCategory = true;

            foreach (var cat in _categories)
            {
                var allSelected = true;

                for (var i = 0; i < cat.Options.Count; i++)
                {
                    if (!cat.Options[i].IsSelected)
                    {
                        allSelected = false;
                        break;
                    }
                }

                cat.CategoryGroup.IsSelected = allSelected;
            }

            _isTogglingCategory = false;
        }

        private void OnOptionSelectionToggle(bool selected)
        {
            if (!_isTogglingCategory)
            {
                RefreshCategorySelection();
                CommitPinnedOptions();
            }
        }

        /// <summary>
        /// When a category mode selection is changed.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="selected"></param>
        private void OnCategorySelectionToggle(CategoryInstance category, bool selected)
        {
            _isTogglingCategory = true;

            for (var i = 0; i < category.Options.Count; i++)
            {
                category.Options[i].IsSelected = selected;
            }

            _isTogglingCategory = false;

            CommitPinnedOptions();
        }

        #region Initialisation

        protected void Populate()
        {
            var sortedOptions = new Dictionary<string, List<OptionDefinition>>();

            foreach (var option in Service.Options.Options)
            {
                // Find a properly list for that category, or create a new one
                List<OptionDefinition> memberList;

                if (!sortedOptions.TryGetValue(option.Category, out memberList))
                {
                    memberList = new List<OptionDefinition>();
                    sortedOptions.Add(option.Category, memberList);
                }

                memberList.Add(option);
            }

            var hasCreated = false;

            foreach (var kv in sortedOptions)
            {
                if (kv.Value.Count == 0)
                {
                    continue;
                }

                hasCreated = true;
                CreateCategory(kv.Key, kv.Value);
            }

            if (hasCreated)
            {
                NoOptionsNotice.SetActive(false);
            }
        }

        protected void CreateCategory(string title, List<OptionDefinition> options)
        {
            options.Sort((d1, d2) => d1.SortPriority.CompareTo(d2.SortPriority));

            var groupInstance = SRInstantiate.Instantiate(CategoryGroupPrefab);
            var categoryInstance = new CategoryInstance(groupInstance);

            _categories.Add(categoryInstance);

            groupInstance.CachedTransform.SetParent(ContentContainer, false);
            groupInstance.Header.text = title;
            groupInstance.SelectionModeEnabled = false;

            categoryInstance.CategoryGroup.SelectionToggle.onValueChanged.AddListener(
                b => OnCategorySelectionToggle(categoryInstance, b));

            foreach (var option in options)
            {
                var control = OptionControlFactory.CreateControl(option, title);

                if (control == null)
                {
                    Debug.LogError("[SRDebugger.OptionsTab] Failed to create option control for {0}".Fmt(option.Name));
                    continue;
                }

                categoryInstance.Options.Add(control);
                control.CachedTransform.SetParent(groupInstance.Container, false);
                control.IsSelected = Service.PinnedUI.HasPinned(option);
                control.SelectionModeEnabled = false;
                control.SelectionModeToggle.onValueChanged.AddListener(OnOptionSelectionToggle);

                _options.Add(option, control);
                _controls.Add(control);
            }
        }

        void Clear()
        {
            foreach (var categoryInstance in _categories)
            {
                Destroy(categoryInstance.CategoryGroup.gameObject);
            }

            _categories.Clear();
            _controls.Clear();
            _options.Clear();
        }

        #endregion
    }
}
