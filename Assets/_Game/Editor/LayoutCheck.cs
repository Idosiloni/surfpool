using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// After a rebuild of the lagoon layout: lists every prop standing in a zone's water and every surfer route that
    /// walks across water (menu Surf Pool > Check Layout, or <see cref="Report"/> via eval). Things that belong in the
    /// water (the pools themselves, buoys) are skipped.
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
                if (root == "World" && (t.parent != null && (t.parent.name == "Sand Island" || t.name == "Ocean"))) continue;
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
            sb.Insert(0, $"Layout check: {props} props in water, {routes} route segments across water\n");
            return sb.ToString();
        }

        static string Path(Transform t)
        {
            var parts = new List<string>();
            for (; t != null && parts.Count < 4; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }
    }
}
