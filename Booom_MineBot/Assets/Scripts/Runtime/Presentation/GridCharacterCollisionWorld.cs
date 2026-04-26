using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class GridCharacterCollisionWorld : ICharacterCollisionWorld2D
    {
        private const float OverlapEpsilon = 0.0001f;
        private const float DepenetrationPadding = 0.0005f;

        private readonly LogicalGridState grid;

        public GridCharacterCollisionWorld(LogicalGridState grid)
        {
            this.grid = grid;
        }

        public bool Sweep(Vector2 origin, Vector2 displacement, CharacterMotorConfig2D config, out CharacterSweepHit2D hit)
        {
            hit = default;
            if (grid == null || displacement.sqrMagnitude < 0.0000001f)
            {
                return false;
            }

            float inflatedRadius = config.CollisionRadius + config.ContactOffset;
            Vector2 goal = origin + displacement;
            int minX = Mathf.FloorToInt(Mathf.Min(origin.x, goal.x) - inflatedRadius) - 1;
            int maxX = Mathf.FloorToInt(Mathf.Max(origin.x, goal.x) + inflatedRadius) + 1;
            int minY = Mathf.FloorToInt(Mathf.Min(origin.y, goal.y) - inflatedRadius) - 1;
            int maxY = Mathf.FloorToInt(Mathf.Max(origin.y, goal.y) + inflatedRadius) + 1;

            bool found = false;
            float bestFraction = float.MaxValue;
            float bestDistance = float.MaxValue;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    GridPosition cell = new GridPosition(x, y);
                    if (!IsBlocking(cell))
                    {
                        continue;
                    }

                    Rect cellRect = GetCellRect(cell);
                    if (IsCircleOverlappingRect(origin, inflatedRadius, cellRect))
                    {
                        Vector2 overlapNormal = ComputeCircleRectDepenetration(origin, cellRect, inflatedRadius).normalized;
                        hit = new CharacterSweepHit2D(origin, overlapNormal, 0f, 0f, cell, true);
                        return true;
                    }

                    if (!TrySweepCircleAgainstRect(origin, displacement, cellRect, inflatedRadius, out float fraction, out Vector2 hitNormal))
                    {
                        continue;
                    }

                    float distance = displacement.magnitude * fraction;
                    if (found && !IsBetterHit(fraction, distance, bestFraction, bestDistance))
                    {
                        continue;
                    }

                    bestFraction = fraction;
                    bestDistance = distance;
                    hit = new CharacterSweepHit2D(origin + displacement * fraction, hitNormal, fraction, distance, cell, false);
                    found = true;
                }
            }

            return found;
        }

        public bool TryDepenetrate(Vector2 position, CharacterMotorConfig2D config, out Vector2 resolvedPosition, out CharacterSweepHit2D hit)
        {
            resolvedPosition = position;
            hit = default;
            if (grid == null)
            {
                return false;
            }

            float inflatedRadius = config.CollisionRadius + config.ContactOffset;
            bool depenetrated = false;

            for (int iteration = 0; iteration < config.MaxIterations; iteration++)
            {
                int minX = Mathf.FloorToInt(resolvedPosition.x - inflatedRadius) - 1;
                int maxX = Mathf.FloorToInt(resolvedPosition.x + inflatedRadius) + 1;
                int minY = Mathf.FloorToInt(resolvedPosition.y - inflatedRadius) - 1;
                int maxY = Mathf.FloorToInt(resolvedPosition.y + inflatedRadius) + 1;

                bool foundOverlap = false;
                Vector2 push = Vector2.zero;
                CharacterSweepHit2D deepestHit = default;
                float deepestMagnitude = 0f;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        GridPosition cell = new GridPosition(x, y);
                        if (!IsBlocking(cell))
                        {
                            continue;
                        }

                        Rect cellRect = GetCellRect(cell);
                        if (!IsCircleOverlappingRect(resolvedPosition, inflatedRadius, cellRect))
                        {
                            continue;
                        }

                        foundOverlap = true;
                        Vector2 correction = ComputeCircleRectDepenetration(resolvedPosition, cellRect, inflatedRadius);
                        push += correction;
                        float magnitude = correction.sqrMagnitude;
                        if (magnitude > deepestMagnitude)
                        {
                            deepestMagnitude = magnitude;
                            deepestHit = new CharacterSweepHit2D(
                                resolvedPosition,
                                correction.normalized,
                                0f,
                                0f,
                                cell,
                                true);
                        }
                    }
                }

                if (!foundOverlap)
                {
                    return depenetrated;
                }

                if (push.sqrMagnitude < 0.0000001f)
                {
                    return depenetrated;
                }

                resolvedPosition += push;
                hit = deepestHit;
                depenetrated = true;
            }

            return depenetrated;
        }

        public GridPosition ResolveOccupancyCell(Vector2 worldPosition, GridPosition fallback)
        {
            if (IsPassable(fallback) && IsPointInCell(worldPosition, fallback))
            {
                return fallback;
            }

            GridPosition direct = ActorContactProbe.WorldToGrid(worldPosition);
            if (IsPassable(direct))
            {
                return direct;
            }

            GridPosition best = fallback;
            float bestScore = float.MaxValue;
            int minX = Mathf.FloorToInt(worldPosition.x) - 1;
            int maxX = Mathf.FloorToInt(worldPosition.x) + 1;
            int minY = Mathf.FloorToInt(worldPosition.y) - 1;
            int maxY = Mathf.FloorToInt(worldPosition.y) + 1;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    GridPosition cell = new GridPosition(x, y);
                    if (!IsPassable(cell))
                    {
                        continue;
                    }

                    Rect rect = new Rect(cell.X, cell.Y, 1f, 1f);
                    float score = DistanceSquaredToRect(worldPosition, rect);
                    if (cell.Equals(fallback))
                    {
                        score -= 0.0001f;
                    }

                    if (score < bestScore)
                    {
                        best = cell;
                        bestScore = score;
                    }
                }
            }

            return bestScore < float.MaxValue ? best : fallback;
        }

        private bool IsBlocking(GridPosition position)
        {
            return !grid.IsInside(position) || !grid.GetCell(position).IsPassable;
        }

        private bool IsPassable(GridPosition position)
        {
            return grid.IsInside(position) && grid.GetCell(position).IsPassable;
        }

        private static Rect GetCellRect(GridPosition cell)
        {
            return new Rect(cell.X, cell.Y, 1f, 1f);
        }

        private static bool IsCircleOverlappingRect(Vector2 center, float radius, Rect rect)
        {
            Vector2 closest = ClosestPointOnRect(center, rect);
            return (center - closest).sqrMagnitude < (radius * radius) - OverlapEpsilon;
        }

        private static bool TrySweepCircleAgainstRect(Vector2 origin, Vector2 displacement, Rect rect, float radius, out float fraction, out Vector2 normal)
        {
            fraction = 0f;
            normal = Vector2.zero;
            bool found = false;
            float bestFraction = float.MaxValue;
            float bestDistance = float.MaxValue;

            if (Mathf.Abs(displacement.x) > OverlapEpsilon)
            {
                float leftFace = (rect.xMin - radius - origin.x) / displacement.x;
                if (TryBuildFaceHit(origin, displacement, rect, leftFace, vertical: true, Vector2.left, out float candidateFraction, out float candidateDistance))
                {
                    UpdateBestHit(candidateFraction, candidateDistance, Vector2.left, ref found, ref bestFraction, ref bestDistance, ref normal);
                }

                float rightFace = (rect.xMax + radius - origin.x) / displacement.x;
                if (TryBuildFaceHit(origin, displacement, rect, rightFace, vertical: true, Vector2.right, out candidateFraction, out candidateDistance))
                {
                    UpdateBestHit(candidateFraction, candidateDistance, Vector2.right, ref found, ref bestFraction, ref bestDistance, ref normal);
                }
            }

            if (Mathf.Abs(displacement.y) > OverlapEpsilon)
            {
                float bottomFace = (rect.yMin - radius - origin.y) / displacement.y;
                if (TryBuildFaceHit(origin, displacement, rect, bottomFace, vertical: false, Vector2.down, out float candidateFraction, out float candidateDistance))
                {
                    UpdateBestHit(candidateFraction, candidateDistance, Vector2.down, ref found, ref bestFraction, ref bestDistance, ref normal);
                }

                float topFace = (rect.yMax + radius - origin.y) / displacement.y;
                if (TryBuildFaceHit(origin, displacement, rect, topFace, vertical: false, Vector2.up, out candidateFraction, out candidateDistance))
                {
                    UpdateBestHit(candidateFraction, candidateDistance, Vector2.up, ref found, ref bestFraction, ref bestDistance, ref normal);
                }
            }

            Vector2 bottomLeft = new Vector2(rect.xMin, rect.yMin);
            Vector2 topLeft = new Vector2(rect.xMin, rect.yMax);
            Vector2 bottomRight = new Vector2(rect.xMax, rect.yMin);
            Vector2 topRight = new Vector2(rect.xMax, rect.yMax);

            TryAddCornerHit(origin, displacement, bottomLeft, radius, ref found, ref bestFraction, ref bestDistance, ref normal);
            TryAddCornerHit(origin, displacement, topLeft, radius, ref found, ref bestFraction, ref bestDistance, ref normal);
            TryAddCornerHit(origin, displacement, bottomRight, radius, ref found, ref bestFraction, ref bestDistance, ref normal);
            TryAddCornerHit(origin, displacement, topRight, radius, ref found, ref bestFraction, ref bestDistance, ref normal);

            if (!found)
            {
                return false;
            }

            fraction = bestFraction;
            return true;
        }

        private static bool TryBuildFaceHit(
            Vector2 origin,
            Vector2 displacement,
            Rect rect,
            float fraction,
            bool vertical,
            Vector2 normal,
            out float candidateFraction,
            out float candidateDistance)
        {
            candidateFraction = 0f;
            candidateDistance = 0f;
            if (fraction < 0f || fraction > 1f)
            {
                return false;
            }

            Vector2 point = origin + displacement * fraction;
            float axis = vertical ? point.y : point.x;
            float min = vertical ? rect.yMin : rect.xMin;
            float max = vertical ? rect.yMax : rect.xMax;
            if (axis < min - OverlapEpsilon || axis > max + OverlapEpsilon)
            {
                return false;
            }

            candidateFraction = fraction;
            candidateDistance = displacement.magnitude * fraction;
            return Vector2.Dot(displacement, normal) < -OverlapEpsilon;
        }

        private static bool IsPointInCell(Vector2 point, GridPosition cell)
        {
            return point.x >= cell.X && point.x < cell.X + 1f
                && point.y >= cell.Y && point.y < cell.Y + 1f;
        }

        private static float DistanceSquaredToRect(Vector2 point, Rect rect)
        {
            float clampedX = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
            float clampedY = Mathf.Clamp(point.y, rect.yMin, rect.yMax);
            return (point - new Vector2(clampedX, clampedY)).sqrMagnitude;
        }

        private static bool IsBetterHit(float fraction, float distance, float bestFraction, float bestDistance)
        {
            if (fraction < bestFraction - OverlapEpsilon)
            {
                return true;
            }

            return Mathf.Abs(fraction - bestFraction) <= OverlapEpsilon && distance < bestDistance;
        }

        private static void TryAddCornerHit(
            Vector2 origin,
            Vector2 displacement,
            Vector2 corner,
            float radius,
            ref bool found,
            ref float bestFraction,
            ref float bestDistance,
            ref Vector2 bestNormal)
        {
            if (!TrySolveRayCircleIntersection(origin, displacement, corner, radius, out float fraction))
            {
                return;
            }

            Vector2 point = origin + displacement * fraction;
            Vector2 closest = ClosestPointOnRect(point, new Rect(corner.x, corner.y, 0f, 0f));
            if ((point - closest).sqrMagnitude > (radius * radius) + OverlapEpsilon)
            {
                return;
            }

            Vector2 normal = (point - corner).normalized;
            if (normal == Vector2.zero)
            {
                normal = (origin - corner).normalized;
            }

            float distance = displacement.magnitude * fraction;
            UpdateBestHit(fraction, distance, normal, ref found, ref bestFraction, ref bestDistance, ref bestNormal);
        }

        private static bool TrySolveRayCircleIntersection(Vector2 origin, Vector2 displacement, Vector2 center, float radius, out float fraction)
        {
            fraction = 0f;
            float a = Vector2.Dot(displacement, displacement);
            if (a < OverlapEpsilon)
            {
                return false;
            }

            Vector2 offset = origin - center;
            float b = 2f * Vector2.Dot(offset, displacement);
            float c = Vector2.Dot(offset, offset) - radius * radius;
            float discriminant = b * b - (4f * a * c);
            if (discriminant < 0f)
            {
                return false;
            }

            float sqrt = Mathf.Sqrt(discriminant);
            float inverse = 0.5f / a;
            float first = (-b - sqrt) * inverse;
            float second = (-b + sqrt) * inverse;

            if (first >= 0f && first <= 1f)
            {
                fraction = first;
                return true;
            }

            if (second >= 0f && second <= 1f)
            {
                fraction = second;
                return true;
            }

            return false;
        }

        private static void UpdateBestHit(
            float fraction,
            float distance,
            Vector2 normal,
            ref bool found,
            ref float bestFraction,
            ref float bestDistance,
            ref Vector2 bestNormal)
        {
            if (found && !IsBetterHit(fraction, distance, bestFraction, bestDistance))
            {
                return;
            }

            found = true;
            bestFraction = fraction;
            bestDistance = distance;
            bestNormal = normal;
        }

        private static Vector2 ClosestPointOnRect(Vector2 point, Rect rect)
        {
            return new Vector2(
                Mathf.Clamp(point.x, rect.xMin, rect.xMax),
                Mathf.Clamp(point.y, rect.yMin, rect.yMax));
        }

        private static Vector2 ComputeCircleRectDepenetration(Vector2 point, Rect rect, float radius)
        {
            Vector2 closest = ClosestPointOnRect(point, rect);
            Vector2 away = point - closest;
            float distance = away.magnitude;
            if (distance > OverlapEpsilon)
            {
                float penetration = radius - distance + DepenetrationPadding;
                return penetration > 0f ? away / distance * penetration : Vector2.zero;
            }

            float toLeft = point.x - rect.xMin;
            float toRight = rect.xMax - point.x;
            float toBottom = point.y - rect.yMin;
            float toTop = rect.yMax - point.y;

            float minDistance = toLeft;
            Vector2 correction = Vector2.left * (radius + toLeft + DepenetrationPadding);

            if (toRight < minDistance)
            {
                minDistance = toRight;
                correction = Vector2.right * (radius + toRight + DepenetrationPadding);
            }

            if (toBottom < minDistance)
            {
                minDistance = toBottom;
                correction = Vector2.down * (radius + toBottom + DepenetrationPadding);
            }

            if (toTop < minDistance)
            {
                correction = Vector2.up * (radius + toTop + DepenetrationPadding);
            }

            return correction;
        }
    }
}
