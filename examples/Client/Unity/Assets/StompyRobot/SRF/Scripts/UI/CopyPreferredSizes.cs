using System;

namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Copies the preferred size of another layout element (useful for a parent object basing its sizing from a child
    /// element).
    /// This does have very quirky behaviour, though.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.CopyPreferredSizes)]
    public class CopyPreferredSizes : LayoutElement
    {
        public enum Operations
        {
            Max,
            Min
        }

        [Serializable]
        public class CopySource
        {
            public RectTransform Rect;

            public float PaddingHeight;
            public float PaddingWidth;
        }

        public CopySource[] CopySources;
        public Operations Operation;
    

        public override float preferredWidth
        {
            get
            {
                if (CopySources == null || CopySources.Length == 0 || !IsActive())
                {
                    return -1f;
                }

                float current = Operation == Operations.Max ? float.MinValue : float.MaxValue;

                for (var i = 0; i < CopySources.Length; i++)
                {
                    if (CopySources[i].Rect == null)
                        continue;

                    float width = LayoutUtility.GetPreferredWidth(CopySources[i].Rect) + CopySources[i].PaddingWidth;
                    if (Operation == Operations.Max && width > current)
                        current = width;
                    else if (Operation == Operations.Min && width < current)
                        current = width;
                }

                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (Operation == Operations.Max && current == float.MinValue) return -1;
                if (Operation == Operations.Min && current == float.MaxValue) return -1;
                // ReSharper restore CompareOfFloatsByEqualityOperator

                return current;
            }
        }

        public override float preferredHeight
        {
            get
            {
                if (CopySources == null || CopySources.Length == 0 || !IsActive())
                {
                    return -1f;
                }

                float current = Operation == Operations.Max ? float.MinValue : float.MaxValue;

                for (var i = 0; i < CopySources.Length; i++)
                {
                    if (CopySources[i].Rect == null)
                        continue;

                    float height = LayoutUtility.GetPreferredHeight(CopySources[i].Rect) + CopySources[i].PaddingHeight;
                    if (Operation == Operations.Max && height > current)
                        current = height;
                    else if (Operation == Operations.Min && height < current)
                        current = height;
                }

                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (Operation == Operations.Max && current == float.MinValue) return -1;
                if (Operation == Operations.Min && current == float.MaxValue) return -1;
                // ReSharper restore CompareOfFloatsByEqualityOperator

                return current;
            }
        }

        public override int layoutPriority
        {
            get { return 2; }
        }
    }
}