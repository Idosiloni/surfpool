using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// The luxury tiers' look (<see cref="ParkGlowUp.TierEarnings"/>: beach club, resort, luxury): the deck paint for
    /// each tier (wooden boardwalk, sandstone tiles, marble) with its textures, made here as PNGs. Called by
    /// <see cref="GlowUpBuilder"/>.
    /// </summary>
    public static class LuxuryBuilder
    {
        const string Folder = "Assets/_Game/Materials";

        /// <summary>Fills the glow-up controller's tier paint: deck, coping and pool tiles for tiers 1-3.</summary>
        public static void Paint(ParkGlowUp glow)
        {
            var wood = Textured("DeckWood", WoodTexture());
            var stone = Textured("DeckStone", StoneTexture());
            var marble = Textured("DeckMarble", MarbleTexture(), 0.45f);
            var copingStone = GreyboxBuilder.Mat("CopingStone", new Color(0.9f, 0.82f, 0.67f), 0.3f);
            var copingMarble = GreyboxBuilder.Mat("CopingMarble", new Color(0.98f, 0.97f, 0.95f), 0.6f);
            var tileResort = TileMat("PoolTileResort", new Color(0.45f, 0.8f, 0.86f));
            var tileLuxury = TileMat("PoolTileLuxury", new Color(0.32f, 0.64f, 0.84f));

            var so = new SerializedObject(glow);
            var tiers = so.FindProperty("tierPaint");
            var sets = new[]
            {
                new Material[] { wood, null, null },
                new Material[] { stone, copingStone, tileResort },
                new Material[] { marble, copingMarble, tileLuxury },
            };
            tiers.arraySize = sets.Length;
            for (int t = 0; t < sets.Length; t++)
            {
                var materials = tiers.GetArrayElementAtIndex(t).FindPropertyRelative("materials");
                materials.arraySize = sets[t].Length;
                for (int i = 0; i < sets[t].Length; i++) materials.GetArrayElementAtIndex(i).objectReferenceValue = sets[t][i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- props: what each tier adds (and takes away) ----------

        /// <summary>
        /// Places the luxury props once the rest of the park is built, only on free deck (away from every walk, the
        /// water and the things already there):
        /// beach club = loungers under beach umbrellas along the sandbar and the north decks;
        /// resort = shade sails over the sandbar loungers (the umbrellas go) and cabanas on the north decks (the
        /// pools also glow after dark: ParkGlowUp.UpdatePoolLights);
        /// luxury = fire pits with loungers between the cabanas and a fountain at the gate.
        /// Props on a zone's deck wait for that zone (GlowItem.requires).
        /// </summary>
        public static void Props()
        {
            var root = new GameObject("Luxury").transform;
            var segments = LayoutCheck.RouteSegments();
            var taken = new System.Collections.Generic.List<Rect>();
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (renderer is ParticleSystemRenderer) continue;
                var b = renderer.bounds;
                if (b.size.x > 9f || b.size.z > 9f || b.max.y < 0.05f || b.min.y > 2.5f) continue; // slabs, sand, sky things
                taken.Add(Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z));
            }
            bool Free(Vector2 c, float r) => FreeArea(c, new Vector2(r, r));
            // A box of half-size `half` around c: clear of walks (1 m), water and what's already there.
            bool FreeArea(Vector2 c, Vector2 half)
            {
                Vector2 min = c - half, max = c + half;
                if (LayoutCheck.Gap(min, max, segments) < 1f) return false;
                var area = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                foreach (var water in GreyboxBuilder.WaterRects) if (water.Overlaps(area)) return false;
                foreach (var t in taken) if (t.Overlaps(area)) return false;
                return true;
            }
            void Take(Vector2 c, float r) => taken.Add(Rect.MinMaxRect(c.x - r, c.y - r, c.x + r, c.y + r));

            // The sandbar: west strip (beside the main pool and the Big Wave zone, facing them) and east strip (the
            // Reef and the Cove's west half). Umbrellas until the resort, then shade sails.
            foreach (var (x, yaw, eastSide) in new[] { (GreyboxBuilder.SandbarWest + 1.9f, 90f, false), (GreyboxBuilder.SandbarEast - 1.4f, -90f, true) })
                for (float z = GreyboxBuilder.MainSouth + 1f; z < GreyboxBuilder.ZonesNorth - 1f; z += 1f)
                {
                    var c = new Vector2(x, z);
                    if (!FreeArea(c, new Vector2(1.3f, 2.7f))) continue; // the umbrella and the sail's posts reach along the strip
                    string requires = eastSide ? (z < GreyboxBuilder.MainNorth + 4f ? "pool_reef" : "pool_cove")
                        : (z < GreyboxBuilder.MainNorth + 4f ? "" : "pool_lagoon");
                    var at = new Vector3(x, 0f, z);
                    Loungers(root, "Sandbar Loungers", at, yaw, 1, 0, requires);
                    Umbrella(root, at, yaw, 2, requires);
                    var sail = Group("Shade Sail", root, at, 2, 0, requires);
                    Model("ShadeSail", sail.transform, at, 90f, 0.72f); // its long side along the strip, posts toward the water
                    Take(c, 5.5f);
                }

            // The Cove's north deck (the Big Wave zone's deck sets come with its dressing, see DeckSet).
            float row = GreyboxBuilder.ZonesNorth + 7f;
            int slot = 0;
            for (float x = GreyboxBuilder.SandbarEast + 3f; x < GreyboxBuilder.CoveEastBeach - 2f; x += 1f)
            {
                var c = new Vector2(x, row);
                if (!FreeArea(c, new Vector2(2.7f, 2.3f))) continue;
                DeckSet(root, new Vector3(x, 0f, row), 0f, slot++ % 2 == 0, "pool_cove");
                Take(c, 6f);
            }

            // The gate: a fountain beside the entrance (luxury).
            var gate = new Vector2(30f, -40f);
            for (float d = 6f; d < 26f; d += 1f)
            {
                bool placed = false;
                foreach (float side in new[] { -1f, 1f })
                {
                    var c = gate + new Vector2(side * (d + 3f), 3f);
                    if (!Free(c, 2.4f)) continue;
                    var at = new Vector3(c.x, 0f, c.y);
                    var fountain = Group("Fountain", root, at, 3, 0, "");
                    Model("Fountain", fountain.transform, at, 0f);
                    Spray(fountain, at + Vector3.up * 2.35f);
                    Solid(fountain, at + new Vector3(0f, 0.6f, 0f), new Vector3(3.9f, 1.2f, 3.9f));
                    Take(c, 2.4f);
                    placed = true;
                    break;
                }
                if (placed) break;
            }
        }

        /// <summary>
        /// A spot on a deck that grows with the tiers, facing the water at `yaw` (0 = toward -z): two loungers under a
        /// beach umbrella, which make way for a cabana (resort) or, on the other spots, for a fire pit with three
        /// loungers round it (luxury).
        /// </summary>
        internal static void DeckSet(Transform parent, Vector3 at, float yaw, bool cabanaSpot, string requires)
        {
            int until = cabanaSpot ? 2 : 3;
            Loungers(parent, "Deck Loungers", at, yaw, 1, until, requires);
            Umbrella(parent, at, yaw, until, requires);
            var turn = Quaternion.Euler(0f, yaw, 0f);
            if (cabanaSpot)
            {
                var cabana = Group("Cabana", parent, at, 2, 0, requires);
                Model("Cabana", cabana.transform, at, yaw);
                Solid(cabana, at + turn * new Vector3(0f, 0.35f, 0.35f), new Vector3(2.3f, 0.7f, 1.9f));
                return;
            }
            var pit = Group("Fire Pit", parent, at, 3, 0, requires);
            Model("FirePit", pit.transform, at, yaw);
            Flames(pit);
            Solid(pit, at + new Vector3(0f, 0.22f, 0f), new Vector3(1.6f, 0.45f, 1.6f));
            for (int k = 0; k < 3; k++)
            {
                float a = yaw + (k - 1) * 55f; // three loungers round the fire, open toward the water
                Model("LoungeChair", pit.transform, at + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 2.1f), a); // feet toward the fire
            }
        }

        // Two loungers side by side facing `yaw` (0 = feet toward -z).
        static GameObject Loungers(Transform parent, string name, Vector3 at, float yaw, int fromTier, int untilTier, string requires)
        {
            var set = Group(name, parent, at, fromTier, untilTier, requires);
            var turn = Quaternion.Euler(0f, yaw, 0f);
            foreach (float side in new[] { -0.48f, 0.48f })
                Model("LoungeChair", set.transform, at + turn * new Vector3(side, 0f, 0f), yaw);
            return set;
        }

        // A beach umbrella beside a pair of loungers (not at their heads, so its canopy stays off a walk behind them).
        static void Umbrella(Transform parent, Vector3 at, float yaw, int untilTier, string requires)
        {
            var spot = at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(1.25f, 0f, 0.3f);
            var umbrella = Group("Beach Umbrella", parent, spot, 1, untilTier, requires);
            Model(Mathf.Repeat(spot.x, 2f) < 1f ? "BeachUmbrellaTeal" : "BeachUmbrellaCoral", umbrella.transform, spot, 0f);
        }

        static GameObject Group(string name, Transform parent, Vector3 at, int fromTier, int untilTier, string requires)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var item = go.AddComponent<GlowItem>();
            item.fromTier = fromTier;
            item.untilTier = untilTier;
            item.requires = requires ?? "";
            go.SetActive(false); // ParkGlowUp shows it from the right tier on
            EditorUtility.SetDirty(item);
            return go;
        }

        static GameObject Model(string name, Transform parent, Vector3 at, float yaw, float scale = 1f)
        {
            var go = WorldArt.Model(name, parent, at, yaw, scale);
            if (go != null) foreach (var collider in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            return go;
        }

        /// <summary>A box collider (world center and size) so the player bumps into the prop.</summary>
        static void Solid(GameObject go, Vector3 center, Vector3 size)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = go.transform.InverseTransformPoint(center);
            box.size = size;
        }

        /// <summary>The fire pit's flames glow orange, with sparks drifting up.</summary>
        static void Flames(GameObject pit)
        {
            var flame = GreyboxBuilder.Mat("FlameGlow", new Color(1f, 0.6f, 0.2f), 0.4f, new Color(3.2f, 1.3f, 0.3f));
            foreach (var renderer in pit.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Bulb")) mats[i] = flame;
                renderer.sharedMaterials = mats;
            }
            var sparks = Particles("Sparks", pit.transform, pit.transform.position + Vector3.up * 0.7f, new Color(1f, 0.7f, 0.3f), 6f, 1.6f, 0.06f, 0.8f,
                GlowUpBuilder.SparkMaterial("SparkFire", true, 1.5f));
            var shape = sparks.shape;
            shape.radius = 0.25f;
        }

        /// <summary>Water spilling from the fountain's top bowl.</summary>
        static void Spray(GameObject fountain, Vector3 top)
        {
            var spray = Particles("Spray", fountain.transform, top, new Color(0.85f, 0.95f, 1f, 0.8f), 40f, 1.4f, 0.12f, 1.1f,
                GlowUpBuilder.SparkMaterial("SparkWater", false, 1f));
            var main = spray.main;
            main.gravityModifier = 1f;
            var shape = spray.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.1f;
        }

        static ParticleSystem Particles(string name, Transform parent, Vector3 at, Color color, float rate, float speed, float size, float life, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = color;
            main.startSpeed = speed;
            main.startSize = size;
            main.startLifetime = life;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        /// <summary>A white material showing a deck texture that covers 2 x 2 m (the decks' UVs are in meters).</summary>
        static Material Textured(string name, Texture2D texture, float smoothness = 0.15f)
        {
            var mat = GreyboxBuilder.Mat(name, Color.white, smoothness);
            mat.SetTexture("_BaseMap", texture);
            mat.SetTextureScale("_BaseMap", new Vector2(0.5f, 0.5f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material TileMat(string name, Color color)
        {
            var mat = GreyboxBuilder.Mat(name, color, 0.45f);
            mat.SetTexture("_BaseMap", GreyboxBuilder.PoolTileTexture());
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- textures (2 x 2 m each, 512 px; soft contrast so big decks don't shimmer far away) ----------

        const int Size = 512, PerMeter = Size / 2;

        static float Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)(a * 374761393 + b * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h ^ (h >> 16)) / (float)uint.MaxValue;
            }
        }

        /// <summary>Boardwalk planks 25 cm wide running along x, each 1 m long with staggered joints and a faint grain.</summary>
        static Texture2D WoodTexture() => Texture("DeckWood", (x, y) =>
        {
            const int plank = PerMeter / 4;
            int row = y / plank, inRow = y % plank;
            int shift = Mathf.FloorToInt(Hash(row, 3) * PerMeter);
            int along = (x + shift) % Size, piece = (x + shift) / PerMeter;
            int joint = along % PerMeter;
            var color = new Color(0.8f, 0.62f, 0.43f) * (0.9f + 0.12f * Hash(row, piece));
            float grain = 1f + 0.035f * Mathf.Sin(x * 0.045f + row * 2.3f + 1.6f * Mathf.Sin(y * 0.21f + piece));
            color *= grain;
            if (inRow < 2 || joint < 2) color *= 0.62f; // the gaps between planks
            return color;
        });

        /// <summary>Warm sandstone tiles, 50 cm, in rows offset by half a tile, with light grout and a fine speckle.</summary>
        static Texture2D StoneTexture() => Texture("DeckStone", (x, y) =>
        {
            const int tile = PerMeter / 2;
            int row = y / tile;
            int shifted = x + (row % 2) * tile / 2;
            int col = shifted / tile;
            int cx = shifted % tile, cy = y % tile;
            var color = new Color(0.93f, 0.85f, 0.7f) * (0.94f + 0.08f * Hash(row, col));
            color *= 0.985f + 0.03f * Hash(x, y);
            int edge = Mathf.Min(Mathf.Min(cx, tile - 1 - cx), Mathf.Min(cy, tile - 1 - cy));
            if (edge < 2) color = Color.Lerp(color, new Color(0.8f, 0.76f, 0.7f), 0.8f);
            return color;
        });

        /// <summary>Big white marble slabs (1 m) with thin seams and soft grey veins.</summary>
        static Texture2D MarbleTexture() => Texture("DeckMarble", (x, y) =>
        {
            const int slab = PerMeter;
            int cx = x % slab, cy = y % slab;
            var color = new Color(0.97f, 0.96f, 0.94f) * (0.97f + 0.03f * Hash(x / slab, y / slab));
            float warp = Mathf.PerlinNoise(x * 0.012f, y * 0.012f) * 60f;
            float vein = Mathf.Abs(Mathf.Sin((x + y * 0.6f + warp) * 0.035f));
            color = Color.Lerp(color, new Color(0.8f, 0.8f, 0.83f), Mathf.SmoothStep(0.94f, 1f, vein) * 0.5f);
            int edge = Mathf.Min(Mathf.Min(cx, slab - 1 - cx), Mathf.Min(cy, slab - 1 - cy));
            if (edge < 1) color *= 0.86f;
            return color;
        });

        /// <summary>Writes (once) a repeating, mipmapped texture to Materials/name.png.</summary>
        static Texture2D Texture(string name, Func<int, int, Color> pixel)
        {
            string path = $"{Folder}/{name}.png";
            if (!System.IO.File.Exists(path))
            {
                var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
                var pixels = new Color[Size * Size];
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var c = pixel(x, y);
                    c.a = 1f;
                    pixels[y * Size + x] = c;
                }
                tex.SetPixels(pixels);
                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.mipmapEnabled = true;
                    importer.anisoLevel = 8;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
