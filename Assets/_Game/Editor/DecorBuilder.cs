using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Builds the build-mode decorations as prefabs (Assets/_Game/Prefabs/Decor/) and the catalog listing them,
    /// plus the green/red ghost materials. Each prefab root has a trigger BoxCollider that marks its footprint
    /// (used to check if it fits) and solid colliders on its parts (so it blocks you and can be sold back).
    /// </summary>
    public static class DecorBuilder
    {
        const string Folder = "Assets/_Game/Prefabs/Decor";
        const string CatalogPath = "Assets/_Game/Settings/DecorCatalog.asset";

        public static DecorCatalog Catalog()
        {
            Directory.CreateDirectory(Folder);
            var catalog = AssetDatabase.LoadAssetAtPath<DecorCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DecorCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var wood = GreyboxBuilder.Mat("DecorWood", new Color(0.62f, 0.43f, 0.27f));
            var darkWood = GreyboxBuilder.Mat("TikiPole", new Color(0.45f, 0.3f, 0.18f));
            var flame = GreyboxBuilder.Mat("TikiFlame", new Color(1f, 0.55f, 0.1f), 0.2f, new Color(3f, 1.4f, 0.2f));
            var leaf = GreyboxBuilder.Mat("Leaf", new Color(0.25f, 0.62f, 0.3f));
            var planter = GreyboxBuilder.Mat("Planter", new Color(0.55f, 0.38f, 0.24f));
            var pink = GreyboxBuilder.Mat("FlowerPink", new Color(1f, 0.45f, 0.65f));
            var yellow = GreyboxBuilder.Mat("FlowerYellow", new Color(1f, 0.85f, 0.25f));
            var orange = GreyboxBuilder.Mat("FlowerOrange", new Color(1f, 0.55f, 0.2f));
            var teal = GreyboxBuilder.Mat("SeatTeal", new Color(0.1f, 0.68f, 0.62f), 0.35f);
            var white = GreyboxBuilder.Mat("Coping", new Color(0.97f, 0.96f, 0.92f), 0.3f);
            var sand = GreyboxBuilder.Mat("CourtSand", new Color(0.96f, 0.88f, 0.68f));
            var coral = GreyboxBuilder.Mat("Sign", new Color(1f, 0.42f, 0.34f), 0.4f);
            var net = GreyboxBuilder.Mat("Net", new Color(0.95f, 0.95f, 0.95f), 0.1f);

            catalog.items = new List<DecorCatalog.Item>
            {
                Make("palm", "Palm Tree", 150, 3, 0, new Vector3(1.2f, 6f, 1.2f), root =>
                {
                    WorldArt.Model("PalmTree", root, Vector3.zero);
                    Solid(root, new Vector3(0, 3f, 0), new Vector3(0.5f, 6f, 0.5f));
                }),
                Make("palm_tall", "Tall Palm", 220, 4, 0, new Vector3(1.2f, 7.5f, 1.2f), root =>
                {
                    WorldArt.Model("PalmTreeTall", root, Vector3.zero);
                    Solid(root, new Vector3(0, 3.7f, 0), new Vector3(0.5f, 7.5f, 0.5f));
                }),
                Make("umbrella_set", "Umbrella & Loungers", 250, 2, 2, new Vector3(2.8f, 2.5f, 2.6f), root =>
                {
                    WorldArt.Model("BeachUmbrellaTeal", root, new Vector3(0, 0, 0.9f));
                    WorldArt.Model("LoungeChair", root, new Vector3(-0.8f, 0, 0), 180f);
                    WorldArt.Model("LoungeChair", root, new Vector3(0.8f, 0, 0), 180f);
                    Solid(root, new Vector3(0, 0.3f, 0), new Vector3(2.4f, 0.6f, 2f));
                    Seat(root, new Vector3(-0.8f, 0.35f, 0f), 0f, true);
                    Seat(root, new Vector3(0.8f, 0.35f, 0f), 0f, true);
                }),
                Make("lounger", "Lounge Chair", 80, 1, 1, new Vector3(0.9f, 0.8f, 2.1f), root =>
                {
                    WorldArt.Model("LoungeChair", root, Vector3.zero, 180f);
                    Solid(root, new Vector3(0, 0.3f, 0), new Vector3(0.8f, 0.6f, 2f));
                    Seat(root, new Vector3(0, 0.35f, 0f), 0f, true);
                }),
                Make("flowers", "Flower Bed", 90, 2, 0, new Vector3(2.4f, 0.7f, 1f), root =>
                {
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.2f, 0), new Vector3(2.4f, 0.4f, 0.9f), planter, true);
                    var colors = new[] { pink, yellow, orange };
                    for (int i = 0; i < 5; i++)
                        Part(root, PrimitiveType.Sphere, new Vector3(-0.9f + i * 0.45f, 0.55f + (i % 2) * 0.08f, ((i % 3) - 1) * 0.15f),
                            Vector3.one * (i % 2 == 0 ? 0.42f : 0.34f), i % 2 == 0 ? leaf : colors[i % 3], false);
                }),
                Make("torch", "Tiki Torch", 60, 1, 0, new Vector3(0.4f, 2.1f, 0.4f), root =>
                {
                    Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.95f, 0), new Vector3(0.12f, 0.95f, 0.12f), darkWood, true);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 2.0f, 0), new Vector3(0.22f, 0.3f, 0.22f), flame, false);
                }),
                Make("bench", "Bench", 100, 1, 2, new Vector3(2.1f, 0.9f, 0.7f), root =>
                {
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.45f, 0), new Vector3(2f, 0.08f, 0.5f), wood, true);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.75f, 0.24f), new Vector3(2f, 0.4f, 0.06f), wood, true);
                    Part(root, PrimitiveType.Cube, new Vector3(-0.85f, 0.22f, 0), new Vector3(0.1f, 0.44f, 0.45f), darkWood, false);
                    Part(root, PrimitiveType.Cube, new Vector3(0.85f, 0.22f, 0), new Vector3(0.1f, 0.44f, 0.45f), darkWood, false);
                    Seat(root, new Vector3(-0.5f, 0.0f, -0.05f), 180f, false);
                    Seat(root, new Vector3(0.5f, 0.0f, -0.05f), 180f, false);
                }),
                Make("rock", "Big Rock", 40, 1, 0, new Vector3(2f, 1.4f, 2f), root =>
                {
                    WorldArt.Model("Rock", root, Vector3.zero, 0f, 1.6f);
                    Solid(root, new Vector3(0, 0.6f, 0), new Vector3(1.8f, 1.2f, 1.8f));
                }),
                Make("tiki", "Tiki Statue", 500, 6, 0, new Vector3(1.1f, 3.4f, 1.1f), root =>
                {
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.2f, 0), new Vector3(1.1f, 0.4f, 1.1f), white, true);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 1.5f, 0), new Vector3(0.8f, 2.2f, 0.7f), darkWood, true);
                    Part(root, PrimitiveType.Cube, new Vector3(-0.18f, 2.0f, -0.36f), new Vector3(0.2f, 0.16f, 0.04f), yellow, false);
                    Part(root, PrimitiveType.Cube, new Vector3(0.18f, 2.0f, -0.36f), new Vector3(0.2f, 0.16f, 0.04f), yellow, false);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 1.72f, -0.37f), new Vector3(0.14f, 0.3f, 0.06f), wood, false);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 1.35f, -0.36f), new Vector3(0.5f, 0.12f, 0.04f), coral, false);
                    for (int i = 0; i < 5; i++)
                        Part(root, PrimitiveType.Cube, new Vector3(-0.4f + i * 0.2f, 2.8f + (i % 2) * 0.15f, 0), new Vector3(0.12f, 0.6f, 0.12f), i % 2 == 0 ? leaf : coral, false);
                }),
                Make("volleyball", "Volleyball Court", 800, 5, 0, new Vector3(10f, 2.6f, 6f), root =>
                {
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.02f, 0), new Vector3(10f, 0.04f, 6f), sand, false);
                    foreach (float x in new[] { -4.9f, 4.9f })
                    foreach (float z in new[] { -2.95f, 2.95f })
                        Part(root, PrimitiveType.Cube, new Vector3(x * 0.98f, 0.045f, z), new Vector3(x < 0 ? 0.08f : 0.08f, 0.01f, 0.08f), white, false);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.045f, -2.95f), new Vector3(9.8f, 0.01f, 0.08f), white, false);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.045f, 2.95f), new Vector3(9.8f, 0.01f, 0.08f), white, false);
                    Part(root, PrimitiveType.Cube, new Vector3(-4.9f, 0.045f, 0), new Vector3(0.08f, 0.01f, 5.9f), white, false);
                    Part(root, PrimitiveType.Cube, new Vector3(4.9f, 0.045f, 0), new Vector3(0.08f, 0.01f, 5.9f), white, false);
                    Part(root, PrimitiveType.Cylinder, new Vector3(0, 1.2f, -3.3f), new Vector3(0.1f, 1.2f, 0.1f), white, true);
                    Part(root, PrimitiveType.Cylinder, new Vector3(0, 1.2f, 3.3f), new Vector3(0.1f, 1.2f, 0.1f), white, true);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 2.0f, 0), new Vector3(0.03f, 0.9f, 6.5f), net, false);
                    Part(root, PrimitiveType.Sphere, new Vector3(2.2f, 0.15f, 1.1f), Vector3.one * 0.25f, yellow, false);
                }),
            };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        public static Material Ghost(bool ok)
        {
            string path = $"Assets/_Game/Materials/Ghost{(ok ? "Ok" : "Bad")}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", ok ? new Color(0.3f, 1f, 0.55f, 0.45f) : new Color(1f, 0.3f, 0.25f, 0.45f));
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static DecorCatalog.Item Make(string id, string title, int cost, int beauty, int seats, Vector3 footprint, Action<Transform> build)
        {
            var root = new GameObject(title);
            build(root.transform);
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true; // footprint only: the parts are what you bump into
            box.size = footprint;
            box.center = new Vector3(0, footprint.y / 2f, 0);
            var decoration = root.AddComponent<Decoration>();
            var so = new SerializedObject(decoration);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("beauty").intValue = beauty;
            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{id}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return new DecorCatalog.Item { id = id, title = title, cost = cost, beauty = beauty, seats = seats, prefab = prefab };
        }

        static GameObject Part(Transform root, PrimitiveType type, Vector3 position, Vector3 scale, Material mat, bool solid)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(root, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Solid(Transform root, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Body", typeof(BoxCollider));
            go.transform.SetParent(root, false);
            var box = go.GetComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        /// <summary>A spot where a beach guest sits (bench) or lies down (lounger). Forward = the way they face.</summary>
        static void Seat(Transform root, Vector3 position, float yaw, bool lying)
        {
            var seat = new GameObject("Seat").transform;
            seat.SetParent(root, false);
            seat.localPosition = position;
            seat.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (lying) new GameObject("Lying").transform.SetParent(seat, false);
        }
    }
}
