using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    [ExecuteAlways]
    public sealed class PixelAmbientEffects : MaskableGraphic
    {
        [Header("跟随对象")]
        [SerializeField] private RectTransform titleTransform;

        [Header("像素水流")]
        [SerializeField] private Color waterColor =
            new Color(0.78f, 0.98f, 1f, 0.96f);
        [SerializeField, Range(1f, 20f)] private float pixelSize = 12f;
        [SerializeField, Range(0.1f, 3f)] private float waterSpeed = 0.95f;
        [SerializeField, Range(0f, 1f)] private float waterOpacity = 1f;
        [SerializeField] private Color particleColor =
            new Color(0.94f, 1f, 1f, 0.92f);
        [SerializeField, Range(8, 120)] private int particleCount = 52;
        [SerializeField, Range(0.1f, 2f)] private float particleSpeed = 0.82f;
        [SerializeField] private Vector2[] waterPoints =
        {
            new Vector2(0.51f, 0.89f),
            new Vector2(0.61f, 0.75f),
            new Vector2(0.47f, 0.61f),
            new Vector2(0.60f, 0.47f),
            new Vector2(0.39f, 0.39f),
            new Vector2(0.53f, 0.30f),
            new Vector2(0.70f, 0.18f),
            new Vector2(0.82f, 0.26f)
        };

        [Header("标题星光")]
        [SerializeField] private Color starColor =
            new Color(1f, 0.94f, 0.40f, 0.95f);
        [SerializeField, Range(0.1f, 3f)] private float starSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float starOpacity = 1f;
        [SerializeField] private bool starsUseFullRect;
        [SerializeField] private Vector2[] titleStarPoints =
        {
            new Vector2(0.05f, 0.76f),
            new Vector2(0.95f, 0.70f),
            new Vector2(0.10f, 0.23f),
            new Vector2(0.89f, 0.28f),
            new Vector2(0.24f, 0.91f),
            new Vector2(0.76f, 0.10f)
        };

        [Header("像素风线")]
        [SerializeField] private Color windColor =
            new Color(0.88f, 1f, 0.94f, 0.36f);
        [SerializeField, Range(0, 32)] private int windLineCount;
        [SerializeField, Range(0.1f, 3f)] private float windSpeed = 0.72f;
        [SerializeField, Range(0f, 1f)] private float windOpacity = 0.5f;

        public void Configure(RectTransform title)
        {
            titleTransform = title;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (isActiveAndEnabled) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            float time = CurrentTime;
            DrawWater(vertexHelper, time);
            DrawWaterParticles(vertexHelper, time);
            DrawWind(vertexHelper, time);
            DrawTitleStars(vertexHelper, time);
        }

        private void DrawWater(VertexHelper vh, float time)
        {
            Rect rect = rectTransform.rect;
            float unit = Mathf.Max(1f, pixelSize);

            for (int i = 0; i < waterPoints.Length; i++)
            {
                Vector2 anchor = NormalizedPoint(rect, waterPoints[i]);
                float splashPhase = Mathf.Repeat(
                    time * waterSpeed * 0.48f + i * 0.193f, 1f);
                if (splashPhase < 0.58f)
                {
                    float progress = splashPhase / 0.58f;
                    float alpha = Mathf.Sin(progress * Mathf.PI) *
                        waterOpacity;
                    Color splash = WithAlpha(waterColor, alpha);
                    float spread = Mathf.Lerp(unit * 1.8f, unit * 7.5f, progress);
                    float lift = Mathf.Sin(progress * Mathf.PI) * unit * 4.6f;

                    for (int drop = -3; drop <= 3; drop++)
                    {
                        if (drop == 0) continue;
                        float side = drop / 3f;
                        float dropLift = lift * (1f - Mathf.Abs(side) * 0.22f) +
                            unit * (1.5f - Mathf.Abs(side));
                        float dropSize = unit * Mathf.Lerp(0.55f, 1.05f,
                            1f - Mathf.Abs(side));
                        AddPixel(vh, anchor + new Vector2(
                            side * spread, dropLift), dropSize, splash);
                    }
                    AddPixel(vh, anchor + Vector2.up * (lift + unit * 2.4f),
                        unit * 0.72f, splash);

                    float rippleWidth = Mathf.Lerp(unit * 2.5f, unit * 11f, progress);
                    AddPixelRipple(vh, anchor + Vector2.down * unit * 0.9f,
                        rippleWidth, unit, splash);
                    if (progress > 0.18f)
                    {
                        Color secondRipple = WithAlpha(waterColor,
                            alpha * (1f - progress) * 0.7f);
                        AddPixelRipple(vh, anchor + Vector2.down * unit * 2.1f,
                            rippleWidth * 0.72f, unit * 0.72f, secondRipple);
                    }
                }

                float flowPhase = Mathf.Repeat(
                    time * waterSpeed + i * 0.137f, 1f);
                Vector2 mote = anchor + new Vector2(
                    Mathf.Sin((time + i) * 1.7f) * unit * 1.5f,
                    Mathf.Lerp(unit * 5f, -unit * 6f, flowPhase));
                float moteAlpha = Mathf.Sin(flowPhase * Mathf.PI) *
                    waterOpacity * 0.62f;
                AddPixel(vh, mote, unit * 0.72f,
                    WithAlpha(waterColor, moteAlpha));
                AddPixel(vh, mote + new Vector2(unit * 1.4f, unit * 0.8f),
                    unit * 0.48f, WithAlpha(waterColor, moteAlpha * 0.65f));

                float glint = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(
                    time * waterSpeed * 2.7f + i * 2.13f)), 9f);
                if (glint > 0.02f)
                {
                    Color shine = WithAlpha(particleColor,
                        glint * waterOpacity);
                    Vector2 shinePoint = anchor + new Vector2(
                        unit * 3.2f, unit * 1.4f);
                    AddRect(vh, shinePoint,
                        new Vector2(unit * 0.7f, unit * 4.5f), shine);
                    AddRect(vh, shinePoint,
                        new Vector2(unit * 4.5f, unit * 0.7f), shine);
                }

                float quietWave = 0.5f + 0.5f * Mathf.Sin(
                    time * waterSpeed * 1.5f + i * 1.37f);
                Color quietRipple = WithAlpha(waterColor,
                    waterOpacity * Mathf.Lerp(0.14f, 0.34f, quietWave));
                AddPixelRipple(vh, anchor + Vector2.down * unit * 2.8f,
                    unit * Mathf.Lerp(5f, 9f, quietWave),
                    unit * 0.72f, quietRipple);
            }
        }

        private void DrawWaterParticles(VertexHelper vh, float time)
        {
            if (waterPoints == null || waterPoints.Length == 0) return;

            Rect rect = rectTransform.rect;
            float unit = Mathf.Max(1f, pixelSize);
            int count = Mathf.Clamp(particleCount, 0, 120);
            for (int i = 0; i < count; i++)
            {
                float seedA = Hash01(i * 17 + 3);
                float seedB = Hash01(i * 31 + 11);
                float seedC = Hash01(i * 47 + 19);
                Vector2 anchor = NormalizedPoint(rect,
                    waterPoints[i % waterPoints.Length]);
                float phase = Mathf.Repeat(time * particleSpeed *
                    Mathf.Lerp(0.68f, 1.25f, seedA) + seedB, 1f);
                float alpha = Mathf.Sin(phase * Mathf.PI) *
                    waterOpacity * Mathf.Lerp(0.38f, 0.82f, seedC);
                Vector2 point = anchor + new Vector2(
                    (seedA - 0.5f) * unit * 15f +
                    Mathf.Sin(time * 1.2f + i) * unit,
                    Mathf.Lerp(-unit * 8f, unit * 13f, phase));
                float size = unit * Mathf.Lerp(0.38f, 0.9f, seedB);
                Color particle = WithAlpha(particleColor, alpha);

                if (i % 4 == 0)
                {
                    AddBubble(vh, point, size * 1.35f, particle);
                }
                else if (i % 3 == 0)
                {
                    AddRect(vh, point,
                        new Vector2(size * 0.65f, size * 2.4f), particle);
                }
                else
                {
                    AddPixel(vh, point, size, particle);
                }
            }
        }

        private void DrawTitleStars(VertexHelper vh, float time)
        {
            Vector2 bottomLeft;
            Vector2 size;
            if (starsUseFullRect)
            {
                Rect rect = rectTransform.rect;
                bottomLeft = rect.min;
                size = rect.size;
            }
            else
            {
                if (titleTransform == null) return;
                Vector3[] corners = new Vector3[4];
                titleTransform.GetWorldCorners(corners);
                bottomLeft = rectTransform.InverseTransformPoint(corners[0]);
                Vector2 topRight =
                    rectTransform.InverseTransformPoint(corners[2]);
                size = topRight - bottomLeft;
            }
            float unit = Mathf.Max(1f, pixelSize);

            for (int i = 0; i < titleStarPoints.Length; i++)
            {
                Vector2 point = bottomLeft + Vector2.Scale(
                    titleStarPoints[i], size);
                float pulseWave = Mathf.Max(0f, Mathf.Sin(
                    time * starSpeed * 2.4f + i * 1.71f));
                float pulse = 0.18f + Mathf.Pow(pulseWave, 7f) * 0.82f;

                Color sparkle = WithAlpha(starColor, pulse * starOpacity);
                float arm = unit * Mathf.Lerp(1.4f, 3.4f, pulse);
                AddPixel(vh, point, unit * 1.35f, sparkle);
                AddRect(vh, point, new Vector2(unit * 0.82f, arm * 2f), sparkle);
                AddRect(vh, point, new Vector2(arm * 2f, unit * 0.82f), sparkle);

                if (pulse > 0.48f)
                {
                    Color corner = WithAlpha(starColor,
                        (pulse - 0.48f) * starOpacity * 1.4f);
                    AddPixel(vh, point + new Vector2(arm, arm), unit * 0.7f, corner);
                    AddPixel(vh, point + new Vector2(-arm, arm), unit * 0.7f, corner);
                    AddPixel(vh, point + new Vector2(arm, -arm), unit * 0.7f, corner);
                    AddPixel(vh, point + new Vector2(-arm, -arm), unit * 0.7f, corner);
                    AddPixel(vh, point + new Vector2(arm * 1.55f, 0f),
                        unit * 0.48f, corner);
                    AddPixel(vh, point + new Vector2(-arm * 1.55f, 0f),
                        unit * 0.48f, corner);
                }
            }
        }

        private void DrawWind(VertexHelper vh, float time)
        {
            if (windLineCount <= 0) return;
            Rect rect = rectTransform.rect;
            float unit = Mathf.Max(1f, pixelSize);
            int count = Mathf.Clamp(windLineCount, 0, 32);
            for (int index = 0; index < count; index++)
            {
                float seedA = Hash01(index * 29 + 7);
                float seedB = Hash01(index * 43 + 17);
                float seedC = Hash01(index * 61 + 31);
                float phase = Mathf.Repeat(time * windSpeed *
                    Mathf.Lerp(0.72f, 1.24f, seedA) + seedB, 1f);
                float x = Mathf.Lerp(rect.xMin - unit * 14f,
                    rect.xMax + unit * 14f, phase);
                float y = Mathf.Lerp(rect.yMin + rect.height * 0.12f,
                    rect.yMax - rect.height * 0.10f, seedC) +
                    Mathf.Sin(time * 0.8f + index) * unit * 1.4f;
                float alpha = Mathf.Sin(phase * Mathf.PI) *
                    windOpacity * Mathf.Lerp(0.32f, 0.72f, seedB);
                Color color = WithAlpha(windColor, alpha);
                float length = unit * Mathf.Lerp(3.5f, 8.5f, seedA);
                float thickness = Mathf.Max(1f, unit * 0.28f);
                AddRect(vh, new Vector2(x, y),
                    new Vector2(length, thickness), color);
                AddRect(vh, new Vector2(x - length * 0.72f,
                        y - unit * 0.8f),
                    new Vector2(length * 0.42f, thickness), color);
                if (index % 3 == 0)
                {
                    AddPixel(vh, new Vector2(x + length * 0.72f,
                        y + unit * 0.55f), thickness * 1.4f, color);
                }
            }
        }

        private static Vector2 NormalizedPoint(Rect rect, Vector2 normalized)
        {
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
        }

        private static Color WithAlpha(Color source, float alpha)
        {
            source.a *= Mathf.Clamp01(alpha);
            return source;
        }

        private static void AddPixel(VertexHelper vh, Vector2 center,
            float size, Color color)
        {
            AddRect(vh, center, Vector2.one * Mathf.Max(1f, size), color);
        }

        private static void AddPixelRipple(VertexHelper vh, Vector2 center,
            float width, float unit, Color color)
        {
            float segment = Mathf.Max(1f, unit * 0.75f);
            AddRect(vh, center + Vector2.left * width * 0.34f,
                new Vector2(width * 0.28f, segment), color);
            AddRect(vh, center + Vector2.right * width * 0.34f,
                new Vector2(width * 0.28f, segment), color);
            AddPixel(vh, center + Vector2.left * width * 0.55f,
                segment * 0.85f, color);
            AddPixel(vh, center + Vector2.right * width * 0.55f,
                segment * 0.85f, color);
        }

        private static void AddBubble(VertexHelper vh, Vector2 center,
            float size, Color color)
        {
            float edge = Mathf.Max(1f, size * 0.42f);
            AddRect(vh, center + Vector2.up * size * 0.5f,
                new Vector2(size, edge), color);
            AddRect(vh, center + Vector2.down * size * 0.5f,
                new Vector2(size, edge), color);
            AddRect(vh, center + Vector2.left * size * 0.5f,
                new Vector2(edge, size), color);
            AddRect(vh, center + Vector2.right * size * 0.5f,
                new Vector2(edge, size), color);
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

        private static void AddRect(VertexHelper vh, Vector2 center,
            Vector2 size, Color color)
        {
            int index = vh.currentVertCount;
            Vector2 half = size * 0.5f;
            vh.AddVert(center + new Vector2(-half.x, -half.y), color, Vector2.zero);
            vh.AddVert(center + new Vector2(-half.x, half.y), color, Vector2.up);
            vh.AddVert(center + new Vector2(half.x, half.y), color, Vector2.one);
            vh.AddVert(center + new Vector2(half.x, -half.y), color, Vector2.right);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
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
    }
}
