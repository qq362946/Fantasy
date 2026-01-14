//#define ENABLE_LOGGING

namespace SRF.Service
{
    using System.Collections;
    using System.Diagnostics;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public abstract class SRSceneServiceBase<T, TImpl> : SRServiceBase<T>, IAsyncService
        where T : class
        where TImpl : Component
    {
        private TImpl _rootObject;

        /// <summary>
        /// Name of the scene this service's contents are within
        /// </summary>
        protected abstract string SceneName { get; }

        /// <summary>
        /// Scene contents root object
        /// </summary>
        protected TImpl RootObject
        {
            get { return _rootObject; }
        }

        public bool IsLoaded
        {
            get { return _rootObject != null; }
        }

        [Conditional("ENABLE_LOGGING")]
        private void Log(string msg, Object target)
        {
            //#if ENABLE_LOGGING
            Debug.Log(msg, target);
            //#endif
        }

        protected override void Start()
        {
            base.Start();

            StartCoroutine(LoadCoroutine());
        }

        protected override void OnDestroy()
        {
            if (IsLoaded)
            {
                Destroy(_rootObject.gameObject);
            }

            base.OnDestroy();
        }

        protected virtual void OnLoaded() {}

        private IEnumerator LoadCoroutine()
        {
            if (_rootObject != null)
            {
                yield break;
            }

            SRServiceManager.LoadingCount++;
#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            if (Application.loadedLevelName == SceneName)
#else
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneName).isLoaded)
#endif
            {
                Log("[Service] Already in service scene {0}. Searching for root object...".Fmt(SceneName), this);
            }
            else
            {
                Log("[Service] Loading scene ({0})".Fmt(SceneName), this);

#if UNITY_PRO_LICENSE || UNITY_5 || UNITY_5_3_OR_NEWER
#if UNITY_4_6 || UNITY_4_7  || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
                yield return Application.LoadLevelAdditiveAsync(SceneName);
#else
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
#endif
#else
                Application.LoadLevelAdditive(SceneName);
				yield return new WaitForEndOfFrame();
#endif

                Log("[Service] Scene loaded. Searching for root object...", this);
            }

            var go = GameObject.Find(SceneName);

            if (go == null)
            {
                goto Error;
            }

            var timpl = go.GetComponent<TImpl>();

            if (timpl == null)
            {
                goto Error;
            }

            _rootObject = timpl;
            _rootObject.transform.parent = CachedTransform;

            DontDestroyOnLoad(go);

            Debug.Log("[Service] Loading {0} complete. (Scene: {1})".Fmt(GetType().Name, SceneName), this);
            SRServiceManager.LoadingCount--;

            OnLoaded();

            yield break;

            Error:

            SRServiceManager.LoadingCount--;
            Debug.LogError("[Service] Root object ({0}) not found".Fmt(SceneName), this);
            enabled = false;
        }
    }
}
