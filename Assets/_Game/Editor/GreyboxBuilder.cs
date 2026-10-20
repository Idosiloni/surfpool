using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Builds the Step 1 greybox scene from scratch: deck, pool, beach ramp, water,
    /// wave machine + button, and the first-person player. Menu: Surf Pool > Build Greybox Scene.
    /// Running it again overwrites the scene.
    /// </summary>
    public static class GreyboxBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Pool.unity";
        const string MaterialFolder = "Assets/_Game/Materials";

        // Pool interior: x 0..60 (machine -> beach), z -12..12, deck top at y = 0, floor at y = -3.
        const float PoolLength = 60f, PoolWidth = 24f, PoolDepth = 3f, WaterLevel = -1.5f;

        [MenuItem("Surf Pool/Build Greybox Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            Directory.CreateDirectory(MaterialFolder);

            var deckMat = Mat("Deck", new Color(0.72f, 0.72f, 0.72f));
            var tileMat = Mat("PoolTile", new Color(0.82f, 0.9f, 0.95f));
            var sandMat = Mat("Sand", new Color(0.9f, 0.83f, 0.62f));
            var waterMat = Mat("Water", new Color(0.1f, 0.45f, 0.78f), 0.9f);
            var machineMat = Mat("Machine", new Color(0.35f, 0.37f, 0.4f));
            var consoleMat = Mat("Console", new Color(0.22f, 0.22f, 0.25f));
            var buttonMat = Mat("Button", Color.white, 0.6f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Object.DestroyImmediate(GameObject.Find("Main Camera"));

            // Deck slabs around the pool. Their inner faces form the pool walls.
            var env = new GameObject("Environment").transform;
            float halfW = PoolWidth / 2f;
            Box("Deck North", env, new Vector3(30, -2, halfW + 14), new Vector3(100, 4, 28), deckMat);
            Box("Deck South", env, new Vector3(30, -2, -halfW - 14), new Vector3(100, 4, 28), deckMat);
            Box("Deck West", env, new Vector3(-10, -2, 0), new Vector3(20, 4, PoolWidth), deckMat);
            Box("Deck East", env, new Vector3(PoolLength + 10, -2, 0), new Vector3(20, 4, PoolWidth), deckMat);

            // Tiled pool lining, floor, and the sloped beach the waves run up.
            var pool = new GameObject("Pool").transform;
            pool.SetParent(env);
            float wallY = -PoolDepth / 2f;
            Box("Wall North", pool, new Vector3(PoolLength / 2, wallY, halfW - 0.05f), new Vector3(PoolLength, PoolDepth, 0.1f), tileMat);
            Box("Wall South", pool, new Vector3(PoolLength / 2, wallY, -halfW + 0.05f), new Vector3(PoolLength, PoolDepth, 0.1f), tileMat);
            Box("Wall Machine", pool, new Vector3(0.05f, wallY, 0), new Vector3(0.1f, PoolDepth, PoolWidth), tileMat);
            Box("Floor", pool, new Vector3(PoolLength / 2, -PoolDepth - 0.5f, 0), new Vector3(PoolLength, 1, PoolWidth), tileMat);

            const float rampStart = 45f;
            float run = PoolLength - rampStart;
            float rampLength = Mathf.Sqrt(run * run + PoolDepth * PoolDepth);
            float rampAngle = Mathf.Atan2(PoolDepth, run) * Mathf.Rad2Deg;
            var ramp = Box("Beach", pool, new Vector3(rampStart + run / 2, -PoolDepth / 2 - 0.15f, 0), new Vector3(rampLength, 0.3f, PoolWidth), sandMat);
            ramp.transform.rotation = Quaternion.Euler(0, 0, rampAngle);

            // Invisible railing so the player can't fall into the pool (the water has no collider yet).
            var rails = new GameObject("Pool Edge Blockers").transform;
            rails.SetParent(env);
            Blocker("Blocker North", rails, new Vector3(PoolLength / 2, 0.75f, halfW + 0.1f), new Vector3(PoolLength, 1.5f, 0.2f));
            Blocker("Blocker South", rails, new Vector3(PoolLength / 2, 0.75f, -halfW - 0.1f), new Vector3(PoolLength, 1.5f, 0.2f));
            Blocker("Blocker Machine", rails, new Vector3(-0.1f, 0.75f, 0), new Vector3(0.2f, 1.5f, PoolWidth));
            Blocker("Blocker Beach", rails, new Vector3(PoolLength + 0.1f, 0.75f, 0), new Vector3(0.2f, 1.5f, PoolWidth));

            // Water surface.
            var waterGo = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
            waterGo.transform.position = new Vector3(0, WaterLevel, 0);
            waterGo.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
            var water = waterGo.AddComponent<WaveWater>();

            // Wave machine housing behind the deep end.
            var machineGo = Box("Wave Machine", null, new Vector3(-9, 2.5f, 0), new Vector3(10, 5, PoolWidth - 2), machineMat);
            var machine = machineGo.AddComponent<WaveMachine>();
            Set(machine, "water", water);

            // Control console with the big button, on the deck by the deep end.
            var console = new GameObject("Wave Console").transform;
            console.position = new Vector3(5, 0, -halfW - 1.5f);
            Box("Pedestal", console, console.position + new Vector3(0, 0.5f, 0), new Vector3(0.8f, 1f, 0.6f), consoleMat);
            var buttonGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonGo.name = "Button";
            buttonGo.transform.SetParent(console, false);
            buttonGo.transform.localPosition = new Vector3(0, 1.04f, 0);
            buttonGo.transform.localScale = new Vector3(0.32f, 0.04f, 0.32f);
            Object.DestroyImmediate(buttonGo.GetComponent<Collider>());
            buttonGo.AddComponent<BoxCollider>();
            buttonGo.GetComponent<MeshRenderer>().sharedMaterial = buttonMat;
            var button = console.gameObject.AddComponent<WaveButton>();
            Set(button, "machine", machine);
            Set(button, "buttonRenderer", buttonGo.GetComponent<MeshRenderer>());

            // First-person player.
            var player = new GameObject("Player");
            player.transform.SetPositionAndRotation(new Vector3(7, 0.05f, -halfW - 5.5f), Quaternion.Euler(0, 10, 0));
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            var camGo = new GameObject("Player Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0, 1.65f, 0);
            var cam = camGo.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 70f;

            var fps = player.AddComponent<FirstPersonController>();
            Set(fps, "cameraPivot", camGo.transform);
            var interactor = player.AddComponent<PlayerInteractor>();
            Set(interactor, "playerCamera", cam);
            var hud = player.AddComponent<SimpleHUD>();
            Set(hud, "interactor", interactor);
            Set(hud, "player", fps);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[GreyboxBuilder] Built " + ScenePath);
        }

        static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static void Blocker(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent, true);
            go.transform.position = center;
            go.GetComponent<BoxCollider>().size = size;
        }

        static Material Mat(string name, Color color, float smoothness = 0.2f)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
