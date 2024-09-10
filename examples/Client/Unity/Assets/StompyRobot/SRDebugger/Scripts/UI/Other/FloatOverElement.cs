using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SRDebugger.UI.Other
{
    [RequireComponent(typeof(RectTransform)), ExecuteAlways]
    public class FloatOverElement : UIBehaviour, ILayoutSelfController
    {
        public RectTransform CopyFrom;

        private DrivenRectTransformTracker _tracker;

        void Copy()
        {
            if (CopyFrom == null) return;

            _tracker.Clear();

            var r = GetComponent<RectTransform>();
            r.anchorMin = CopyFrom.anchorMin;
            r.anchorMax = CopyFrom.anchorMax;
            r.anchoredPosition = CopyFrom.anchoredPosition;
            r.offsetMin = CopyFrom.offsetMin;
            r.offsetMax = CopyFrom.offsetMax;
            r.sizeDelta = CopyFrom.sizeDelta;
            r.localScale = CopyFrom.localScale;
            r.pivot = CopyFrom.pivot;

            _tracker.Add(this, r, DrivenTransformProperties.All);
        }

        public void SetLayoutHorizontal()
        {
            Copy();
        }

        public void SetLayoutVertical()
        {
            Copy();
        }

    }
}