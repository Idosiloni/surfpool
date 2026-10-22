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
