namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [RequireComponent(typeof (RectTransform))]
    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.ContentFitText)]
    public class ContentFitText : UIBehaviour, ILayoutElement
    {
        public SRText CopySource;
        public Vector2 Padding;

        public float minWidth
        {
            get
            {
                if (CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetMinWidth(CopySource.rectTransform) + Padding.x;
            }
        }

        public float preferredWidth
        {
            get
            {
                if (CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetPreferredWidth(CopySource.rectTransform) + Padding.x;
            }
        }

        public float flexibleWidth
        {
            get
            {
                if (CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetFlexibleWidth(CopySource.rectTransform);
            }
        }

        public float minHeight
        {
            get
            {
                if (CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetFlexibleHeight(CopySource.rectTransform) + Padding.y;
            }
        }

        public float preferredHeight
        {
            get
            {
                if (CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetPreferredHeight(CopySource.rectTransform) + Padding.y;
            }
        }

        public float flexibleHeight
        {
            get
            {
                if (CopySource == null)
                {
                    return -1f;
                }
                return LayoutUtility.GetFlexibleHeight(CopySource.rectTransform);
            }
        }

        public int layoutPriority
        {
            get { return 0; }
        }

        public void CalculateLayoutInputHorizontal()
        {
            CopySource.CalculateLayoutInputHorizontal();
        }

        public void CalculateLayoutInputVertical()
        {
            CopySource.CalculateLayoutInputVertical();
        }

        protected override void OnEnable()
        {
            SetDirty();
            CopySource.LayoutDirty += CopySourceOnLayoutDirty;
        }

        private void CopySourceOnLayoutDirty(SRText srText)
        {
            SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            SetDirty();
        }

        protected override void OnDisable()
        {
            CopySource.LayoutDirty -= CopySourceOnLayoutDirty;
            SetDirty();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            SetDirty();
        }

        protected void SetDirty()
        {
            if (!IsActive())
            {
                return;
            }

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        }
    }
}
