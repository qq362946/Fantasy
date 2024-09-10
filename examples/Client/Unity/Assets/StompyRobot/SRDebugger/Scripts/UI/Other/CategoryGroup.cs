namespace SRDebugger.UI.Other
{
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class CategoryGroup : SRMonoBehaviourEx
    {
        [RequiredField] public RectTransform Container;
        [RequiredField] public Text Header;
        [RequiredField] public GameObject Background;
        [RequiredField] public Toggle SelectionToggle;

        public GameObject[] EnabledDuringSelectionMode = new GameObject[0];

        private bool _selectionModeEnabled = true;

        public bool IsSelected
        {
            get
            {
                return SelectionToggle.isOn;
            }
            set
            {
                SelectionToggle.isOn = value;

                if (SelectionToggle.graphic != null)
                {
                    SelectionToggle.graphic.CrossFadeAlpha(value ? _selectionModeEnabled ? 1.0f : 0.2f : 0f, 0, true);
                }
            }
        }

        public bool SelectionModeEnabled
        {
            get { return _selectionModeEnabled; }

            set
            {
                if (value == _selectionModeEnabled)
                {
                    return;
                }

                _selectionModeEnabled = value;

                for (var i = 0; i < EnabledDuringSelectionMode.Length; i++)
                {
                    EnabledDuringSelectionMode[i].SetActive(_selectionModeEnabled);
                }
            }
        }

    }
}
