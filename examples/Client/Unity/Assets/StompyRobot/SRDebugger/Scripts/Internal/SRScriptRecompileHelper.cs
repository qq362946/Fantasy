#if UNITY_EDITOR
using UnityEngine;

namespace SRDebugger.Scripts.Internal
{
    /// <summary>
    /// Behaviour that supports SRDebugger reloading itself after a script recompile is detected.
    /// </summary>
    public class SRScriptRecompileHelper : MonoBehaviour
    {
        private static SRScriptRecompileHelper _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("SRDebugger Script Recompile Helper (Editor Only)");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
            go.AddComponent<SRScriptRecompileHelper>();
        }

        private bool _hasEnabled;
        private bool _srdebuggerHasInitialized;

        void OnEnable()
        {
            if(_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Don't take any action on the first OnEnable()
            if (!_hasEnabled)
            {
                _hasEnabled = true;
                return;
            }

            // Next OnEnable() will be due to script reload.
            AutoInitialize.OnLoadBeforeScene();

            if (_srdebuggerHasInitialized)
            {
                Debug.Log("[SRScriptRecompileHelper] Restoring SRDebugger after script reload.", this);
                SRDebug.Init();
            }
        }

        void OnApplicationQuit()
        {
            // Destroy this object when leaving play mode (otherwise it will linger and a new instance will be created next time play mode is entered).
            Destroy(gameObject);
        }

        public static void SetHasInitialized()
        {
            if (_instance == null)
            {
                Initialize();
            }
            _instance._srdebuggerHasInitialized = true;
        }
    }
}
#endif