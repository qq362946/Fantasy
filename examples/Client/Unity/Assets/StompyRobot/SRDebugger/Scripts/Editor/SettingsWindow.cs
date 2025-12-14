namespace SRDebugger.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using SRDebugger.Internal;
    using SRDebugger.Internal.Editor;
    using SRF;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    public class SRDebuggerSettingsWindow : EditorWindow
    {
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

        [MenuItem(SRDebugPaths.SettingsMenuItemPath)]
        public static void Open()
        {
            GetWindowWithRect<SRDebuggerSettingsWindow>(new Rect(0, 0, 449, 520), true, "SRDebugger - Settings", true);
        }

        private void OnEnable()
        {
            _currentEntryCode = GetEntryCodeString();

            if (string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                _showBugReportSignupForm = true;
            }
        }

        private void OnGUI()
        {
            // Draw header area 
            SRDebugEditorUtil.BeginDrawBackground();
            SRDebugEditorUtil.DrawLogo(SRDebugEditorUtil.GetLogo());
            SRDebugEditorUtil.EndDrawBackground();

            // Draw header/content divider
            EditorGUILayout.BeginVertical(SRDebugEditorUtil.Styles.SettingsHeaderBoxStyle);
            EditorGUILayout.EndVertical();

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
                    DrawTabGeneral();
                    break;

                case Tabs.Layout:
                    DrawTabLayout();
                    break;

                case Tabs.Bug_Reporter:
                    DrawTabBugReporter();
                    break;

                case Tabs.Shortcuts:
                    DrawTabShortcuts();
                    break;

                case Tabs.Advanced:
                    DrawTabAdvanced();
                    break;
            }

            EditorGUILayout.EndVertical();

            // Display rating prompt and link buttons

            EditorGUILayout.LabelField(SRDebugStrings.Current.SettingsRateBoxContents, EditorStyles.miniLabel);

            SRDebugEditorUtil.DrawFooterLayout(position.width);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(Settings.Instance);
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

        private void DrawTabGeneral()
        {
            GUILayout.Label("Loading", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            if (GUILayout.Toggle(!Settings.Instance.IsEnabled, "Disabled", SRDebugEditorUtil.Styles.RadioButton))
            {
                Settings.Instance.IsEnabled = false;
            }

            GUILayout.Label("Do not load SRDebugger until a manual call to <i>SRDebug.Init()</i>.",
                SRDebugEditorUtil.Styles.RadioButtonDescription);

            var msg = "Automatic (recommended)";
            
            if (GUILayout.Toggle(Settings.Instance.IsEnabled, msg,
                SRDebugEditorUtil.Styles.RadioButton))
            {
                Settings.Instance.IsEnabled = true;
            }

            GUILayout.Label("SRDebugger loads automatically when your game starts.",
                SRDebugEditorUtil.Styles.RadioButtonDescription);

            EditorGUILayout.EndVertical();

            GUILayout.Label("Panel Access", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

            EditorGUILayout.HelpBox("Configure trigger location in the layout tab.", MessageType.None, true);

            Settings.Instance.EnableTrigger =
                (Settings.TriggerEnableModes)
                    EditorGUILayout.EnumPopup(new GUIContent("Trigger Mode"),
                        Settings.Instance.EnableTrigger);

            EditorGUI.BeginDisabledGroup(Settings.Instance.EnableTrigger == Settings.TriggerEnableModes.Off);

            Settings.Instance.TriggerBehaviour =
                (Settings.TriggerBehaviours)
                    EditorGUILayout.EnumPopup(new GUIContent("Trigger Behaviour"),
                        Settings.Instance.TriggerBehaviour);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            Settings.Instance.DefaultTab =
                (DefaultTabs)
                    EditorGUILayout.EnumPopup(
                        new GUIContent("Default Tab", SRDebugStrings.Current.SettingsDefaultTabTooltip),
                        Settings.Instance.DefaultTab);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            Settings.Instance.RequireCode = EditorGUILayout.Toggle(new GUIContent("Require Entry Code"),
                Settings.Instance.RequireCode);

            EditorGUI.BeginDisabledGroup(!Settings.Instance.RequireCode);

            Settings.Instance.RequireEntryCodeEveryTime = EditorGUILayout.Toggle(new GUIContent("...Every Time", "Require the user to enter the PIN every time they access the debug panel."),
                Settings.Instance.RequireEntryCodeEveryTime);

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
                    UpdateEntryCode(newCode);
                }
            }

            EditorGUI.EndDisabledGroup();

            Settings.Instance.AutomaticallyShowCursor =
                EditorGUILayout.Toggle(
                    new GUIContent("Show Cursor",
                        "Automatically set the cursor to visible when the debug panel is opened, and revert when closed."),
                    Settings.Instance.AutomaticallyShowCursor);

            // Expand content area to fit all available space
            GUILayout.FlexibleSpace();
        }

        private void DrawTabLayout()
        {
            GUILayout.Label("Pinned Tool Positions", SRDebugEditorUtil.Styles.HeaderLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var rect = GUILayoutUtility.GetRect(360, 210);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            SRDebugEditorUtil.DrawLayoutPreview(rect);

            EditorGUILayout.BeginHorizontal();

            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Console", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

                Settings.Instance.ConsoleAlignment =
                    (ConsoleAlignment) EditorGUILayout.EnumPopup(Settings.Instance.ConsoleAlignment);

                EditorGUILayout.EndVertical();
            }

            {
                EditorGUI.BeginDisabledGroup(Settings.Instance.EnableTrigger == Settings.TriggerEnableModes.Off);

                EditorGUILayout.BeginVertical();

                GUILayout.Label("Entry Trigger", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

                Settings.Instance.TriggerPosition =
                    (PinAlignment) EditorGUILayout.EnumPopup(Settings.Instance.TriggerPosition);

                EditorGUILayout.EndVertical();

                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Profiler", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

                Settings.Instance.ProfilerAlignment =
                    (PinAlignment) EditorGUILayout.EnumPopup((ProfilerAlignment)Settings.Instance.ProfilerAlignment);

                EditorGUILayout.EndVertical();
            }

            {
                EditorGUILayout.BeginVertical();

                GUILayout.Label("Options", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

                Settings.Instance.OptionsAlignment =
                    (PinAlignment) EditorGUILayout.EnumPopup((OptionsAlignment)Settings.Instance.OptionsAlignment);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            // Expand content area to fit all available space
            GUILayout.FlexibleSpace();
        }

        private bool _enableButton;

        private void DrawTabBugReporter()
        {
            if (_showBugReportSignupForm)
            {
                DrawBugReportSignupForm();
                return;
            }

            GUILayout.Label("Bug Reporter", SRDebugEditorUtil.Styles.HeaderLabel);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(Settings.Instance.ApiKey));

            Settings.Instance.EnableBugReporter = EditorGUILayout.Toggle("Enable Bug Reporter",
                Settings.Instance.EnableBugReporter);

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();

            Settings.Instance.ApiKey = EditorGUILayout.TextField("API Key", Settings.Instance.ApiKey);

            if (GUILayout.Button("Verify", GUILayout.ExpandWidth(false)))
            {
                EditorUtility.DisplayDialog("Verify API Key", ApiSignup.Verify(Settings.Instance.ApiKey), "OK");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.Label(
                "If you need to change your account email address, or have any other questions or concerns, please email us at contact@stompyrobot.uk.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(Settings.Instance.ApiKey))
            {
                GUILayout.Label("Reset", SRDebugEditorUtil.Styles.InspectorHeaderStyle);
                GUILayout.Label("Click the button below to clear the API key and show the signup form.",
                    SRDebugEditorUtil.Styles.ParagraphLabel);

                EditorGUILayout.BeginHorizontal();

                _enableButton = EditorGUILayout.Toggle("Enable Button", _enableButton, GUILayout.ExpandWidth(false));

                EditorGUI.BeginDisabledGroup(!_enableButton);

                if (GUILayout.Button("Reset"))
                {
                    Settings.Instance.ApiKey = null;
                    Settings.Instance.EnableBugReporter = false;
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

        private void DrawBugReportSignupForm()
        {
            var isWeb = false;

#if UNITY_WEBPLAYER
			EditorGUILayout.HelpBox("Signup form is not available when build target is Web Player.", MessageType.Error);
			isWeb = true;
#endif

            EditorGUI.BeginDisabledGroup(isWeb || !_enableTabChange);

            GUILayout.Label("Signup Form", SRDebugEditorUtil.Styles.HeaderLabel);
            GUILayout.Label(
                "SRDebugger requires a free API key to enable the bug reporter system. This form will acquire one for you.",
                SRDebugEditorUtil.Styles.ParagraphLabel);

            if (
                SRDebugEditorUtil.ClickableLabel(
                    "Already got an API key? <color={0}>Click here</color>.".Fmt(SRDebugEditorUtil.Styles.LinkColour),
                    SRDebugEditorUtil.Styles.RichTextLabel))
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

            if (SRDebugEditorUtil.ClickableLabel(
                "I agree to the <color={0}>terms and conditions</color>.".Fmt(SRDebugEditorUtil.Styles.LinkColour),
                SRDebugEditorUtil.Styles.RichTextLabel))
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
                    ApiSignup.SignUp(_emailAddress, _invoiceNumber, OnSignupResult);
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

        private void OnSignupResult(bool didSucceed, string apiKey, string email, string error)
        {
            _enableTabChange = true;
            _selectedTab = Tabs.Bug_Reporter;

            if (!didSucceed)
            {
                _errorMessage = error;
                return;
            }

            Settings.Instance.ApiKey = apiKey;
            Settings.Instance.EnableBugReporter = true;

            EditorUtility.DisplayDialog("SRDebugger API",
                "API key has been created successfully. An email has been sent to your email address ({0}) with a verification link. You must verify your email before you can receive any bug reports."
                    .Fmt(email), "OK");

            _showBugReportSignupForm = false;
        }

        private ReorderableList _keyboardShortcutList;
        private Vector2 _scrollPosition;

        private void DrawTabShortcuts()
        {
            if (_keyboardShortcutList == null)
            {
                _keyboardShortcutList = new ReorderableList((IList) Settings.Instance.KeyboardShortcuts,
                    typeof (Settings.KeyboardShortcut), false, true, true, true);
                _keyboardShortcutList.drawHeaderCallback = DrawKeyboardListHeaderCallback;
                _keyboardShortcutList.drawElementCallback = DrawKeyboardListItemCallback;
                _keyboardShortcutList.onAddCallback += OnAddKeyboardListCallback;
                _keyboardShortcutList.onRemoveCallback += OnRemoveKeyboardListCallback;
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            Settings.Instance.EnableKeyboardShortcuts = EditorGUILayout.Toggle(
                new GUIContent("Enable", SRDebugStrings.Current.SettingsKeyboardShortcutsTooltip),
                Settings.Instance.EnableKeyboardShortcuts);

            EditorGUI.BeginDisabledGroup(!Settings.Instance.EnableKeyboardShortcuts);

            Settings.Instance.KeyboardEscapeClose =
                EditorGUILayout.Toggle(
                    new GUIContent("Close on Esc", SRDebugStrings.Current.SettingsCloseOnEscapeTooltip),
                    Settings.Instance.KeyboardEscapeClose);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            var dupe = DetectDuplicateKeyboardShortcuts();

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

        private void DrawTabAdvanced()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, true);

            GUILayout.Label("Console", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

            Settings.Instance.CollapseDuplicateLogEntries =
                EditorGUILayout.Toggle(
                    new GUIContent("Collapse Log Entries", "Collapse duplicate log entries into single log."),
                    Settings.Instance.CollapseDuplicateLogEntries);

            Settings.Instance.RichTextInConsole =
                EditorGUILayout.Toggle(
                    new GUIContent("Rich Text in Console", "Parse rich text tags in console log entries."),
                    Settings.Instance.RichTextInConsole);

            Settings.Instance.MaximumConsoleEntries =
                EditorGUILayout.IntSlider(
                    new GUIContent("Max Console Entries",
                        "The maximum size of the console buffer. Higher values may cause performance issues on slower devices."),
                    Settings.Instance.MaximumConsoleEntries, 100, 6000);

            EditorGUILayout.Separator();
            GUILayout.Label("Display", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

            Settings.Instance.EnableBackgroundTransparency =
                EditorGUILayout.Toggle(new GUIContent("Transparent Background"),
                    Settings.Instance.EnableBackgroundTransparency);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(new GUIContent("Layer", "The layer the debug panel UI will be drawn to"));

            Settings.Instance.DebugLayer = EditorGUILayout.LayerField(Settings.Instance.DebugLayer);

            EditorGUILayout.EndHorizontal();

            Settings.Instance.UseDebugCamera =
                EditorGUILayout.Toggle(
                    new GUIContent("Use Debug Camera", SRDebugStrings.Current.SettingsDebugCameraTooltip),
                    Settings.Instance.UseDebugCamera);

            EditorGUI.BeginDisabledGroup(!Settings.Instance.UseDebugCamera);

            Settings.Instance.DebugCameraDepth = EditorGUILayout.Slider(new GUIContent("Debug Camera Depth"),
                Settings.Instance.DebugCameraDepth, -100, 100);

            EditorGUI.EndDisabledGroup();

            Settings.Instance.UIScale =
                EditorGUILayout.Slider(new GUIContent("UI Scale"), Settings.Instance.UIScale, 1f, 3f);

            EditorGUILayout.Separator();
            GUILayout.Label("Enabled Tabs", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

            GUILayout.Label(SRDebugStrings.Current.SettingsEnabledTabsDescription, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            var disabledTabs = Settings.Instance.DisabledTabs.ToList();

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
                    SRDebugEditorUtil.Styles.LeftToggleButton);

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
                Settings.Instance.DisabledTabs = disabledTabs;
            }

            GUILayout.Label("Other", SRDebugEditorUtil.Styles.InspectorHeaderStyle);

            Settings.Instance.EnableEventSystemGeneration =
            EditorGUILayout.Toggle(
                new GUIContent("Automatic Event System", "Automatically create a UGUI EventSystem if none is found in the scene."),
                Settings.Instance.EnableEventSystemGeneration);

            EditorGUILayout.Separator();

            if (GUILayout.Button("Run Migrations"))
            {
                Migrations.RunMigrations(true);
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Entry Code Utility

        private string GetEntryCodeString()
        {
            var entryCode = Settings.Instance.EntryCode;

            if (entryCode.Count == 0)
            {
                Settings.Instance.EntryCode = new[] {0, 0, 0, 0};
            }

            var code = "";

            for (var i = 0; i < entryCode.Count; i++)
            {
                code += entryCode[i];
            }

            return code;
        }

        private void UpdateEntryCode(string str)
        {
            var newCode = new List<int>();

            for (var i = 0; i < str.Length; i++)
            {
                newCode.Add(int.Parse(str[i].ToString(), NumberStyles.Integer));
            }

            Settings.Instance.EntryCode = newCode;
            _currentEntryCode = GetEntryCodeString();
        }

        #endregion

        #region Keyboard Shortcut Utility

        private Settings.KeyboardShortcut DetectDuplicateKeyboardShortcuts()
        {
            var s = Settings.Instance.KeyboardShortcuts;

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
            var item = Settings.Instance.KeyboardShortcuts[index];

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
            var shortcuts = Settings.Instance.KeyboardShortcuts.ToList();
            shortcuts.Add(new Settings.KeyboardShortcut());

            Settings.Instance.KeyboardShortcuts = shortcuts;
            list.list = (IList) Settings.Instance.KeyboardShortcuts;
        }

        private void OnRemoveKeyboardListCallback(ReorderableList list)
        {
            var shortcuts = Settings.Instance.KeyboardShortcuts.ToList();
            shortcuts.RemoveAt(list.index);

            Settings.Instance.KeyboardShortcuts = shortcuts;
            list.list = (IList) Settings.Instance.KeyboardShortcuts;
        }

        #endregion
    }
}
