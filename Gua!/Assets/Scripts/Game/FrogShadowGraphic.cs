using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.Gameplay
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FrogShadowGraphic : MaskableGraphic
    {
        [SerializeField] private Color groundedColor =
            new Color(0.10f, 0.22f, 0.24f, 0.42f);
        private RectTransform shadowRect;

        public void SetState(float lift, bool eliminated)
        {
            lift = Mathf.Clamp01(lift);
            if (shadowRect == null) shadowRect = rectTransform;
            float width = Mathf.Lerp(1f, 0.58f, lift);
            float height = Mathf.Lerp(1f, 0.72f, lift);
            shadowRect.localScale = new Vector3(width, height, 1f);
            Color next = groundedColor;
            next.a *= Mathf.Lerp(1f, 0.32f, lift) *
                (eliminated ? 0.48f : 1f);
            color = next;
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            shadowRect = rectTransform;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            Vector2 radius = rect.size * 0.5f;
            const int segments = 20;
            int start = mesh.currentVertCount;
            mesh.AddVert(center, color, Vector2.zero);
            for (int index = 0; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                mesh.AddVert(center + new Vector2(
                    Mathf.Cos(angle) * radius.x,
                    Mathf.Sin(angle) * radius.y), color, Vector2.zero);
                if (index > 0)
                    mesh.AddTriangle(start, start + index,
                        start + index + 1);
            }
        }
    }
}
