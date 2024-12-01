namespace SRF.Components
{
    using System.Diagnostics;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Singleton MonoBehaviour class which automatically creates an instance if one does not already exist.
    /// </summary>
    public abstract class SRAutoSingleton<T> : SRMonoBehaviour where T : SRAutoSingleton<T>
    {
        private static T _instance;

        /// <summary>
        /// Get (or create) the instance of this Singleton
        /// </summary>
        public static T Instance
        {
            [DebuggerStepThrough]
            get
            {
                // Instance required for the first time, we look for it
                if (_instance == null && Application.isPlaying)
                {
#if UNITY_EDITOR
                    // Support reloading scripts after a recompile - static reference will be cleared, but we can find it again.
                    T autoSingleton = FindObjectOfType<T>();
                    if (autoSingleton != null)
                    {
                        _instance = autoSingleton;
                        return _instance;
                    }
#endif

                    var go = new GameObject("_" + typeof (T).Name);
                    go.AddComponent<T>(); // _instance set by Awake() constructor
                }
                return _instance;
            }
        }

        public static bool HasInstance
        {
            get { return _instance != null; }
        }

        // If no other monobehaviour request the instance in an awake function
        // executing before this one, no need to search the object.
        protected virtual void Awake()
        {
            if (_instance != null)
            {
                Debug.LogWarning("More than one singleton object of type {0} exists.".Fmt(typeof (T).Name));
                return;
            }

            _instance = (T) this;
        }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR
            // Restore reference after C# recompile.
            _instance = (T) this;
#endif
        }

        // Make sure the instance isn't referenced anymore when the user quit, just in case.
        private void OnApplicationQuit()
        {
            _instance = null;
        }
    }
}
