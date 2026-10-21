using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Shared tinted copies of materials. Painting people and boards with property blocks switches off URP's SRP
    /// Batcher for them (thousands of extra set-pass calls with a full park), so instead every (material, color,
    /// texture) combo gets one material copy that everyone wearing it shares. The copy keeps the original name, so
    /// code that picks slots by material name still works after repainting.
    /// </summary>
    public static class PaintCache
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly Dictionary<(Material, Color32, Texture), Material> cache = new();
        static readonly Dictionary<Material, Material> source = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cache.Clear();
            source.Clear();
        }

        /// <summary>The original material behind a painted copy (or the material itself if it isn't one).</summary>
        public static Material Original(Material material) =>
            material != null && source.TryGetValue(material, out var original) ? original : material;

        /// <summary>A shared copy of <paramref name="material"/> tinted <paramref name="color"/> (and textured, if given).</summary>
        public static Material Get(Material material, Color color, Texture texture = null)
        {
            material = Original(material);
            var key = (material, (Color32)color, texture);
            if (cache.TryGetValue(key, out var painted) && painted != null) return painted;
            painted = new Material(material) { name = material.name };
            painted.SetColor(BaseColor, color);
            if (texture != null || material.HasProperty(BaseMap)) painted.SetTexture(BaseMap, texture);
            cache[key] = painted;
            source[painted] = material;
            return painted;
        }
    }
}
