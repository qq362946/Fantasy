
#pragma warning disable 169
#pragma warning disable 649

namespace SRDebugger.UI.Controls
{
    using System;
    using System.Collections;
    using Internal;
    using Services;
    using SRF;
    using SRF.UI.Layout;
    using UnityEngine;
    using UnityEngine.UI;

    public class ConsoleLogControl : SRMonoBehaviourEx
    {
        [RequiredField] [SerializeField] private VirtualVerticalLayoutGroup _consoleScrollLayoutGroup;

        [RequiredField] [SerializeField] private ScrollRect _consoleScrollRect;

        private bool _isDirty;
        private Vector2? _scrollPosition;
        private bool _showErrors = true;
        private bool _showInfo = true;
        private bool _showWarnings = true;
        public Action<ConsoleEntry> SelectedItemChanged;
        private string _filter;

        public bool ShowErrors
        {
            get { return _showErrors; }
            set
            {
                _showErrors = value;
                SetIsDirty();
            }
        }

        public bool ShowWarnings
        {
            get { return _showWarnings; }
            set
            {
                _showWarnings = value;
                SetIsDirty();
            }
        }

        public bool ShowInfo
        {
            get { return _showInfo; }
            set
            {
                _showInfo = value;
                SetIsDirty();
            }
        }

        public bool EnableSelection
        {
            get { return _consoleScrollLayoutGroup.EnableSelection; }
            set { _consoleScrollLayoutGroup.EnableSelection = value; }
        }

        public string Filter
        {
            get { return _filter; }
            set {
                if (_filter != value)
                {
                    _filter = value;
                    _isDirty = true;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();

            _consoleScrollLayoutGroup.SelectedItemChanged.AddListener(OnSelectedItemChanged);
            Service.Console.Updated += ConsoleOnUpdated;
        }

        protected override void Start()
        {
            base.Start();
            SetIsDirty();
            StartCoroutine(ScrollToBottom());
        }

        IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            _scrollPosition = new Vector2(0,0);
        }

        protected override void OnDestroy()
        {
            if (Service.Console != null)
            {
                Service.Console.Updated -= ConsoleOnUpdated;
            }

            base.OnDestroy();
        }

        private void OnSelectedItemChanged(object arg0)
        {
            var entry = arg0 as ConsoleEntry;

            if (SelectedItemChanged != null)
            {
                SelectedItemChanged(entry);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (_scrollPosition.HasValue)
            {
                _consoleScrollRect.normalizedPosition = _scrollPosition.Value;
                _scrollPosition = null;
            }

            if (_isDirty)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_consoleScrollRect.normalizedPosition.y < 0.01f)
            {
                _scrollPosition = _consoleScrollRect.normalizedPosition;
            }

            _consoleScrollLayoutGroup.ClearItems();

            var entries = Service.Console.Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];

                if ((e.LogType == LogType.Error || e.LogType == LogType.Exception || e.LogType == LogType.Assert) &&
                    !ShowErrors)
                {
                    if (e == _consoleScrollLayoutGroup.SelectedItem) _consoleScrollLayoutGroup.SelectedItem = null;
                    continue;
                }

                if (e.LogType == LogType.Warning && !ShowWarnings)
                {
                    if (e == _consoleScrollLayoutGroup.SelectedItem) _consoleScrollLayoutGroup.SelectedItem = null;
                    continue;
                }

                if (e.LogType == LogType.Log && !ShowInfo)
                {
                    if (e == _consoleScrollLayoutGroup.SelectedItem) _consoleScrollLayoutGroup.SelectedItem = null;
                    continue;
                }

                if (!string.IsNullOrEmpty(Filter))
                {
                    if (e.Message.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        if (e == _consoleScrollLayoutGroup.SelectedItem) _consoleScrollLayoutGroup.SelectedItem = null;
                        continue;
                    }
                }

                _consoleScrollLayoutGroup.AddItem(e);
            }

            _isDirty = false;
        }

        private void SetIsDirty()
        {
            _isDirty = true;
        }

        private void ConsoleOnUpdated(IConsoleService console)
        {
            SetIsDirty();
        }
    }
}
