using FrogCamp.Networking;
using UnityEngine;

namespace FrogCamp.Game
{
    /// <summary>
    /// Scene-only visualization for the authoritative pond collision map.
    /// Select CollisionVolumes in the hierarchy to inspect every volume.
    /// </summary>
    [ExecuteAlways]
    public sealed class PondCollisionDebugView : MonoBehaviour
    {
        [SerializeField] private PondCollisionConfig collisionConfig;
        [SerializeField] private Color boundaryColor =
            new Color(1f, 0.32f, 0.2f, 0.9f);
        [SerializeField] private Color obstacleColor =
            new Color(1f, 0.78f, 0.15f, 0.9f);
        [SerializeField] private bool showInternalObstacleGizmos;

        public PondCollisionConfig CollisionConfig => collisionConfig;
        public Color BoundaryColor => boundaryColor;

        public void SetCollisionConfig(PondCollisionConfig config)
        {
            collisionConfig = config;
        }

        private void OnDrawGizmosSelected()
        {
            RectTransform rect = transform as RectTransform;
            if (rect == null) return;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Color oldColor = Gizmos.color;
            Gizmos.matrix = rect.localToWorldMatrix;

            Gizmos.color = boundaryColor;
            var boundary = collisionConfig != null &&
                           collisionConfig.RockBoundary.Count >= 3
                ? collisionConfig.RockBoundary
                : PondObstacleMap.Boundary;
            for (int index = 0; index < boundary.Count; index++)
            {
                Vector3 a = LogicalToLocal(boundary[index]);
                Vector3 b = LogicalToLocal(
                    boundary[(index + 1) % boundary.Count]);
                Gizmos.DrawLine(a, b);
            }

            if (showInternalObstacleGizmos)
            {
                Gizmos.color = obstacleColor;
                if (collisionConfig != null &&
                    collisionConfig.ObstacleRegions.Count > 0)
                {
                    foreach (PondCollisionRegion region in
                             collisionConfig.ObstacleRegions)
                        DrawPolygon(rect, region.Points);
                }
            }

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }

        private static void DrawPolygon(RectTransform rect,
            System.Collections.Generic.IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 2) return;
            Gizmos.matrix = rect.localToWorldMatrix;
            for (int index = 0; index < points.Count; index++)
            {
                Gizmos.DrawLine(
                    LogicalToLocal(points[index]),
                    LogicalToLocal(points[(index + 1) % points.Count]));
            }
        }

        public static Vector3 LogicalToLocal(Vector2 point)
        {
            return new Vector3(point.x * 2f - 960f,
                540f - point.y * 2f, 0f);
        }

        public static Vector2 LocalToLogical(Vector3 point)
        {
            return new Vector2(
                Mathf.Clamp((point.x + 960f) * 0.5f, 0f, 960f),
                Mathf.Clamp((540f - point.y) * 0.5f, 0f, 540f));
        }

        private static void DrawCircle(float radius)
        {
            const int segments = 24;
            Vector3 previous = new Vector3(radius, 0f, 0f);
            for (int index = 1; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                Vector3 current = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}
