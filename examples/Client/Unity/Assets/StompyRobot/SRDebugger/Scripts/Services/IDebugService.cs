using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SRDebugger.Services
{
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

        /// <summary>
        /// True if new errors cause the trigger to display an error notification.
        /// Note: <see cref="IsTriggerEnabled"/> must also be true for notification to display.
        /// </summary>
        bool IsTriggerErrorNotificationEnabled { get; set; }


        IDockConsoleService DockConsole { get; }

        /// <summary>
        /// Access the SRDebugger console current filter states.
        /// </summary>
        IConsoleFilterState ConsoleFilter { get; }

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
        /// Set the entry code required to open the debug panel.
        /// Entry code requirement will be enabled if it is not already.
        /// 
        /// If the user has already entered the correct pin code, their authorization will be reset
        /// and they will be required to enter the new pin code next time they open the debug panel.
        /// 
        /// Use <see cref="DisableEntryCode"/> to disable the entry code requirement.
        /// </summary>
        /// <param name="newCode">New entry code.</param>
        void SetEntryCode(EntryCode newCode);

        /// <summary>
        /// Disable the requirement for an entry code when opening the debug panel.
        /// Use <see cref="SetEntryCode"/> to enable entry code the requirement again.
        /// </summary>
        void DisableEntryCode();

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
        /// Add an option to the options panel.
        /// </summary>
        void AddOption(OptionDefinition option);

        /// <summary>
        /// Remove an option from the options panel.
        /// </summary>
        /// <returns>True if option was successfully removed, otherwise false.</returns>
        bool RemoveOption(OptionDefinition option);

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
        /// the report is sent, if enabled in settings)
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
        /// This feature is for advanced users only who know what they are doing. Only limited support will be provided
        /// for this method.
        /// The debug panel will be made visible if it is not already.
        /// </summary>
        /// <returns>The debug panel RectTransform.</returns>
        RectTransform EnableWorldSpaceMode();

        /// <summary>
        /// Set a custom bug reporter handler.
        /// NOTE: This should be done on startup, ideally before the debug panel is opened.
        /// The visibility of the bug report tab will be determined when the debug panel opens so the bug reporter handler
        /// should be set before then.
        /// </summary>
        /// <param name="bugReporterHandler">Custom bug report handler.</param>
        void SetBugReporterHandler(IBugReporterHandler bugReporterHandler);
    }
}

namespace SRDebugger
{
    public delegate void VisibilityChangedDelegate(bool isVisible);

    public delegate void ActionCompleteCallback(bool success);

    public delegate void PinnedUiCanvasCreated(RectTransform canvasTransform);

    public struct EntryCode : IReadOnlyList<int>
    {
        public readonly int Digit1;
        public readonly int Digit2;
        public readonly int Digit3;
        public readonly int Digit4;

        public EntryCode(int digit1, int digit2, int digit3, int digit4)
        {
            if (digit1 < 0 || digit1 > 9) throw new ArgumentException("Pin digit must be between 0 and 9", "digit1");
            if (digit2 < 0 || digit2 > 9) throw new ArgumentException("Pin digit must be between 0 and 9", "digit2");
            if (digit3 < 0 || digit3 > 9) throw new ArgumentException("Pin digit must be between 0 and 9", "digit3");
            if (digit4 < 0 || digit4 > 9) throw new ArgumentException("Pin digit must be between 0 and 9", "digit4");

            Digit1 = digit1;
            Digit2 = digit2;
            Digit3 = digit3;
            Digit4 = digit4;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return new List<int> { Digit1, Digit2, Digit3, Digit4 }.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return 4; }
        }

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Digit1;
                    case 1: return Digit2;
                    case 2: return Digit3;
                    case 3: return Digit4;
                    default: throw new ArgumentOutOfRangeException("index");
                }
            }
        }
    }
}
