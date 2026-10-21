using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>Hides Uncle Kai's 12 lost treasures around the island, each with a soft light beam so you can spot it.</summary>
    public static class TreasureBuilder
    {
        struct Spot
        {
            public Vector3 position;
            public string title, note;
            public System.Action<Transform> look;
        }

        public static void Build()
        {
            var root = new GameObject("Treasures").transform;
            var gold = GreyboxBuilder.Mat("TreasureGold", new Color(1f, 0.8f, 0.25f), 0.8f, new Color(0.6f, 0.45f, 0.1f));
            var pink = GreyboxBuilder.Mat("ShellPink", new Color(1f, 0.7f, 0.75f), 0.6f, new Color(0.25f, 0.12f, 0.14f));
            var glass = GreyboxBuilder.Mat("BottleGlass", new Color(0.4f, 0.8f, 0.5f), 0.9f, new Color(0.05f, 0.2f, 0.1f));
            var brown = GreyboxBuilder.Mat("Coconut", new Color(0.45f, 0.3f, 0.18f), 0.2f);
            var white = GreyboxBuilder.Mat("Pearl", new Color(1f, 0.98f, 0.95f), 0.95f, new Color(0.3f, 0.3f, 0.3f));
            var black = GreyboxBuilder.Mat("Console", new Color(0.22f, 0.22f, 0.25f));
            var paper = GreyboxBuilder.Mat("Parchment", new Color(0.93f, 0.85f, 0.65f));
            var wood = GreyboxBuilder.Mat("DecorWood", new Color(0.62f, 0.43f, 0.27f));
            var beam = Beam();

            Spot[] spots =
            {
                new() { position = new(-72, -0.1f, -92), title = "Uncle Kai's lucky shell", note = "He always kept it in his pocket.",
                    look = t => { Part(t, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.5f, 0.25f, 0.4f), pink); Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.1f, -0.2f), new Vector3(0.15f, 0.05f, 0.15f), pink); } },
                new() { position = new(132, -0.1f, -90), title = "a message in a bottle", note = "\"Keep the waves rolling. - K\"",
                    look = t => { Part(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.18f, 0.25f, 0.18f), glass, 90f); Part(t, PrimitiveType.Cylinder, new Vector3(0, 0, 0.3f), new Vector3(0.08f, 0.06f, 0.08f), wood, 90f); } },
                new() { position = new(-74, -0.1f, 196), title = "Uncle Kai's first surfboard", note = "A tiny wooden board from 1979.",
                    look = t => Board(t, wood) },
                new() { position = new(134, -0.1f, 198), title = "a golden starfish", note = "Shiny!",
                    look = t => { for (int i = 0; i < 5; i++) { var arm = Part(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.1f, 0.05f, 0.45f), gold); arm.transform.localRotation = Quaternion.Euler(0, i * 72f, 0); arm.transform.localPosition = arm.transform.localRotation * new Vector3(0, 0, 0.18f); } } },
                new() { position = new(-66, -0.1f, 40), title = "some old sunglasses", note = "Very 80s.",
                    look = t => { Part(t, PrimitiveType.Cube, new Vector3(-0.12f, 0, 0), new Vector3(0.18f, 0.1f, 0.03f), black); Part(t, PrimitiveType.Cube, new Vector3(0.12f, 0, 0), new Vector3(0.18f, 0.1f, 0.03f), black); Part(t, PrimitiveType.Cube, new Vector3(0, 0.03f, 0), new Vector3(0.08f, 0.02f, 0.02f), gold); } },
                new() { position = new(128, -0.1f, 30), title = "a giant pearl", note = "Worth a fortune.",
                    look = t => Part(t, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.35f, white) },
                new() { position = new(30, -0.1f, -94), title = "a coconut with a smiley face", note = "Someone was bored.",
                    look = t => { Part(t, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.4f, brown); Part(t, PrimitiveType.Cube, new Vector3(0, 0.02f, -0.2f), new Vector3(0.2f, 0.04f, 0.02f), white); } },
                new() { position = new(40, -0.1f, GreyboxBuilder.LagoonZ + GreyboxBuilder.LagoonWidth / 2f + 30f), title = "Uncle Kai's surf trophy", note = "1st place, 1984 Pro Tour.",
                    look = t => { Part(t, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.25f, 0.06f, 0.25f), wood); Part(t, PrimitiveType.Cylinder, new Vector3(0, 0.25f, 0), new Vector3(0.08f, 0.2f, 0.08f), gold); Part(t, PrimitiveType.Sphere, new Vector3(0, 0.5f, 0), Vector3.one * 0.22f, gold); } },
                new() { position = new(-40, -0.1f, -70), title = "a scrap of treasure map", note = "There are more out there...",
                    look = t => Part(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.45f, 0.02f, 0.35f), paper) },
                new() { position = new(100, -0.1f, -60), title = "a lost action camera", note = "Full of wipeout videos.",
                    look = t => { Part(t, PrimitiveType.Cube, Vector3.zero, new Vector3(0.2f, 0.15f, 0.12f), black); Part(t, PrimitiveType.Cylinder, new Vector3(0, 0, -0.08f), new Vector3(0.08f, 0.02f, 0.08f), glass, 90f); } },
                new() { position = new(-50, -0.1f, 150), title = "a conch shell", note = "You can hear the ocean.",
                    look = t => { var c = Part(t, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.25f, 0.25f, 0.25f), pink, 90f); c.transform.localRotation = Quaternion.Euler(90f, 30f, 0f); } },
                new() { position = new(105, -0.1f, 160), title = "Uncle Kai's ukulele", note = "Still in tune!",
                    look = t => { Part(t, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.35f, 0.1f, 0.4f), wood); Part(t, PrimitiveType.Cube, new Vector3(0, 0.02f, 0.4f), new Vector3(0.06f, 0.04f, 0.5f), brown); } },
            };

            for (int i = 0; i < spots.Length; i++)
            {
                var spot = spots[i];
                var treasure = new GameObject($"Treasure {i + 1}: {spot.title}");
                treasure.transform.SetParent(root, false);
                treasure.transform.position = spot.position;
                var model = new GameObject("Model").transform;
                model.SetParent(treasure.transform, false);
                spot.look(model);
                var beamGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beamGo.name = "Glow Beam";
                Object.DestroyImmediate(beamGo.GetComponent<Collider>());
                beamGo.transform.SetParent(treasure.transform, false);
                beamGo.transform.localPosition = new Vector3(0, 3f, 0);
                beamGo.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
                var renderer = beamGo.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = beam;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var sphere = treasure.AddComponent<SphereCollider>();
                sphere.radius = 0.45f;
                var component = treasure.AddComponent<Treasure>();
                var so = new SerializedObject(component);
                so.FindProperty("index").intValue = i;
                so.FindProperty("title").stringValue = spot.title;
                so.FindProperty("note").stringValue = spot.note;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static GameObject Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material mat, float pitch = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static void Board(Transform parent, Material mat)
        {
            var go = new GameObject("Tiny Board", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.45f;
            go.transform.localRotation = Quaternion.Euler(0, 30f, 0);
            go.GetComponent<MeshFilter>().sharedMesh = WorldArt.ModelMesh("Surfboard");
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        /// <summary>A see-through glowing column so treasures can be spotted from far away.</summary>
        static Material Beam()
        {
            const string path = "Assets/_Game/Materials/TreasureBeam.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.45f, 0.22f));
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
