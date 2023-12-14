using System;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TopDownShooter
{
    public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public bool IsTouched;
        public Image JoystickOutCircle;
        public Image Stick;

        public float Horizontal;
        public float Vertical;
        public Vector3 InputDirection;

        private Vector2 _position;

        private void Start()
        {
            if (!JoystickOutCircle) Debug.LogError("Missing JoystickOutCircle image");
            if (!Stick) Debug.LogError("Missing Stick image");
            InputDirection = Vector3.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            //Get InputDirection
            RectTransformUtility.ScreenPointToLocalPointInRectangle
            (JoystickOutCircle.rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out _position);

            var sizeDelta = JoystickOutCircle.rectTransform.sizeDelta;
            _position.x = (_position.x / sizeDelta.x);
            _position.y = (_position.y / sizeDelta.y);

            Horizontal = (Math.Abs(JoystickOutCircle.rectTransform.pivot.x - 1f) < 0.01f)
                ? _position.x * 2 + 1
                : _position.x * 2 - 1;
            Vertical = (Math.Abs(JoystickOutCircle.rectTransform.pivot.y - 1f) < 0.01f)
                ? _position.y * 2 + 1
                : _position.y * 2 - 1;
            InputDirection = new Vector3(Horizontal, Vertical, 0);
            InputDirection = (InputDirection.magnitude > 1) ? InputDirection.normalized : InputDirection;

            //define the area in which joystick can move around
            Stick.rectTransform.anchoredPosition = new Vector3(
                InputDirection.x * (JoystickOutCircle.rectTransform.sizeDelta.x / 3)
                , InputDirection.y * (JoystickOutCircle.rectTransform.sizeDelta.y) / 3);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
            IsTouched = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            InputDirection = Vector3.zero;
            Horizontal = 0;
            Vertical = 0;
            Stick.rectTransform.anchoredPosition = Vector3.zero;
            IsTouched = false;
        }
    }
}