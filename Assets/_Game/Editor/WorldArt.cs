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
        const float N = GreyboxBuilder.MainNorth; // the main pool's north wall (north-deck dressing hangs off it)
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

            // Flat sand under the park except the zones' water (their decks sit on top of the sand). Cut into rectangles
            // along every edge of the holes, merged along rows so there are only a few slabs. Outside it: Shore's dunes.
            float DecksNorth = GreyboxBuilder.ZonesNorth + 16f;
            var sand = new GameObject("Sand Island").transform;
            sand.SetParent(root, false);
            var flats = Shore.FlatRects;
            var holes = GreyboxBuilder.WaterRects;
            var xs = new System.Collections.Generic.SortedSet<float>();
            var zs = new System.Collections.Generic.SortedSet<float>();
            foreach (var r in flats)
            {
                xs.Add(r.xMin); xs.Add(r.xMax);
                zs.Add(r.yMin); zs.Add(r.yMax);
            }
            foreach (var h in holes)
            {
                xs.Add(h.xMin); xs.Add(h.xMax);
                zs.Add(h.yMin); zs.Add(h.yMax);
            }
            var xa = new System.Collections.Generic.List<float>(xs);
            var za = new System.Collections.Generic.List<float>(zs);
            bool Solid(float x, float z)
            {
                foreach (var h in holes) if (h.Contains(new Vector2(x, z))) return false;
                foreach (var r in flats) if (r.Contains(new Vector2(x, z))) return true;
                return false;
            }
            int slabs = 0;
            for (int j = 0; j < za.Count - 1; j++)
            {
                float z0 = za[j], z1 = za[j + 1], zm = (z0 + z1) / 2f;
                for (int i = 0; i < xa.Count - 1;)
                {
                    if (!Solid((xa[i] + xa[i + 1]) / 2f, zm)) { i++; continue; }
                    int k = i;
                    while (k + 1 < xa.Count - 1 && Solid((xa[k + 1] + xa[k + 2]) / 2f, zm)) k++;
                    float x0 = xa[i], x1 = xa[k + 1];
                    SandSlab(sand, $"{slabs++}", new Vector3((x0 + x1) / 2f, Shore.SandTop - 0.5f, (z0 + z1) / 2f), new Vector3(x1 - x0, 1, z1 - z0), sandMat);
                    i = k + 1;
                }
            }
            Shore.BuildGround(root, sandMat);
            Shore.Dress(root);
            Shore.FarIslands(root);

            var ocean = new GameObject("Ocean", typeof(MeshFilter), typeof(MeshRenderer));
            ocean.transform.SetParent(root, false);
            ocean.transform.position = new Vector3(0f, Shore.SeaLevel, 0f);
            ocean.GetComponent<MeshFilter>().sharedMesh = Shore.OceanMesh();
            ocean.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
            ocean.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var rng = new System.Random(42);
            float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // Palms framing the deck and dotted over the sand.
            var palms = new GameObject("Palms").transform;
            palms.SetParent(root, false);
            Vector3[] palmSpots =
            {
                new(-18, 0, N + 2f), new(-18, 0, -14), new(-3, 0, N + 4f), new(12, 0, N + 3f), new(26, 0, N + 4.5f), new(44, 0, N + 3f),
                new(58, 0, N + 4f), new(72, 0, N), new(76, 0, -6), new(74, 0, -20), new(88, 0, -40), new(8, 0, -38),
                new(-14, 0, -34), new(36, 0, -38), new(52, 0, -30), new(-24, 0, 0),
                new(83, 0, 30), new(-35, 0, 25), new(-38, 0, -30), new(88, 0, 62), new(-30, 0, 70), new(90, 0, 110), new(-32, 0, 125),
                new(20, 0, -52), new(92, 0, -62), new(83, 0, -20), new(-45, 0, 5), new(240, 0, -20), new(238, 0, 30), new(200, 0, -66),
            };
            bool Blocked(Vector3 spot)
            {
                foreach (var h in GreyboxBuilder.WaterRects)
                    if (spot.x > h.xMin - 2.5f && spot.x < h.xMax + 2.5f && spot.z > h.yMin - 2.5f && spot.z < h.yMax + 2.5f) return true;
                return spot.x > GreyboxBuilder.SandbarWest - 1f && spot.x < GreyboxBuilder.SandbarEast + 1f && spot.z > -40f && spot.z < DecksNorth;
            }
            for (int i = 0; i < palmSpots.Length; i++)
            {
                var spot = palmSpots[i];
                if (Blocked(spot)) continue;
                float y = Shore.InFlat(spot.x, spot.z) && !(Mathf.Abs(spot.x - 30) > 50 || spot.z < -40f || spot.z > N + GreyboxBuilder.MainNorthDeck) ? 0f : Shore.Height(spot.x, spot.z);
                Model(i % 3 == 0 ? "PalmTreeTall" : "PalmTree", palms, new Vector3(spot.x, y, spot.z), Range(0, 360), Range(0.85f, 1.15f));
            }

            // Sunbathing on the grandstand pier north of the main pool: umbrellas with pairs of loungers facing the pool,
            // behind where the stands go (the walk to the Big Wave zone runs along the pier's far edge).
            var lounge = new GameObject("Sunbathing").transform;
            lounge.SetParent(root, false);
            for (int i = 0; i < 6; i++)
            {
                float x = 4 + i * 8f;
                Model(i % 2 == 0 ? "BeachUmbrellaTeal" : "BeachUmbrellaCoral", lounge, new Vector3(x, 0, N + 7.6f), Range(0, 360));
                Model("LoungeChair", lounge, new Vector3(x - 0.8f, 0, N + 6.8f), 180f);
                Model("LoungeChair", lounge, new Vector3(x + 0.8f, 0, N + 6.8f), 180f);
            }

            var rocks = new GameObject("Rocks").transform;
            rocks.SetParent(root, false);
            for (int i = 0; i < 30; i++)
            {
                float angle = Range(0, Mathf.PI * 2);
                float radius = Range(100, 120);
                var spot = new Vector3(30 + Mathf.Cos(angle) * radius, 0f, 52 + Mathf.Sin(angle) * radius * 1.3f);
                bool inPark = Shore.FlatDistance(spot.x, spot.z) < 4f;
                if (inPark || spot.z < -95 || spot.z > 238) continue;
                spot.y = Mathf.Max(Shore.Height(spot.x, spot.z), Shore.SeaLevel - 0.6f) - 0.2f; // in the sea they stick out of the water
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

        /// <summary>Warm afternoon sun, cartoon sky with clouds, soft fog, and color grading.</summary>
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
            RenderSettings.ambientIntensity = 0.8f; // the cartoon sky is bright; more washes the park out
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0016f; // light haze: the far islands fade into the horizon
            RenderSettings.fogColor = new Color(0.74f, 0.87f, 0.96f); // = the sky's horizon color
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
                mat = new Material(Shader.Find("SurfPool/StylizedSky"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = Shader.Find("SurfPool/StylizedSky");
            mat.SetColor("_SkyTint", new Color(0.24f, 0.5f, 0.93f));
            mat.SetColor("_HorizonColor", new Color(0.74f, 0.87f, 0.96f));
            mat.SetFloat("_Exposure", 1f);
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
    }
}
