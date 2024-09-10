#if UNITY_2018_1_OR_NEWER

namespace SRDebugger.Profiler
{
    using System.Collections;
    using System.Diagnostics;
    using SRDebugger.Services;
    using SRF;
    using SRF.Service;
    using UnityEngine;
#if UNITY_2019_3_OR_NEWER
    using UnityEngine.Rendering;
#else
    using UnityEngine.Experimental.Rendering;
#endif

    public class SRPProfilerService : SRServiceBase<IProfilerService>, IProfilerService
    {
        public float AverageFrameTime { get; private set; }
        public float LastFrameTime { get; private set; }

        public CircularBuffer<ProfilerFrame> FrameBuffer
        {
            get { return _frameBuffer; }
        }

        private const int FrameBufferSize = 400;
        private readonly CircularBuffer<ProfilerFrame> _frameBuffer = new CircularBuffer<ProfilerFrame>(FrameBufferSize);

        private ProfilerLateUpdateListener _lateUpdateListener;

        // Time between first Update() and last LateUpdate()
        private double _updateDuration;

        // Time that render pipeline starts
        private double _renderStartTime;

        // Time between scripted render pipeline starts + EndOfFrame
        private double _renderDuration;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        protected override void Awake()
        {
            base.Awake();
            _lateUpdateListener = gameObject.AddComponent<ProfilerLateUpdateListener>();
            _lateUpdateListener.OnLateUpdate = OnLateUpdate;

            CachedGameObject.hideFlags = HideFlags.NotEditable;
            CachedTransform.SetParent(Hierarchy.Get("SRDebugger"), true);

#if UNITY_2019_3_OR_NEWER
            RenderPipelineManager.beginFrameRendering += RenderPipelineOnBeginFrameRendering;
#else
            RenderPipeline.beginFrameRendering += RenderPipelineOnBeginFrameRendering;
#endif

            StartCoroutine(EndOfFrameCoroutine());
        }

        protected override void Update()
        {
            base.Update();

            EndFrame();

            // Set the frame time for the last frame
            if (FrameBuffer.Count > 0)
            {
                var frame = FrameBuffer.Back();
                frame.FrameTime = Time.unscaledDeltaTime;
                FrameBuffer[FrameBuffer.Count - 1] = frame;
            }

            LastFrameTime = Time.unscaledDeltaTime;

            var frameCount = Mathf.Min(20, FrameBuffer.Count);

            var f = 0d;
            for (var i = 0; i < frameCount; i++)
            {
                f += FrameBuffer[FrameBuffer.Count - 1 - i].FrameTime;
            }

            AverageFrameTime = (float)f / frameCount;

            _stopwatch.Start();
        }

        IEnumerator EndOfFrameCoroutine()
        {
			var endOfFrame = new WaitForEndOfFrame();
			
            while (true)
            {
                yield return endOfFrame;
                _renderDuration = _stopwatch.Elapsed.TotalSeconds - _renderStartTime;
            }
        }

        protected void PushFrame(double totalTime, double updateTime, double renderTime)
        {
            _frameBuffer.PushBack(new ProfilerFrame
            {
                OtherTime = totalTime - updateTime - renderTime,
                UpdateTime = updateTime,
                RenderTime = renderTime
            });
        }

        private void OnLateUpdate()
        {
            _updateDuration = _stopwatch.Elapsed.TotalSeconds;
        }

#if UNITY_2019_3_OR_NEWER
        private void RenderPipelineOnBeginFrameRendering(ScriptableRenderContext context, Camera[] cameras)
#else
        private void RenderPipelineOnBeginFrameRendering(Camera[] obj)
#endif
        {
            _renderStartTime = _stopwatch.Elapsed.TotalSeconds;
        }

        private void EndFrame()
        {
            if (_stopwatch.IsRunning)
            {
                PushFrame(_stopwatch.Elapsed.TotalSeconds, _updateDuration, _renderDuration);

                _stopwatch.Reset();
                _stopwatch.Start();
            }

            _updateDuration = _renderDuration = 0;
        }
    }
}
#endif