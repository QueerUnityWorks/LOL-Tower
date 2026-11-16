using System.Collections.Generic;
using UnityEngine;

namespace ElementalTactics
{
    public static class MapRouteGeometry
    {
        public const int Segments = 160;

        public static Vector2 Point(Vector2 from, Vector2 to, float bend, float fraction)
        {
            Vector2 direction = to - from;
            Vector2 normal = new Vector2(direction.y, -direction.x) * (.38f * bend);
            return from + direction * fraction + normal * (3 * fraction * (1 - fraction));
        }

        public static bool Inside(Vector2 point, Vector2 center, bool boss, float padding = 0)
        {
            Vector2 offset = point - center;
            float halfWidth = (boss ? 58 : 36) + padding;
            float bottom = (boss ? 62 : 36) + padding;
            float top = (boss ? 70 : 36) + padding;
            return offset.x >= -halfWidth && offset.x <= halfWidth && offset.y >= -bottom && offset.y <= top;
        }

        public static Vector2[] SamplePoints(Vector2 from, Vector2 to, float bend)
        {
            var points = new Vector2[Segments + 1];
            for (int index = 0; index <= Segments; index++) points[index] = Point(from, to, bend, index / (float)Segments);
            return points;
        }

        public static Vector2[] VisiblePoints(Vector2 from, Vector2 to, float bend, bool fromBoss, bool toBoss, float padding)
        {
            int start = 0, end = Segments;
            while (start <= end && Inside(Point(from, to, bend, start / (float)Segments), from, fromBoss, padding)) start++;
            while (end >= start && Inside(Point(from, to, bend, end / (float)Segments), to, toBoss, padding)) end--;
            var points = new List<Vector2>();
            for (int index = start; index <= end; index++) points.Add(Point(from, to, bend, index / (float)Segments));
            return points.ToArray();
        }

        public static bool HitsNode(Vector2[] points, Vector2 center, bool boss, float padding)
        {
            float halfWidth = (boss ? 58 : 36) + padding;
            float bottom = (boss ? 62 : 36) + padding;
            float top = (boss ? 70 : 36) + padding;
            Vector2 lowerLeft = center + new Vector2(-halfWidth, -bottom);
            Vector2 lowerRight = center + new Vector2(halfWidth, -bottom);
            Vector2 upperRight = center + new Vector2(halfWidth, top);
            Vector2 upperLeft = center + new Vector2(-halfWidth, top);
            foreach (var point in points) if (Inside(point, center, boss, padding)) return true;
            for (int index = 1; index < points.Length; index++)
                if (Intersects(points[index - 1], points[index], lowerLeft, lowerRight)
                    || Intersects(points[index - 1], points[index], lowerRight, upperRight)
                    || Intersects(points[index - 1], points[index], upperRight, upperLeft)
                    || Intersects(points[index - 1], points[index], upperLeft, lowerLeft)) return true;
            return false;
        }

        public static bool Crosses(Vector2[] first, Vector2[] second)
        {
            for (int firstIndex = 1; firstIndex < first.Length; firstIndex++)
                for (int secondIndex = 1; secondIndex < second.Length; secondIndex++)
                    if (Intersects(first[firstIndex - 1], first[firstIndex], second[secondIndex - 1], second[secondIndex])) return true;
            return false;
        }

        private static bool Intersects(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart, Vector2 secondEnd)
        {
            if (System.Math.Max(firstStart.x, firstEnd.x) < System.Math.Min(secondStart.x, secondEnd.x)
                || System.Math.Min(firstStart.x, firstEnd.x) > System.Math.Max(secondStart.x, secondEnd.x)
                || System.Math.Max(firstStart.y, firstEnd.y) < System.Math.Min(secondStart.y, secondEnd.y)
                || System.Math.Min(firstStart.y, firstEnd.y) > System.Math.Max(secondStart.y, secondEnd.y)) return false;
            return Turn(firstStart, firstEnd, secondStart) * Turn(firstStart, firstEnd, secondEnd) <= 0
                && Turn(secondStart, secondEnd, firstStart) * Turn(secondStart, secondEnd, firstEnd) <= 0;
        }

        private static float Turn(Vector2 start, Vector2 end, Vector2 point)
            => (end.x - start.x) * (point.y - start.y) - (end.y - start.y) * (point.x - start.x);
    }
}
