using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FrogCamp.Gameplay
{
    [RequireComponent(typeof(Graphic))]
    public sealed class SoftSilhouetteShadow : BaseMeshEffect
    {
        [SerializeField] private Color shadowColor =
            new Color(0.08f, 0.18f, 0.18f, 0.22f);
        [SerializeField, Range(1f, 6f)]
        private float shadowRadius = 2.6f;

        private readonly List<UIVertex> source =
            new List<UIVertex>();
        private readonly List<UIVertex> output =
            new List<UIVertex>();

        public void Configure(Color color, float radius)
        {
            shadowColor = color;
            shadowRadius = Mathf.Max(1f, radius);
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper.currentVertCount == 0)
                return;

            source.Clear();
            output.Clear();
            vertexHelper.GetUIVertexStream(source);
            output.Capacity = Mathf.Max(output.Capacity,
                source.Count * 29);

            AddRing(16, shadowRadius, 0.17f);
            AddRing(12, shadowRadius * 0.52f, 0.28f);
            output.AddRange(source);
            vertexHelper.Clear();
            vertexHelper.AddUIVertexTriangleStream(output);
        }

        private void AddRing(int count, float radius, float alphaScale)
        {
            for (int direction = 0; direction < count; direction++)
            {
                float angle = direction * Mathf.PI * 2f / count;
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                for (int index = 0; index < source.Count; index++)
                {
                    UIVertex vertex = source[index];
                    vertex.position += new Vector3(
                        offset.x, offset.y, 0f);
                    Color32 original = vertex.color;
                    Color color = shadowColor;
                    color.a *= alphaScale * original.a / 255f;
                    vertex.color = color;
                    output.Add(vertex);
                }
            }
        }
    }
}
