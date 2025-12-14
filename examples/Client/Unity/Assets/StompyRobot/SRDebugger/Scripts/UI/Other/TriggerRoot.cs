namespace SRDebugger.UI.Other
{
    using Controls;
    using SRF;
    using SRF.UI;
    using UnityEngine;
    using UnityEngine.Serialization;

    public class TriggerRoot : SRMonoBehaviourEx
    {
        [RequiredField] public Canvas Canvas;

        [RequiredField] public LongPressButton TapHoldButton;

        [RequiredField] public RectTransform TriggerTransform;

        [RequiredField] [FormerlySerializedAs("TriggerButton")] public MultiTapButton TripleTapButton;
    }
}
