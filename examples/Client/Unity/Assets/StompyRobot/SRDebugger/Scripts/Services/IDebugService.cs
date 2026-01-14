using UnityEngine;

namespace SRDebugger
{
    public delegate void VisibilityChangedDelegate(bool isVisible);

    public delegate void ActionCompleteCallback(bool success);

    public delegate void PinnedUiCanvasCreated(RectTransform canvasTransform);
}

namespace SRDebugger.Services
{
    using UnityEngine;

    public interface IDebugService
    {
        /// <summary>
        /// Current settings being used by the debugger
        /// </summary>
        Settings Settings { get; }

        /// <summary>
        /// True if the debug panel is currently being shown
        /// </summary>
        bool IsDebugPanelVisible { get; }

        /// <summary>
        /// True if the trigger is currently enabled
        /// </summary>
        bool IsTriggerEnabled { get; set; }

        IDockConsoleService DockConsole { get; }

        bool IsProfilerDocked { get; set; }

        /// <summary>
        /// Add <paramref name="entry"/> to the system information tab. See <seealso cref="InfoEntry"/> for how to create
        /// an info instance.
        /// </summary>
        /// <param name="entry">The entry to be added.</param>
        /// <param name="category">The category the entry should be added to.</param>
        void AddSystemInfo(InfoEntry entry, string category = "Default");

        /// <summary>
        /// Show the debug panel
        /// </summary>
        /// <param name="requireEntryCode">
        /// If true and entry code is enabled in settings, the user will be prompted for a passcode
        /// before opening the panel.
        /// </param>
        void ShowDebugPanel(bool requireEntryCode = true);

        /// <summary>
        /// Show the debug panel and open a certain tab
        /// </summary>
        /// <param name="tab">Tab that will appear when the debug panel is opened</param>
        /// <param name="requireEntryCode">
        /// If true and entry code is enabled in settings, the user will be prompted for a passcode
        /// before opening the panel.
        /// </param>
        void ShowDebugPanel(DefaultTabs tab, bool requireEntryCode = true);

        /// <summary>
        /// Hide the debug panel
        /// </summary>
        void HideDebugPanel();

        /// <summary>
        /// Hide the debug panel, then remove it from the scene to save memory.
        /// </summary>
        void DestroyDebugPanel();

        /// <summary>
        /// Add all an objects compatible properties and methods to the options panel.
        /// <remarks>NOTE: It is not recommended to use this on a MonoBehaviour, it should be used on a standard
        /// class made specifically for use as a settings object.</remarks>
        /// </summary>
        /// <param name="container">The object to add.</param>
        void AddOptionContainer(object container);
        
        /// <summary>
        /// Remove all properties and methods that the <paramref name="container"/> added to the options panel.
        /// </summary>
        /// <param name="container">The container to remove.</param>
        void RemoveOptionContainer(object container);

        /// <summary>
        /// Pin all options in a category.
        /// </summary>
        /// <param name="category"></param>
        void PinAllOptions(string category);

        /// <summary>
        /// Unpin all options in a category.
        /// </summary>
        /// <param name="category"></param>
        void UnpinAllOptions(string category);

        void PinOption(string name);

        void UnpinOption(string name);

        /// <summary>
        /// Clear all pinned options.
        /// </summary>
        void ClearPinnedOptions();

        /// <summary>
        /// Open a bug report sheet.
        /// </summary>
        /// <param name="onComplete">Callback to invoke once the bug report is completed or cancelled. Null to ignore.</param>
        /// <param name="takeScreenshot">
        /// Take a screenshot before opening the report sheet (otherwise a screenshot will be taken as
        /// the report is sent)
        /// </param>
        /// <param name="descriptionContent">Initial content of the bug report description</param>
        void ShowBugReportSheet(ActionCompleteCallback onComplete = null, bool takeScreenshot = true,
            string descriptionContent = null);

        /// <summary>
        /// Event invoked whenever the debug panel opens or closes
        /// </summary>
        event VisibilityChangedDelegate PanelVisibilityChanged;

        event PinnedUiCanvasCreated PinnedUiCanvasCreated;

        /// <summary>
        /// ADVANCED FEATURE. This will convert the debug panel to a world space object and return the RectTransform.
        /// This can be used to position the SRDebugger panel somewhere in your scene.
        /// This feature is for advanced users online who know what they are doing. Only limited support will be provided
        /// for this method.
        /// The debug panel will be made visible if it is not already.
        /// </summary>
        /// <returns>The debug panel RectTransform.</returns>
        RectTransform EnableWorldSpaceMode();
    }
}
