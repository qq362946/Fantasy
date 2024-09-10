=====================
SRDebugger - (C) Stompy Robot LTD 2021
=====================

Visit https://www.stompyrobot.uk/tools/srdebugger/documentation for more detailed documentation.

# Getting Started:

Open StompyRobot/SRDebugger/Scenes/Sample.unity for a simple example scene.

## Setup

### Unity 2019 / 2020 / 2021 / 2022

No setup is required. SRDebugger will automatically load at runtime unless disabled in settings. 
By default, the trigger to open the debug panel is attached to the top-left of the game view. Triple-tap there to open the panel. (This can be changed in the settings)

## Configuration

On the menu bar, click "Window/SRDebugger Settings" to open the settings pane for SRDebugger. You can set up trigger behaviour, pin entry, and more here.

# Other

For documentation on other features, including the options tab, bug reporter, profiler, etc, visit the documentation online at https://www.stompyrobot.uk/tools/srdebugger/documentation

# Restrictions

 - Icons included in this pack must only be used in the SRDebugger panel. If you wish to use the icons outside of the debug panel, consider licensing from icons8.com/buy
 - Unauthorised distribution of this library is not permitted. See Unity Asset Store EULA for details.
 
# Credits

- Programming/Design by Simon Moles @ Stompy Robot (simon@stompyrobot.uk, www.stompyrobot.uk)
- Icons provided by Icons8 (www.icons8.com)
- Side-bar background pattern provided by Subtle Patterns (www.subtlepatterns.com)
- Orbitron font provided by the League of Moveable Type (theleagueofmoveabletype.com) (Open Font License 1.1)
- Source Code Pro font provided by Adobe (github.com/adobe-fonts/source-code-pro) (Open Font License 1.1)

# Change Log

1.12.1
----------

** Minimum supported version is now 2019.3 **

Features:
- Background opacity can now be configured in settings (Settings/Advanced/Background Opacity)
- Application ID will now be displayed in emails from the bug reporter (the ID set in Project Preferences)
- New option to disable the welcome popup (Settings/Advanced/Disable Welcome Popup)

API changes:
- New API for changing the bug report handler. This allows you to implement a custom bug report endpoint without modifying SRDebugger source code. (SRDebug.Instance.SetBugReportHandler)
- New API for changing console filter state (info/warning/error visibility)
- New API for enabling or disabling error notification (SRDebug.Instance.IsTriggerErrorNotificationEnabled)
- New API for checking if SRDebugger is initialized (SRDebug.IsInitialized)

Changes:
- Info/Warning/Error filter state now syncs between console tab and pinned console, and persists if debug panel is unloaded.
- Option categories are now sorted alphabetically.
- MonoBehaviors can now be added as an Option Container. Any properties or methods derived from MonoBehavior will be ignored.
- Properties and Methods with [Browsable(false)] attribute will not be displayed in the options tab (applies to SROptions and Option Containers)

Fixes:
- (Options) Enum and Number property names are no longer truncated.
- (Options) Improved error message when an unsupported property type is encountered.
- Debug panel no longer consumes CPU resources rendering canvases when hidden.
- Misc optimisations and bug fixes.

1.11.0
----------

New:
- Initial support for notched displays (safe area)
- Copy log message / stack trace to clipboard (on supported platforms)

Changes:
- Improvements to UI scaling behaviour on high-dpi phone displays
- Display warning at runtime when issues are detected with log message callbacks
- Expanded input area for scroll bar on console view (easier to drag on touch screens)
- Added additional graphics info to system tab
- Press and hold "refresh" button on system tab to activate updating entries every frame
- (Editor) Cleanup and restore SRDebugger after domain reload when in play mode (script recompile)

Fixes:
- Fix exception when adding custom option entry via property getter
- Fix exception when opening settings window a second time
- Additional fixes to support enter play mode without domain reload

1.10.0
----------

** Minimum supported version is now 2018.4 **

Features:
- New API: Custom "options containers" that can dynamically add/remove options at runtime without using reflection (i.e. no underlying C# property/method for each option).
- Support for new Unity Input System.
- (Experimental) Editor UI and script API for enabling/disabling SRDebugger. This enables you completely remove SRDebugger from builds of your game without uninstalling the plugin.

Changes:
- Added option to disable taking screenshot when making a bug report.
- Performance improvements for options tab when there are many option values changing frequently.

Fixes:
- Update SROptions window in editor when using SRDebug.Init()
- Fix errors relating to "domain reload" when entering play mode in editor.
- MissingReferenceException when using options containers while debug panel is unloaded.
- Prevent trigger from taking input focus via navigation events.
- Fix access to Unity property from background thread.

1.9.1
----------

Fixed:
- No longer auto-initializes when auto-initialization is disabled. 
- Improved support for higher levels of 'managed stripping' in AOT compiled platforms (IL2CPP)
- Fix error notifier calling native Unity methods from background thread.
- Fix number increment/decrement issues on non-english language platforms.

1.9.0
----------

Attention: If upgrading from a previous version, please see upgrade notes below.

New:
- Added a notification to the trigger whenever an error is added to the console log (disable in settings).

Changed:
- Converted to asmdef packages.
- Added note to auto-created event system to inform that it is created by SRDebugger and how to disable it.

Fixed:
- Support Domain Reload in 2019.3
- Fix settings window display in 2019.3


Upgrade Notes:

This package now uses assembly definition files (asmdef) to isolate the script assets. 
Please import this new version over your existing implementation and overwrite any modified files.

For more information on assembly definition files, see the Unity docs: 
https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html

The options panel (SROptions) has required some changes to support asmdef, however most user code interacting with SROptions should not require any changes.

For assistance please contact us at contact@stompyrobot.uk or post on the forums.

1.8.4
----------

New:
- Support for 2019.3.
- Added option to completely unload the debug panel whenever it is closed.

Changed:
- Use https for bug reporter on all platforms.

Fixed:
- Console not clearing correctly after the first time.
- Obsolete API warnings.
- Incorrect layout on options tab when running on Unity 2019.1

1.8.2
----------

Fixed:
- Exception during bug report when no logs exist.

1.8.1
----------

Fixed:
- Fixed warning from using an obsolete Unity API on 2018.2.
- Welcome/Settings screen sizing on high dpi displays.

1.8.0
----------

Note: Minimum supported Unity version is now 5.6.

New:
- Profiler support for scriptable render pipelines.

Changed:
- Adjusted profiler to improve performance. V-Sync delay is now in "Other" category.
- Updated bug reporter to use UnityWebRequest.
- Removed "Prefab" initialization mode because Unity 4 is no longer supported.
	- If you had "Prefab" mode enabled then you will be switched to "Automatic" mode.
- Console logs are now gathered from a earlier point in initialization.

Fixed:
- Time.timeScale set to 0 would break profiler.
- Fix errors when changing UI scale after ending play mode.

1.7.1
----------

Fixed:
- "Internal Server Error" message when sending a bug report on some locales when using .NET 4.6

1.7.0
----------

New:
- Added "UI Scale" setting to Advanced tab in Settings Window (and API via SRDebug.Instance.Settings.UIScale)
- Added "Application Version" to system information.

Fixed:
- Small numbers in SROptions display correctly.
- Clearing the console a second time wouldn't clear the log messages.

1.6.2
----------

Fixed:
- Compatibility with Unity 2017.1

1.6.1
----------

New:
- Added "Development Builds Only" option to trigger behaviour.

Fixed:
- Crash caused by a known issue in Unity 5.6.0f3.
- Compile warnings in Unity 5.6.0f3.

1.6.0
----------

New:
- Cursor is automatically shown when debug panel is opened. (can be disabled in settings)
- Added an API for converting the debug panel into a world object that can be positioned in the scene (useful for VR). See docs for information.
   ^ This is an advanced feature that is not officially supported but provided for users who know how to use it.

Changed:
- Use Rect sprite packing for SRDebugger UI assets to prevent rendering artifacts.

Fixed:
- OnPropertyChanged in SROptions having no effect.
- Cameras disabled during the frame preventing profiler from recording frames. (Google VR compatibility)

1.5.1
----------

Fixed:
- Compile on WSA builds.
- Editor resources not being found when using Mad Compile Time Optimizer to move scripts.
- Log messages from other threads not being captured.

New:
- Added setting to disable automatically generated EventSystem.

1.5.0
----------

New:
- Console can be filtered/searched.
- Console now has a "Scroll to Bottom" button.
- Option categories can now be pinned/unpinned all at once.
- API for pinning/unpinning options.
- Additional "Option Containers" can be registered with the SRDebug api, enabling your own objects to populate the Options tab. See docs.
- API for adding information to the System tab (which will also be sent with bug reports).

Changed:
- Compatibility with Unity 5.5
- Bug reporter autofills email field with last used email address.
- Limited maximum console messages. (Default 1500, configurable in settings window)
- Performance improvements

Fixed:
- Windows Store builds with .NET Native now work correctly.
- Incorrect behaviour when creating default EventSystem when using manual init.

1.4.9
----------

Changed: 
- Compatibility with Unity 5.4.
- SROptions: Read-only string options now expand to display entire string.

1.4.8
----------

New:
- Added "SROptions Window" for tweaking SROptions parameters while working in the Unity Editor. (Unity 5 only)

1.4.7
----------

New:
- Trigger can now be positioned in CenterLeft, CenterRight, BottomCenter, TopCenter positions.
- Options can now be positioned in TopCenter and BottomCenter positions.

Changes:
- Console now scrolls to the last log entry when first opened.
- Moved "using" statements inside namespace to prevent conflicts with user code.
- Renamed the hierarchy names of all prefabs to include an SR_ prefix to prevent conflicts with user code.

Fixes:
- Fixed input bug when using Unity 5.3.3p2.
- Allocation per frame when pin entry form is visible has been removed.
- Mono usage profiler correctly reports when not supported on 5.3+


1.4.6
----------

Fixes:
- Editor resources used by SRDebugger are no longer included in non-editor builds.

Known Issues:

- On Unity 5.3.0f4, errors are printed when resizing the docked console and profiler. This is a Unity bug and should be fixed in a future Unity update. See http://issuetracker.unity3d.com/issues/layoutrebuilder-errors-when-changing-rect-transform-width-in-layout-element-component for details.

1.4.5
----------

Changes:
- Added notice about known issue to Welcome window when running Unity 5.3
- Unity 4.7 is now minimum supported version.

Fixes:
- Bug reporter signup form continues to the next page correctly after submitting.

1.4.4
----------

Changes:
- Support for Bug Reporter on WebGL platform.
- Enabled HTTPS for bug reporter on iOS to comply with TLS restrictions.
- Documented pin entry API, and deprecated an obsolete parameter. (See documentation for example of how to use pin entry API)

Fixes:
- TouchInputModule is now added to default event system on Unity 4, allowing touch input to be recognised by SRDebugger.
- Welcome window no longer causes errors on Unity 4.

1.4.2 & 1.4.3
----------

Changes:
- Compatibility with Unity 5.3.0.
- Performance improvements when scrolling console log.

Fixes:
- Profiler no longer stops updating when a camera in the scene is disabled.
- (1.4.3) Fix build on Windows Store platform.

1.4.1
----------

Fixes:
- Bug reporter tab no longer requests pin entry after taking screenshot when "require pin every time" enabled.
- Compile fixes for Unity 5.2.2

1.4.0
----------

New:
- Brand new Settings window with more intuitive layout and tabbed interface.
- Added "Welcome" window that opens on first import to help first-time users.
- Can now customize the docked tools layout from the new settings window.
- Docked console alignment can be adjusted from the API (SRDebug.Instance.DockConsole.Alignment).
- Added new "Double Tap" mode for entry trigger.
- (EXPERIMENTAL) Added PlayMaker actions package (Open bug report sheet, Open/Close debug panel, Dock/Undock Console/Profiler, Enable/Disable trigger, etc).

Changes:
- Keyboard shortcuts can now have modifier keys set per-shortcut, instead of only for all shortcuts.
- Bug reporter signup form now provides more helpful error messages.

Fixes:
- Stack trace area no longer jumps to the bottom of the scroll area when selecting a log entry.
- DisplayName attribute now works correctly on methods in SROptions.
- Bug reporter progress bar no longer only fills half-way when submitting bug reports.
- Exception no longer occurs when opening debug panel if you have a custom tab.
- Fixed intertia in scroll views not being enabled when on mobile platforms.

1.3.0
----------

New:
- Profiler can now be docked. Enable by pressing the "pin" icon on profiler tab or via API (SRDebug.Instance.IsProfilerDocked), or via keyboard shortcuts
- Resize docked profiler by dragging edges
- Added IncrementAttribute for use with SROptions, used to specify how much a number will be incremented/decremented when buttons are pressed
- Can disable specific tabs in SRDebugger settings
- Added "Runtime" and "Display" categories to system tab (this information is also sent with bug reports)
- Support for Unity 5.2

Changes:
- Namespace remaining code in SRF library to avoid conflicts. (If you're using any of this code you may need to import SRF namespace in your files)

Fixes:
- Fixed opacity on docked console not resetting after failed resize drag
- Truncate long log messages to improve performance and prevent UGUI errors

1.2.1
----------

New:
- Added DisplayName attribute for use with SROptions.

Changes:
- Read-only properties are now added to options tab (but can't be modified).
- Sort attribute can now be applied to methods.

Fixes:
- Fixed compile errors when NGUI is imported in the same project.
- Removed excess logging when holding a number button in options tab.

1.2.0
----------

New:
- Dock console at the top of the screen. (open from the console tab, SRDebug API or keyboard shortcuts)
- Collapse duplicate log entries (enable in settings)
- Bug Report popover. Show bug reporter without granting access to the debug panel. Open via keyboard shortcut or the SRDebug API.
- Added Sort attribute to sort items in options tab. (See SROptions.Test.cs for examples)
- Added SROptions PropertyChanged support. Call OnPropertyChanged() in your setters and pinned options will update to reflect the new value.
- Entry code can now be entered with keyboard.

Changes:
- Sending screenshot with bug report now supported on web player.

Fixes:
- Fixed pin entry canvas not using correct UI camera.
- Modified namespaces and naming of internal classes to reduce conflicts with other assets.
- Fixed script updater having to run for Unity 5.1
- Misc bug fixes

1.1.2
----------

Changes:
- Bug reporter is now supported on Web Player builds (now uses Unity WWW instead of HttpWebRequest for API calls)
- System Information area now shows IL2CPP status on iOS builds
- Application.platform value is now included with bug reports
- Support for Unity 5.1

Fixes:
- Fixed issues with options panel and IL2CPP on iOS
- Unity Cloud Build information now formatted correctly
- Fixed Settings UI issue on Unity 5.1 beta
- Fixed Entry Code setting having no effect
- Fixed keyboard shortcuts bypassing entry code if enabled

1.1.1
----------

Changes:
- The version of SRF (https://github.com/StompyRobot/SRF) has been changed to the "Lite" version, containing only scripts relevant to SRDebugger. If you want the full SRF library it is available free on GitHub.

Fixes:
- SRDebugger no longer creates an event system in a scene if one already exists on Unity 5 using Auto-Init.
- Fixed CategoryAttribute being in the wrong namespace when when compiling for Windows 8 platforms.

1.1.0
----------

New:
- (Unity 5) Can enable "Auto-Init" in the Settings pane to automatically initialize SRDebugger without SRDebugger.Init prefab included in the scene.
- (BETA) Bug Reporter - Users can submit bug reports, with console log and system information included. These will be forwarded to you by email. (Enable in Settings)
- (BETA) Windows Store support
- Added support for Keyboard Shortcuts
- Added Trigger Behaviour option. Switch between "Triple-Tap" and "Tap-And-Hold" methods for opening debug panel
- Added Default Tab option in Settings pane
- Added Layer option to settings panel to choose which layer UI will be on
- Added Debug Camera mode (render debug panel UI to a camera instead of overlay)
- SRDebug.Init() method added for custom initialisation of SRDebugger without SRDebugger.Init prefab
- Event added to SRDebug on panel open/close

Changes:
- Scroll sensitivity has been improved for desktop platforms

1.0.2
----------

Fixed:
- Fixed console layout with Unity 4.6.3+
- Trigger Position setting now checked on init

1.0.1
----------

New:
- Unity 5.0 Support.
- Added option to Settings pane to require the entry code for every time the panel opens, instead of just the first time.

Fixed:
- Removed debug message when opening Options tab for first time.
- Fixed conflict with NGUI RealTime class.
- Fixed layout of pinned options when number of items exceeds screen width.

1.0.0
----------

Initial version.
