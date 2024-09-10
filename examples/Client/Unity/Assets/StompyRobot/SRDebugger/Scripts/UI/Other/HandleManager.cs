namespace SRDebugger.UI.Other
{
    using SRF;
    using UnityEngine;

    /// <summary>
    /// Handles enabling/disabling handle objects for different anchoring modes
    /// </summary>
    public class HandleManager : SRMonoBehaviour
    {
        private bool _hasSet;
        public GameObject BottomHandle;
        public GameObject BottomLeftHandle;
        public GameObject BottomRightHandle;
        public PinAlignment DefaultAlignment;
        public GameObject LeftHandle;
        public GameObject RightHandle;
        public GameObject TopHandle;
        public GameObject TopLeftHandle;
        public GameObject TopRightHandle;

        private void Start()
        {
            if (!_hasSet)
            {
                SetAlignment(DefaultAlignment);
            }
        }

        public void SetAlignment(PinAlignment alignment)
        {
            _hasSet = true;

            switch (alignment)
            {
                case PinAlignment.TopLeft:
                case PinAlignment.TopRight:
                    SetActive(BottomHandle, true);
                    SetActive(TopHandle, false);
                    SetActive(TopLeftHandle, false);
                    SetActive(TopRightHandle, false);
                    break;

                case PinAlignment.BottomLeft:
                case PinAlignment.BottomRight:
                    SetActive(BottomHandle, false);
                    SetActive(TopHandle, true);
                    SetActive(BottomLeftHandle, false);
                    SetActive(BottomRightHandle, false);
                    break;
            }

            switch (alignment)
            {
                case PinAlignment.TopLeft:
                case PinAlignment.BottomLeft:
                    SetActive(LeftHandle, false);
                    SetActive(RightHandle, true);
                    SetActive(TopLeftHandle, false);
                    SetActive(BottomLeftHandle, false);
                    break;

                case PinAlignment.TopRight:
                case PinAlignment.BottomRight:
                    SetActive(LeftHandle, true);
                    SetActive(RightHandle, false);
                    SetActive(TopRightHandle, false);
                    SetActive(BottomRightHandle, false);
                    break;
            }

            switch (alignment)
            {
                case PinAlignment.TopLeft:
                    SetActive(BottomLeftHandle, false);
                    SetActive(BottomRightHandle, true);
                    break;

                case PinAlignment.TopRight:
                    SetActive(BottomLeftHandle, true);
                    SetActive(BottomRightHandle, false);
                    break;

                case PinAlignment.BottomLeft:
                    SetActive(TopLeftHandle, false);
                    SetActive(TopRightHandle, true);
                    break;

                case PinAlignment.BottomRight:
                    SetActive(TopLeftHandle, true);
                    SetActive(TopRightHandle, false);
                    break;
            }
        }

        private void SetActive(GameObject obj, bool active)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetActive(active);
        }
    }
}
