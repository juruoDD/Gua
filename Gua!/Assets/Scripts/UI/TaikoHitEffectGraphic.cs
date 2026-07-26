using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    public sealed class TaikoHitEffectGraphic : MaskableGraphic
    {
        [SerializeField, Range(0.2f, 0.8f)]
        private float duration = 0.38f;
        [SerializeField, Range(4f, 20f)] private float pixelSize = 11f;
        [SerializeField, Range(16, 48)] private int ringBlockCount = 32;
        [SerializeField, Range(8, 20)] private int rayCount = 12;
        [SerializeField, Range(12, 40)] private int fragmentCount = 28;
        [SerializeField] private Color coreColor =
            new Color(1f, 0.20f, 0.035f, 1f);
        [SerializeField] private Color ringColor =
            new Color(1f, 0.58f, 0f, 1f);
        [SerializeField] private Color hotColor =
            new Color(1f, 0.94f, 0.02f, 1f);
        [SerializeField] private Color sparkColor =
            new Color(1f, 0.98f, 0.68f, 1f);

        private float elapsed = 99f;

        public void ApplyStrongPixelStyle()
        {
            duration = 0.38f;
            pixelSize = 11f;
            ringBlockCount = 32;
            rayCount = 12;
            fragmentCount = 28;
            coreColor = new Color(1f, 0.20f, 0.035f, 1f);
            ringColor = new Color(1f, 0.58f, 0f, 1f);
            hotColor = new Color(1f, 0.94f, 0.02f, 1f);
            sparkColor = new Color(1f, 0.98f, 0.68f, 1f);
            SetVerticesDirty();
        }

        public void Trigger()
        {
            elapsed = 0f;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (elapsed >= duration) return;
            elapsed += Time.unscaledDeltaTime;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (elapsed >= duration) return;

            Rect bounds = rectTransform.rect;
            Vector2 center = PixelSnap(bounds.center);
            float progress = Mathf.Clamp01(elapsed / duration);
            float burst = 1f - Mathf.Pow(1f - progress, 3f);
            float alpha = Mathf.Clamp01((1f - progress) * 1.35f);
            float limit = Mathf.Min(bounds.width, bounds.height);
            float outerRadius = Mathf.Lerp(limit * 0.10f, limit * 0.45f, burst);

            AddCoreFlash(vertexHelper, center, progress, alpha);
            AddBlockRing(vertexHelper, center, outerRadius,
                pixelSize * Mathf.Lerp(1.55f, 0.85f, progress),
                ringBlockCount, 0f, WithAlpha(ringColor, alpha));
            AddBlockRing(vertexHelper, center, outerRadius * 0.72f,
                pixelSize * Mathf.Lerp(1.15f, 0.65f, progress),
                Mathf.Max(16, ringBlockCount - 8),
                Mathf.PI / ringBlockCount,
                WithAlpha(hotColor, alpha * 0.92f));
            AddSegmentedRays(vertexHelper, center, outerRadius,
                progress, alpha);
            AddFlyingFragments(vertexHelper, center, limit,
                burst, progress, alpha);
        }

        private void AddCoreFlash(VertexHelper helper, Vector2 center,
            float progress, float alpha)
        {
            float flash = Mathf.Clamp01(1f - progress * 3.2f);
            if (flash <= 0f) return;

            float unit = pixelSize * Mathf.Lerp(1.7f, 1.1f, progress);
            Color centerColor = WithAlpha(sparkColor, alpha * flash);
            Color edgeColor = WithAlpha(coreColor, alpha * flash);
            AddPixel(helper, center, unit * 2.4f, centerColor);
            AddPixel(helper, center + new Vector2(unit * 2f, 0f),
                unit, edgeColor);
            AddPixel(helper, center + new Vector2(-unit * 2f, 0f),
                unit, edgeColor);
            AddPixel(helper, center + new Vector2(0f, unit * 2f),
                unit, edgeColor);
            AddPixel(helper, center + new Vector2(0f, -unit * 2f),
                unit, edgeColor);
        }

        private void AddBlockRing(VertexHelper helper, Vector2 center,
            float radius, float size, int count, float angleOffset,
            Color color)
        {
            for (int index = 0; index < count; index++)
            {
                float angle = index * Mathf.PI * 2f / count + angleOffset;
                float alternate = index % 2 == 0 ? 1f : 0.92f;
                Vector2 position = center + Direction(angle) *
                    radius * alternate;
                AddPixel(helper, PixelSnap(position),
                    size * (index % 4 == 0 ? 1.18f : 1f), color);
            }
        }

        private void AddSegmentedRays(VertexHelper helper, Vector2 center,
            float radius, float progress, float alpha)
        {
            for (int ray = 0; ray < rayCount; ray++)
            {
                float angle = ray * Mathf.PI * 2f / rayCount;
                Vector2 direction = Direction(angle);
                int blocks = ray % 3 == 0 ? 4 : 3;
                for (int block = 0; block < blocks; block++)
                {
                    float distance = radius *
                        (0.48f + block * 0.18f + (ray % 2) * 0.035f);
                    float size = pixelSize *
                        Mathf.Lerp(1.35f, 0.72f, progress) *
                        (1f - block * 0.10f);
                    Color rayColor = block == 0 ? hotColor : sparkColor;
                    AddPixel(helper,
                        PixelSnap(center + direction * distance),
                        size,
                        WithAlpha(rayColor, alpha *
                            (1f - block * 0.12f)));
                }
            }
        }

        private void AddFlyingFragments(VertexHelper helper, Vector2 center,
            float limit, float burst, float progress, float alpha)
        {
            for (int index = 0; index < fragmentCount; index++)
            {
                float angle = index * 2.399963f + (index % 4) * 0.13f;
                float speed = 0.22f + (index % 7) * 0.026f;
                float distance = limit * speed * burst;
                Vector2 direction = Direction(angle);
                Vector2 drift = new Vector2(
                    ((index * 17) % 5 - 2) * pixelSize,
                    ((index * 11) % 5 - 2) * pixelSize);
                float size = pixelSize *
                    (index % 5 == 0 ? 1.05f : 0.68f) *
                    Mathf.Lerp(1f, 0.7f, progress);
                Color fragmentColor = index % 3 == 0
                    ? coreColor
                    : (index % 3 == 1 ? hotColor : sparkColor);
                AddPixel(helper,
                    PixelSnap(center + direction * distance +
                              drift * burst),
                    size,
                    WithAlpha(fragmentColor, alpha * 0.95f));
            }
        }

        private void AddPixel(VertexHelper helper, Vector2 center,
            float requestedSize, Color color)
        {
            float size = Mathf.Max(pixelSize * 0.5f,
                Mathf.Round(requestedSize / pixelSize) * pixelSize);
            Vector2 half = Vector2.one * size * 0.5f;
            int start = helper.currentVertCount;
            AddVertex(helper, center + new Vector2(-half.x, -half.y), color);
            AddVertex(helper, center + new Vector2(-half.x, half.y), color);
            AddVertex(helper, center + new Vector2(half.x, half.y), color);
            AddVertex(helper, center + new Vector2(half.x, -half.y), color);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }

        private Vector2 PixelSnap(Vector2 position)
        {
            return new Vector2(
                Mathf.Round(position.x / pixelSize) * pixelSize,
                Mathf.Round(position.y / pixelSize) * pixelSize);
        }

        private static void AddVertex(VertexHelper helper,
            Vector2 position, Color color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            helper.AddVert(vertex);
        }

        private static Vector2 Direction(float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a *= Mathf.Clamp01(alpha);
            return color;
        }
    }
}
