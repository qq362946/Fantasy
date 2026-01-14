namespace SRDebugger.Services.Implementation
{
    using SRF;
    using SRF.Service;
    using UnityEngine;

    [Service(typeof (IDebugCameraService))]
    public class DebugCameraServiceImpl : IDebugCameraService
    {
        private Camera _debugCamera;

        public DebugCameraServiceImpl()
        {
            if (Settings.Instance.UseDebugCamera)
            {
                _debugCamera = new GameObject("SRDebugCamera").AddComponent<Camera>();

                _debugCamera.cullingMask = 1 << Settings.Instance.DebugLayer;
                _debugCamera.depth = Settings.Instance.DebugCameraDepth;

                _debugCamera.clearFlags = CameraClearFlags.Depth;

                _debugCamera.transform.SetParent(Hierarchy.Get("SRDebugger"));
            }
        }

        public Camera Camera
        {
            get { return _debugCamera; }
        }
    }
}
