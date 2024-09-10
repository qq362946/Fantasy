namespace SRDebugger.UI.Other
{
    using SRF;
    using UnityEngine.UI;

    public class VersionTextBehaviour : SRMonoBehaviourEx
    {
        public string Format = "SRDebugger {0}";

        [RequiredField] public Text Text;

        protected override void Start()
        {
            base.Start();

            Text.text = string.Format(Format, SRDebug.Version);
        }
    }
}
