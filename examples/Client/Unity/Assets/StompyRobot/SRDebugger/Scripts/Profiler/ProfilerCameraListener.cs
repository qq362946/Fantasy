namespace SRDebugger.Profiler
{
    using System;
    using System.Diagnostics;
    using UnityEngine;

    [RequireComponent(typeof (Camera))]
    public class ProfilerCameraListener : MonoBehaviour
    {
        private Camera _camera;
        private Stopwatch _stopwatch;
        public Action<ProfilerCameraListener, double> RenderDurationCallback;

        protected Stopwatch Stopwatch
        {
            get
            {
                if (_stopwatch == null)
                {
                    _stopwatch = new Stopwatch();
                }

                return _stopwatch;
            }
        }

        public Camera Camera
        {
            get
            {
                if (_camera == null)
                {
                    _camera = GetComponent<Camera>();
                }

                return _camera;
            }
        }
        
        private void OnPreCull()
        {
            UnityEngine.Debug.Log("OnPreCull");

            if (!isActiveAndEnabled)
            {
                return;
            }

            Stopwatch.Start();
        }

        private void OnPostRender()
        {
            UnityEngine.Debug.Log("OnPostRender");
            if (!isActiveAndEnabled)
            {
                return;
            }

            var renderTime = _stopwatch.Elapsed.TotalSeconds;

            Stopwatch.Stop();
            Stopwatch.Reset();

            if (RenderDurationCallback == null)
            {
                Destroy(this);
                return;
            }

            RenderDurationCallback(this, renderTime);
        }
    }
}
