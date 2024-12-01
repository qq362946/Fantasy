namespace SRDebugger.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
#if !DISABLE_SRDEBUGGER
    using SRDebugger.Internal;
#endif
    using SRF;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    class SRDebuggerSettingsWindow : EditorWindow
    {
        [MenuItem(SRDebugEditorPaths.SettingsMenuItemPath)]
        public static void Open()
        {
            GetWindowWithRect<SRDebuggerSettingsWindow>(new Rect(0, 0, 449, 520), true, "SRDebugger - Settings",
                    true);
        }

#if DISABLE_SRDEBUGGER

        private bool _isWorking;

        private void OnGUI()
        {
            SRDebugEditor.DrawDisabledWindowGui(ref _isWorking);
        }

#else

        private enum ProfilerAlignment
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3
        }

        private enum OptionsAlignment
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3,
            TopCenter = 6,
            BottomCenter = 7
        }

        private string _currentEntryCode;
        private bool _enableTabChange = true;
        private Tabs _selectedTab;
        private bool _showBugReportSignupForm;
        private string[] _tabs = Enum.GetNames(typeof (Tabs)).Select(s => s.Replace('_', ' ')).ToArray();

        [NonSerialized] private bool _hasError;
        [NonSerialized] private string _error;

        private bool _isAppearing = true;

        private void Reset()
        {
            if (_isAppearing)
            {
                return;
            }

            SRInternalEditorUtil.EditorSettings.ClearCache();
            _hasError = false;
        }

        private bool SettingsReady(out Settings settings)
        {
            if (!_hasError)
            {
                string message;
                SRInternalEditorUtil.SettingsResult result =
                    SRInternalEditorUtil.EditorSettings.TryGetOrCreate(out settings, out message);

                switch (result)
                {
                    case SRInternalEditorUtil.SettingsResult.Loaded:
                    {
                        // Perform on-load logic
                        _currentEntryCode = GetEntryCodeString(settings);

                        if (string.IsNullOrEmpty(settings.ApiKey))
                        {
                            _showBugReportSignupForm = true;
                        }

                        return true;
                    }

                    case SRInternalEditorUtil.SettingsResult.Cache:
                    {
                        return true;
                    }

                    case SRInternalEditorUtil.SettingsResult.Waiting:
                    {
                        EditorGUILayout.Space();
                        GUILayout.Label(message, SRInternalEditorUtil.Styles.ParagraphLabel);
                        return false;
                    }

                    case SRInternalEditorUtil.SettingsResult.Error:
                    {
                        _error = message;
                        _hasError = true;
                        break;
                    }
                }
            }

            // Display Error UI
            settings = null;

            EditorGUILayout.Space();
            GUILayout.Label("An error has occurred while loading settings.");

            EditorGUILayout.Space();

            GUILayout.Label("Message: ", EditorStyles.boldLabel);
            GUILayout.Label(_error, SRInternalEditorUtil.Styles.ParagraphLabel);

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Integrity Checker"))
            {
                SRIntegrityCheckWindow.Open();
            }

            if (GUILayout.Button("Retry"))
            {
                Reset();
                Repaint();
            }

            return false;
        }

        private void OnGUI()
        {
            _isAppearing = false;

            // Draw header area 
            SRInternalEditorUtil.BeginDrawBackground();
            SRInternalEditorUtil.DrawLogo(SRInternalEditorUtil.GetLogo());
            SRInternalEditorUtil.EndDrawBackground();

            // Draw header/content divider
            EditorGUILayout.BeginVertical(SRInternalEditorUtil.Styles.SettingsHeaderBoxStyle);
            EditorGUILayout.EndVertical();

            Settings settings;
            if (!SettingsReady(out settings))
            {
                return;
            }

            // Draw tab buttons
            var rect = EditorGUILayout.BeginVertical(GUI.skin.box);

            --rect.width;
            var height = 18;

            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(!_enableTabChange);

            for (var i = 0; i < _tabs.Length; ++i)
            {
                var xStart = Mathf.RoundToInt(i*rect.width/_tabs.Length);
                var xEnd = Mathf.RoundToInt((i + 1)*rect.width/_tabs.Length);

                var pos = new Rect(rect.x + xStart, rect.y, xEnd - xStart, height);

                if (GUI.Toggle(pos, (int) _selectedTab == i, new GUIContent(_tabs[i]), EditorStyles.toolbarButton))
                {
                    _selectedTab = (Tabs) i;
                }
            }

            GUILayoutUtility.GetRect(10f, height);

            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                _scrollPosition = Vector2.zero;
                GUIUtility.keyboardControl = 0;
            }

            // Draw selected tab

            switch (_selectedTab)
            {
                case Tabs.General:
                    DrawTabGeneral(settings);
                    break;

                case Tabs.Layout:
                    DrawTabLayout(settings);
                    break;

                case Tabs.Bug_Reporter:
                    DrawTabBugReporter(settings);
                    break;

                case Tabs.Shortcuts:
                    DrawTabShortcuts(settings);
                    break;

                case Tabs.Advanced:
                    DrawTabAdvanced(settings);
                    break;
            }

            EditorGUILayout.EndVertical();

            // Display rating prompt and link buttons

            EditorGUILayout.LabelField(SRDebugEditorStrings.Current.SettingsRateBoxContents, EditorStyles.miniLabel);

            SRInternalEditorUtil.DrawFooterLayout(position.width);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
            }
        }

        private enum Tabs
        {
            General,
            Layout,
            Bug_Reporter,
            Shortcuts,
            Advanced
        }

            #region Tabs

        private void DrawTabGeneral(Settings settings)
        {
            GUILayout.Label("Loading", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            if (GUILayout.Toggle(!settings.IsEnabled, "Disabled", SRInternalEditorUtil.Styles.RadioButton))
            {
                settings.IsEnabled = false;
            }

            GUILayout.Label("Do not load SRDebugger until a manual call to <i>SRDebug.Init()</i>.",
                SRInternalEditorUtil.Styles.RadioButtonDescription);

            var msg = "Automatic (recommended)";
            
            if (GUILayout.Toggle(settings.IsEnabled, msg,
                SRInternalEditorUtil.Styles.RadioButton))
            {
                settings.IsEnabled = true;
            }

            GUILayout.Label("SRDebugger loads automatically when your game starts.",
                SRInternalEditorUtil.Styles.RadioButtonDescription);

            EditorGUILayout.EndVertical();

            GUILayout.Label("Panel Access", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

            EditorGUILayout.HelpBox("Configure trigger location in the layout tab.", MessageType.None, true);

            settings.EnableTrigger =
                (Settings.TriggerEnableModes)
                    EditorGUILayout.EnumPopup(new GUIContent("Trigger Mode"),
                        settings.EnableTrigger);

            EditorGUI.BeginDisabledGroup(settings.EnableTrigger == Settings.TriggerEnableModes.Off);

            settings.TriggerBehaviour =
                (Settings.TriggerBehaviours)
                    EditorGUILayout.EnumPopup(new GUIContent("Trigger Behaviour"),
                        settings.TriggerBehaviour);

            settings.ErrorNotification =
                EditorGUILayout.Toggle(
                    new GUIContent("Error Notification",
                        "Display a notification on the panel trigger when an error is printed to the log."),
                    settings.ErrorNotification);
            
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            settings.DefaultTab =
                (DefaultTabs)
                    EditorGUILayout.EnumPopup(
                        new GUIContent("Default Tab", SRDebugEditorStrings.Current.SettingsDefaultTabTooltip),
                        settings.DefaultTab);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            settings.RequireCode = EditorGUILayout.Toggle(new GUIContent("Require Entry Code"),
                settings.RequireCode);

            EditorGUI.BeginDisabledGroup(!settings.RequireCode);

            settings.RequireEntryCodeEveryTime = EditorGUILayout.Toggle(new GUIContent("...Every Time", "Require the user to enter the PIN every time they access the debug panel."),
                settings.RequireEntryCodeEveryTime);

            EditorGUILayout.EndHorizontal();

            var newCode = EditorGUILayout.TextField("Entry Code", _currentEntryCode);

            if (newCode != _currentEntryCode)
            {
                // Strip out alpha numeric chars
                newCode = new string(newCode.Where(char.IsDigit).ToArray());

                // Max length = 4
                newCode = newCode.Substring(0, Mathf.Min(4, newCode.Length));

                if (newCode.Length == 4)
                {
                    UpdateEntryCode(settings, newCode);
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            settings.AutomaticallyShowCursor =
                EditorGUILayout.Toggle(
                    new GUIContent("Show Cursor",
                        "Automatically set the cursor to visible when the debug panel is opened, and revert when closed."),
                    settings.AutomaticallyShowCursor);

            // Expand content area to fit all available space
            GUILayout.FlexibleSpace();
        }

        private void DrawTabLayout(Settings settings)
        {
            GUILayout.Label("Pinned Tool Positions", SRInternalEditorUtil.Styles.HeaderLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var rect = GUILayoutUtility.GetRect(360, 210);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            SRInternalEditorUtil.DrawLayoutPreview(rect, settings);

            EditorGUILayout.BeginHorizontal();

            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Console", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

                settings.ConsoleAlignment =
                    (ConsoleAlignment) EditorGUILayout.EnumPopup(settings.ConsoleAlignment);

                EditorGUILayout.EndVertical();
            }

            {
                EditorGUI.BeginDisabledGroup(settings.EnableTrigger == Settings.TriggerEnableModes.Off);

                EditorGUILayout.BeginVertical();

                GUILayout.Label("Entry Trigger", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

                settings.TriggerPosition =
                    (PinAlignment) EditorGUILayout.EnumPopup(settings.TriggerPosition);

                EditorGUILayout.EndVertical();

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Profiler", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

                settings.ProfilerAlignment =
                    (PinAlignment) EditorGUILayout.EnumPopup((ProfilerAlignment)settings.ProfilerAlignment);

                EditorGUILayout.EndVertical();
            }

            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Options", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

                settings.OptionsAlignment =
                    (PinAlignment) EditorGUILayout.EnumPopup((OptionsAlignment)settings.OptionsAlignment);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            // Expand content area to fit all available space
            GUILayout.FlexibleSpace();
        }

        private bool _enableButton;

        private void DrawTabBugReporter(Settings settings)
        {
            if (_showBugReportSignupForm)
            {
                DrawBugReportSignupForm(settings);
                return;
            }

            GUILayout.Label("Bug Reporter", SRInternalEditorUtil.Styles.HeaderLabel);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(settings.ApiKey));

            settings.EnableBugReporter = EditorGUILayout.Toggle("Enable Bug Reporter",
                settings.EnableBugReporter);

            
            settings.EnableBugReportScreenshot = EditorGUILayout.Toggle("Take Screenshot",
                settings.EnableBugReportScreenshot);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();

            settings.ApiKey = EditorGUILayout.TextField("API Key", settings.ApiKey);

            if (GUILayout.Button("Verify", GUILayout.ExpandWidth(false)))
            {
                EditorUtility.DisplayDialog("Verify API Key", ApiSignup.Verify(settings.ApiKey), "OK");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.Label(
                "If you need to change your account email address, or have any other questions or concerns, please email us at contact@stompyrobot.uk.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                GUILayout.Label("Reset", SRInternalEditorUtil.Styles.InspectorHeaderStyle);
                GUILayout.Label("Click the button below to clear the API key and show the signup form.",
                    SRInternalEditorUtil.Styles.ParagraphLabel);

                EditorGUILayout.BeginHorizontal();

                _enableButton = EditorGUILayout.Toggle("Enable Button", _enableButton, GUILayout.ExpandWidth(false));

                EditorGUI.BeginDisabledGroup(!_enableButton);

                if (GUILayout.Button("Reset"))
                {
                    settings.ApiKey = null;
                    settings.EnableBugReporter = false;
                    _enableButton = false;
                    _showBugReportSignupForm = true;
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
            }
            else
            {
                if (GUILayout.Button("Show Signup Form"))
                {
                    _showBugReportSignupForm = true;
                }
            }
        }

        private string _invoiceNumber;
        private string _emailAddress;
        private bool _agreeLegal;
        private string _errorMessage;

        private void DrawBugReportSignupForm(Settings settings)
        {
            var isWeb = false;

#if UNITY_WEBPLAYER
			EditorGUILayout.HelpBox("Signup form is not available when build target is Web Player.", MessageType.Error);
			isWeb = true;
#endif

            EditorGUI.BeginDisabledGroup(isWeb || !_enableTabChange);

            GUILayout.Label("Signup Form", SRInternalEditorUtil.Styles.HeaderLabel);
            GUILayout.Label(
                "SRDebugger requires a free API key to enable the bug reporter system. This form will acquire one for you.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            if (
                SRInternalEditorUtil.ClickableLabel(
                    "Already got an API key? <color={0}>Click here</color>.".Fmt(SRInternalEditorUtil.Styles.LinkColour),
                    SRInternalEditorUtil.Styles.RichTextLabel))
            {
                _showBugReportSignupForm = false;
                Repaint();
            }

            EditorGUILayout.Space();

            GUILayout.Label("Invoice/Order Number", EditorStyles.boldLabel);

            GUILayout.Label(
                "Enter the order number from your Asset Store purchase email.",
                EditorStyles.miniLabel);

            _invoiceNumber = EditorGUILayout.TextField(_invoiceNumber);

            EditorGUILayout.Space();

            GUILayout.Label("Email Address", EditorStyles.boldLabel);

            GUILayout.Label(
                "Provide an email address where the bug reports should be sent.",
                EditorStyles.miniLabel);

            _emailAddress = EditorGUILayout.TextField(_emailAddress);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (SRInternalEditorUtil.ClickableLabel(
                "I agree to the <color={0}>terms and conditions</color>.".Fmt(SRInternalEditorUtil.Styles.LinkColour),
                SRInternalEditorUtil.Styles.RichTextLabel))
            {
                ApiSignupTermsWindow.Open();
            }

            _agreeLegal = EditorGUILayout.Toggle(_agreeLegal);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            var isEnabled = !string.IsNullOrEmpty(_invoiceNumber) && !string.IsNullOrEmpty(_emailAddress) && _agreeLegal;
            EditorGUI.BeginDisabledGroup(!isEnabled);

            if (GUILayout.Button("Submit"))
            {
                _errorMessage = null;
                _enableTabChange = false;

                EditorApplication.delayCall += () =>
                {
                    ApiSignup.SignUp(_emailAddress, _invoiceNumber,
                        (success, key, email, error) => OnSignupResult(success, key, email, error, settings));
                    Repaint();
                };
            }

            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error, true);
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("Having trouble? Please email contact@stompyrobot.uk for assistance.",
                EditorStyles.miniLabel);

            EditorGUI.EndDisabledGroup();
        }

        private void OnSignupResult(bool didSucceed, string apiKey, string email, string error, Settings settings)
        {
            _enableTabChange = true;
            _selectedTab = Tabs.Bug_Reporter;

            if (!didSucceed)
            {
                _errorMessage = error;
                return;
            }

            settings.ApiKey = apiKey;
            settings.EnableBugReporter = true;

            EditorUtility.DisplayDialog("SRDebugger API",
                "API key has been created successfully. An email has been sent to your email address ({0}) with a verification link. You must verify your email before you can receive any bug reports."
                    .Fmt(email), "OK");

            _showBugReportSignupForm = false;
        }

        private ReorderableList _keyboardShortcutList;
        private Vector2 _scrollPosition;

        private void DrawTabShortcuts(Settings settings)
        {
            if (_keyboardShortcutList == null)
            {
                _keyboardShortcutList = new ReorderableList((IList) settings.KeyboardShortcuts,
                    typeof (Settings.KeyboardShortcut), false, true, true, true);
                _keyboardShortcutList.drawHeaderCallback = DrawKeyboardListHeaderCallback;
                _keyboardShortcutList.drawElementCallback = DrawKeyboardListItemCallback;
                _keyboardShortcutList.onAddCallback += OnAddKeyboardListCallback;
                _keyboardShortcutList.onRemoveCallback += OnRemoveKeyboardListCallback;
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            settings.EnableKeyboardShortcuts = EditorGUILayout.Toggle(
                new GUIContent("Enable", SRDebugEditorStrings.Current.SettingsKeyboardShortcutsTooltip),
                settings.EnableKeyboardShortcuts);

            EditorGUI.BeginDisabledGroup(!settings.EnableKeyboardShortcuts);

            settings.KeyboardEscapeClose =
                EditorGUILayout.Toggle(
                    new GUIContent("Close on Esc", SRDebugEditorStrings.Current.SettingsCloseOnEscapeTooltip),
                    settings.KeyboardEscapeClose);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            var dupe = DetectDuplicateKeyboardShortcuts(settings);

            if (dupe != null)
            {
                var shortcut = "";

                if (dupe.Control)
                {
                    shortcut += "Ctrl";
                }

                if (dupe.Shift)
                {
                    if (shortcut.Length > 0)
                    {
                        shortcut += "-";
                    }

                    shortcut += "Shift";
                }

                if (dupe.Alt)
                {
                    if (shortcut.Length > 0)
                    {
                        shortcut += "-";
                    }

                    shortcut += "Alt";
                }

                if (shortcut.Length > 0)
                {
                    shortcut += "-";
                }

                shortcut += dupe.Key;

                EditorGUILayout.HelpBox(
                    "Duplicate shortcut ({0}). Only one shortcut per key is supported.".Fmt(shortcut),
                    MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, false,
                GUILayout.Width(position.width - 11));

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width - 30));

            _keyboardShortcutList.DoLayoutList();

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            EditorGUI.EndDisabledGroup();
        }

        private void DrawTabAdvanced(Settings settings)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, true);

            GUILayout.Label("Console", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

            settings.CollapseDuplicateLogEntries =
                EditorGUILayout.Toggle(
                    new GUIContent("Collapse Log Entries", "Collapse duplicate log entries into single log."),
                    settings.CollapseDuplicateLogEntries);

            settings.RichTextInConsole =
                EditorGUILayout.Toggle(
                    new GUIContent("Rich Text in Console", "Parse rich text tags in console log entries."),
                    settings.RichTextInConsole);

            settings.MaximumConsoleEntries =
                EditorGUILayout.IntSlider(
                    new GUIContent("Max Console Entries",
                        "The maximum size of the console buffer. Higher values may cause performance issues on slower devices."),
                    settings.MaximumConsoleEntries, 100, 6000);

            EditorGUILayout.Separator();
            GUILayout.Label("Display", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

            settings.EnableBackgroundTransparency =
                EditorGUILayout.Toggle(new GUIContent("Transparent Background"),
                    settings.EnableBackgroundTransparency);

            EditorGUI.BeginDisabledGroup(!settings.EnableBackgroundTransparency);

            settings.BackgroundTransparency = EditorGUILayout.Slider(new GUIContent("Background Opacity"),
                settings.BackgroundTransparency, 0.0f, 1.0f);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(new GUIContent("Layer", "The layer the debug panel UI will be drawn to"));

            settings.DebugLayer = EditorGUILayout.LayerField(settings.DebugLayer);

            EditorGUILayout.EndHorizontal();

            settings.UseDebugCamera =
                EditorGUILayout.Toggle(
                    new GUIContent("Use Debug Camera", SRDebugEditorStrings.Current.SettingsDebugCameraTooltip),
                    settings.UseDebugCamera);

            EditorGUI.BeginDisabledGroup(!settings.UseDebugCamera);

            settings.DebugCameraDepth = EditorGUILayout.Slider(new GUIContent("Debug Camera Depth"),
                settings.DebugCameraDepth, -100, 100);

            EditorGUI.EndDisabledGroup();

            settings.UIScale =
                EditorGUILayout.Slider(new GUIContent("UI Scale"), settings.UIScale, 1f, 3f);

            EditorGUILayout.Separator();
            GUILayout.Label("Enabled Tabs", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

            GUILayout.Label(SRDebugEditorStrings.Current.SettingsEnabledTabsDescription, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            var disabledTabs = settings.DisabledTabs.ToList();

            var tabNames = Enum.GetNames(typeof (DefaultTabs));
            var tabValues = Enum.GetValues(typeof (DefaultTabs));

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            var changed = false;
            for (var i = 0; i < tabNames.Length; i++)
            {
                var tabName = tabNames[i];
                var tabValue = (DefaultTabs) (tabValues.GetValue(i));

                if (tabName == "BugReporter")
                {
                    continue;
                }

                if (tabName == "SystemInformation")
                {
                    tabName = "System Information";
                }

                EditorGUILayout.BeginHorizontal();

                var isEnabled = !disabledTabs.Contains(tabValue);

                var isNowEnabled = EditorGUILayout.ToggleLeft(tabName, isEnabled,
                    SRInternalEditorUtil.Styles.LeftToggleButton);

                if (isEnabled && !isNowEnabled)
                {
                    disabledTabs.Add(tabValue);
                    changed = true;
                }
                else if (!isEnabled && isNowEnabled)
                {
                    disabledTabs.Remove(tabValue);
                    changed = true;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (changed)
            {
                settings.DisabledTabs = disabledTabs;
            }

            GUILayout.Label("Other", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

            settings.EnableEventSystemGeneration =
            EditorGUILayout.Toggle(
                new GUIContent("Automatic Event System", "Automatically create a UGUI EventSystem if none is found in the scene."),
                settings.EnableEventSystemGeneration);

#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            using (new EditorGUI.DisabledScope(!Settings.Instance.EnableEventSystemGeneration))
            {
                Settings.Instance.UIInputMode =
                    (Settings.UIModes) EditorGUILayout.EnumPopup(new GUIContent("Input Mode"), Settings.Instance.UIInputMode);
            }
#endif

            settings.UnloadOnClose =
            EditorGUILayout.Toggle(
                new GUIContent("Unload When Closed", "Unload the debug panel from the scene when it is closed."),
                settings.UnloadOnClose);

            EditorGUILayout.HelpBox(
                "The panel loads again automatically when opened. You can always unload the panel by holding down the close button.",
                MessageType.Info);

            settings.DisableWelcomePopup =
            EditorGUILayout.Toggle(
                new GUIContent("Disable Welcome Popup", "Disable the welcome popup that appears when a project with SRDebugger is opened for the first time."),
                settings.DisableWelcomePopup);

            EditorGUILayout.Separator();

            if (GUILayout.Button("Run Migrations"))
            {
                Migrations.RunMigrations(true);
            }

            EditorGUILayout.Separator();

            GUILayout.Label("Disable SRDebugger (beta)", SRInternalEditorUtil.Styles.InspectorHeaderStyle);

            GUILayout.Label("Disabling will exclude any SRDebugger assets and scripts from your game.", SRInternalEditorUtil.Styles.ParagraphLabel);
            EditorGUILayout.HelpBox("This is an experimental feature. Please make sure your project is backed up via source control.", MessageType.Warning);

            GUILayout.Label("• " + SRDebugEditor.DisableSRDebuggerCompileDefine + " compiler define will be added to all build configurations.", SRInternalEditorUtil.Styles.ListBulletPoint);
            GUILayout.Label("• Some SRDebugger folders will be renamed to prevent Unity from importing them.", SRInternalEditorUtil.Styles.ListBulletPoint);
            GUILayout.Label("• Any code that interacts with SRDebugger (e.g. SROptions or SRDebug API) should use the `#if !"+ SRDebugEditor.DisableSRDebuggerCompileDefine + "` preprocessor directive.", SRInternalEditorUtil.Styles.ListBulletPoint);
            GUILayout.Label("• You can enable SRDebugger again at any time.", SRInternalEditorUtil.Styles.ListBulletPoint);

            if (GUILayout.Button("Disable SRDebugger"))
            {
                EditorApplication.delayCall += () =>
                {
                    SRDebugEditor.SetEnabled(false);
                    Reset();
                };
            }


            EditorGUILayout.EndScrollView();
        }

            #endregion

            #region Entry Code Utility

        private string GetEntryCodeString(Settings settings)
        {
            var entryCode = settings.EntryCode;

            if (entryCode.Count == 0)
            {
                settings.EntryCode = new[] {0, 0, 0, 0};
            }

            var code = "";

            for (var i = 0; i < entryCode.Count; i++)
            {
                code += entryCode[i];
            }

            return code;
        }

        private void UpdateEntryCode(Settings settings, string str)
        {
            var newCode = new List<int>();

            for (var i = 0; i < str.Length; i++)
            {
                newCode.Add(int.Parse(str[i].ToString(), NumberStyles.Integer));
            }

            settings.EntryCode = newCode;
            _currentEntryCode = GetEntryCodeString(settings);
        }

        #endregion

            #region Keyboard Shortcut Utility

        private Settings.KeyboardShortcut DetectDuplicateKeyboardShortcuts(Settings settings)
        {
            var s = settings.KeyboardShortcuts;

            return
                s.FirstOrDefault(
                    shortcut =>
                        s.Any(
                            p =>
                                p != shortcut && p.Shift == shortcut.Shift && p.Control == shortcut.Control &&
                                p.Alt == shortcut.Alt &&
                                p.Key == shortcut.Key));
        }

        private void DrawKeyboardListHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Keyboard Shortcuts");
        }

        private void DrawKeyboardListItemCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            Settings settings;
            if (!SettingsReady(out settings))
            {
                return;
            }

            var item = settings.KeyboardShortcuts[index];

            rect.y += 2;

            var buttonWidth = 40;
            var padding = 5;

            item.Control = GUI.Toggle(new Rect(rect.x, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight),
                item.Control,
                "Ctrl", "Button");
            rect.x += buttonWidth + padding;
            rect.width -= buttonWidth + padding;

            item.Alt = GUI.Toggle(new Rect(rect.x, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight), item.Alt,
                "Alt",
                "Button");
            rect.x += buttonWidth + padding;
            rect.width -= buttonWidth + padding;

            item.Shift = GUI.Toggle(new Rect(rect.x, rect.y, buttonWidth, EditorGUIUtility.singleLineHeight), item.Shift,
                "Shift",
                "Button");
            rect.x += buttonWidth + padding;
            rect.width -= buttonWidth + padding;

            item.Key =
                (KeyCode) EditorGUI.EnumPopup(new Rect(rect.x, rect.y, 80, EditorGUIUtility.singleLineHeight), item.Key);

            rect.x += 80 + padding;
            rect.width -= 80 + padding;

            item.Action =
                (Settings.ShortcutActions)
                    EditorGUI.EnumPopup(new Rect(rect.x, rect.y, rect.width - 4, EditorGUIUtility.singleLineHeight),
                        item.Action);
        }

        private void OnAddKeyboardListCallback(ReorderableList list)
        {
            Settings settings;
            if (!SettingsReady(out settings))
            {
                return;
            }

            var shortcuts = settings.KeyboardShortcuts.ToList();
            shortcuts.Add(new Settings.KeyboardShortcut());

            settings.KeyboardShortcuts = shortcuts;
            list.list = (IList) settings.KeyboardShortcuts;
        }

        private void OnRemoveKeyboardListCallback(ReorderableList list)
        {
            Settings settings;
            if (!SettingsReady(out settings))
            {
                return;
            }

            var shortcuts = settings.KeyboardShortcuts.ToList();
            shortcuts.RemoveAt(list.index);

            settings.KeyboardShortcuts = shortcuts;
            list.list = (IList) settings.KeyboardShortcuts;
        }

            #endregion

#endif
        }
}
