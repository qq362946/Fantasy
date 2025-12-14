namespace SRDebugger.UI.Controls
{
    using SRF;
    using SRF.UI;
    using UnityEngine;
    using UnityEngine.UI;

    public class SRTabButton : SRMonoBehaviourEx
    {
        [RequiredField] public Behaviour ActiveToggle;

        [RequiredField] public UnityEngine.UI.Button Button;

        [RequiredField] public RectTransform ExtraContentContainer;

        [RequiredField] public StyleComponent IconStyleComponent;

        [RequiredField] public Text TitleText;

        public bool IsActive
        {
            get { return ActiveToggle.enabled; }
            set { ActiveToggle.enabled = value; }
        }
    }
}
