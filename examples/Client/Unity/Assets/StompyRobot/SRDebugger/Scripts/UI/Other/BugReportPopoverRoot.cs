namespace SRDebugger.UI.Other
{
    using SRF;
    using UnityEngine;

    public class BugReportPopoverRoot : SRMonoBehaviourEx
    {
        [RequiredField] public CanvasGroup CanvasGroup;

        [RequiredField] public RectTransform Container;
    }
}
