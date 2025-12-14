namespace SRDebugger.Internal
{
    public class SRDebugStrings
    {
        public static readonly SRDebugStrings Current = new SRDebugStrings();
        public readonly string Console_MessageTruncated = "-- Message Truncated --";
        public readonly string Console_NoStackTrace = "-- No Stack Trace Available --";
        public readonly string PinEntryPrompt = "Enter code to open debug panel";

        public readonly string Profiler_DisableProfilerInfo =
            "Unity profiler is currently <b>enabled</b>. Disable to improve performance.";

        public readonly string Profiler_EnableProfilerInfo =
            "Unity profiler is currently <b>disabled</b>. Enable to show more information.";

        public readonly string Profiler_NoProInfo =
            "Unity profiler is currently <b>disabled</b>. Unity Pro is required to enable it.";

        public readonly string Profiler_NotSupported = "Unity profiler is <b>not supported</b> in this build.";

        public readonly string ProfilerCameraListenerHelp =
            "This behaviour is attached by the SRDebugger profiler to calculate render times.";

#if UNITY_EDITOR

        public readonly string SettingsIsEnabledTooltip =
            "If false, SRDebugger.Init prefab will not load SRDebugger. Manual calls to SRDebug.Instance.ShowDebugPanel() will still work.";

        public readonly string SettingsAutoLoadTooltip =
            "Automatically load SRDebugger when the game loads, even if SRDebugger.Init prefab is not present.";

        public readonly string SettingsDefaultTabTooltip =
            "Visible tab when panel is first opened.";

        public readonly string SettingsKeyboardShortcutsTooltip =
            "Enable Keyboard Shortcuts";

        public readonly string SettingsCloseOnEscapeTooltip =
            "Close debug panel when Escape is pressed.";

        public readonly string SettingsKeyboardModifersTooltip =
            "Modifier keys that must be held for keyboard shortcuts to execute.";

        public readonly string SettingsDebugCameraTooltip =
            "UI will render to a camera instead of overlaying the entire scene.";

        public readonly string SettingsRateBoxContents =
            "If you like SRDebugger, please consider leaving a rating on the Asset Store.";

        public readonly string SettingsWebSiteUrl = "https://www.stompyrobot.uk/tools/srdebugger";

        public readonly string SettingsAssetStoreUrl = "https://www.assetstore.unity3d.com/en/#!/content/27688";

        public readonly string SettingsDocumentationUrl = "https://www.stompyrobot.uk/tools/srdebugger/documentation";

        public readonly string SettingsSupportUrl =
            "http://forum.unity3d.com/threads/srdebugger-debug-and-tweak-your-game-while-on-device-released.296403/";

        public readonly string SettingsEnabledTabsDescription =
            "Deselect any tabs that you do not wish to be available in the debug panel.";

#endif
    }
}
