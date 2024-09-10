namespace SRDebugger.Services.Implementation
{
    using Internal;
    using SRF.Service;
    using UI.Other;
    using UnityEngine;

    [Service(typeof (IDockConsoleService))]
    public class DockConsoleServiceImpl : IDockConsoleService
    {
        private ConsoleAlignment _alignment;
        private DockConsoleController _consoleRoot;
        private bool _didSuspendTrigger;
        private bool _isExpanded = true;
        private bool _isVisible;

        public DockConsoleServiceImpl()
        {
            _alignment = Settings.Instance.ConsoleAlignment;
        }

        public bool IsVisible
        {
            get { return _isVisible; }

            set
            {
                if (value == _isVisible)
                {
                    return;
                }

                _isVisible = value;

                if (_consoleRoot == null && value)
                {
                    Load();
                }
                else
                {
                    _consoleRoot.CachedGameObject.SetActive(value);
                }

                CheckTrigger();
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }

            set
            {
                if (value == _isExpanded)
                {
                    return;
                }

                _isExpanded = value;

                if (_consoleRoot == null && value)
                {
                    Load();
                }
                else
                {
                    _consoleRoot.SetDropdownVisibility(value);
                }

                CheckTrigger();
            }
        }

        public ConsoleAlignment Alignment
        {
            get { return _alignment; }
            set
            {
                _alignment = value;

                if (_consoleRoot != null)
                {
                    _consoleRoot.SetAlignmentMode(value);
                }

                CheckTrigger();
            }
        }

        private void Load()
        {
            var dockService = SRServiceManager.GetService<IPinnedUIService>();

            if (dockService == null)
            {
                Debug.LogError("[DockConsoleService] PinnedUIService not found");
                return;
            }

            var pinService = dockService as PinnedUIServiceImpl;

            if (pinService == null)
            {
                Debug.LogError("[DockConsoleService] Expected IPinnedUIService to be PinnedUIServiceImpl");
                return;
            }

            _consoleRoot = pinService.DockConsoleController;

            _consoleRoot.SetDropdownVisibility(_isExpanded);
            _consoleRoot.IsVisible = _isVisible;
            _consoleRoot.SetAlignmentMode(_alignment);

            CheckTrigger();
        }

        private void CheckTrigger()
        {
            ConsoleAlignment? triggerAlignment = null;
            var pinAlignment = Service.Trigger.Position;

            if (pinAlignment == PinAlignment.TopLeft ||
                pinAlignment == PinAlignment.TopRight || pinAlignment == PinAlignment.TopCenter)
            {
                triggerAlignment = ConsoleAlignment.Top;
            } else if (pinAlignment == PinAlignment.BottomLeft ||
                       pinAlignment == PinAlignment.BottomRight ||
                       pinAlignment == PinAlignment.BottomCenter)
            {
                triggerAlignment = ConsoleAlignment.Bottom;
            }

            var shouldHide = triggerAlignment.HasValue &&  IsVisible && Alignment == triggerAlignment.Value;

            // Show trigger if we have hidden it, and we no longer need to hide it.
            if (_didSuspendTrigger && !shouldHide)
            {
                Service.Trigger.IsEnabled = true;
                _didSuspendTrigger = false;
            }
            else if (Service.Trigger.IsEnabled && shouldHide)
            {
                Service.Trigger.IsEnabled = false;
                _didSuspendTrigger = true;
            }
        }
    }
}
