namespace SRF.UI
{
    using UnityEngine;

    [ExecuteInEditMode]
    [RequireComponent(typeof (RectTransform))]
    public abstract class ResponsiveBase : SRMonoBehaviour
    {
        private bool _queueRefresh;

        protected RectTransform RectTransform
        {
            get { return (RectTransform) CachedTransform; }
        }

        protected void OnEnable()
        {
            _queueRefresh = true;
        }

        protected void OnRectTransformDimensionsChange()
        {
            _queueRefresh = true;
        }

        protected void Update()
        {
#if UNITY_EDITOR

            // Refresh whenever we can in the editor, since layout has quirky update behaviour
            // when not in play mode
            if (!Application.isPlaying)
            {
                Refresh();
                return;
            }

#endif

            if (_queueRefresh)
            {
                Refresh();
                _queueRefresh = false;
            }
        }

        protected abstract void Refresh();

        [ContextMenu("Refresh")]
        private void DoRefresh()
        {
            Refresh();
        }
    }
}
