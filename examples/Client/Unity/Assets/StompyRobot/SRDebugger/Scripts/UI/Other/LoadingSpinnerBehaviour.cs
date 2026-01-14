namespace SRDebugger.UI.Other
{
    using SRF;
    using UnityEngine;

    public class LoadingSpinnerBehaviour : SRMonoBehaviour
    {
        private float _dt;
        public int FrameCount = 12;
        public float SpinDuration = 0.8f;

        private void Update()
        {
            _dt += Time.unscaledDeltaTime;

            var localRotation = CachedTransform.localRotation.eulerAngles;
            var r = localRotation.z;

            var fTime = SpinDuration/FrameCount;
            var hasChanged = false;

            while (_dt > fTime)
            {
                r -= 360f/FrameCount;
                _dt -= fTime;
                hasChanged = true;
            }

            if (hasChanged)
            {
                CachedTransform.localRotation = Quaternion.Euler(localRotation.x, localRotation.y, r);
            }
        }
    }
}
