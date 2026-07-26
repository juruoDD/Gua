using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MusicWaveformGraphic : MaskableGraphic
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Range(16, 96)] private int barCount = 64;
        [SerializeField, Range(0.5f, 8f)] private float sensitivity = 3.2f;
        [SerializeField, Range(1f, 24f)] private float smoothing = 11f;
        [SerializeField] private Color quietColor =
            new Color(0.32f, 0.49f, 0.35f, 0.76f);
        [SerializeField] private Color loudColor =
            new Color(0.12f, 0.25f, 0.20f, 0.98f);
        [SerializeField] private Color beatColor =
            new Color(0.88f, 0.55f, 0.30f, 1f);

        private readonly float[] spectrum = new float[256];
        private float[] heights;
        private float[] targets;
        private float beatPulse;
        private float lastMusicTime = -1f;

        public void Configure(AudioSource source)
        {
            musicSource = source;
            EnsureHeights();
        }

        public void ApplyReferenceStyle()
        {
            barCount = 64;
            sensitivity = 3.2f;
            smoothing = 11f;
            quietColor = new Color(0.32f, 0.49f, 0.35f, 0.76f);
            loudColor = new Color(0.12f, 0.25f, 0.20f, 0.98f);
            beatColor = new Color(0.88f, 0.55f, 0.30f, 1f);
            EnsureHeights();
            SetVerticesDirty();
        }

        public void SetMusicTime(float musicTime)
        {
            if (lastMusicTime < 0f || musicTime < lastMusicTime)
            {
                lastMusicTime = musicTime;
                return;
            }

            var beats = CadenceBeatTable.Points;
            for (int index = 0; index < beats.Count; index++)
            {
                if (beats[index].time <= lastMusicTime) continue;
                if (beats[index].time > musicTime) break;
                beatPulse = 1f;
            }
            lastMusicTime = musicTime;
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            EnsureHeights();
        }

        private void Update()
        {
            EnsureHeights();
            if (musicSource != null && musicSource.isPlaying)
                musicSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
            else
                System.Array.Clear(spectrum, 0, spectrum.Length);

            float blend = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            for (int index = 0; index < targets.Length; index++)
            {
                float normalized = (index + 0.5f) / heights.Length;
                int sample = Mathf.Clamp(
                    Mathf.FloorToInt(Mathf.Pow(normalized, 1.8f) *
                                     (spectrum.Length - 1)),
                    0, spectrum.Length - 1);
                float energy = Mathf.Sqrt(spectrum[sample]) * sensitivity;
                float neighbor = sample + 1 < spectrum.Length
                    ? Mathf.Sqrt(spectrum[sample + 1]) * sensitivity : energy;
                targets[index] = Mathf.Clamp01((energy + neighbor) * 0.5f);
            }
            for (int index = 0; index < heights.Length; index++)
            {
                float left = targets[Mathf.Max(0, index - 1)];
                float right = targets[Mathf.Min(targets.Length - 1, index + 1)];
                float spatiallySmoothed = (left + targets[index] * 2f + right) * 0.25f;
                heights[index] = Mathf.Lerp(
                    heights[index], spatiallySmoothed, blend);
            }

            beatPulse = Mathf.MoveTowards(beatPulse, 0f,
                Time.unscaledDeltaTime * 3.8f);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            EnsureHeights();
            Rect area = GetPixelAdjustedRect();
            float gap = Mathf.Max(3f, area.width * 0.0022f);
            float barWidth = (area.width - gap * (barCount - 1)) / barCount;
            float radius = Mathf.Max(1.5f, barWidth * 0.5f);
            float baseY = area.yMin + radius;
            float availableHeight = area.height - radius * 2f;

            for (int index = 0; index < barCount; index++)
            {
                float edgeFade = Mathf.Sin((index + 0.5f) / barCount * Mathf.PI);
                float pulseShape = 0.35f + edgeFade * 0.65f;
                float signal = heights[index];
#if UNITY_EDITOR
                if (!Application.isPlaying && signal < 0.001f)
                {
                    float x = (index + 0.5f) / barCount;
                    signal = 0.06f +
                        0.62f * Mathf.Exp(-Mathf.Pow((x - 0.19f) / 0.10f, 2f)) +
                        0.34f * Mathf.Exp(-Mathf.Pow((x - 0.62f) / 0.09f, 2f)) +
                        0.12f * Mathf.Exp(-Mathf.Pow((x - 0.84f) / 0.13f, 2f));
                }
#endif
                float level = Mathf.Clamp01(
                    signal + beatPulse * 0.42f * pulseShape);
                float height = Mathf.Lerp(radius * 2f, availableHeight, level);
                float xMin = area.xMin + index * (barWidth + gap);
                Color barColor = Color.Lerp(quietColor, loudColor, level);
                barColor = Color.Lerp(barColor, beatColor, beatPulse * 0.72f);
                AddCapsule(vertexHelper, xMin + barWidth * 0.5f,
                    baseY, height, radius, barColor);
            }
        }

        private void EnsureHeights()
        {
            if (heights == null || heights.Length != barCount)
                heights = new float[Mathf.Max(1, barCount)];
            if (targets == null || targets.Length != barCount)
                targets = new float[Mathf.Max(1, barCount)];
        }

        private static void AddCapsule(VertexHelper helper, float centerX,
            float baseY, float height, float radius, Color color)
        {
            height = Mathf.Max(height, radius * 2f);
            float bottomCenter = baseY;
            float topCenter = baseY + height - radius * 2f;
            int center = helper.currentVertCount;
            helper.AddVert(new Vector3(centerX,
                (bottomCenter + topCenter) * 0.5f), color, Vector2.one * 0.5f);

            const int arcSegments = 6;
            int firstBoundary = helper.currentVertCount;
            for (int step = 0; step <= arcSegments; step++)
            {
                float angle = Mathf.Lerp(0f, Mathf.PI, step / (float)arcSegments);
                helper.AddVert(new Vector3(centerX + Mathf.Cos(angle) * radius,
                    topCenter + Mathf.Sin(angle) * radius), color, Vector2.zero);
            }
            for (int step = 0; step <= arcSegments; step++)
            {
                float angle = Mathf.Lerp(Mathf.PI, Mathf.PI * 2f,
                    step / (float)arcSegments);
                helper.AddVert(new Vector3(centerX + Mathf.Cos(angle) * radius,
                    bottomCenter + Mathf.Sin(angle) * radius), color, Vector2.zero);
            }

            int boundaryCount = (arcSegments + 1) * 2;
            for (int index = 0; index < boundaryCount; index++)
                helper.AddTriangle(center, firstBoundary + index,
                    firstBoundary + (index + 1) % boundaryCount);
        }
    }
}
