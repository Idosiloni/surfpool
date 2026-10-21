using UnityEngine;

namespace SurfPool
{
    /// <summary>Little helpers the shops share: spawning a prop model and tinting one of its materials.</summary>
    public static class ShopArt
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        /// <summary>Puts a copy of a model under a parent (random spin), optionally popping it in.</summary>
        public static GameObject Spawn(GameObject model, Transform parent, Vector3 localPosition, float scale, bool pop = false, bool randomSpin = true)
        {
            if (model == null) return new GameObject("Missing Model");
            var go = Object.Instantiate(model, parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = randomSpin ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
            go.transform.localScale = Vector3.one * scale;
            if (pop) go.AddComponent<PopIn>();
            return go;
        }

        /// <summary>Tints the parts of a model that use the named material (e.g. "Garment", "Paint", "MachineBody").</summary>
        public static void Tint(GameObject go, string materialName, Color color)
        {
            if (go == null) return;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].name.StartsWith(materialName)) continue;
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, i);
                    block.SetColor(BaseColor, color);
                    renderer.SetPropertyBlock(block, i);
                }
            }
        }
    }
}
