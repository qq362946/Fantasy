namespace SRDebugger.Services.Implementation
{
    using System.Collections.Generic;
    using Internal;
    using SRF;
    using SRF.Service;
    using UnityEngine;

    [Service(typeof (KeyboardShortcutListenerService))]
    public class KeyboardShortcutListenerService : SRServiceBase<KeyboardShortcutListenerService>
    {
        private List<Settings.KeyboardShortcut> _shortcuts;

        protected override void Awake()
        {
            base.Awake();

            CachedTransform.SetParent(Hierarchy.Get("SRDebugger"));

            _shortcuts = new List<Settings.KeyboardShortcut>(Settings.Instance.KeyboardShortcuts);
        }

        private void ToggleTab(DefaultTabs t)
        {
            var activeTab = Service.Panel.ActiveTab;

            if (Service.Panel.IsVisible && activeTab.HasValue && activeTab.Value == t)
            {
                SRDebug.Instance.HideDebugPanel();
            }
            else
            {
                SRDebug.Instance.ShowDebugPanel(t);
            }
        }

        private void ExecuteShortcut(Settings.KeyboardShortcut shortcut)
        {
            switch (shortcut.Action)
            {
                case Settings.ShortcutActions.OpenSystemInfoTab:

                    ToggleTab(DefaultTabs.SystemInformation);

                    break;

                case Settings.ShortcutActions.OpenConsoleTab:

                    ToggleTab(DefaultTabs.Console);

                    break;

                case Settings.ShortcutActions.OpenOptionsTab:

                    ToggleTab(DefaultTabs.Options);

                    break;

                case Settings.ShortcutActions.OpenProfilerTab:

                    ToggleTab(DefaultTabs.Profiler);

                    break;

                case Settings.ShortcutActions.OpenBugReporterTab:

                    ToggleTab(DefaultTabs.BugReporter);

                    break;

                case Settings.ShortcutActions.ClosePanel:

                    SRDebug.Instance.HideDebugPanel();

                    break;

                case Settings.ShortcutActions.OpenPanel:

                    SRDebug.Instance.ShowDebugPanel();

                    break;

                case Settings.ShortcutActions.TogglePanel:

                    if (SRDebug.Instance.IsDebugPanelVisible)
                    {
                        SRDebug.Instance.HideDebugPanel();
                    }
                    else
                    {
                        SRDebug.Instance.ShowDebugPanel();
                    }

                    break;

                case Settings.ShortcutActions.ShowBugReportPopover:

                    SRDebug.Instance.ShowBugReportSheet();

                    break;

                case Settings.ShortcutActions.ToggleDockedConsole:

                    SRDebug.Instance.DockConsole.IsVisible = !SRDebug.Instance.DockConsole.IsVisible;

                    break;

                case Settings.ShortcutActions.ToggleDockedProfiler:

                    SRDebug.Instance.IsProfilerDocked = !SRDebug.Instance.IsProfilerDocked;

                    break;

                default:

                    Debug.LogWarning("[SRDebugger] Unhandled keyboard shortcut: " + shortcut.Action);

                    break;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (Settings.Instance.KeyboardEscapeClose && Input.GetKeyDown(KeyCode.Escape) && Service.Panel.IsVisible)
            {
                SRDebug.Instance.HideDebugPanel();
            }

            var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            for (var i = 0; i < _shortcuts.Count; i++)
            {
                var s = _shortcuts[i];

                if (s.Control && !ctrl)
                {
                    continue;
                }

                if (s.Shift && !shift)
                {
                    continue;
                }

                if (s.Alt && !alt)
                {
                    continue;
                }

                if (Input.GetKeyDown(s.Key))
                {
                    ExecuteShortcut(s);
                    break;
                }
            }
        }
    }
}
