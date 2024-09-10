#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR || UNITY_STANDALONE
#define COPY_TO_CLIPBOARD_SUPPORTED
#endif

using System;
using System.Runtime.CompilerServices;
using SRDebugger.Services;
using SRF.Service;
using UnityEngine;

[assembly: InternalsVisibleTo("StompyRobot.SRDebugger.Editor")]

public static class SRDebug
{
    public const string Version = SRDebugger.VersionInfo.Version;

    public static bool IsInitialized { get; private set; }

    public static IDebugService Instance
    {
        get { return SRServiceManager.GetService<IDebugService>(); }
    }

    /// <summary>
    /// Action to be invoked whenever the user selects "copy" in the console window.
    /// If null, copy/paste will not be available.
    /// </summary>
    public static Action<ConsoleEntry> CopyConsoleItemCallback = GetDefaultCopyConsoleItemCallback();

    public static void Init()
    {
        IsInitialized = true;

        SRServiceManager.RegisterAssembly<IDebugService>();

        // Initialize console if it hasn't already initialized.
        SRServiceManager.GetService<IConsoleService>();

        // Load the debug service
        SRServiceManager.GetService<IDebugService>();

#if UNITY_EDITOR
        SRDebugger.Scripts.Internal.SRScriptRecompileHelper.SetHasInitialized();
#endif
    }

    public static Action<ConsoleEntry> GetDefaultCopyConsoleItemCallback()
    {
#if COPY_TO_CLIPBOARD_SUPPORTED
        return entry =>
        {
            GUIUtility.systemCopyBuffer =
                string.Format("{0}: {1}\n\r\n\r{2}", entry.LogType, entry.Message, entry.StackTrace);
        };
#else
        return null;
#endif
    }
}
