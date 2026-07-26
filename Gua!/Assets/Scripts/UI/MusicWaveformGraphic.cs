using FrogCamp.Networking;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MusicWaveformGraphic : MaskableGraphic
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Range(16, 96)] private int barCount = 80;
        [SerializeField, Range(0.5f, 16f)] private float sensitivity = 8.5f;
        [SerializeField, Range(1f, 24f)] private float smoothing = 9f;
        [SerializeField, Range(0f, 0.35f)] private float minimumLevel = 0.11f;
        [SerializeField, Range(0.4f, 1f)] private float responseCurve = 0.68f;
        [SerializeField, Range(0f, 0.6f)] private float glowOpacity = 0.12f;
        [SerializeField, Range(0.2f, 0.9f)] private float barWidthRatio = 0.72f;
        [SerializeField, Range(2f, 12f)] private float pixelBlockHeight = 6f;
        [SerializeField, Range(0f, 5f)] private float pixelBlockGap = 1.5f;
        [SerializeField, Range(0, 500)] private int pixelParticleCount = 320;
        [SerializeField, Range(0.5f, 3f)] private float particleSizeMultiplier = 0.9f;
        [SerializeField, Range(0.1f, 0.8f)] private float particleRiseRatio = 0.62f;
        [SerializeField, Range(0f, 1.5f)] private float particleHorizontalDrift = 0.55f;
        [SerializeField, Range(0.2f, 2f)] private float particleGlowStrength = 0.35f;
        [SerializeField, Range(0.5f, 3f)] private float beatParticleBurst = 1.35f;
        [FormerlySerializedAs("forceWarmTheme")]
        [FormerlySerializedAs("forceGreenTheme")]
        [SerializeField] private bool forceReferenceGreenTheme = true;
        [SerializeField] private Color quietColor =
            new Color(0.47f, 0.62f, 0.48f, 0.98f);
        [SerializeField] private Color loudColor =
            new Color(0.20f, 0.36f, 0.26f, 1f);
        [SerializeField] private Color beatColor =
            new Color(0.66f, 0.82f, 0.45f, 1f);

        private readonly float[] spectrum = new float[256];
        private readonly float[] secondarySpectrum = new float[256];
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
            barCount = 80;
            sensitivity = 8.5f;
            smoothing = 9f;
            minimumLevel = 0.11f;
            responseCurve = 0.68f;
            glowOpacity = 0.12f;
            barWidthRatio = 0.72f;
            pixelBlockHeight = 6f;
            pixelBlockGap = 1.5f;
            pixelParticleCount = 320;
            particleSizeMultiplier = 0.9f;
            particleRiseRatio = 0.62f;
            particleHorizontalDrift = 0.55f;
            particleGlowStrength = 0.35f;
            beatParticleBurst = 1.35f;
            forceReferenceGreenTheme = true;
            quietColor = new Color(0.47f, 0.62f, 0.48f, 0.98f);
            loudColor = new Color(0.20f, 0.36f, 0.26f, 1f);
            beatColor = new Color(0.66f, 0.82f, 0.45f, 1f);
            EnsureHeights();
            SetVerticesDirty();
        }

        public void SetMusicTime(float musicTime)
        {
            if (lastMusicTime < 0f)
            {
                lastMusicTime = musicTime;
                return;
            }
            if (musicTime < lastMusicTime)
                lastMusicTime = CadenceBeatTable.LoopStartTime - 0.001f;

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
            {
                musicSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
                if (musicSource.clip != null && musicSource.clip.channels > 1)
                    musicSource.GetSpectrumData(
                        secondarySpectrum, 1, FFTWindow.BlackmanHarris);
                else
                    System.Array.Copy(spectrum, secondarySpectrum, spectrum.Length);
            }
            else
            {
                System.Array.Clear(spectrum, 0, spectrum.Length);
                System.Array.Clear(secondarySpectrum, 0, secondarySpectrum.Length);
            }

            float blend = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            for (int index = 0; index < targets.Length; index++)
            {
                float bandStart = index / (float)targets.Length;
                float bandEnd = (index + 1f) / targets.Length;

                // Most music energy is below roughly 7 kHz. Distributing those
                // bins logarithmically keeps the entire strip active instead of
                // spending its right half on nearly silent high frequencies.
                const int highestUsefulBin = 80;
                int firstSample = Mathf.Clamp(Mathf.FloorToInt(
                    Mathf.Pow(bandStart, 2.2f) * highestUsefulBin),
                    0, spectrum.Length - 1);
                int lastSample = Mathf.Clamp(Mathf.CeilToInt(
                    Mathf.Pow(bandEnd, 2.2f) * highestUsefulBin),
                    firstSample + 1, spectrum.Length);

                float energy = 0f;
                for (int sample = firstSample; sample < lastSample; sample++)
                    energy += (spectrum[sample] + secondarySpectrum[sample]) * 0.5f;
                energy = Mathf.Sqrt(energy / (lastSample - firstSample));

                float normalized = (index + 0.5f) / targets.Length;
                float highFrequencyLift = Mathf.Lerp(1f, 2.6f, normalized);
                targets[index] = Mathf.Clamp01(
                    energy * sensitivity * highFrequencyLift);
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
            float cellWidth = area.width / barCount;
            float barWidth = Mathf.Max(2f, cellWidth * barWidthRatio);
            float blockHeight = Mathf.Min(pixelBlockHeight, barWidth);
            float blockStep = Mathf.Max(1f, blockHeight + pixelBlockGap);
            float availableHeight = area.height;
            ResolveColors(out Color quiet, out Color loud, out Color beat);

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
                    minimumLevel +
                    Mathf.Pow(Mathf.Clamp01(signal), responseCurve) *
                    (1f - minimumLevel) +
                    beatPulse * 0.55f * pulseShape);
                float height = Mathf.Lerp(blockHeight, availableHeight, level);
                float centerX = area.xMin + cellWidth * (index + 0.5f);
                Color barColor = Color.Lerp(quiet, loud, level);
                barColor = Color.Lerp(barColor, beat, beatPulse * 0.82f);
                int blockCount = Mathf.Max(1,
                    Mathf.FloorToInt((height + pixelBlockGap) / blockStep));
                for (int block = 0; block < blockCount; block++)
                {
                    float centerY = area.yMin + block * blockStep +
                        blockHeight * 0.5f;
                    if (centerY + blockHeight * 0.5f > area.yMax)
                        break;
                    AddRect(vertexHelper, new Vector2(centerX, centerY),
                        new Vector2(barWidth, blockHeight), barColor);
                }
            }

            DrawPixelParticles(vertexHelper, area, cellWidth,
                quiet, loud, beat);
        }

        private void EnsureHeights()
        {
            if (heights == null || heights.Length != barCount)
                heights = new float[Mathf.Max(1, barCount)];
            if (targets == null || targets.Length != barCount)
                targets = new float[Mathf.Max(1, barCount)];
        }

        private void ResolveColors(out Color quiet, out Color loud,
            out Color beat)
        {
            if (forceReferenceGreenTheme)
            {
                quiet = new Color(0.47f, 0.62f, 0.48f, 0.98f);
                loud = new Color(0.20f, 0.36f, 0.26f, 1f);
                beat = new Color(0.66f, 0.82f, 0.45f, 1f);
                return;
            }
            quiet = quietColor;
            loud = loudColor;
            beat = beatColor;
        }

        private void DrawPixelParticles(VertexHelper helper, Rect area,
            float cellWidth, Color quiet, Color loud, Color beat)
        {
            int count = Mathf.Clamp(pixelParticleCount, 0, 500);
            float time = CurrentTime;
            for (int index = 0; index < count; index++)
            {
                float seedA = Hash01(index * 19 + 5);
                float seedB = Hash01(index * 37 + 13);
                float seedC = Hash01(index * 53 + 23);
                int barIndex = Mathf.Clamp(
                    Mathf.FloorToInt(seedA * barCount), 0, barCount - 1);
                float signal = Mathf.Clamp01(heights[barIndex]);
                float level = Mathf.Clamp01(minimumLevel +
                    Mathf.Pow(signal, responseCurve) * (1f - minimumLevel) +
                    beatPulse * 0.62f * beatParticleBurst);
                float phase = Mathf.Repeat(time *
                    Mathf.Lerp(0.52f, 1.08f, seedB) + seedC, 1f);
                float alpha = Mathf.Sin(phase * Mathf.PI) *
                    Mathf.Clamp01(level * 1.65f +
                        beatPulse * 0.82f * beatParticleBurst);
                if (alpha < 0.02f) continue;

                float x = area.xMin + cellWidth * (barIndex + 0.5f) +
                    (seedB - 0.5f) * cellWidth * 0.9f +
                    Mathf.Sin((phase + seedA) * Mathf.PI * 2f) *
                    cellWidth * particleHorizontalDrift;
                float peakY = area.yMin + level * area.height * 0.42f;
                float y = Mathf.Min(area.yMax - 2f,
                    peakY + phase * area.height * particleRiseRatio);
                float size = Mathf.Lerp(2.4f, 5.4f, seedC) *
                    particleSizeMultiplier;
                Color particle = Color.Lerp(quiet, loud, seedB);
                particle = Color.Lerp(particle, beat, beatPulse * 0.75f);
                particle.a *= alpha;
                Color outerGlow = particle;
                outerGlow.a *= glowOpacity * 0.28f *
                    particleGlowStrength;
                AddRect(helper, new Vector2(x, y),
                    Vector2.one * (size + 5f), outerGlow);
                Color innerGlow = particle;
                innerGlow.a *= glowOpacity * 0.62f *
                    particleGlowStrength;
                AddRect(helper, new Vector2(x, y),
                    Vector2.one * (size + 2f), innerGlow);
                AddRect(helper, new Vector2(x, y),
                    Vector2.one * size, particle);
            }
        }

        private static float Hash01(int value)
        {
            uint x = (uint)value;
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return (x & 0x00ffffff) / 16777215f;
        }

        private static float CurrentTime
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return (float)UnityEditor.EditorApplication.timeSinceStartup;
#endif
                return Time.unscaledTime;
            }
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

        private static void AddRect(VertexHelper helper, Vector2 center,
            Vector2 size, Color color)
        {
            int first = helper.currentVertCount;
            Vector2 half = size * 0.5f;
            helper.AddVert(center + new Vector2(-half.x, -half.y),
                color, Vector2.zero);
            helper.AddVert(center + new Vector2(-half.x, half.y),
                color, Vector2.up);
            helper.AddVert(center + new Vector2(half.x, half.y),
                color, Vector2.one);
            helper.AddVert(center + new Vector2(half.x, -half.y),
                color, Vector2.right);
            helper.AddTriangle(first, first + 1, first + 2);
            helper.AddTriangle(first, first + 2, first + 3);
        }
    }
}
