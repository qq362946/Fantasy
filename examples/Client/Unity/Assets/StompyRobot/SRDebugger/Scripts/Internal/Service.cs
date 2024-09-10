namespace SRDebugger.Internal
{
    using Services;
    using SRF.Service;

    public static class Service
    {
        private static IConsoleService _consoleService;
        private static IDebugPanelService _debugPanelService;
        private static IDebugTriggerService _debugTriggerService;
        private static IPinnedUIService _pinnedUiService;
        private static IDebugCameraService _debugCameraService;
        private static IOptionsService _optionsService;
        private static IDockConsoleService _dockConsoleService;

#if UNITY_EDITOR && ((!UNITY_2017 && !UNITY_2018 && !UNITY_2019) || UNITY_2019_3_OR_NEWER)
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void RuntimeInitialize()
        {
            // Clear service references at startup in case of "enter play mode without domain reload"
            _consoleService = null;
            _debugPanelService = null;
            _debugTriggerService = null;
            _pinnedUiService = null;
            _debugCameraService = null;
            _optionsService = null;
            _dockConsoleService = null;
        }
#endif

        public static IConsoleService Console
        {
            get
            {
                if (_consoleService == null)
                {
                    _consoleService = SRServiceManager.GetService<IConsoleService>();
                }

                return _consoleService;
            }
        }

        public static IDockConsoleService DockConsole
        {
            get
            {
                if (_dockConsoleService == null)
                {
                    _dockConsoleService = SRServiceManager.GetService<IDockConsoleService>();
                }

                return _dockConsoleService;
            }
        }

        public static IDebugPanelService Panel
        {
            get
            {
                if (_debugPanelService == null)
                {
                    _debugPanelService = SRServiceManager.GetService<IDebugPanelService>();
                }

                return _debugPanelService;
            }
        }

        public static IDebugTriggerService Trigger
        {
            get
            {
                if (_debugTriggerService == null)
                {
                    _debugTriggerService = SRServiceManager.GetService<IDebugTriggerService>();
                }

                return _debugTriggerService;
            }
        }

        public static IPinnedUIService PinnedUI
        {
            get
            {
                if (_pinnedUiService == null)
                {
                    _pinnedUiService = SRServiceManager.GetService<IPinnedUIService>();
                }

                return _pinnedUiService;
            }
        }

        public static IDebugCameraService DebugCamera
        {
            get
            {
                if (_debugCameraService == null)
                {
                    _debugCameraService = SRServiceManager.GetService<IDebugCameraService>();
                }

                return _debugCameraService;
            }
        }

        public static IOptionsService Options
        {
            get
            {
                if (_optionsService == null)
                {
                    _optionsService = SRServiceManager.GetService<IOptionsService>();
                }

                return _optionsService;
            }
        }
    }
}
