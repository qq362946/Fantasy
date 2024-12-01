namespace SRDebugger.UI.Other
{
    using SRF;
    using SRF.UI;
    using UnityEngine;

    [RequireComponent(typeof (StyleComponent))]
    public class DebugPanelBackgroundBehaviour : SRMonoBehaviour
    {
        private StyleComponent _styleComponent;
        public string TransparentStyleKey = "";

        [SerializeField]
        private StyleSheet _styleSheet;

        private void Awake()
        {
            _styleComponent = GetComponent<StyleComponent>();

            if (Settings.Instance.EnableBackgroundTransparency)
            {
                // Update transparent style to have the transparency set in the settings menu.
                Style style = _styleSheet.GetStyle(TransparentStyleKey);
                Color c = style.NormalColor;
                c.a = Settings.Instance.BackgroundTransparency;
                style.NormalColor = c;

                _styleComponent.StyleKey = TransparentStyleKey;
            }
        }
    }
}
