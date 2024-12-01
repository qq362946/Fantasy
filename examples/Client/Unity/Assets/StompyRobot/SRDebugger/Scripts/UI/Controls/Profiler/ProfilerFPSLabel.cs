namespace SRDebugger.UI
{
    using Services;
    using SRF;
    using UnityEngine;
    using UnityEngine.UI;

    public class ProfilerFPSLabel : SRMonoBehaviourEx
    {
        private float _nextUpdate;

        protected override void Update()
        {
            base.Update();

            if (Time.realtimeSinceStartup > _nextUpdate)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            _text.text = "FPS: {0:0.00}".Fmt(1f/_profilerService.AverageFrameTime);

            _nextUpdate = Time.realtimeSinceStartup + UpdateFrequency;
        }
#pragma warning disable 649

        [Import] private IProfilerService _profilerService;

        public float UpdateFrequency = 1f;

        [RequiredField] [SerializeField] private Text _text;

#pragma warning restore 649
    }
}
