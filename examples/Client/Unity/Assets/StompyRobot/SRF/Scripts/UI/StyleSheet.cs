namespace SRF.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Helpers;
    using UnityEngine;

    [Serializable]
    public class Style
    {
        public Color ActiveColor = Color.white;
        public Color DisabledColor = Color.white;
        public Color HoverColor = Color.white;
        public Sprite Image;
        public Color NormalColor = Color.white;

        public Style Copy()
        {
            var s = new Style();
            s.CopyFrom(this);
            return s;
        }

        public void CopyFrom(Style style)
        {
            Image = style.Image;
            NormalColor = style.NormalColor;
            HoverColor = style.HoverColor;
            ActiveColor = style.ActiveColor;
            DisabledColor = style.DisabledColor;
        }
    }

    [Serializable]
    public class StyleSheet : ScriptableObject
    {
        [SerializeField] private List<string> _keys = new List<string>();

        [SerializeField] private List<Style> _styles = new List<Style>();

        [SerializeField] public StyleSheet Parent;

        public Style GetStyle(string key, bool searchParent = true)
        {
            var i = _keys.IndexOf(key);

            if (i < 0)
            {
                if (searchParent && Parent != null)
                {
                    return Parent.GetStyle(key);
                }

                return null;
            }

            return _styles[i];
        }

#if UNITY_EDITOR

        public int AddStyle(string key)
        {
            if (_keys.Contains(key))
            {
                throw new ArgumentException("key already exists");
            }

            _keys.Add(key);
            _styles.Add(new Style());

            return _keys.Count - 1;
        }

        public bool DeleteStyle(string key)
        {
            var i = _keys.IndexOf(key);

            if (i < 0)
            {
                return false;
            }

            _keys.RemoveAt(i);
            _styles.RemoveAt(i);

            return true;
        }

        public IEnumerable<string> GetStyleKeys(bool includeParent = true)
        {
            if (Parent != null && includeParent)
            {
                return _keys.Union(Parent.GetStyleKeys());
            }

            return _keys.ToList();
        }

        [UnityEditor.MenuItem("Assets/Create/SRF/Style Sheet")]
        public static void CreateStyleSheet()
        {
            var o = AssetUtil.CreateAsset<StyleSheet>();
            AssetUtil.SelectAssetInProjectView(o);
        }

#endif
    }
}
