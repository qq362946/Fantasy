using System.ComponentModel;

namespace SRDebugger
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using SRF;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

#pragma warning disable 0649

    public enum DefaultTabs
    {
        SystemInformation = 0,
        Options = 1,
        Console = 2,
        Profiler = 3,
        BugReporter = 4
    }

    public enum PinAlignment
    {
        TopLeft = 0,
        TopRight = 1,
        BottomLeft = 2,
        BottomRight = 3,
        CenterLeft = 4,
        CenterRight = 5,
        TopCenter = 6,
        BottomCenter = 7
    }

    public enum ConsoleAlignment
    {
        Top,
        Bottom
    }

    public class Settings : ScriptableObject
    {
        public enum ShortcutActions
        {
            None = 0,

            OpenSystemInfoTab = 1,
            OpenConsoleTab = 2,
            OpenOptionsTab = 3,
            OpenProfilerTab = 4,
            OpenBugReporterTab = 5,

            ClosePanel = 6,
            OpenPanel = 7,
            TogglePanel = 8,

            ShowBugReportPopover = 9,
            ToggleDockedConsole = 10,
            ToggleDockedProfiler = 11
        }

        public enum TriggerBehaviours
        {
            TripleTap,
            TapAndHold,
            DoubleTap
        }

        public enum TriggerEnableModes
        {
            Enabled,
            MobileOnly,
            Off,
            DevelopmentBuildsOnly
        }

        private const string ResourcesPath = "/usr/Resources/SRDebugger";
        private const string ResourcesName = "Settings";
        private static Settings _instance;

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GetOrCreateInstance();
                }

                if (_instance._keyboardShortcuts != null && _instance._keyboardShortcuts.Length > 0)
                {
                    _instance.UpgradeKeyboardShortcuts();
                }

                return _instance;
            }
        }

        private static KeyboardShortcut[] GetDefaultKeyboardShortcuts()
        {
            return new[]
            {
                new KeyboardShortcut
                {
                    Control = true,
                    Shift = true,
                    Key = KeyCode.F1,
                    Action = ShortcutActions.OpenSystemInfoTab
                },
                new KeyboardShortcut
                {
                    Control = true,
                    Shift = true,
                    Key = KeyCode.F2,
                    Action = ShortcutActions.OpenConsoleTab
                },
                new KeyboardShortcut
                {
                    Control = true,
                    Shift = true,
                    Key = KeyCode.F3,
                    Action = ShortcutActions.OpenOptionsTab
                },
                new KeyboardShortcut
                {
                    Control = true,
                    Shift = true,
                    Key = KeyCode.F4,
                    Action = ShortcutActions.OpenProfilerTab
                }
            };
        }

        private void UpgradeKeyboardShortcuts()
        {
            Debug.Log("[SRDebugger] Upgrading Settings format");

            var newShortcuts = new List<KeyboardShortcut>();

            for (var i = 0; i < _keyboardShortcuts.Length; i++)
            {
                var s = _keyboardShortcuts[i];

                newShortcuts.Add(new KeyboardShortcut
                {
                    Action = s.Action,
                    Key = s.Key,
                    Alt = _keyboardModifierAlt,
                    Shift = _keyboardModifierShift,
                    Control = _keyboardModifierControl
                });
            }

            _keyboardShortcuts = new KeyboardShortcut[0];
            _newKeyboardShortcuts = newShortcuts.ToArray();

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        [Serializable]
        public sealed class KeyboardShortcut
        {
            [SerializeField] public ShortcutActions Action;

            [SerializeField] public bool Alt;

            [SerializeField] public bool Control;

            [SerializeField] public KeyCode Key;

            [SerializeField] public bool Shift;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region Settings

        public bool IsEnabled
        {
            get { return _isEnabled; }
#if UNITY_EDITOR
            set { _isEnabled = value; }
#endif
        }

        public DefaultTabs DefaultTab
        {
            get { return _defaultTab; }
#if UNITY_EDITOR
            set { _defaultTab = value; }
#endif
        }

        /// <summary>
        /// Enable the triple-tap button.
        /// </summary>
        public TriggerEnableModes EnableTrigger
        {
            get { return _triggerEnableMode; }
#if UNITY_EDITOR
            set { _triggerEnableMode = value; }
#endif
        }

        /// <summary>
        /// Enable the triple-tap button.
        /// </summary>
        public TriggerBehaviours TriggerBehaviour
        {
            get { return _triggerBehaviour; }
#if UNITY_EDITOR
            set { _triggerBehaviour = value; }
#endif
        }

        public bool EnableKeyboardShortcuts
        {
            get { return _enableKeyboardShortcuts; }
#if UNITY_EDITOR
            set { _enableKeyboardShortcuts = value; }
#endif
        }

        public IList<KeyboardShortcut> KeyboardShortcuts
        {
            get { return _newKeyboardShortcuts; }
#if UNITY_EDITOR
            set { _newKeyboardShortcuts = value.ToArray(); }
#endif
        }

        public bool KeyboardEscapeClose
        {
            get { return _keyboardEscapeClose; }
#if UNITY_EDITOR
            set { _keyboardEscapeClose = value; }
#endif
        }

        public bool EnableBackgroundTransparency
        {
            get { return _enableBackgroundTransparency; }
#if UNITY_EDITOR
            set { _enableBackgroundTransparency = value; }
#endif
        }

        public bool RequireCode
        {
            get { return _requireEntryCode; }
#if UNITY_EDITOR
            set { _requireEntryCode = value; }
#endif
        }

        public bool RequireEntryCodeEveryTime
        {
            get { return _requireEntryCodeEveryTime; }
#if UNITY_EDITOR
            set { _requireEntryCodeEveryTime = value; }
#endif
        }

        public IList<int> EntryCode
        {
            get { return new ReadOnlyCollection<int>(_entryCode); }
            set
            {
                if (value.Count != 4)
                {
                    throw new Exception("Entry code must be length 4");
                }
                if (value.Any(p => p > 9 || p < 0))
                {
                    throw new Exception("All digits in entry code must be >= 0 and <= 9");
                }

                _entryCode = value.ToArray();
            }
        }

        public bool UseDebugCamera
        {
            get { return _useDebugCamera; }
#if UNITY_EDITOR
            set { _useDebugCamera = value; }
#endif
        }

        public int DebugLayer
        {
            get { return _debugLayer; }
#if UNITY_EDITOR
            set { _debugLayer = value; }
#endif
        }

        public float DebugCameraDepth
        {
            get { return _debugCameraDepth; }
#if UNITY_EDITOR
            set { _debugCameraDepth = value; }
#endif
        }

        public bool CollapseDuplicateLogEntries
        {
            get { return _collapseDuplicateLogEntries; }
#if UNITY_EDITOR
            set { _collapseDuplicateLogEntries = value; }
#endif
        }

        public bool RichTextInConsole
        {
            get { return _richTextInConsole; }
#if UNITY_EDITOR
            set { _richTextInConsole = value; }
#endif
        }

        public string ApiKey
        {
            get { return _apiKey; }
#if UNITY_EDITOR
            set
            {
                _apiKey = value;
                EditorUtility.SetDirty(this);
            }
#endif
        }

        public bool EnableBugReporter
        {
            get { return _enableBugReporter; }
#if UNITY_EDITOR
            set { _enableBugReporter = value; }
#endif
        }

        public IList<DefaultTabs> DisabledTabs
        {
            get { return _disabledTabs; }
#if UNITY_EDITOR
            set { _disabledTabs = value.ToArray(); }
#endif
        }

        /// <summary>
        /// Position for the triple-tap button
        /// </summary>
        public PinAlignment TriggerPosition
        {
            get { return _triggerPosition; }
#if UNITY_EDITOR
            set
            {
                var prevValue = _triggerPosition;
                _triggerPosition = value;
                if (OptionsAlignment == value)
                {
                    OptionsAlignment = prevValue;
                }
                if (ProfilerAlignment == value)
                {
                    ProfilerAlignment = prevValue;
                }
            }
#endif
        }

        public PinAlignment ProfilerAlignment
        {
            get { return _profilerAlignment; }
#if UNITY_EDITOR
            set
            {
                // Profiler only supports corners
                if (value == PinAlignment.CenterRight || value == PinAlignment.CenterLeft ||
                    value == PinAlignment.TopCenter || value == PinAlignment.BottomCenter)
                {
                    // Prefer bottom left
                    if (TriggerPosition != PinAlignment.BottomLeft && OptionsAlignment != PinAlignment.BottomLeft)
                    {
                        _profilerAlignment = PinAlignment.BottomLeft;
                        return;
                    }

                    // Find next available opening
                    for (var i = 0; i < 4; i++)
                    {
                        var pin = (PinAlignment) i;
                        if (_triggerPosition == pin || _optionsAlignment == pin)
                            continue;

                        _profilerAlignment = pin;
                        return;
                    }
                    
                    _profilerAlignment = PinAlignment.BottomLeft;
                    return;
                }

                var prevValue = _profilerAlignment;
                _profilerAlignment = value;

                if (TriggerPosition == value)
                {
                    TriggerPosition = prevValue;
                }
                if (OptionsAlignment == value)
                {
                    OptionsAlignment = prevValue;
                }
            }
#endif
        }

        public PinAlignment OptionsAlignment
        {
            get { return _optionsAlignment; }
#if UNITY_EDITOR
            set
            {
                // Options doesn't support CenterRight and CenterLeft
                if (value == PinAlignment.CenterRight || value == PinAlignment.CenterLeft)
                {                    
                    // Prefer bottom right
                    if (TriggerPosition != PinAlignment.BottomRight && ProfilerAlignment != PinAlignment.BottomRight)
                    {
                        _optionsAlignment = PinAlignment.BottomRight;
                        return;
                    }
                    for (var i = 0; i < 4; i++)
                    {
                        var pin = (PinAlignment) i;
                        if (_triggerPosition == pin || _profilerAlignment == pin)
                            continue;

                        _optionsAlignment = pin;
                        return;
                    }

                    _optionsAlignment = PinAlignment.BottomRight;
                    return;
                }

                var prevValue = _optionsAlignment;
                _optionsAlignment = value;

                if (TriggerPosition == value)
                {
                    TriggerPosition = prevValue;
                }

                if (ProfilerAlignment == value)
                {
                    ProfilerAlignment = prevValue;
                }
            }
#endif
        }

        public ConsoleAlignment ConsoleAlignment
        {
            get { return _consoleAlignment; }
            set { _consoleAlignment = value; }
        }

        public int MaximumConsoleEntries
        {
            get { return _maximumConsoleEntries; }
            set { _maximumConsoleEntries = value; }
        }

        public bool EnableEventSystemGeneration
        {
            get { return _enableEventSystemCreation; }
            set { _enableEventSystemCreation = value; }
        }

        public bool AutomaticallyShowCursor
        {
            get { return _automaticShowCursor; }
#if UNITY_EDITOR
            set { _automaticShowCursor = value; }
#endif
        }

        public float UIScale
        {
            get { return _uiScale; }
            set
            {
                if (value == _uiScale) return;
                _uiScale = value;
                OnPropertyChanged("UIScale");
            }
        }

        #endregion

        #region Serialization

        [SerializeField] private bool _isEnabled = true;

        [SerializeField] private DefaultTabs _defaultTab = DefaultTabs.SystemInformation;

        [SerializeField] private TriggerEnableModes _triggerEnableMode = TriggerEnableModes.Enabled;

        [SerializeField] private TriggerBehaviours _triggerBehaviour = TriggerBehaviours.TripleTap;

        [SerializeField] private bool _enableKeyboardShortcuts = true;

        // Legacy keyboard shortcuts, should be upgraded
        [SerializeField] private KeyboardShortcut[] _keyboardShortcuts;

        // New keyboard shortcut array, containing upgraded shortcuts
        [SerializeField] private KeyboardShortcut[] _newKeyboardShortcuts = GetDefaultKeyboardShortcuts();

        [SerializeField] private bool _keyboardModifierControl = true;

        [SerializeField] private bool _keyboardModifierAlt = false;

        [SerializeField] private bool _keyboardModifierShift = true;

        [SerializeField] private bool _keyboardEscapeClose = true;

        [SerializeField] private bool _enableBackgroundTransparency = true;

        [SerializeField] private bool _collapseDuplicateLogEntries = true;

        [SerializeField] private bool _richTextInConsole = true;

        [SerializeField] private bool _requireEntryCode;

        [SerializeField] private bool _requireEntryCodeEveryTime;

        [SerializeField] private int[] _entryCode = {0, 0, 0, 0};

        [SerializeField] private bool _useDebugCamera;

        [SerializeField] private int _debugLayer = 5;

        [SerializeField] [Range(-100, 100)] private float _debugCameraDepth = 100f;

        [SerializeField] private string _apiKey = "";

        [SerializeField] private bool _enableBugReporter;

        [SerializeField] private DefaultTabs[] _disabledTabs = {};

        [SerializeField] private PinAlignment _profilerAlignment = PinAlignment.BottomLeft;

        [SerializeField] private PinAlignment _optionsAlignment = PinAlignment.BottomRight;

        [SerializeField] private ConsoleAlignment _consoleAlignment = ConsoleAlignment.Top;

        [SerializeField] private PinAlignment _triggerPosition = PinAlignment.TopLeft;

        [SerializeField] private int _maximumConsoleEntries = 1500;

        [SerializeField] private bool _enableEventSystemCreation = true;

        [SerializeField] private bool _automaticShowCursor = true;

        [SerializeField] private float _uiScale = 1;

        #endregion

        #region Property Changed

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region Saving/Loading

        private static Settings GetOrCreateInstance()
        {
            var instance = Resources.Load<Settings>("SRDebugger/" + ResourcesName);

            if (instance == null)
            {
                // Create instance
                instance = CreateInstance<Settings>();

#if UNITY_EDITOR

                // Get resources folder path
                var resourcesPath = GetResourcesPath();

                if (resourcesPath == null)
                {
                    Debug.LogError(
                        "[SRDebugger] Error finding Resources path. Please make sure SRDebugger folder is intact");
                    return instance;
                }

                Debug.Log("[SRDebugger] Creating settings asset at {0}/{1}".Fmt(resourcesPath, ResourcesName));

                // Create directory if it doesn't exist
                Directory.CreateDirectory(resourcesPath);

                // Save instance if in editor
                AssetDatabase.CreateAsset(instance, resourcesPath + "/" + ResourcesName + ".asset");

#endif
            }

            return instance;
        }

#if UNITY_EDITOR

        private static string GetResourcesPath()
        {
            try
            {
                return Internal.Editor.SRDebugEditorUtil.GetRootPath() + ResourcesPath;
            }
            catch
            {
                return null;
            }
        }

#endif

        #endregion
    }
}
