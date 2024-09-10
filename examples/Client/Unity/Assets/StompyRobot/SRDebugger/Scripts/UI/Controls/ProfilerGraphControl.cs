//#define LOGGING

#if UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1
#define LEGACY_UI
#endif

namespace SRDebugger.UI.Controls
{
    using System.Collections.Generic;
    using Services;
    using SRF;
    using SRF.Service;
    using UnityEngine;
    using UnityEngine.UI;

    [ExecuteInEditMode]
    [RequireComponent(typeof (RectTransform))]
    [RequireComponent(typeof (CanvasRenderer))]
    public class ProfilerGraphControl : Graphic
    {
        public enum VerticalAlignments
        {
            Top,
            Bottom
        }

        public VerticalAlignments VerticalAlignment = VerticalAlignments.Bottom;

        private static readonly float[] ScaleSteps =
        {
            1f/200f,
            1f/160f,
            1f/120f,
            1f/100f,
            1f/60f,
            1f/30f,
            1f/20f,
            1f/12f,
            1f/6f
        };

        /// <summary>
        /// Resize the y-axis to fit the nearest useful fps value
        /// </summary>
        public bool FloatingScale;

        /// <summary>
        /// If not using FloatingScale, use the target FPS set by Application.targetFrameRate for TargetFps
        /// </summary>
        public bool TargetFpsUseApplication;

        /// <summary>
        /// Toggle drawing of the various axes
        /// </summary>
        public bool DrawAxes = true;

        /// <summary>
        /// If FloatingScale is disabled, use this value to determine y-axis
        /// </summary>
        public int TargetFps = 60;

        public bool Clip = true;

        public const float DataPointMargin = 2f;
        public const float DataPointVerticalMargin = 2f;

        public const float DataPointWidth = 4f;

        public int VerticalPadding = 10;

        public const int LineCount = 3;

        public Color[] LineColours = new Color[0];

        private IProfilerService _profilerService;

        private ProfilerGraphAxisLabel[] _axisLabels;

        private Rect _clipBounds;

#if LEGACY_UI
        private List<UIVertex> _vbo;
#else
        private readonly List<Vector3> _meshVertices = new List<Vector3>();
        private readonly List<Color32> _meshVertexColors = new List<Color32>();
        private readonly List<int> _meshTriangles = new List<int>();
#endif

        protected override void Awake()
        {
            base.Awake();
            _profilerService = SRServiceManager.GetService<IProfilerService>();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected void Update()
        {
            SetVerticesDirty();
        }

#if LEGACY_UI
        protected override void OnFillVBO(List<UIVertex> vbo)
#else
        [System.ObsoleteAttribute]
        protected override void OnPopulateMesh(Mesh m)
#endif
        {
#if LEGACY_UI
            _vbo = vbo;
#else
            _meshVertices.Clear();
            _meshVertexColors.Clear();
            _meshTriangles.Clear();
#endif

#if LOGGING
			if(!Application.isPlaying)
				Debug.Log("Draw");
#endif

            var graphWidth = rectTransform.rect.width;
            var graphHeight = rectTransform.rect.height;

            _clipBounds = new Rect(0, 0, graphWidth, graphHeight);

            var targetFps = TargetFps;

            if (Application.isPlaying && TargetFpsUseApplication && Application.targetFrameRate > 0)
            {
                targetFps = Application.targetFrameRate;
            }

            var maxValue = 1f/targetFps;

            // Holds the index of the nearest 'useful' FPS step
            var fpsStep = -1;

            var maxFrameTime = FloatingScale ? CalculateMaxFrameTime() : 1f/targetFps;

            if (FloatingScale)
            {
                for (var i = 0; i < ScaleSteps.Length; i++)
                {
                    var step = ScaleSteps[i];

                    if (maxFrameTime < step*1.1f)
                    {
                        maxValue = step;
                        fpsStep = i;
                        break;
                    }
                }

                // Fall back on the largest one
                if (fpsStep < 0)
                {
                    fpsStep = ScaleSteps.Length - 1;
                    maxValue = ScaleSteps[fpsStep];
                }
            }
            else
            {
                // Search for the next scale step after the user-provided step
                for (var i = 0; i < ScaleSteps.Length; i++)
                {
                    var step = ScaleSteps[i];

                    if (maxFrameTime > step)
                    {
                        fpsStep = i;
                    }
                }
            }

            var verticalScale = (graphHeight - (VerticalPadding*2))/maxValue;

            // Number of data points that can fit into the graph space
            var availableDataPoints = CalculateVisibleDataPointCount();

            // Reallocate vertex array if insufficient length (or not yet created)
            var sampleCount = GetFrameBufferCurrentSize();

            for (var i = 0; i < sampleCount; i++)
            {
                // Break loop if all visible data points have been drawn
                if (i >= availableDataPoints)
                {
                    break;
                }

                // When using right-alignment, read from the end of the profiler buffer
                var frame = GetFrame(sampleCount - i - 1);

                // Left-hand x coord
                var lx = graphWidth - DataPointWidth*i - DataPointWidth - graphWidth/2f;

                DrawDataPoint(lx, verticalScale, frame);
            }

            if (DrawAxes)
            {
                if (!FloatingScale)
                {
                    DrawAxis(maxValue, maxValue*verticalScale, GetAxisLabel(0));
                }

                var axisCount = 2;
                var j = 0;

                if (!FloatingScale)
                {
                    j++;
                }

                for (var i = fpsStep; i >= 0; --i)
                {
                    if (j >= axisCount)
                    {
                        break;
                    }

                    DrawAxis(ScaleSteps[i], ScaleSteps[i]*verticalScale, GetAxisLabel(j));
                    ++j;
                }
            }

#if !LEGACY_UI

            m.Clear();
            m.SetVertices(_meshVertices);
            m.SetColors(_meshVertexColors);
            m.SetTriangles(_meshTriangles, 0);

#endif
        }

        protected void DrawDataPoint(float xPosition, float verticalScale, ProfilerFrame frame)
        {
            // Right-hand x-coord
            var rx = Mathf.Min(_clipBounds.width/2f, xPosition + DataPointWidth - DataPointMargin);

            var currentLineHeight = 0f;

            for (var j = 0; j < LineCount; j++)
            {
                var lineIndex = j;

                var value = 0f;

                if (j == 0)
                {
                    value = (float) frame.UpdateTime;
                }
                else if (j == 1)
                {
                    value = (float) frame.RenderTime;
                }
                else if (j == 2)
                {
                    value = (float) frame.OtherTime;
                }

                value *= verticalScale;

                if (value.ApproxZero() || value - DataPointVerticalMargin*2f < 0f)
                {
                    continue;
                }

                // Lower y-coord
                var ly = currentLineHeight + DataPointVerticalMargin - rectTransform.rect.height/2f;

                if (VerticalAlignment == VerticalAlignments.Top)
                {
                    ly = rectTransform.rect.height/2f - currentLineHeight - DataPointVerticalMargin;
                }

                // Upper y-coord
                var uy = ly + value - DataPointVerticalMargin;

                if (VerticalAlignment == VerticalAlignments.Top)
                {
                    uy = ly - value + DataPointVerticalMargin;
                }

                var c = LineColours[lineIndex];

                AddRect(new Vector3(Mathf.Max(-_clipBounds.width/2f, xPosition), ly),
                    new Vector3(Mathf.Max(-_clipBounds.width/2f, xPosition), uy), new Vector3(rx, uy),
                    new Vector3(rx, ly), c);

                currentLineHeight += value;
            }
        }

        protected void DrawAxis(float frameTime, float yPosition, ProfilerGraphAxisLabel label)
        {
#if LOGGING
			if(!Application.isPlaying)
				Debug.Log("Draw Axis: {0}".Fmt(yPosition));
#endif

            var lx = -rectTransform.rect.width*0.5f;
            var rx = -lx;

            var uy = yPosition - rectTransform.rect.height*0.5f + 0.5f;
            var ly = yPosition - rectTransform.rect.height*0.5f - 0.5f;

            var c = new Color(1f, 1f, 1f, 0.4f);

            AddRect(new Vector3(lx, ly), new Vector3(lx, uy), new Vector3(rx, uy), new Vector3(rx, ly), c);

            if (label != null)
            {
                label.SetValue(frameTime, yPosition);
            }
        }

        protected void AddRect(Vector3 tl, Vector3 tr, Vector3 bl, Vector3 br, Color c)
        {
#if LEGACY_UI

            var v = UIVertex.simpleVert;
            v.color = c;

            v.position = tl;
            _vbo.Add(v);

            v.position = tr;
            _vbo.Add(v);

            v.position = bl;
            _vbo.Add(v);

            v.position = br;
            _vbo.Add(v);

#else

    // New UI system uses triangles

            _meshVertices.Add(tl);
            _meshVertices.Add(tr);
            _meshVertices.Add(bl);
            _meshVertices.Add(br);

            _meshTriangles.Add(_meshVertices.Count - 4); // tl
            _meshTriangles.Add(_meshVertices.Count - 3); // tr
            _meshTriangles.Add(_meshVertices.Count - 1); // br

            _meshTriangles.Add(_meshVertices.Count - 2); // bl
            _meshTriangles.Add(_meshVertices.Count - 1); // br
            _meshTriangles.Add(_meshVertices.Count - 3); // tr

            _meshVertexColors.Add(c);
            _meshVertexColors.Add(c);
            _meshVertexColors.Add(c);
            _meshVertexColors.Add(c);

#endif
        }

        protected ProfilerFrame GetFrame(int i)
        {
#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                return TestData[i];
            }

#endif

            return _profilerService.FrameBuffer[i];
        }

        protected int CalculateVisibleDataPointCount()
        {
            return Mathf.RoundToInt(rectTransform.rect.width/DataPointWidth);
        }

        protected int GetFrameBufferCurrentSize()
        {
#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                return TestData.Length;
            }

#endif

            return _profilerService.FrameBuffer.Count;
        }

        protected int GetFrameBufferMaxSize()
        {
#if UNITY_EDITOR

            if (!Application.isPlaying)
            {
                return TestData.Length;
            }

#endif

            return _profilerService.FrameBuffer.Capacity;
        }

        protected float CalculateMaxFrameTime()
        {
            var frameCount = GetFrameBufferCurrentSize();
            var c = Mathf.Min(CalculateVisibleDataPointCount(), frameCount);

            var max = 0d;

            for (var i = 0; i < c; i++)
            {
                var frameNumber = frameCount - i - 1;

                var t = GetFrame(frameNumber);

                if (t.FrameTime > max)
                {
                    max = t.FrameTime;
                }
            }

            return (float) max;
        }

        private ProfilerGraphAxisLabel GetAxisLabel(int index)
        {
            if (_axisLabels == null || !Application.isPlaying)
            {
                _axisLabels = GetComponentsInChildren<ProfilerGraphAxisLabel>();
            }

            if (_axisLabels.Length > index)
            {
                return _axisLabels[index];
            }

            Debug.LogWarning("[SRDebugger.Profiler] Not enough axis labels in pool");

            return null;
        }

        #region Editor Only test data

#if UNITY_EDITOR

        private ProfilerFrame[] TestData
        {
            get
            {
                if (_testData == null)
                {
                    _testData = GenerateSampleData();
                }

                return _testData;
            }
        }

        private ProfilerFrame[] _testData;

        protected static ProfilerFrame[] GenerateSampleData()
        {
            var sampleCount = 200;

            var data = new ProfilerFrame[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var frame = new ProfilerFrame();

                for (var j = 0; j < 3; j++)
                {
                    var v = 0d;

                    if (j == 0)
                    {
                        v = Mathf.PerlinNoise(i/200f, 0);
                    }
                    else if (j == 1)
                    {
                        v = Mathf.PerlinNoise(0, i/200f);
                    }
                    else
                    {
                        v = Random.Range(0, 1f);
                    }

                    v *= (1f/60f)*0.333f;

                    // Simulate spikes
                    if (Random.value > 0.8f)
                    {
                        v *= Random.Range(1.2f, 1.8f);
                    }

                    if (j == 2)
                    {
                        v *= 0.1f;
                    }

                    if (j == 0)
                    {
                        frame.UpdateTime = v;
                    }
                    else if (j == 1)
                    {
                        frame.RenderTime = v;
                    }
                    else if (j == 2)
                    {
                        frame.FrameTime = frame.RenderTime + frame.UpdateTime + v;
                    }
                }

                data[i] = frame;
            }

            data[0] = new ProfilerFrame
            {
                FrameTime = 0.005,
                RenderTime = 0.005,
                UpdateTime = 0.005
            };

            data[sampleCount - 1] = new ProfilerFrame
            {
                FrameTime = 1d/60d,
                RenderTime = 0.007,
                UpdateTime = 0.007
            };

            return data;
        }

#endif

        #endregion
    }
}
