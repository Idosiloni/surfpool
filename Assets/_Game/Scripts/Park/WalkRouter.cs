using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Simple walking routes for staff and guests: straight to the goal unless a pool, wave machine or shop is in
    /// the way, in which case the route goes around it via its corners.
    /// </summary>
    public static class WalkRouter
    {
        /// <summary>Straight there if nothing's in the way, otherwise around the blocking pool via its corners.</summary>
        public static void Plan(Vector3 start, Vector3 destination, List<Vector3> route)
        {
            route.Clear();
            Vector2 from = new(start.x, start.z), to = new(destination.x, destination.z);
            for (int guard = 0; guard < 6; guard++)
            {
                var blocker = FirstBlocker(from, to);
                if (blocker == null) break;
                var rect = blocker.Value;
                // Walk to the corner that makes the trip shortest.
                Vector2[] corners = { new(rect.xMin, rect.yMin), new(rect.xMax, rect.yMin), new(rect.xMin, rect.yMax), new(rect.xMax, rect.yMax) };
                Vector2 best = corners[0];
                float bestLength = float.MaxValue;
                foreach (var corner in corners)
                {
                    if ((corner - from).sqrMagnitude < 0.01f || Crosses(from, corner, Shrink(rect))) continue;
                    float length = Vector2.Distance(from, corner) + Vector2.Distance(corner, to);
                    if (length < bestLength)
                    {
                        bestLength = length;
                        best = corner;
                    }
                }
                route.Add(new Vector3(best.x, 0f, best.y));
                from = best;
            }
            route.Add(new Vector3(to.x, 0f, to.y));
        }

        static Rect Shrink(Rect rect) => new(rect.x + 0.05f, rect.y + 0.05f, rect.width - 0.1f, rect.height - 0.1f);

        static Rect? FirstBlocker(Vector2 from, Vector2 to)
        {
            Rect? first = null;
            float firstDistance = float.MaxValue;
            foreach (var (rect, soft) in Obstacles())
            {
                // Shops are "soft": you can start or end inside one (customers at the counter, staff at work).
                if (soft && (rect.Contains(from) || rect.Contains(to))) continue;
                if (!Crosses(from, to, Shrink(rect))) continue;
                float distance = Vector2.Distance(from, rect.center);
                if (distance < firstDistance)
                {
                    firstDistance = distance;
                    first = rect;
                }
            }
            return first;
        }

        static IEnumerable<(Rect rect, bool soft)> Obstacles()
        {
            foreach (var pool in PoolRegistry.All)
            {
                if (!pool.IsOpen) continue;
                yield return (pool.Footprint(1f), false);
                yield return (pool.MachineHouseFootprint(1f), false);
            }
            foreach (var shop in ShopCounter.All) yield return (shop.Footprint(0.4f), true);
        }

        /// <summary>Does the segment a-b pass through the rectangle?</summary>
        static bool Crosses(Vector2 a, Vector2 b, Rect rect)
        {
            if (rect.Contains(a) || rect.Contains(b)) return true;
            Vector2 p00 = new(rect.xMin, rect.yMin), p10 = new(rect.xMax, rect.yMin), p01 = new(rect.xMin, rect.yMax), p11 = new(rect.xMax, rect.yMax);
            return SegmentsCross(a, b, p00, p10) || SegmentsCross(a, b, p10, p11) || SegmentsCross(a, b, p11, p01) || SegmentsCross(a, b, p01, p00);
        }

        static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float Cross(Vector2 o, Vector2 p, Vector2 q) => (p.x - o.x) * (q.y - o.y) - (p.y - o.y) * (q.x - o.x);
            float d1 = Cross(c, d, a), d2 = Cross(c, d, b), d3 = Cross(a, b, c), d4 = Cross(a, b, d);
            return ((d1 > 0) != (d2 > 0)) && ((d3 > 0) != (d4 > 0));
        }
    }
}
