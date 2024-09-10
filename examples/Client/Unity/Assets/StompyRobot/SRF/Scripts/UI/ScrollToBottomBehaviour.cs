namespace SRF.UI
{
    using System;
    using Internal;
    using UnityEngine;
    using UnityEngine.UI;

    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu(ComponentMenuPaths.ScrollToBottom)]
    public class ScrollToBottomBehaviour : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        private ScrollRect _scrollRect;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private bool _scrollToTop;
#pragma warning restore 649


        public void Start()
        {
            if (_scrollRect == null)
            {
                Debug.LogError("[ScrollToBottomBehaviour] ScrollRect not set");
                return;
            }

            if (_canvasGroup == null)
            {
                Debug.LogError("[ScrollToBottomBehaviour] CanvasGroup not set");
                return;
            }
            
            _scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
            Refresh();
        }

        void OnEnable()
        {
            Refresh();
        }

        public void Trigger()
        {
            if (_scrollToTop)
            {
                _scrollRect.normalizedPosition = new Vector2(0, 1);
            }
            else
            {
                _scrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }

        private void OnScrollRectValueChanged(Vector2 position)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_scrollRect == null)
                return;

            var position = _scrollRect.normalizedPosition;

            if (position.y < 0.001f || (_scrollToTop && position.y >= 0.999f))
            {
                SetVisible(false);
            }
            else
            {
                SetVisible(true);
            }
        }

        void SetVisible(bool truth)
        {
            if (truth)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
