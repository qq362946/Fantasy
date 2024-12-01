using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SRDebugger.UI.Other
{
    /// <summary>
    /// 
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class SafeAreaSizer : UIBehaviour, ILayoutElement
    {
        public RectTransform.Edge Edge
        {
            get { return _edge; }
            set
            {
                if (_edge != value)
                {
                    _edge = value;
                    LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
                }
            }
        }

        [SerializeField, FormerlySerializedAs("Edge")]
        private RectTransform.Edge _edge;
        public float Scale = 1f;

        private float _height;
        private float _width;


        public float preferredWidth
        {
            get
            {
                return _width;
            }
        }

        public float preferredHeight
        {
            get
            {
                return _height;
            }
        }

        public float minWidth
        {
            get
            {
                return _width;
            }
        }

        public float minHeight
        {
            get
            {
                return _height;
            }
        }

        public int layoutPriority
        {
            get { return 2; }
        }

        public float flexibleHeight
        {
            get { return -1; }
        }

        public float flexibleWidth
        {
            get { return -1; }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (Application.isPlaying)
            {
                Refresh();
            }
        }

        void Update()
        {
            _width = _height = 0;
        }
#endif

        void Refresh()
        {
            // Determine the distance in local coords
            Rect safeArea = Screen.safeArea;
            Canvas myCanvas = GetComponentInParent<Canvas>();
            if (myCanvas == null)
            {
                return;
            }

            RectTransform canvasRect = myCanvas.GetComponent<RectTransform>();

           // RectTransformUtility.PixelAdjustRect()
            _width = _height = 0;

            switch (_edge)
            {
                case RectTransform.Edge.Left:
                    _width = (safeArea.x / myCanvas.pixelRect.width) * canvasRect.rect.width;
                    break;
                case RectTransform.Edge.Right:
                    _width = (Screen.width - safeArea.width - safeArea.x) / myCanvas.pixelRect.width * canvasRect.rect.width;
                    break;
                case RectTransform.Edge.Top:
                    _height = (Screen.height - safeArea.height - safeArea.y) / myCanvas.pixelRect.height * canvasRect.rect.height;
                    break;
                case RectTransform.Edge.Bottom:
                    _height = (safeArea.y / myCanvas.pixelRect.height) * canvasRect.rect.height;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _width *= Scale;
            _height *= Scale;
        }

        public void CalculateLayoutInputHorizontal()
        {
            if (Application.isPlaying)
            {
                Refresh();
            }
        }

        public void CalculateLayoutInputVertical()
        {
            if (Application.isPlaying)
            {
                Refresh();
            }
        }
    }
}
