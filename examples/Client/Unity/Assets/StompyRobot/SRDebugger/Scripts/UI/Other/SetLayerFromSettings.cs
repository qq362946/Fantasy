namespace SRDebugger.UI.Other
{
    using SRF;

    public class SetLayerFromSettings : SRMonoBehaviour
    {
        private void Start()
        {
            gameObject.SetLayerRecursive(Settings.Instance.DebugLayer);
        }
    }
}
