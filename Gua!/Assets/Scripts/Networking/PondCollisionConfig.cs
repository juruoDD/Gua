using System.Collections.Generic;
using UnityEngine;

namespace FrogCamp.Networking
{
    [System.Serializable]
    public sealed class PondCollisionRegion
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private List<Vector2> points =
            new List<Vector2>();

        public string Id => id;
        public string DisplayName => displayName;
        public IReadOnlyList<Vector2> Points => points;

        public PondCollisionRegion(string regionId, string regionName,
            IEnumerable<Vector2> regionPoints)
        {
            id = regionId;
            displayName = regionName;
            if (regionPoints != null) points.AddRange(regionPoints);
        }

        public void ReplacePoints(IEnumerable<Vector2> regionPoints)
        {
            points.Clear();
            if (regionPoints != null) points.AddRange(regionPoints);
        }

        public void SetPoint(int index, Vector2 point)
        {
            if (index < 0 || index >= points.Count) return;
            points[index] = point;
        }

        public void AddPoint(Vector2 point)
        {
            points.Add(point);
        }

        public void RemoveLastPoint()
        {
            if (points.Count > 0) points.RemoveAt(points.Count - 1);
        }
    }

    [CreateAssetMenu(
        fileName = "PondCollisionConfig",
        menuName = "Frog Camp/Pond Collision Config")]
    public sealed class PondCollisionConfig : ScriptableObject
    {
        [SerializeField] private List<Vector2> rockBoundary =
            new List<Vector2>();
        [SerializeField] private List<PondCollisionRegion> obstacleRegions =
            new List<PondCollisionRegion>();

        public IReadOnlyList<Vector2> RockBoundary => rockBoundary;
        public IReadOnlyList<PondCollisionRegion> ObstacleRegions =>
            obstacleRegions;

        public void ReplaceBoundary(IEnumerable<Vector2> points)
        {
            rockBoundary.Clear();
            if (points != null) rockBoundary.AddRange(points);
        }

        public void SetBoundaryPoint(int index, Vector2 point)
        {
            if (index < 0 || index >= rockBoundary.Count) return;
            rockBoundary[index] = point;
        }

        public void AddBoundaryPoint(Vector2 point)
        {
            rockBoundary.Add(point);
        }

        public void RemoveLastBoundaryPoint()
        {
            if (rockBoundary.Count > 0)
                rockBoundary.RemoveAt(rockBoundary.Count - 1);
        }

        public void ReplaceObstacleRegions(
            IEnumerable<PondObstacleDefinition> definitions)
        {
            obstacleRegions.Clear();
            if (definitions == null) return;
            foreach (PondObstacleDefinition definition in definitions)
            {
                obstacleRegions.Add(new PondCollisionRegion(
                    definition.id, definition.displayName,
                    BuildPolygon(definition)));
            }
        }

        private static IEnumerable<Vector2> BuildPolygon(
            PondObstacleDefinition definition)
        {
            if (definition.shape == PondObstacleShape.Circle)
            {
                const int segments = 12;
                float radius = definition.size.x * 0.5f;
                for (int index = 0; index < segments; index++)
                {
                    float angle = index * Mathf.PI * 2f / segments;
                    yield return definition.center + new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius);
                }
                yield break;
            }

            Vector2 half = definition.size * 0.5f;
            Vector2[] corners =
            {
                new Vector2(-half.x, -half.y),
                new Vector2(half.x, -half.y),
                new Vector2(half.x, half.y),
                new Vector2(-half.x, half.y)
            };
            float radians = definition.rotation * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            foreach (Vector2 corner in corners)
            {
                Vector2 rotated = new Vector2(
                    corner.x * cosine - corner.y * sine,
                    corner.x * sine + corner.y * cosine);
                yield return definition.center + rotated;
            }
        }
    }
}
