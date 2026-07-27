using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    [ExecuteAlways]
    public sealed class SettlementResultEffect : MaskableGraphic
    {
        [SerializeField] private bool victory = true;
        [SerializeField] private Color victoryGold =
            new Color(1f, 0.82f, 0.18f, 0.95f);
        [SerializeField] private Color victoryMint =
            new Color(0.48f, 1f, 0.68f, 0.9f);
        [SerializeField] private Color defeatColor =
            new Color(0.42f, 0.72f, 0.84f, 0.78f);
        [SerializeField] private Color defeatAccent =
            new Color(0.95f, 0.32f, 0.38f, 0.78f);

        public void SetVictory(bool value)
        {
            victory = value;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private void Update()
        {
            if (isActiveAndEnabled) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (victory) DrawVictory(vh, CurrentTime);
            else DrawDefeat(vh, CurrentTime);
        }

        private void DrawVictory(VertexHelper vh, float time)
        {
            for (int index = 0; index < 26; index++)
            {
                float seed = Hash01(index * 37 + 9);
                float phase = Mathf.Repeat(time * Mathf.Lerp(.35f, .62f, seed) +
                    index * .137f, 1f);
                float angle = index * 2.39996f + time * .35f;
                float radius = Mathf.Lerp(88f, 154f, seed);
                Vector2 point = new Vector2(Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius * .72f + Mathf.Lerp(-35f, 145f, phase));
                float alpha = Mathf.Sin(phase * Mathf.PI);
                Color color = Color.Lerp(victoryGold, victoryMint, seed);
                color.a *= alpha;
                float size = Mathf.Lerp(5f, 10f, seed);
                if (index % 4 == 0) AddStar(vh, point, size * 1.6f, color);
                else AddRect(vh, point, new Vector2(size, size * .55f), color,
                    angle + phase * 5f);
            }

            Vector2[] burstPoints =
            {
                new Vector2(-112f, 48f), new Vector2(108f, 68f),
                new Vector2(-82f, 148f), new Vector2(76f, 166f),
                new Vector2(2f, 205f)
            };
            for (int index = 0; index < burstPoints.Length; index++)
            {
                float pulse = .28f + Mathf.Pow(Mathf.Max(0f, Mathf.Sin(
                    time * 2.8f + index * 1.37f)), 5f) * .72f;
                AddStar(vh, burstPoints[index],
                    Mathf.Lerp(7f, 14f, pulse),
                    WithAlpha(index % 2 == 0 ? victoryGold : victoryMint,
                        pulse));
            }
        }

        private void DrawDefeat(VertexHelper vh, float time)
        {
            for (int index = 0; index < 34; index++)
            {
                float seed = Hash01(index * 43 + 17);
                float phase = Mathf.Repeat(time * Mathf.Lerp(.28f, .5f, seed) +
                    index * .113f, 1f);
                float x = Mathf.Lerp(-152f, 152f, seed);
                float y = Mathf.Lerp(190f, -92f, phase);
                float drift = Mathf.Sin(time * 1.3f + index) * 14f;
                Color color = Color.Lerp(defeatColor, defeatAccent,
                    index % 3 == 0 ? .7f : .15f);
                color.a *= Mathf.Sin(phase * Mathf.PI);
                float size = Mathf.Lerp(8f, 18f, Hash01(index * 19 + 5));
                AddRect(vh, new Vector2(x + drift, y),
                    new Vector2(size * .72f, size * 1.55f), color, -.12f);
            }

            for (int index = 0; index < 8; index++)
            {
                float pulse = .24f + Mathf.Max(0f, Mathf.Sin(
                    time * 1.7f + index * 1.21f)) * .32f;
                Vector2 point = new Vector2(-154f + index * 44f,
                    -48f + (index % 2) * 18f);
                AddRect(vh, point, new Vector2(32f, 5f),
                    WithAlpha(defeatColor, pulse), 0f);
                AddRect(vh, point + new Vector2(14f, -9f),
                    new Vector2(15f, 4f),
                    WithAlpha(defeatColor, pulse * .7f), 0f);
            }

            DrawTearTrail(vh, new Vector2(-112f, 132f), time, 0f);
            DrawTearTrail(vh, new Vector2(116f, 104f), time, .46f);
        }

        private void DrawTearTrail(VertexHelper vh, Vector2 origin,
            float time, float phaseOffset)
        {
            for (int index = 0; index < 5; index++)
            {
                float phase = Mathf.Repeat(time * .55f + phaseOffset +
                    index * .16f, 1f);
                Vector2 point = origin + new Vector2(
                    Mathf.Sin(time * 1.2f + index) * 5f,
                    -phase * 150f);
                float alpha = Mathf.Sin(phase * Mathf.PI) * .86f;
                float size = Mathf.Lerp(12f, 6f, phase);
                AddRect(vh, point, new Vector2(size * .65f, size * 1.8f),
                    WithAlpha(defeatColor, alpha), 0f);
                AddRect(vh, point + new Vector2(0f, size),
                    new Vector2(size * .38f, size * .65f),
                    WithAlpha(defeatColor, alpha * .65f), 0f);
            }
        }

        private static void AddStar(VertexHelper vh, Vector2 center,
            float size, Color color)
        {
            AddRect(vh, center, new Vector2(size * .32f, size * 2f), color, 0f);
            AddRect(vh, center, new Vector2(size * 2f, size * .32f), color, 0f);
            AddRect(vh, center, Vector2.one * size * .6f, color, 0f);
        }

        private static void AddRect(VertexHelper vh, Vector2 center,
            Vector2 size, Color color, float rotation)
        {
            int start = vh.currentVertCount;
            Vector2 half = size * .5f;
            float sin = Mathf.Sin(rotation);
            float cos = Mathf.Cos(rotation);
            Vector2[] corners =
            {
                new Vector2(-half.x, -half.y), new Vector2(-half.x, half.y),
                new Vector2(half.x, half.y), new Vector2(half.x, -half.y)
            };
            for (int index = 0; index < corners.Length; index++)
            {
                Vector2 corner = corners[index];
                Vector2 rotated = new Vector2(
                    corner.x * cos - corner.y * sin,
                    corner.x * sin + corner.y * cos);
                vh.AddVert(center + rotated, color, Vector2.zero);
            }
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a *= Mathf.Clamp01(alpha);
            return color;
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
    }
}
