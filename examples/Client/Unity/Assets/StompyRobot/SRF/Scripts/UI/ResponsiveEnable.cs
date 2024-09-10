namespace SRF.UI
{
    using System;
    using Internal;
    using UnityEngine;

    [ExecuteInEditMode]
    [RequireComponent(typeof (RectTransform))]
    [AddComponentMenu(ComponentMenuPaths.ResponsiveEnable)]
    public class ResponsiveEnable : ResponsiveBase
    {
        public enum Modes
        {
            EnableAbove,
            EnableBelow
        }

        public Entry[] Entries = new Entry[0];

        protected override void Refresh()
        {
            var rect = RectTransform.rect;

            for (var i = 0; i < Entries.Length; i++)
            {
                var e = Entries[i];

                var enable = true;

                switch (e.Mode)
                {
                    case Modes.EnableAbove:
                    {
                        if (e.ThresholdHeight > 0)
                        {
                            enable = rect.height >= e.ThresholdHeight && enable;
                        }

                        if (e.ThresholdWidth > 0)
                        {
                            enable = rect.width >= e.ThresholdWidth && enable;
                        }

                        break;
                    }
                    case Modes.EnableBelow:
                    {
                        if (e.ThresholdHeight > 0)
                        {
                            enable = rect.height <= e.ThresholdHeight && enable;
                        }

                        if (e.ThresholdWidth > 0)
                        {
                            enable = rect.width <= e.ThresholdWidth && enable;
                        }

                        break;
                    }
                    default:
                        throw new IndexOutOfRangeException();
                }

                if (e.GameObjects != null)
                {
                    for (var j = 0; j < e.GameObjects.Length; j++)
                    {
                        var go = e.GameObjects[j];

                        if (go != null)
                        {
                            go.SetActive(enable);
                        }
                    }
                }

                if (e.Components != null)
                {
                    for (var j = 0; j < e.Components.Length; j++)
                    {
                        var go = e.Components[j];

                        if (go != null)
                        {
                            go.enabled = enable;
                        }
                    }
                }
            }
        }

        [Serializable]
        public struct Entry
        {
            public Behaviour[] Components;
            public GameObject[] GameObjects;
            public Modes Mode;
            public float ThresholdHeight;
            public float ThresholdWidth;
        }
    }
}
