namespace SRF.UI
{
    using System;
    using Internal;
    using UnityEngine;
    using UnityEngine.UI;

    [ExecuteInEditMode]
    [RequireComponent(typeof (RectTransform))]
    [AddComponentMenu(ComponentMenuPaths.ResponsiveResize)]
    public class ResponsiveResize : ResponsiveBase
    {
        public Element[] Elements = new Element[0];

        protected override void Refresh()
        {
            var rect = RectTransform.rect;

            for (var i = 0; i < Elements.Length; i++)
            {
                var e = Elements[i];

                if (e.Target == null)
                {
                    continue;
                }

                var maxWidth = float.MinValue;
                var selectedWidth = -1f;

                for (var j = 0; j < e.SizeDefinitions.Length; j++)
                {
                    var d = e.SizeDefinitions[j];

                    // If the threshold applies
                    if (d.ThresholdWidth <= rect.width)
                    {
                        // And it is the largest width so far
                        if (d.ThresholdWidth > maxWidth)
                        {
                            // Set it as active
                            maxWidth = d.ThresholdWidth;
                            selectedWidth = d.ElementWidth;
                        }
                    }
                }

                if (selectedWidth > 0)
                {
                    e.Target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, selectedWidth);

                    var le = e.Target.GetComponent<LayoutElement>();

                    if (le != null)
                    {
                        le.preferredWidth = selectedWidth;
                    }
                }
            }
        }

        [Serializable]
        public struct Element
        {
            public SizeDefinition[] SizeDefinitions;
            public RectTransform Target;

            [Serializable]
            public struct SizeDefinition
            {
                [Tooltip("Width to apply when over the threshold width")] public float ElementWidth;

                [Tooltip("Threshold over which this width will take effect")] public float ThresholdWidth;
            }
        }
    }
}
