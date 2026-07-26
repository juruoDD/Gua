using System.Collections.Generic;
using UnityEngine;

namespace FrogCamp.Networking
{
    public enum PondObstacleShape
    {
        Circle,
        Box
    }

    public readonly struct PondObstacleDefinition
    {
        public readonly string id;
        public readonly string displayName;
        public readonly PondObstacleShape shape;
        public readonly Vector2 center;
        public readonly Vector2 size;
        public readonly float rotation;

        public PondObstacleDefinition(string id, string displayName,
            PondObstacleShape shape, Vector2 center, Vector2 size,
            float rotation = 0f)
        {
            this.id = id;
            this.displayName = displayName;
            this.shape = shape;
            this.center = center;
            this.size = size;
            this.rotation = rotation;
        }
    }

    /// <summary>
    /// Authoritative, top-left-origin collision data for the 960 x 540 pond.
    /// Visual swaying never moves these volumes.
    /// </summary>
    public static class PondObstacleMap
    {
        private static readonly Vector2[] DefaultPondBoundary =
        {
            new Vector2(78f, 25f),
            new Vector2(155f, 31f),
            new Vector2(226f, 23f),
            new Vector2(295f, 48f),
            new Vector2(361f, 25f),
            new Vector2(438f, 20f),
            new Vector2(516f, 31f),
            new Vector2(596f, 20f),
            new Vector2(680f, 23f),
            new Vector2(760f, 19f),
            new Vector2(837f, 32f),
            new Vector2(894f, 68f),
            new Vector2(919f, 116f),
            new Vector2(908f, 165f),
            new Vector2(929f, 214f),
            new Vector2(913f, 270f),
            new Vector2(925f, 324f),
            new Vector2(906f, 374f),
            new Vector2(923f, 423f),
            new Vector2(894f, 473f),
            new Vector2(835f, 503f),
            new Vector2(756f, 505f),
            new Vector2(687f, 492f),
            new Vector2(612f, 505f),
            new Vector2(535f, 497f),
            new Vector2(457f, 508f),
            new Vector2(379f, 497f),
            new Vector2(303f, 506f),
            new Vector2(229f, 493f),
            new Vector2(157f, 505f),
            new Vector2(92f, 482f),
            new Vector2(50f, 443f),
            new Vector2(39f, 392f),
            new Vector2(55f, 341f),
            new Vector2(42f, 288f),
            new Vector2(55f, 238f),
            new Vector2(37f, 184f),
            new Vector2(51f, 132f),
            new Vector2(48f, 84f)
        };

        private static readonly PondObstacleDefinition[] Obstacles =
        {
            Circle("UpperLeftGrassA", "草 A", 101f, 124f, 18f),
            Box("UpperLeftGrassB", "草 B", 137f, 72f, 29f, 37f),
            Box("UpperLeftReedA", "芦苇 A", 156f, 98f, 31f, 47f),
            Box("UpperLeftReedB", "芦苇 B", 197f, 43f, 31f, 47f),
            Circle("UpperLeftGrassC", "草 C", 198f, 124f, 18f),
            Box("UpperLeftReedC", "芦苇 C", 257f, 75f, 31f, 47f),
            Box("UpperLeftReedD", "芦苇 D", 257f, 127f, 31f, 47f),
            Circle("UpperLeftGrassD", "草 D", 318f, 85f, 18f),

            Circle("BottomLeftFlowerA", "花朵 A", 118f, 408f, 13f),
            Circle("BottomLeftFlowerB", "花朵 B", 162f, 440f, 12f),

            Box("UpperRightPlank", "木板平台", 727f, 104f, 92f, 48f),
            Box("UpperRightCrateA", "木板箱 A", 742f, 97f, 27f, 34f),
            Box("UpperRightCrateB", "木板箱 B", 788f, 112f, 27f, 34f),
            Box("UpperRightCrateC", "木板箱 C", 775f, 139f, 25f, 31f),
            Box("UpperRightCrateD", "木板箱 D", 750f, 154f, 24f, 29f, 90f),

            Box("BottomRightNest", "鸟窝", 749f, 400f, 84f, 58f)
        };

        private static PondCollisionConfig collisionConfig;

        public static IReadOnlyList<Vector2> DefaultBoundary =>
            DefaultPondBoundary;
        public static IReadOnlyList<Vector2> Boundary
        {
            get
            {
                if (collisionConfig == null)
                    collisionConfig = Resources.Load<PondCollisionConfig>(
                        "PondCollisionConfig");
                IReadOnlyList<Vector2> configured =
                    collisionConfig == null
                        ? null
                        : collisionConfig.RockBoundary;
                return configured != null && configured.Count >= 3
                    ? configured
                    : DefaultPondBoundary;
            }
        }
        public static IReadOnlyList<PondObstacleDefinition> Definitions => Obstacles;

        public static bool CanOccupy(Vector2 point, float actorRadius)
        {
            if (!InsideBoundary(point, actorRadius)) return false;
            IReadOnlyList<PondCollisionRegion> regions =
                collisionConfig == null
                    ? null
                    : collisionConfig.ObstacleRegions;
            if (regions != null && regions.Count > 0)
            {
                for (int index = 0; index < regions.Count; index++)
                {
                    if (Overlaps(regions[index].Points, point, actorRadius))
                        return false;
                }
            }
            else
            {
                for (int index = 0; index < Obstacles.Length; index++)
                {
                    if (Overlaps(Obstacles[index], point, actorRadius))
                        return false;
                }
            }
            return true;
        }

        private static bool InsideBoundary(Vector2 point, float radius)
        {
            IReadOnlyList<Vector2> boundary = Boundary;
            bool inside = false;
            for (int current = 0, previous = boundary.Count - 1;
                 current < boundary.Count; previous = current++)
            {
                Vector2 a = boundary[previous];
                Vector2 b = boundary[current];
                bool crosses = (a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) /
                    (b.y - a.y) + a.x;
                if (crosses) inside = !inside;
                if (DistanceToSegmentSquared(point, a, b) < radius * radius)
                    return false;
            }
            return inside;
        }

        private static bool Overlaps(PondObstacleDefinition obstacle,
            Vector2 point, float radius)
        {
            Vector2 local = Rotate(point - obstacle.center, -obstacle.rotation);
            if (obstacle.shape == PondObstacleShape.Circle)
            {
                float combined = obstacle.size.x * 0.5f + radius;
                return local.sqrMagnitude < combined * combined;
            }

            Vector2 half = obstacle.size * 0.5f;
            Vector2 closest = new Vector2(
                Mathf.Clamp(local.x, -half.x, half.x),
                Mathf.Clamp(local.y, -half.y, half.y));
            return (local - closest).sqrMagnitude < radius * radius;
        }

        private static bool Overlaps(IReadOnlyList<Vector2> polygon,
            Vector2 point, float radius)
        {
            if (polygon == null || polygon.Count < 3) return false;
            bool inside = false;
            for (int current = 0, previous = polygon.Count - 1;
                 current < polygon.Count; previous = current++)
            {
                Vector2 a = polygon[previous];
                Vector2 b = polygon[current];
                bool crosses = (a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) /
                    (b.y - a.y) + a.x;
                if (crosses) inside = !inside;
                if (DistanceToSegmentSquared(point, a, b) <
                    radius * radius)
                    return true;
            }
            return inside;
        }

        private static Vector2 Rotate(Vector2 point, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            return new Vector2(
                point.x * cosine - point.y * sine,
                point.x * sine + point.y * cosine);
        }

        private static float DistanceToSegmentSquared(
            Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 segment = b - a;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.0001f) return (point - a).sqrMagnitude;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) /
                                    lengthSquared);
            return (point - (a + segment * t)).sqrMagnitude;
        }

        private static PondObstacleDefinition Circle(
            string id, string label, float x, float y, float radius)
        {
            return new PondObstacleDefinition(id, label,
                PondObstacleShape.Circle, new Vector2(x, y),
                Vector2.one * radius * 2f);
        }

        private static PondObstacleDefinition Box(
            string id, string label, float x, float y,
            float width, float height, float rotation = 0f)
        {
            return new PondObstacleDefinition(id, label,
                PondObstacleShape.Box, new Vector2(x, y),
                new Vector2(width, height), rotation);
        }
    }
}
