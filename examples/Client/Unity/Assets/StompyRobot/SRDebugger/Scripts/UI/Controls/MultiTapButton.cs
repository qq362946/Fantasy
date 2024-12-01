namespace SRDebugger.UI.Controls
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class MultiTapButton : UnityEngine.UI.Button
    {
        private float _lastTap;
        private int _tapCount;
        public int RequiredTapCount = 3;
        public float ResetTime = 0.5f;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (Time.unscaledTime - _lastTap > ResetTime)
            {
                _tapCount = 0;
            }

            _lastTap = Time.unscaledTime;
            _tapCount++;

            if (_tapCount == RequiredTapCount)
            {
                base.OnPointerClick(eventData);
                _tapCount = 0;
            }
        }
    }
}
