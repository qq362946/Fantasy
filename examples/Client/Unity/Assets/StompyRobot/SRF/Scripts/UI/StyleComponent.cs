namespace SRF.UI
{
    using Internal;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UI;

    [ExecuteInEditMode]
    [AddComponentMenu(ComponentMenuPaths.StyleComponent)]
    public class StyleComponent : SRMonoBehaviour
    {
        private Style _activeStyle;
        private StyleRoot _cachedRoot;
        private Graphic _graphic;
        private bool _hasStarted;
        private Image _image;
        private Selectable _selectable;

        [SerializeField] [FormerlySerializedAs("StyleKey")] [HideInInspector] private string _styleKey;

        public bool IgnoreImage = false;

        public string StyleKey
        {
            get { return _styleKey; }
            set
            {
                _styleKey = value;
                Refresh(false);
            }
        }

        private void Start()
        {
            Refresh(true);
            _hasStarted = true;
        }

        private void OnEnable()
        {
            if (_hasStarted)
            {
                Refresh(false);
            }
        }

#if UNITY_EDITOR

        /// <summary>
        /// This method is not included in exported builds - don't worry about it showing up in the profiler.
        /// </summary>
        private void Update()
        {
            if (!Application.isPlaying)
            {
                ApplyStyle();
            }
        }

#endif

        public void Refresh(bool invalidateCache)
        {
            if (string.IsNullOrEmpty(StyleKey))
            {
                _activeStyle = null;
                return;
            }

            if (_cachedRoot == null || invalidateCache)
            {
                _cachedRoot = GetStyleRoot();
            }

            if (_cachedRoot == null)
            {
                Debug.LogWarning("[StyleComponent] No active StyleRoot object found in parents.", this);
                _activeStyle = null;
                return;
            }

            var s = _cachedRoot.GetStyle(StyleKey);

            if (s == null)
            {
                Debug.LogWarning("[StyleComponent] Style not found ({0})".Fmt(StyleKey), this);
                _activeStyle = null;
                return;
            }

            _activeStyle = s;
            ApplyStyle();
        }

        /// <summary>
        /// Find the nearest enable style root component in parents
        /// </summary>
        /// <returns></returns>
        private StyleRoot GetStyleRoot()
        {
            var t = CachedTransform;
            StyleRoot root;

            var i = 0;

            do
            {
                root = t.GetComponentInParent<StyleRoot>();

                if (root != null)
                {
                    t = root.transform.parent;
                }

                ++i;

                if (i > 100)
                {
                    Debug.LogWarning("Breaking Loop");
                    break;
                }
            } while ((root != null && !root.enabled) && t != null);

            return root;
        }

        private void ApplyStyle()
        {
            if (_activeStyle == null)
            {
                return;
            }

            if (_graphic == null)
            {
                _graphic = GetComponent<Graphic>();
            }

            if (_selectable == null)
            {
                _selectable = GetComponent<Selectable>();
            }

            if (_image == null)
            {
                _image = GetComponent<Image>();
            }

            if (!IgnoreImage && _image != null)
            {
                _image.sprite = _activeStyle.Image;
            }

            if (_selectable != null)
            {
                var colours = _selectable.colors;
                colours.normalColor = _activeStyle.NormalColor;
                colours.highlightedColor = _activeStyle.HoverColor;
                colours.pressedColor = _activeStyle.ActiveColor;
                colours.disabledColor = _activeStyle.DisabledColor;
                colours.colorMultiplier = 1f;

                _selectable.colors = colours;

                if (_graphic != null)
                {
                    _graphic.color = Color.white;
                }
            }
            else if (_graphic != null)
            {
                _graphic.color = _activeStyle.NormalColor;
            }
        }

        private void SRStyleDirty()
        {
            // If inactive, invalidate the cached root and return. Next time it is enabled
            // a new root will be found
            if (!CachedGameObject.activeInHierarchy)
            {
                _cachedRoot = null;
                return;
            }

            Refresh(true);
        }
    }
}
