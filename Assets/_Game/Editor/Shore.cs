using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// The island around the park. The park itself stands on flat sand (<see cref="FlatRects"/>, exact slabs cut around
    /// the pools, built by <see cref="WorldArt"/>); outside it a ring of rolling dunes with grassy backs runs down to a
    /// beach with wet sand at the waterline and a wavy coastline, and the sea floor slopes away under the ocean, which
    /// turns pale aqua over the shallows with a line of foam along the shore. An invisible wall stands knee-deep in the
    /// water (low under the pier and bridge decks). Far islands sit on the horizon. Everything that stands on the ground outside the park (palms, rocks,
    /// treasures, dune dressing) asks <see cref="Height"/>.
    /// </summary>
    public static class Shore
    {
        internal const float SandTop = -0.1f, SeaLevel = -0.75f;
        const float Beach = 16f;      // dry beach from the waterline up to the dunes
        const float Cell = 2f;        // ground and ocean grid
        const float WallDepth = 3.5f; // the invisible wall stands this far out from the waterline
        const float BridgeX = 65f;    // the road bridge south (ParkExtras.ParkingLot)
        const float PierZ = 20f;      // the fishing pier west (ParkExtras.FishingPier)
        static readonly Rect Area = Rect.MinMaxRect(-150f, -200f, 350f, 300f);
        // The coast's rough shape: a rounded rectangle around the park, made wavy by noise.
        static readonly Rect CoastCore = Rect.MinMaxRect(-84f, -112f, 266f, 196f);
        const float CoastCorner = 44f;
        const string MeshPath = "Assets/_Game/Models/IslandGround.asset";

        /// <summary>Flat park ground on the 2 m grid: the park (every pool, deck, shop and path), the parking lot, the road.</summary>
        internal static Rect[] FlatRects => new[]
        {
            Rect.MinMaxRect(-28f, -52f, 232f, 162f),
            Rect.MinMaxRect(40f, -78f, 90f, -52f),
            Rect.MinMaxRect(54f, -100f, 82f, -78f),
        };

        struct Lane
        {
            public Vector2[] points;
            public bool closed;
            public float clearance;
        }

        static List<Lane> lanes;
        static Vector2[] landSpots;

        static void Prepare()
        {
            lanes = new List<Lane>();
            foreach (var route in GlowUpBuilder.BoatRoutes)
            {
                var points = new Vector2[route.Length];
                for (int i = 0; i < route.Length; i++) points[i] = new Vector2(route[i].x, route[i].z);
                lanes.Add(new Lane { points = points, closed = true, clearance = 18f });
            }
            lanes.Add(new Lane { points = new[] { new Vector2(-88f, PierZ), new Vector2(-130f, PierZ) }, clearance = 4f });
            lanes.Add(new Lane { points = new[] { new Vector2(BridgeX, -108f), new Vector2(BridgeX, -330f) }, clearance = 4f });
            var treasures = TreasureBuilder.Places;
            landSpots = new Vector2[treasures.Length + 1];
            treasures.CopyTo(landSpots, 0);
            landSpots[treasures.Length] = new Vector2(-72f, PierZ); // the pier starts on the beach
        }

        /// <summary>Meters inland from the waterline (negative = out at sea).</summary>
        internal static float Inland(float x, float z)
        {
            if (lanes == null) Prepare();
            float c = -RoundRectOutside(CoastCore, CoastCorner, x, z);
            c += (Mathf.PerlinNoise(x * 0.011f + 3.7f, z * 0.011f + 8.1f) - 0.5f) * 30f
               + (Mathf.PerlinNoise(x * 0.04f + 1.3f, z * 0.04f + 5.2f) - 0.5f) * 8f;
            // Land: 26 m of sand around the park and the parking lot, beach around every treasure and the pier's foot.
            var flat = FlatRects;
            c = SmoothMax(c, 26f - RectDistance(flat[0], x, z), 10f);
            c = SmoothMax(c, 22f - RectDistance(flat[1], x, z), 10f);
            foreach (var spot in landSpots) c = SmoothMax(c, 12f - Vector2.Distance(spot, new Vector2(x, z)), 6f);
            // Sea: open water along every boat's route, under the fishing pier and under the bridge.
            foreach (var lane in lanes) c = SmoothMin(c, LaneDistance(lane, x, z) - lane.clearance, 8f);
            return c;
        }

        /// <summary>Ground height at (x, z): flat sand in the park, dunes and beach outside it, sea floor under the ocean.</summary>
        internal static float Height(float x, float z) => Height(x, z, out _);

        static float Height(float x, float z, out float dune)
        {
            dune = 0f;
            if (InFlat(x, z)) return SandTop;
            float c = Inland(x, z);
            float h = c <= 0f ? Mathf.Max(SeaLevel + c * 0.12f, -4f) : Mathf.Lerp(SeaLevel, SandTop, Smooth01(c / Beach));
            dune = DuneHeight(x, z, c);
            h += dune;
            // A sandy ramp up to the fishing pier's deck (top at y = 0), so you can step onto it from the beach.
            if (x > -80.5f && x < -58f)
            {
                float pad = 1f - Smooth01((Mathf.Abs(z - PierZ) - 2.2f) / 3f);
                h = Mathf.Lerp(h, Mathf.Max(h, -0.04f), pad);
            }
            // A dip under the bridge so the beach never pokes through its deck.
            if (Mathf.Abs(x - BridgeX) < 6f && z < -100f) h = Mathf.Min(h, Mathf.Lerp(SandTop, -0.9f, Mathf.InverseLerp(-100f, -103f, z)));
            return h;
        }

        /// <summary>How much dune there is here (0 = beach or near the park), for placing grass and fences.</summary>
        internal static float DuneAt(float x, float z)
        {
            Height(x, z, out float dune);
            return dune;
        }

        static float DuneHeight(float x, float z, float c)
        {
            float amount = Smooth01((c - Beach) / 12f) * Smooth01((FlatDistance(x, z) - 5f) / 14f);
            if (amount <= 0f) return 0f;
            // Wind-blown ridges with a few lumps on them.
            float ridge = 1f - Mathf.Abs(Mathf.PerlinNoise(x * 0.028f + 11f, z * 0.028f + 4f) * 2f - 1f);
            float lumps = Mathf.PerlinNoise(x * 0.07f + 2f, z * 0.07f + 9f);
            return (ridge * ridge * 3.4f + lumps * 0.9f) * amount;
        }

        internal static bool InFlat(float x, float z)
        {
            foreach (var r in FlatRects)
                if (x >= r.xMin && x <= r.xMax && z >= r.yMin && z <= r.yMax) return true;
            return false;
        }

        internal static float FlatDistance(float x, float z)
        {
            float d = float.MaxValue;
            foreach (var r in FlatRects) d = Mathf.Min(d, RectDistance(r, x, z));
            return d;
        }

        // ---------------------------------------------------------------- ground mesh

        static readonly Color DrySand = new(0.93f, 0.84f, 0.64f), WetSand = new(0.74f, 0.62f, 0.46f), SeaFloor = new(0.56f, 0.6f, 0.52f);

        /// <summary>RGB = sand (dry, wet, sea floor); A = how grassy (the Ground shader draws the grass patches).</summary>
        static Color GroundColor(float x, float z, float h, float dune)
        {
            var c = Color.Lerp(SeaFloor, WetSand, Smooth01((h - (SeaLevel - 1.2f)) / 1.2f));
            c = Color.Lerp(c, DrySand, Smooth01((h - (SeaLevel + 0.1f)) / 0.22f)).linear;
            c.a = Grassiness(x, z, dune);
            return c;
        }

        /// <summary>How grassy the dunes are here (0..1): dune backs in patches; the grass clumps grow on the same spots.</summary>
        internal static float Grassiness(float x, float z, float dune) =>
            Smooth01((dune - 0.5f) / 1.1f) * Smooth01((GrassPatch(x, z) - 0.38f) / 0.22f);

        /// <summary>Patches where dune grass grows (above 0.5), shared by the ground colors and the grass clumps.</summary>
        internal static float GrassPatch(float x, float z) => Mathf.PerlinNoise(x * 0.05f + 40f, z * 0.05f + 17f);

        static int nx, nz;
        static float[,] heights, inland, dunes;

        static void Sample()
        {
            Prepare();
            nx = Mathf.RoundToInt(Area.width / Cell);
            nz = Mathf.RoundToInt(Area.height / Cell);
            heights = new float[nx + 1, nz + 1];
            inland = new float[nx + 1, nz + 1];
            dunes = new float[nx + 1, nz + 1];
            for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++)
            {
                float x = Area.xMin + i * Cell, z = Area.yMin + j * Cell;
                heights[i, j] = Height(x, z, out float dune);
                dunes[i, j] = dune;
                inland[i, j] = InFlat(x, z) ? 999f : Inland(x, z);
            }
        }

        static bool CellInFlat(int i, int j) => InFlat(Area.xMin + (i + 0.5f) * Cell, Area.yMin + (j + 0.5f) * Cell);

        /// <summary>Builds the dunes, beach and sea floor around the park (with colliders) and the wall in the water.</summary>
        public static void BuildGround(Transform parent, Material sandMat)
        {
            Sample();
            UseGroundShader(sandMat, false);
            if (AssetDatabase.LoadAssetAtPath<Object>(MeshPath) != null) AssetDatabase.DeleteAsset(MeshPath);
            var terrainMat = TerrainMaterial();
            var root = new GameObject("Island Ground").transform;
            root.SetParent(parent, false);
            const int chunk = 50;
            bool first = true;
            void Save(Mesh mesh)
            {
                if (first) AssetDatabase.CreateAsset(mesh, MeshPath);
                else AssetDatabase.AddObjectToAsset(mesh, MeshPath);
                first = false;
            }
            for (int cz = 0; cz < nz; cz += chunk)
            for (int cx = 0; cx < nx; cx += chunk)
            {
                var mesh = GroundChunk(cx, cz, Mathf.Min(cx + chunk, nx), Mathf.Min(cz + chunk, nz));
                if (mesh == null) continue;
                Save(mesh);
                var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                go.transform.SetParent(root, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial = terrainMat;
                go.GetComponent<MeshCollider>().sharedMesh = mesh;
            }

            var wall = SeaWallMesh();
            Save(wall);
            var wallGo = new GameObject("Sea Wall", typeof(MeshCollider));
            wallGo.transform.SetParent(root, false);
            wallGo.GetComponent<MeshCollider>().sharedMesh = wall;
            AssetDatabase.SaveAssets();
        }

        static Mesh GroundChunk(int i0, int j0, int i1, int j1)
        {
            int w = i1 - i0 + 1;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            for (int j = j0; j <= j1; j++)
            for (int i = i0; i <= i1; i++)
            {
                float x = Area.xMin + i * Cell, z = Area.yMin + j * Cell;
                vertices.Add(new Vector3(x, heights[i, j], z));
                colors.Add(GroundColor(x, z, heights[i, j], dunes[i, j]));
            }
            var triangles = new List<int>();
            for (int j = j0; j < j1; j++)
            for (int i = i0; i < i1; i++)
            {
                if (CellInFlat(i, j)) continue;
                // Deep sea floor nobody can see under the ocean.
                if (Mathf.Max(Mathf.Max(heights[i, j], heights[i + 1, j]), Mathf.Max(heights[i, j + 1], heights[i + 1, j + 1])) < SeaLevel - 2.2f) continue;
                int a = (j - j0) * w + (i - i0), b = a + 1, c = a + w, d = c + 1;
                if (((i + j) & 1) == 0) triangles.AddRange(new[] { a, c, b, b, c, d });
                else triangles.AddRange(new[] { a, c, d, a, d, b });
            }
            if (triangles.Count == 0) return null;
            return Compact($"Ground {i0 / 50}_{j0 / 50}", vertices, colors, triangles);
        }

        /// <summary>A mesh of only the vertices the triangles use (the grids skip land, park and deep cells).</summary>
        static Mesh Compact(string name, List<Vector3> vertices, List<Color> colors, List<int> triangles)
        {
            var remap = new Dictionary<int, int>();
            var usedVertices = new List<Vector3>();
            var usedColors = new List<Color32>(); // 8 bits per channel is plenty and keeps the saved mesh small
            for (int k = 0; k < triangles.Count; k++)
            {
                if (!remap.TryGetValue(triangles[k], out int index))
                {
                    index = usedVertices.Count;
                    remap[triangles[k]] = index;
                    usedVertices.Add(vertices[triangles[k]]);
                    usedColors.Add(colors[triangles[k]]);
                }
                triangles[k] = index;
            }
            var mesh = new Mesh { name = name, indexFormat = usedVertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(usedVertices);
            mesh.SetColors(usedColors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Vertical quads along the line <see cref="WallDepth"/> m out from the waterline (marching squares on the inland
        /// distance), kept below the decks where the fishing pier and the bridge go out over the water.
        /// </summary>
        static Mesh SeaWallMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float level = -WallDepth;
            Vector2 Corner(int i, int j) => new(Area.xMin + i * Cell, Area.yMin + j * Cell);
            Vector2 Cross(int ia, int ja, int ib, int jb)
            {
                float va = inland[ia, ja], vb = inland[ib, jb];
                return Vector2.Lerp(Corner(ia, ja), Corner(ib, jb), (level - va) / (vb - va));
            }
            void Segment(Vector2 p, Vector2 q)
            {
                // Under the fishing pier and the bridge the wall stops just below their decks: still too tall to wade
                // past, but you can walk out over it on the deck.
                var mid = (p + q) / 2f;
                float top = 3f;
                if (mid.x < -74f && Mathf.Abs(mid.y - PierZ) < 4f) top = -0.2f;
                if (Mathf.Abs(mid.x - BridgeX) < 6f && mid.y < -96f) top = -0.5f;
                int k = vertices.Count;
                vertices.Add(new Vector3(p.x, -4.5f, p.y));
                vertices.Add(new Vector3(q.x, -4.5f, q.y));
                vertices.Add(new Vector3(p.x, top, p.y));
                vertices.Add(new Vector3(q.x, top, q.y));
                triangles.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3, k, k + 1, k + 2, k + 1, k + 3, k + 2 });
            }
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                // Corners counter-clockwise: (i,j) (i+1,j) (i+1,j+1) (i,j+1); edges 0..3 between them.
                bool a = inland[i, j] > level, b = inland[i + 1, j] > level, c = inland[i + 1, j + 1] > level, d = inland[i, j + 1] > level;
                var crossings = new List<Vector2>(4);
                if (a != b) crossings.Add(Cross(i, j, i + 1, j));
                if (b != c) crossings.Add(Cross(i + 1, j, i + 1, j + 1));
                if (c != d) crossings.Add(Cross(i + 1, j + 1, i, j + 1));
                if (d != a) crossings.Add(Cross(i, j + 1, i, j));
                if (crossings.Count >= 2) Segment(crossings[0], crossings[1]);
                if (crossings.Count == 4) Segment(crossings[2], crossings[3]);
            }
            var mesh = new Mesh { name = "Sea Wall", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- dressing

        /// <summary>
        /// Life on the dunes and beach: palm groves, dune grass on the grassy backs, coastal bushes, broken runs of sand
        /// fence along the foot of the dunes, driftwood and rocks on the beach. Static (batched), no colliders, kept off
        /// treasures, the pier and the bridge.
        /// </summary>
        public static void Dress(Transform parent)
        {
            if (lanes == null) Prepare();
            var root = new GameObject("Dune Life").transform;
            root.SetParent(parent, false);
            var rng = new System.Random(7);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var taken = new List<Vector3>(); // (x, z, radius)

            bool Free(float x, float z, float radius)
            {
                foreach (var t in taken)
                    if ((t.x - x) * (t.x - x) + (t.y - z) * (t.y - z) < (t.z + radius) * (t.z + radius)) return false;
                foreach (var spot in landSpots)
                    if (Vector2.Distance(spot, new Vector2(x, z)) < 3.5f + radius) return false;
                if (x < -66f && Mathf.Abs(z - PierZ) < 6f) return false;
                if (Mathf.Abs(x - BridgeX) < 9f && z < -90f) return false;
                return FlatDistance(x, z) > 2f + radius;
            }

            GameObject Put(string model, float x, float z, float radius, float scale, bool shadows = true, float sink = 0.05f)
            {
                taken.Add(new Vector3(x, z, radius));
                var go = WorldArt.Model(model, root, new Vector3(x, Height(x, z) - sink, z), Range(0f, 360f), scale);
                if (go == null) return null;
                // Lean a little with the slope so nothing floats on a dune side.
                var n = Normal(x, z);
                go.transform.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, n), 0.6f) * go.transform.rotation;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                    r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                return go;
            }

            // Palm groves at the foot of the dunes and in the hollows behind them.
            string[] palms = { "PalmTree", "PalmTreeTall", "PalmTreeShort" };
            for (int tries = 0, groves = 0; tries < 600 && groves < 18; tries++)
            {
                float x = Range(Area.xMin, Area.xMax), z = Range(Area.yMin, Area.yMax);
                float c = Inland(x, z);
                if (c < Beach - 6f || c > Beach + 40f || !Free(x, z, 6f)) continue;
                groves++;
                int count = rng.Next(3, 7);
                for (int k = 0; k < count; k++)
                {
                    float px = x + Range(-6f, 6f), pz = z + Range(-6f, 6f);
                    if (Inland(px, pz) < 4f || !Free(px, pz, 1.6f)) continue;
                    var palm = Put(palms[rng.Next(palms.Length)], px, pz, 1.6f, Range(0.85f, 1.15f), sink: 0.1f);
                    if (palm != null) palm.transform.rotation = Quaternion.Euler(0f, Range(0f, 360f), 0f); // trunks grow straight up
                }
                for (int k = rng.Next(1, 4); k > 0; k--)
                {
                    float bx = x + Range(-5f, 5f), bz = z + Range(-5f, 5f);
                    if (Inland(bx, bz) > 6f && Free(bx, bz, 0.9f)) Put(k % 2 == 0 ? "Bush" : "BushSmall", bx, bz, 0.9f, Range(0.8f, 1.2f));
                }
            }

            // Dune grass in clusters on the grassy dune backs (the same patches the ground shader paints green).
            for (int tries = 0; tries < 9000; tries++)
            {
                float x = Range(Area.xMin, Area.xMax), z = Range(Area.yMin, Area.yMax);
                float dune = DuneAt(x, z);
                if (dune < 0.3f) continue;
                float chance = Grassiness(x, z, dune) * 0.8f + 0.05f;
                if (rng.NextDouble() > chance) continue;
                for (int k = rng.Next(2, 6); k > 0; k--)
                {
                    float gx = x + Range(-1.8f, 1.8f), gz = z + Range(-1.8f, 1.8f);
                    if (!Free(gx, gz, 0.35f)) continue;
                    Put(rng.NextDouble() < 0.35 ? "DuneGrassTall" : "DuneGrass", gx, gz, 0.35f, Range(0.8f, 1.35f), shadows: false);
                }
            }

            // Bushes on the dunes, more of them toward the park.
            for (int tries = 0, bushes = 0; tries < 4000 && bushes < 140; tries++)
            {
                float x = Range(Area.xMin, Area.xMax), z = Range(Area.yMin, Area.yMax);
                if (Inland(x, z) < Beach + 2f || FlatDistance(x, z) > 40f || rng.NextDouble() > 0.5 || !Free(x, z, 1f)) continue;
                bushes++;
                Put(rng.NextDouble() < 0.45 ? "Bush" : "BushSmall", x, z, 1f, Range(0.75f, 1.3f));
            }

            // Sand fence: broken runs along the foot of the dunes, following the coast.
            for (int tries = 0, runs = 0; tries < 6000 && runs < 16; tries++)
            {
                float x = Range(Area.xMin, Area.xMax), z = Range(Area.yMin, Area.yMax);
                if (Mathf.Abs(Inland(x, z) - (Beach + 1f)) > 1.5f || !Free(x, z, 2.2f)) continue;
                runs++;
                var p = new Vector2(x, z);
                for (int k = rng.Next(2, 6); k > 0; k--)
                {
                    var g = Gradient(p.x, p.y);
                    var along = new Vector2(-g.y, g.x);
                    if (rng.NextDouble() > 0.15 && Free(p.x, p.y, 1.8f))
                    {
                        var fence = Put("SandFence", p.x, p.y, 1.8f, 1f, sink: 0.15f);
                        if (fence != null)
                        {
                            // The fence runs along its local X: turn that to follow the coast (yaw y maps X to (cos y, -sin y)).
                            float yaw = Mathf.Atan2(-along.y, along.x) * Mathf.Rad2Deg + Range(-6f, 6f);
                            fence.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                        }
                    }
                    p += along * 4.1f;
                }
            }

            // Driftwood and rocks on the beach.
            for (int tries = 0, logs = 0; tries < 3000 && logs < 16; tries++)
            {
                float x = Range(Area.xMin, Area.xMax), z = Range(Area.yMin, Area.yMax);
                float c = Inland(x, z);
                if (c < 2.5f || c > 10f || !Free(x, z, 1.6f)) continue;
                logs++;
                Put("Driftwood", x, z, 1.6f, Range(0.8f, 1.2f), sink: 0.08f);
            }
            for (int tries = 0, rocks = 0; tries < 3000 && rocks < 10; tries++)
            {
                float x = Range(Area.xMin, Area.xMax), z = Range(Area.yMin, Area.yMax);
                float c = Inland(x, z);
                if (c < -3f || c > 4f || !Free(x, z, 2f)) continue;
                rocks++;
                Put("Rock", x, z, 2f, Range(0.7f, 1.6f), sink: 0.25f);
                if (rng.NextDouble() < 0.6) Put("Rock", x + Range(-2.5f, 2.5f), z + Range(-2.5f, 2.5f), 1f, Range(0.4f, 0.8f), sink: 0.2f);
            }
        }

        /// <summary>
        /// A pool plot that isn't built yet (GreyboxBuilder's construction lot, in the pool's own frame: x from the
        /// machine to the beach, z across): survey stakes with orange tape mark out where the pool will go, sand piles
        /// wait for the diggers, and weeds, bushes and a few palms have grown in meanwhile. It all hides with the lot.
        /// </summary>
        internal static void DressLot(Transform lot, float poolLength, float zc, float halfWidth, int seed)
        {
            var rng = new System.Random(seed);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var taken = new List<Vector3>();
            bool Free(float x, float z, float radius)
            {
                foreach (var t in taken)
                    if ((t.x - x) * (t.x - x) + (t.y - z) * (t.y - z) < (t.z + radius) * (t.z + radius)) return false;
                return true;
            }
            var sand = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Materials/Sand.mat");
            void Put(string model, float x, float z, float radius, float scale, float yaw, bool shadows = true)
            {
                taken.Add(new Vector3(x, z, radius));
                var go = WorldArt.Model(model, lot, Vector3.zero, 0f, scale);
                if (go == null) return;
                if (model == "SandPile" && sand != null) // the same sand as the ground, color noise and all
                    foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterial = sand;
                go.transform.SetParent(lot, false);
                go.transform.localPosition = new Vector3(x, SandTop - 0.03f, z);
                go.transform.localRotation = Quaternion.Euler(0f, yaw + 180f, 0f);
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                    r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            }

            // Stakes along the outline of the pool-to-be, every 8 m or so, tape blowing the same way.
            float south = zc - halfWidth, north = zc + halfWidth;
            int along = Mathf.Max(2, Mathf.RoundToInt(poolLength / 8f));
            for (int i = 0; i <= along; i++)
            {
                float x = poolLength * i / along;
                Put("SurveyStake", x, south, 0.3f, 1f, Range(-10f, 10f));
                Put("SurveyStake", x, north, 0.3f, 1f, Range(-10f, 10f));
            }
            int across = Mathf.Max(2, Mathf.RoundToInt(halfWidth * 2f / 8f));
            for (int i = 1; i < across; i++)
            {
                float z = south + 2f * halfWidth * i / across;
                Put("SurveyStake", 0f, z, 0.3f, 1f, Range(-10f, 10f));
                Put("SurveyStake", poolLength, z, 0.3f, 1f, Range(-10f, 10f));
            }

            // Sand piles where the digging will start, near the machine end.
            for (int i = 0, tries = 0; i < 3 && tries < 50; tries++)
            {
                float x = Range(6f, poolLength * 0.45f), z = Range(south + 6f, north - 6f);
                if (!Free(x, z, 3.2f)) continue;
                Put("SandPile", x, z, 3.2f, Range(0.8f, 1.25f), Range(0f, 360f));
                i++;
            }

            // Weeds have taken over: grass clumps, bushes, a few palms along the fences.
            for (int i = 0, tries = 0; i < 30 && tries < 300; tries++)
            {
                float x = Range(2f, poolLength - 2f), z = Range(south + 1f, north - 1f);
                if (!Free(x, z, 0.5f)) continue;
                Put(rng.NextDouble() < 0.3 ? "DuneGrassTall" : rng.NextDouble() < 0.5 ? "DuneGrass" : "Weeds", x, z, 0.5f, Range(0.8f, 1.3f), Range(0f, 360f), false);
                i++;
            }
            for (int i = 0, tries = 0; i < 7 && tries < 100; tries++)
            {
                float x = Range(3f, poolLength - 3f), z = Range(south + 2f, north - 2f);
                if (!Free(x, z, 1.1f)) continue;
                Put(rng.NextDouble() < 0.5 ? "Bush" : "BushSmall", x, z, 1.1f, Range(0.75f, 1.2f), Range(0f, 360f));
                i++;
            }
            string[] palms = { "PalmTree", "PalmTreeShort", "PalmTreeTall" };
            for (int i = 0, tries = 0; i < 4 && tries < 100; tries++)
            {
                float x = Range(4f, poolLength - 4f), z = rng.NextDouble() < 0.5 ? Range(south + 1.5f, south + 4f) : Range(north - 4f, north - 1.5f);
                if (!Free(x, z, 2f)) continue;
                Put(palms[rng.Next(palms.Length)], x, z, 2f, Range(0.85f, 1.1f), Range(0f, 360f));
                i++;
            }
        }

        /// <summary>Which way is uphill/inland (the gradient of <see cref="Inland"/>), normalized.</summary>
        static Vector2 Gradient(float x, float z)
        {
            const float e = 1f;
            var g = new Vector2(Inland(x + e, z) - Inland(x - e, z), Inland(x, z + e) - Inland(x, z - e));
            return g.sqrMagnitude > 1e-6f ? g.normalized : Vector2.up;
        }

        static Vector3 Normal(float x, float z)
        {
            const float e = 0.5f;
            return new Vector3(Height(x - e, z) - Height(x + e, z), 2f * e, Height(x, z - e) - Height(x, z + e)).normalized;
        }

        // ---------------------------------------------------------------- ocean

        /// <summary>
        /// The ocean: a fine grid around the island (vertex colors for the stylized water shader: B = shallow aqua over
        /// the sand, R = foam along the waterline and a fainter line where small waves break further out) and a coarse
        /// skirt out to the horizon. Built in world space; the ocean object sits at y = <see cref="SeaLevel"/>.
        /// </summary>
        public static Mesh OceanMesh()
        {
            const string path = "Assets/_Game/Models/OceanPlane.asset";
            if (heights == null) Sample();
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            int w = nx + 1;
            for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++)
            {
                float depth = SeaLevel - heights[i, j];
                float shallow = 1f - Smooth01(depth / 3.2f);
                float shoreFoam = 1f - Smooth01((depth + 0.05f) / 0.5f);
                float breakLine = 0.5f * Mathf.Exp(-Mathf.Pow((depth - 1.15f) / 0.22f, 2f));
                float foam = Mathf.Max(shoreFoam * 0.95f, breakLine);
                float fresh = shoreFoam >= breakLine ? 1f : 0.35f;
                vertices.Add(new Vector3(Area.xMin + i * Cell, 0f, Area.yMin + j * Cell));
                colors.Add(new Color(foam, 0f, shallow, fresh));
            }
            // Fine 2 m cells near the shore (shallows and foam); out in deep water one 10 m tile per block of 5 x 5
            // cells (the ocean is flat, so the seams between them don't crack).
            const int block = 5;
            for (int bj = 0; bj < nz; bj += block)
            for (int bi = 0; bi < nx; bi += block)
            {
                int ej = Mathf.Min(bj + block, nz), ei = Mathf.Min(bi + block, nx);
                bool deep = true;
                for (int j = bj; j <= ej && deep; j++)
                for (int i = bi; i <= ei && deep; i++)
                    deep = SeaLevel - heights[i, j] > 3f; // the sea floor bottoms out at 3.25 m
                if (deep)
                {
                    int a = bj * w + bi, b = bj * w + ei, c = ej * w + bi, d = ej * w + ei;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                    continue;
                }
                for (int j = bj; j < ej; j++)
                for (int i = bi; i < ei; i++)
                {
                    if (CellInFlat(i, j)) continue;
                    float wet = Mathf.Min(Mathf.Min(heights[i, j], heights[i + 1, j]), Mathf.Min(heights[i, j + 1], heights[i + 1, j + 1]));
                    if (wet > SeaLevel + 0.02f) continue; // all dry
                    int a = j * w + i, b = a + 1, c = a + w, d = c + 1;
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            }

            // The skirt: 50 m cells out to 2.5 km, around the fine grid.
            const float far = 2500f, big = 50f;
            int n = Mathf.RoundToInt(2f * far / big);
            int start = vertices.Count;
            for (int j = 0; j <= n; j++)
            for (int i = 0; i <= n; i++)
            {
                vertices.Add(new Vector3(-far + i * big, 0f, -far + j * big));
                colors.Add(new Color(0f, 0f, 0f, 1f));
            }
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                var center = new Vector2(-far + (i + 0.5f) * big, -far + (j + 0.5f) * big);
                if (Area.Contains(center)) continue;
                int a = start + j * (n + 1) + i, b = a + 1, c = a + n + 1, d = c + 1;
                triangles.AddRange(new[] { a, c, b, b, c, d });
            }

            var mesh = Compact("OceanPlane", vertices, colors, triangles);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ---------------------------------------------------------------- far islands

        /// <summary>Low-poly islands on the horizon (no colliders or shadows), so the sea doesn't end in an empty line.</summary>
        public static void FarIslands(Transform parent)
        {
            const string path = "Assets/_Game/Models/FarIslands.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            var mat = TerrainMaterial();
            var root = new GameObject("Far Islands").transform;
            root.SetParent(parent, false);
            // (center, radius x, radius z, peak height, yaw, seed)
            (Vector2 center, float rx, float rz, float peak, float yaw, int seed)[] isles =
            {
                (new(-440f, 380f), 200f, 150f, 125f, 20f, 1),  // a big green volcano to the northwest
                (new(170f, 660f), 340f, 110f, 48f, -8f, 2),    // a long low island to the north
                (new(640f, 140f), 95f, 70f, 36f, 40f, 3),      // a rocky islet to the east
                (new(580f, -200f), 60f, 50f, 22f, 0f, 4),
                (new(-540f, -260f), 230f, 140f, 78f, -30f, 5), // a headland to the southwest
            };
            for (int k = 0; k < isles.Length; k++)
            {
                var isle = isles[k];
                var mesh = IslandMesh(isle.rx, isle.rz, isle.peak, isle.seed);
                if (k == 0) AssetDatabase.CreateAsset(mesh, path);
                else AssetDatabase.AddObjectToAsset(mesh, path);
                var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(new Vector3(isle.center.x, SeaLevel, isle.center.y), Quaternion.Euler(0f, isle.yaw, 0f));
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            AssetDatabase.SaveAssets();
        }

        static Mesh IslandMesh(float rx, float rz, float peak, int seed)
        {
            const int rings = 18, segments = 48;
            float ox = seed * 17.3f, oz = seed * 9.1f;
            var rock = new Color(0.55f, 0.52f, 0.5f);
            var jungle = new Color(0.27f, 0.55f, 0.32f);
            var jungleLight = new Color(0.42f, 0.66f, 0.34f);
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            vertices.Add(new Vector3(0f, peak, 0f));
            colors.Add(NoGrass(rock));
            for (int r = 1; r <= rings; r++)
            {
                float t = r / (float)(rings - 3); // the last rings run out past the shore and under the water
                for (int s = 0; s < segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    float wobble = 1f + (Mathf.PerlinNoise(ox + Mathf.Cos(a) * 1.3f, oz + Mathf.Sin(a) * 1.3f) - 0.5f) * 0.45f;
                    float x = Mathf.Cos(a) * rx * t * wobble, z = Mathf.Sin(a) * rz * t * wobble;
                    float profile = t < 1f ? Mathf.Pow(1f - t, 1.6f) : 0f;
                    float bumps = 1f + (Mathf.PerlinNoise(ox + x * 0.02f, oz + z * 0.02f) - 0.5f) * 0.5f;
                    float y = t < 1f ? peak * profile * bumps + 1.2f : Mathf.Lerp(1.2f, -12f, (t - 1f) / 0.15f);
                    vertices.Add(new Vector3(x, y, z));
                    float slope = peak / Mathf.Max(rx, rz) * 1.6f * Mathf.Pow(Mathf.Max(1f - t, 0f), 0.6f);
                    Color color = Color.Lerp(jungle, jungleLight, Mathf.PerlinNoise(ox + x * 0.04f, oz + z * 0.04f));
                    if (y > peak * 0.78f || slope > 1.1f) color = Color.Lerp(color, rock, 0.75f);
                    if (y < 4f) color = Color.Lerp(DrySand, color, Smooth01((y - 1.5f) / 2.5f));
                    colors.Add(NoGrass(color));
                }
            }
            for (int s = 0; s < segments; s++)
            {
                int s1 = (s + 1) % segments;
                triangles.AddRange(new[] { 0, 1 + s1, 1 + s });
            }
            for (int r = 1; r < rings; r++)
            for (int s = 0; s < segments; s++)
            {
                int s1 = (s + 1) % segments;
                int a = 1 + (r - 1) * segments + s, b = 1 + (r - 1) * segments + s1;
                int c = 1 + r * segments + s, d = 1 + r * segments + s1;
                triangles.AddRange(new[] { a, b, c, b, d, c });
            }
            var mesh = new Mesh { name = $"Far Island {seed}" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- materials and math

        /// <summary>A vertex color for the Ground shader without grass (A = 0).</summary>
        static Color NoGrass(Color color)
        {
            var c = color.linear;
            c.a = 0f;
            return c;
        }

        static Material TerrainMaterial()
        {
            const string path = "Assets/_Game/Materials/IslandGround.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("SurfPool/Ground"));
                AssetDatabase.CreateAsset(mat, path);
            }
            UseGroundShader(mat, true);
            return mat;
        }

        /// <summary>
        /// Puts a material on the Ground shader: soft color noise over flat sand, or (island ground and far islands)
        /// vertex colors with faceted low-poly lighting on a white base.
        /// </summary>
        static void UseGroundShader(Material mat, bool vertexColors)
        {
            var color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            mat.shader = Shader.Find("SurfPool/Ground");
            mat.SetColor("_BaseColor", vertexColors ? Color.white : color);
            mat.SetFloat("_VertexColors", vertexColors ? 1f : 0f);
            mat.SetFloat("_Faceted", vertexColors ? 1f : 0f);
            EditorUtility.SetDirty(mat);
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        static float SmoothMin(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        static float SmoothMax(float a, float b, float k) => -SmoothMin(-a, -b, k);

        /// <summary>Signed distance to a rounded rectangle (positive outside).</summary>
        static float RoundRectOutside(Rect r, float radius, float x, float z)
        {
            float qx = Mathf.Abs(x - r.center.x) - (r.width / 2f - radius);
            float qz = Mathf.Abs(z - r.center.y) - (r.height / 2f - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qz, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qz), 0f) - radius;
        }

        /// <summary>Distance from a point outside a rectangle to it (0 inside).</summary>
        static float RectDistance(Rect r, float x, float z)
        {
            float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax);
            float dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static float LaneDistance(Lane lane, float x, float z)
        {
            var p = new Vector2(x, z);
            float best = float.MaxValue;
            int count = lane.closed ? lane.points.Length : lane.points.Length - 1;
            for (int i = 0; i < count; i++)
            {
                var a = lane.points[i];
                var b = lane.points[(i + 1) % lane.points.Length];
                var ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }
    }
}
