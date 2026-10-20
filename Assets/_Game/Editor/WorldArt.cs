using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// World dressing for the greybox scene: the low-poly models from tools/blender/build_models.py, the sand
    /// island and ocean around the park, palms and sunbathing areas, sky, sun, fog and color grading.
    /// Called by <see cref="GreyboxBuilder"/>.
    /// </summary>
    public static class WorldArt
    {
        const string ModelFolder = "Assets/_Game/Models";

        /// <summary>Places a Blender model. Blender exports face 180° away from Unity's forward, so yaw adds 180.</summary>
        public static GameObject Model(string name, Transform parent, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{name}.fbx");
            if (asset == null)
            {
                Debug.LogWarning($"[WorldArt] Missing model {name}. Run tools/blender/build_models.py in Blender.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw + 180f, 0));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        public static Mesh ModelMesh(string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelFolder}/{name}.fbx");
            return asset != null ? asset.GetComponentInChildren<MeshFilter>().sharedMesh : null;
        }

        /// <summary>Sand island under the park, ocean to the horizon, palms, umbrellas, loungers and rocks.</summary>
        public static void BuildEnvironment(Material sandMat, Material waterMat)
        {
            var root = new GameObject("World").transform;

            // Sand all around the park (pools and their lots cover x -20..80, z -40..144), leaving the pool holes open.
            var sand = new GameObject("Sand Island").transform;
            sand.SetParent(root, false);
            SandSlab(sand, "North", new Vector3(30, -0.6f, 174), new Vector3(220, 1, 60), sandMat);
            SandSlab(sand, "South", new Vector3(30, -0.6f, -70), new Vector3(220, 1, 60), sandMat);
            SandSlab(sand, "West", new Vector3(-50, -0.6f, 52), new Vector3(60, 1, 184), sandMat);
            SandSlab(sand, "East", new Vector3(110, -0.6f, 52), new Vector3(60, 1, 184), sandMat);

            var ocean = new GameObject("Ocean", typeof(MeshFilter), typeof(MeshRenderer));
            ocean.transform.SetParent(root, false);
            ocean.transform.position = new Vector3(30, -0.75f, 0);
            ocean.GetComponent<MeshFilter>().sharedMesh = OceanMesh();
            ocean.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
            ocean.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var rng = new System.Random(42);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // Palms framing the deck and dotted over the sand.
            var palms = new GameObject("Palms").transform;
            palms.SetParent(root, false);
            Vector3[] palmSpots =
            {
                new(-18, 0, 14), new(-18, 0, -14), new(-3, 0, 16), new(12, 0, 15), new(26, 0, 16.5f), new(44, 0, 15),
                new(58, 0, 16), new(72, 0, 12), new(76, 0, -6), new(74, 0, -20), new(88, 0, -40), new(8, 0, -38),
                new(-14, 0, -34), new(36, 0, -38), new(52, 0, -30), new(-24, 0, 0),
                new(88, 0, 30), new(95, 0, -10), new(-35, 0, 25), new(-38, 0, -30), new(88, 0, 60), new(-30, 0, 70), new(90, 0, 110), new(-32, 0, 125),
                new(20, 0, -52), new(92, 0, -62), new(105, 0, 20), new(-45, 0, 5),
            };
            for (int i = 0; i < palmSpots.Length; i++)
            {
                var spot = palmSpots[i];
                float y = Mathf.Abs(spot.x - 30) > 50 || Mathf.Abs(spot.z) > 40 ? -0.1f : 0f;
                Model(i % 3 == 0 ? "PalmTreeTall" : "PalmTree", palms, new Vector3(spot.x, y, spot.z), Range(0, 360), Range(0.85f, 1.15f));
            }

            // Sunbathing deck on the north side: umbrellas with pairs of loungers facing the pool.
            var lounge = new GameObject("Sunbathing").transform;
            lounge.SetParent(root, false);
            for (int i = 0; i < 6; i++)
            {
                float x = 4 + i * 8f;
                Model(i % 2 == 0 ? "BeachUmbrellaTeal" : "BeachUmbrellaCoral", lounge, new Vector3(x, 0, 20f), Range(0, 360));
                Model("LoungeChair", lounge, new Vector3(x - 0.8f, 0, 19.2f), 180f);
                Model("LoungeChair", lounge, new Vector3(x + 0.8f, 0, 19.2f), 180f);
            }
            // A few more on the sand past the beach end.
            for (int i = 0; i < 4; i++)
                Model(i % 2 == 0 ? "BeachUmbrellaCoral" : "BeachUmbrellaTeal", lounge, new Vector3(86 + i * 5f, -0.1f, -8 + i * 6f), Range(0, 360));

            var rocks = new GameObject("Rocks").transform;
            rocks.SetParent(root, false);
            for (int i = 0; i < 30; i++)
            {
                float angle = Range(0, Mathf.PI * 2);
                float radius = Range(100, 120);
                var spot = new Vector3(30 + Mathf.Cos(angle) * radius, -0.3f, 52 + Mathf.Sin(angle) * radius * 1.3f);
                bool inPark = spot.x > -25 && spot.x < 85 && spot.z > -45 && spot.z < 150;
                if (inPark || spot.z < -95 || spot.z > 200) continue;
                Model("Rock", rocks, spot, Range(0, 360), Range(0.8f, 2.4f));
            }
        }

        static void SandSlab(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Sand " + name;
            slab.transform.SetParent(parent, false);
            slab.transform.position = center;
            slab.transform.localScale = size;
            slab.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        /// <summary>Warm afternoon sun, blue procedural sky, soft fog, and color grading.</summary>
        public static void Lighting(Light sun, Camera camera)
        {
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            var sky = SkyMaterial();
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0028f;
            RenderSettings.fogColor = new Color(0.72f, 0.86f, 0.96f);
            DynamicGI.UpdateEnvironment();

            var volumeGo = new GameObject("Post Processing", typeof(Volume));
            var volume = volumeGo.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = PostProfile();
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camera.farClipPlane = 1200f;
        }

        static Material SkyMaterial()
        {
            const string path = "Assets/_Game/Materials/Sky.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_SunSize", 0.05f);
            mat.SetFloat("_AtmosphereThickness", 0.75f);
            mat.SetColor("_SkyTint", new Color(0.42f, 0.66f, 1f));
            mat.SetColor("_GroundColor", new Color(0.55f, 0.75f, 0.85f));
            mat.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static VolumeProfile PostProfile()
        {
            const string path = "Assets/_Game/Settings/BeachPost.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.Neutral);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(18f);
            color.postExposure.Override(0.15f);
            color.contrast.Override(8f);
            var whiteBalance = profile.Add<WhiteBalance>(true);
            whiteBalance.temperature.Override(8f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.5f);

            foreach (var component in profile.components)
            {
                component.name = component.GetType().Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>
        /// A big flat grid with black vertex colors (no foam) for the stylized water shader, with a hole where the
        /// sand island is so it never covers the pools (the ocean sits above the pool water level).
        /// </summary>
        static Mesh OceanMesh()
        {
            const string path = "Assets/_Game/Models/OceanPlane.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            Mesh mesh;

            const int n = 80;
            const float size = 1600f;
            // Island footprint in the ocean's local space (ocean is centered at x = 30, z = 0).
            var hole = Rect.MinMaxRect(-80 - 30 + 2, -100 + 2, 140 - 30 - 2, 204 - 2);
            var vertices = new Vector3[(n + 1) * (n + 1)];
            var colors = new Color[vertices.Length];
            for (int z = 0; z <= n; z++)
            for (int x = 0; x <= n; x++)
            {
                vertices[z * (n + 1) + x] = new Vector3((x / (float)n - 0.5f) * size, 0, (z / (float)n - 0.5f) * size);
                colors[z * (n + 1) + x] = new Color(0, 0, 0, 1);
            }
            var triangles = new int[n * n * 6];
            int k = 0;
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int i = z * (n + 1) + x;
                var a = vertices[i];
                var b = vertices[i + n + 2];
                var cell = Rect.MinMaxRect(a.x, a.z, b.x, b.z);
                if (hole.Contains(cell.min) && hole.Contains(cell.max)) continue; // under the island
                triangles[k++] = i; triangles[k++] = i + n + 1; triangles[k++] = i + 1;
                triangles[k++] = i + 1; triangles[k++] = i + n + 1; triangles[k++] = i + n + 2;
            }
            System.Array.Resize(ref triangles, k);
            mesh = new Mesh { name = "OceanPlane", vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
