namespace SRDebugger.Editor
{
    class SRDebugEditorStrings
    {
        public static readonly SRDebugEditorStrings Current = new SRDebugEditorStrings();

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

        public readonly string SettingsAssetStoreUrl = "http://u3d.as/aZc";

        public readonly string SettingsDocumentationUrl = "https://www.stompyrobot.uk/tools/srdebugger/documentation";

        public readonly string SettingsSupportUrl =
            "http://forum.unity3d.com/threads/srdebugger-debug-and-tweak-your-game-while-on-device-released.296403/";

        public readonly string SettingsEnabledTabsDescription =
            "Deselect any tabs that you do not wish to be available in the debug panel.";
    }
}