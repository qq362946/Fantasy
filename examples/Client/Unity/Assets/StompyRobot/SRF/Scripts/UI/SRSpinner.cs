namespace SRF.UI
{
    using System;
    using Internal;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [AddComponentMenu(ComponentMenuPaths.SRSpinner)]
    public class SRSpinner : Selectable, IDragHandler, IBeginDragHandler
    {
        private float _dragDelta;

        [SerializeField] private SpinEvent _onSpinDecrement = new SpinEvent();

        [SerializeField] private SpinEvent _onSpinIncrement = new SpinEvent();

        /// <summary>
        /// Number of units a drag must accumulate to trigger a change
        /// </summary>
        public float DragThreshold = 20f;

        public SpinEvent OnSpinIncrement
        {
            get { return _onSpinIncrement; }
            set { _onSpinIncrement = value; }
        }

        public SpinEvent OnSpinDecrement
        {
            get { return _onSpinDecrement; }
            set { _onSpinDecrement = value; }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragDelta = 0;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!interactable)
            {
                return;
            }

            _dragDelta += eventData.delta.x;

            if (Mathf.Abs(_dragDelta) > DragThreshold)
            {
                var direction = Mathf.Sign(_dragDelta);
                var quantity = Mathf.FloorToInt(Mathf.Abs(_dragDelta)/DragThreshold);

                if (direction > 0)
                {
                    OnIncrement(quantity);
                }
                else
                {
                    OnDecrement(quantity);
                }

                _dragDelta -= quantity*DragThreshold*direction;
            }
        }

        private void OnIncrement(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                OnSpinIncrement.Invoke();
            }
        }

        private void OnDecrement(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                OnSpinDecrement.Invoke();
            }
        }

        [Serializable]
        public class SpinEvent : UnityEvent {}
    }
}
