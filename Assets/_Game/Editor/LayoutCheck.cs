using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// After a rebuild of the lagoon layout: lists every prop standing in a zone's water, every surfer route that
    /// walks across water, every luxury-tier prop standing on a route, treasures by the sea and boats that would run
    /// aground on the island (menu Surf Pool > Check Layout, or
    /// <see cref="Report"/> via eval). Things that belong in the water (the pools themselves, buoys) are skipped.
    /// </summary>
    public static class LayoutCheck
    {
        [MenuItem("Surf Pool/Check Layout")]
        static void Menu() => Debug.Log(Report());

        static bool InWater(Vector2 p, float shrink, out int zone)
        {
            var rects = GreyboxBuilder.WaterRects;
            for (zone = 0; zone < rects.Length; zone++)
            {
                var r = rects[zone];
                if (p.x > r.xMin + shrink && p.x < r.xMax - shrink && p.y > r.yMin + shrink && p.y < r.yMax - shrink) return true;
            }
            zone = -1;
            return false;
        }

        public static string Report()
        {
            var sb = new StringBuilder();
            int props = 0, routes = 0;

            // Props: anything drawn that isn't part of a pool, the ocean or the island.
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                var t = renderer.transform;
                if (renderer is ParticleSystemRenderer || t.GetComponentInParent<PoolArea>(true) != null || t.GetComponentInParent<Buoy>(true) != null) continue;
                string root = t.root.name;
                if (root == "World" && (t.parent != null && (t.parent.name == "Sand Island" || t.parent.name == "Island Ground"
                    || t.parent.name == "Far Islands" || t.name == "Ocean"))) continue;
                var c = renderer.bounds.center;
                if (c.y > 6f) continue; // birds, the blimp and the helicopter fly over the water
                if (!InWater(new Vector2(c.x, c.z), 0.5f, out int zone)) continue;
                if (++props <= 60) sb.AppendLine($"PROP in water {zone}: {Path(t)} at ({c.x:0.0}, {c.z:0.0})");
            }

            // Routes: every walk segment, sampled each half meter. Only the last step may touch the pool it leads into.
            void Route(string name, IList<Vector3> points, PoolArea own)
            {
                for (int i = 0; i + 1 < points.Count; i++)
                {
                    var a = new Vector2(points[i].x, points[i].z);
                    var b = new Vector2(points[i + 1].x, points[i + 1].z);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.5f));
                    for (int s = 0; s <= steps; s++)
                    {
                        var p = Vector2.Lerp(a, b, s / (float)steps);
                        if (!InWater(p, 0.3f, out int zone)) continue;
                        if (++routes <= 60) sb.AppendLine($"ROUTE {name} segment {i}->{i + 1} crosses water {zone} at ({p.x:0.0}, {p.y:0.0})");
                        break;
                    }
                }
            }
            static List<Vector3> Points(Transform[] ts)
            {
                var list = new List<Vector3>();
                if (ts != null) foreach (var t in ts) if (t != null) list.Add(t.position);
                return list;
            }
            var spawner = Object.FindAnyObjectByType<SurferSpawner>(FindObjectsInactive.Include);
            foreach (var pool in Object.FindObjectsByType<PoolArea>(FindObjectsInactive.Include))
            {
                var toPool = new List<Vector3>(spawner != null ? spawner.PathToPool(pool) : new Vector3[0]);
                // The very end steps down into the water at the edge: check up to the point before it.
                if (toPool.Count > 1) toPool.RemoveAt(toPool.Count - 1);
                Route(pool.DisplayName + " in", toPool, pool);
                var outPath = Points(pool.PathOut);
                outPath.Insert(0, pool.BeachLanding.position);
                if (outPath.Count > 1) outPath.RemoveAt(0); // starts on its own beach
                Route(pool.DisplayName + " out", outPath, pool);
                Route(pool.DisplayName + " return out", Points(pool.ReturnPathOut), pool);
            }
            if (spawner != null)
            {
                Route("path in", spawner.PathIn, null);
                Route("rental in", spawner.RentalPathIn, null);
            }
            // Luxury props (they come later, so nothing was laid out around them): none may stand on a walk.
            int blocking = 0;
            var segments = RouteSegments();
            foreach (var item in Object.FindObjectsByType<GlowItem>(FindObjectsInactive.Include))
            {
                if (item.fromTier <= 0) continue;
                foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is ParticleSystemRenderer) continue;
                    var b = renderer.bounds;
                    if (b.min.y > 2.2f) continue; // overhead (a sail, a roof): walkers pass under
                    float gap = Gap(new Vector2(b.min.x, b.min.z), new Vector2(b.max.x, b.max.z), segments);
                    if (gap >= 0.6f) continue;
                    if (++blocking <= 60) sb.AppendLine($"LUXURY on a route: {Path(item.transform)} at ({item.transform.position.x:0.0}, {item.transform.position.z:0.0}), {gap:0.0} m away");
                    break;
                }
            }
            // The island (Shore): every treasure up on dry sand, every boat route out in open water.
            int shore = 0;
            foreach (var treasure in Object.FindObjectsByType<Treasure>(FindObjectsInactive.Include))
            {
                var p = treasure.transform.position;
                if (Shore.Inland(p.x, p.z) > 4f && p.y > Shore.SeaLevel + 0.2f) continue;
                if (++shore <= 60) sb.AppendLine($"TREASURE by the water: {treasure.name} at ({p.x:0.0}, {p.z:0.0})");
            }
            foreach (var cruiser in Object.FindObjectsByType<Cruiser>(FindObjectsInactive.Include))
            {
                var points = new SerializedObject(cruiser).FindProperty("points");
                for (int i = 0; i < points.arraySize; i++)
                {
                    var a = points.GetArrayElementAtIndex(i).vector3Value;
                    var b = points.GetArrayElementAtIndex((i + 1) % points.arraySize).vector3Value;
                    if (a.y > 6f) break; // the blimp and the helicopter fly
                    bool aground = false;
                    for (int k = 0; k < 10 && !aground; k++)
                    {
                        var p = Vector3.Lerp(a, b, k / 10f);
                        aground = Shore.Inland(p.x, p.z) > -10f;
                    }
                    if (!aground) continue;
                    if (++shore <= 60) sb.AppendLine($"BOAT {cruiser.name} runs aground near ({a.x:0.0}, {a.z:0.0})");
                    break;
                }
            }
            sb.Insert(0, $"Layout check: {props} props in water, {routes} route segments across water, {blocking} luxury props on routes, {shore} shore problems\n");
            return sb.ToString();
        }

        /// <summary>Every walk in the park as ground segments (x, z): surfers' routes in, out and to each pool.</summary>
        internal static List<(Vector2 a, Vector2 b)> RouteSegments()
        {
            var list = new List<(Vector2, Vector2)>();
            void Add(IList<Vector3> points)
            {
                for (int i = 0; i + 1 < points.Count; i++)
                    list.Add((new Vector2(points[i].x, points[i].z), new Vector2(points[i + 1].x, points[i + 1].z)));
            }
            var spawner = Object.FindAnyObjectByType<SurferSpawner>(FindObjectsInactive.Include);
            foreach (var pool in Object.FindObjectsByType<PoolArea>(FindObjectsInactive.Include))
            {
                if (spawner != null) Add(spawner.PathToPool(pool));
                var outPath = new List<Vector3> { pool.BeachLanding.position };
                if (pool.PathOut != null) foreach (var t in pool.PathOut) if (t != null) outPath.Add(t.position);
                Add(outPath);
                var back = new List<Vector3>();
                if (pool.ReturnPathOut != null) foreach (var t in pool.ReturnPathOut) if (t != null) back.Add(t.position);
                Add(back);
            }
            if (spawner != null)
            {
                Add(spawner.PathIn);
                Add(spawner.RentalPathIn);
            }
            return list;
        }

        /// <summary>Shortest ground distance from a rectangle (min, max) to any of the segments.</summary>
        internal static float Gap(Vector2 min, Vector2 max, List<(Vector2 a, Vector2 b)> segments)
        {
            float best = float.MaxValue;
            foreach (var (a, b) in segments)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 0.25f));
                for (int s = 0; s <= steps; s++)
                {
                    var p = Vector2.Lerp(a, b, s / (float)steps);
                    float dx = Mathf.Max(min.x - p.x, 0f, p.x - max.x), dz = Mathf.Max(min.y - p.y, 0f, p.y - max.y);
                    best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz));
                }
            }
            return best;
        }

        static string Path(Transform t)
        {
            var parts = new List<string>();
            for (; t != null && parts.Count < 4; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
