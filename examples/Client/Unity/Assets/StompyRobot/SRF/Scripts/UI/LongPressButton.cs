namespace SRF.UI
{
    using Internal;
    using UnityEngine;

    [AddComponentMenu(ComponentMenuPaths.LongPressButton)]
    public class LongPressButton : UnityEngine.UI.Button
    {
        private bool _handled;
        [SerializeField] private ButtonClickedEvent _onLongPress = new ButtonClickedEvent();
        private bool _pressed;
        private float _pressedTime;
        public float LongPressDuration = 0.9f;

        public ButtonClickedEvent onLongPress
        {
            get { return _onLongPress; }
            set { _onLongPress = value; }
        }

        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            _pressed = false;
        }

        public override void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            if (eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left)
            {
                return;
            }

            _pressed = true;
            _handled = false;
            _pressedTime = Time.realtimeSinceStartup;
        }

        public override void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!_handled)
            {
                base.OnPointerUp(eventData);
            }

            _pressed = false;
        }

        public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!_handled)
            {
                base.OnPointerClick(eventData);
            }
        }

        private void Update()
        {
            if (!_pressed)
            {
                return;
            }

            if (Time.realtimeSinceStartup - _pressedTime >= LongPressDuration)
            {
                _pressed = false;
                _handled = true;
                onLongPress.Invoke();
            }
        }
    }
}
