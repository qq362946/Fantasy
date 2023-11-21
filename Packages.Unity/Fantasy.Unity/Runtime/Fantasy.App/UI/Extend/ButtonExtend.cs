using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Fantasy
{
    public class ButtonExtend : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _rectTransform;

        [FormerlySerializedAs("PointerDown")] 
        [SerializeField, Header("Press Zoom Percentage"),]
        private float pointerDown = 1f;
        public void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _rectTransform.localScale = Vector3.one * pointerDown;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _rectTransform.localScale = Vector3.one;
        }

        private void OnDisable()
        {
            _rectTransform.localScale = Vector3.one;
        }
    }
}