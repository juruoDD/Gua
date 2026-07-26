using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.UI
{
    [ExecuteAlways]
    public sealed class PixelLaneLineGraphic : MaskableGraphic
    {
        [SerializeField, Range(8f, 30f)] private float dashWidth = 14f;
        [SerializeField, Range(2f, 12f)] private float dashGap = 5f;
        [SerializeField, Range(2f, 8f)] private float dashHeight = 4f;
        [SerializeField, Range(0f, 6f)] private float shadowOffset = 2f;
        [SerializeField, Range(2, 12)] private int accentInterval = 5;
        [SerializeField] private Color lineColor =
            new Color(1f, 0.95f, 0.70f, 0.94f);
        [SerializeField] private Color accentColor =
            new Color(1f, 0.65f, 0.06f, 1f);
        [SerializeField] private Color shadowColor =
            new Color(0.12f, 0.25f, 0.18f, 0.76f);

        public void ApplyDefaultStyle()
        {
            dashWidth = 14f;
            dashGap = 5f;
            dashHeight = 4f;
            shadowOffset = 2f;
            accentInterval = 5;
            lineColor = new Color(1f, 0.95f, 0.70f, 0.94f);
            accentColor = new Color(1f, 0.65f, 0.06f, 1f);
            shadowColor = new Color(0.12f, 0.25f, 0.18f, 0.76f);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            float step = Mathf.Max(1f, dashWidth + dashGap);
            int count = Mathf.Max(1, Mathf.FloorToInt(
                (rect.width + dashGap) / step));
            float usedWidth = count * dashWidth +
                              Mathf.Max(0, count - 1) * dashGap;
            float startX = rect.center.x - usedWidth * 0.5f;
            float centerY = Mathf.Round(rect.center.y);

            for (int index = 0; index < count; index++)
            {
                float x = Mathf.Round(startX + index * step);
                AddQuad(helper,
                    new Rect(x, centerY - dashHeight * 0.5f -
                                 shadowOffset,
                        dashWidth, dashHeight),
                    shadowColor);

                Color segmentColor =
                    index % accentInterval == accentInterval - 1
                        ? accentColor
                        : lineColor;
                AddQuad(helper,
                    new Rect(x, centerY - dashHeight * 0.5f,
                        dashWidth, dashHeight),
                    segmentColor);
            }
        }

        private static void AddQuad(VertexHelper helper, Rect rect,
            Color color)
        {
            int start = helper.currentVertCount;
            AddVertex(helper, new Vector2(rect.xMin, rect.yMin), color);
            AddVertex(helper, new Vector2(rect.xMin, rect.yMax), color);
            AddVertex(helper, new Vector2(rect.xMax, rect.yMax), color);
            AddVertex(helper, new Vector2(rect.xMax, rect.yMin), color);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddVertex(VertexHelper helper,
            Vector2 position, Color color)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            helper.AddVert(vertex);
        }
    }
}
