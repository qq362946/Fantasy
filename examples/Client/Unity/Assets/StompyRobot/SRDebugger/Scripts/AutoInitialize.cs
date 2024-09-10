using SRDebugger.Services;
using SRF.Service;

namespace SRDebugger
{
    using UnityEngine;

    public static class AutoInitialize
    {
#if UNITY_2018
        private const RuntimeInitializeLoadType InitializeLoadType = RuntimeInitializeLoadType.BeforeSceneLoad;
#else
        private const RuntimeInitializeLoadType InitializeLoadType = RuntimeInitializeLoadType.SubsystemRegistration;
#endif

        /// <summary>
        /// Initialize the console service before the scene has loaded to catch more of the initialization log.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(InitializeLoadType)]
        public static void OnLoadBeforeScene()
        {
            // Populate service manager with types from SRDebugger assembly (asmdef)
            SRServiceManager.RegisterAssembly<IDebugService>();

            if (Settings.Instance.IsEnabled)
            {
                // Initialize console if it hasn't already initialized.
                SRServiceManager.GetService<IConsoleService>();
            }
        }

        /// <summary>
        /// Initialize SRDebugger after the scene has loaded.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void OnLoad()
        {
            if (Settings.Instance.IsEnabled)
            {
                SRDebug.Init();
            }
        }
    }
}