namespace SRDebugger.Services.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Internal;
    using SRF;
    using SRF.Service;
    using UI.Controls;
    using UI.Other;
    using UnityEngine;

    [Service(typeof (IPinnedUIService))]
    public class PinnedUIServiceImpl : SRServiceBase<IPinnedUIService>, IPinnedUIService
    {
        private readonly List<OptionsControlBase> _controlList = new List<OptionsControlBase>();

        private readonly Dictionary<OptionDefinition, OptionsControlBase> _pinnedObjects =
            new Dictionary<OptionDefinition, OptionsControlBase>();

        private bool _queueRefresh;
        private PinnedUIRoot _uiRoot;

        public DockConsoleController DockConsoleController
        {
            get
            {
                if (_uiRoot == null)
                {
                    Load();
                }
                return _uiRoot.DockConsoleController;
            }
        }

        public event Action<OptionDefinition, bool> OptionPinStateChanged;
        public event Action<RectTransform> OptionsCanvasCreated;

        public bool IsProfilerPinned
        {
            get
            {
                if (_uiRoot == null)
                {
                    return false;
                }
                return _uiRoot.Profiler.activeSelf;
            }
            set
            {
                if (_uiRoot == null)
                {
                    Load();
                }
                _uiRoot.Profiler.SetActive(value);
            }
        }

        public void Pin(OptionDefinition obj, int order = -1)
        {
            if (_uiRoot == null)
            {
                Load();
            }

            if (_pinnedObjects.ContainsKey(obj))
            {
                return;
            }

            var control = OptionControlFactory.CreateControl(obj);

            control.CachedTransform.SetParent(_uiRoot.Container, false);

            if (order >= 0)
            {
                control.CachedTransform.SetSiblingIndex(order);
            }

            _pinnedObjects.Add(obj, control);
            _controlList.Add(control);

            OnPinnedStateChanged(obj, true);
        }

        public void Unpin(OptionDefinition obj)
        {
            if (!_pinnedObjects.ContainsKey(obj))
            {
                return;
            }

            var control = _pinnedObjects[obj];

            _pinnedObjects.Remove(obj);
            _controlList.Remove(control);

            Destroy(control.CachedGameObject);

            OnPinnedStateChanged(obj, false);
        }

        private void OnPinnedStateChanged(OptionDefinition option, bool isPinned)
        {
            if (OptionPinStateChanged != null)
            {
                OptionPinStateChanged(option, isPinned);
            }
        }

        public void UnpinAll()
        {
            foreach (var op in _pinnedObjects)
            {
                Destroy(op.Value.CachedGameObject);
            }

            _pinnedObjects.Clear();
            _controlList.Clear();
        }

        public bool HasPinned(OptionDefinition option)
        {
            return _pinnedObjects.ContainsKey(option);
        }

        protected override void Awake()
        {
            base.Awake();

            CachedTransform.SetParent(Hierarchy.Get("SRDebugger"));
        }

        private void Load()
        {
            var prefab = Resources.Load<PinnedUIRoot>(SRDebugPaths.PinnedUIPrefabPath);

            if (prefab == null)
            {
                Debug.LogError("[SRDebugger.PinnedUI] Error loading ui prefab");
                return;
            }

            var instance = SRInstantiate.Instantiate(prefab);
            instance.CachedTransform.SetParent(CachedTransform, false);

            _uiRoot = instance;
            UpdateAnchors();
            SRDebug.Instance.PanelVisibilityChanged += OnDebugPanelVisibilityChanged;

            Service.Options.OptionsUpdated += OnOptionsUpdated;
            Service.Options.OptionsValueUpdated += OptionsOnPropertyChanged;

            if (OptionsCanvasCreated != null)
            {
                OptionsCanvasCreated(_uiRoot.Canvas.GetComponent<RectTransform>());
            }
        }

        private void UpdateAnchors()
        {
            // Setup alignment of Profiler/Options splitter
            switch (Settings.Instance.ProfilerAlignment)
            {
                case PinAlignment.BottomLeft:
                case PinAlignment.TopLeft:
                case PinAlignment.CenterLeft:
                    _uiRoot.Profiler.transform.SetSiblingIndex(0);
                    break;

                case PinAlignment.BottomRight:
                case PinAlignment.TopRight:
                case PinAlignment.CenterRight:
                    _uiRoot.Profiler.transform.SetSiblingIndex(1);
                    break;
            }

            // Setup alignment of Profiler vertical layout group
            switch (Settings.Instance.ProfilerAlignment)
            {
                case PinAlignment.TopRight:
                case PinAlignment.TopLeft:
                    _uiRoot.ProfilerVerticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                    break;

                case PinAlignment.BottomRight:
                case PinAlignment.BottomLeft:
                    _uiRoot.ProfilerVerticalLayoutGroup.childAlignment = TextAnchor.LowerCenter;
                    break;

                case PinAlignment.CenterLeft:
                case PinAlignment.CenterRight:
                    _uiRoot.ProfilerVerticalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                    break;
            }

            _uiRoot.ProfilerHandleManager.SetAlignment(Settings.Instance.ProfilerAlignment);

            // Setup alignment of options flow layout group
            switch (Settings.Instance.OptionsAlignment)
            {
                case PinAlignment.BottomLeft: // OptionsBottomLeft
                    _uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.LowerLeft;
                    break;
                case PinAlignment.TopLeft:
                    _uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.UpperLeft;
                    break;
                case PinAlignment.BottomRight:
                    _uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.LowerRight;
                    break;
                case PinAlignment.TopRight:
                    _uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.UpperRight;
                    break;
                case PinAlignment.BottomCenter:
                    _uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.LowerCenter;
                    break;
                case PinAlignment.TopCenter:
                    _uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                    break;
                case PinAlignment.CenterLeft:
                    _uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                    break;
                case PinAlignment.CenterRight:
                    _uiRoot.OptionsLayoutGroup.childAlignment = TextAnchor.MiddleRight;
                    break;
            }
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

        private void OnOptionsUpdated(object sender, EventArgs eventArgs)
        {
            // Check for removed options.
            var pinned = _pinnedObjects.Keys.ToList();

            foreach (var op in pinned)
            {
                if (!Service.Options.Options.Contains(op))
                {
                    Unpin(op);
                }
            }
        }

        private void OptionsOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            _queueRefresh = true;
        }

        private void OnDebugPanelVisibilityChanged(bool isVisible)
        {
            // Refresh bindings when debug panel is no longer visible
            if (!isVisible)
            {
                _queueRefresh = true;
            }
        }

        private void Refresh()
        {
            for (var i = 0; i < _controlList.Count; i++)
            {
                _controlList[i].Refresh();
            }
        }
    }
}
