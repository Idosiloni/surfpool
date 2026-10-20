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

        // Pool interior: x 0..60 (machine -> beach), z -12..12, deck top at y = 0, floor at y = -4.
        // Water sits low so the biggest waves crest just below the deck instead of spilling over it.
        const float PoolLength = 60f, PoolWidth = 24f, PoolDepth = 4f, WaterLevel = -2.2f;
        const float DeckThickness = PoolDepth + 1f;

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
            var deskMat = Mat("Desk", new Color(0.6f, 0.45f, 0.3f));
            var surferMat = Mat("Surfer", Color.white, 0.3f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Object.DestroyImmediate(GameObject.Find("Main Camera"));

            // Deck slabs around the pool. Their inner faces form the pool walls.
            var env = new GameObject("Environment").transform;
            float halfW = PoolWidth / 2f;
            Box("Deck North", env, new Vector3(30, -DeckThickness / 2, halfW + 14), new Vector3(100, DeckThickness, 28), deckMat);
            Box("Deck South", env, new Vector3(30, -DeckThickness / 2, -halfW - 14), new Vector3(100, DeckThickness, 28), deckMat);
            Box("Deck West", env, new Vector3(-10, -DeckThickness / 2, 0), new Vector3(20, DeckThickness, PoolWidth), deckMat);
            Box("Deck East", env, new Vector3(PoolLength + 10, -DeckThickness / 2, 0), new Vector3(20, DeckThickness, PoolWidth), deckMat);

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

            // Money.
            var game = new GameObject("Game");
            game.AddComponent<Wallet>();

            // Ticket desk on the south deck. The player stands on the pool side, customers on the far side.
            var desk = new GameObject("Ticket Desk").transform;
            desk.position = new Vector3(20, 0, -22.5f);
            Box("Counter", desk, desk.position + new Vector3(0, 0.55f, 0), new Vector3(3, 1.1f, 1), deskMat);
            Box("Register", desk, desk.position + new Vector3(0.6f, 1.25f, 0), new Vector3(0.5f, 0.3f, 0.4f), consoleMat);
            var register = desk.gameObject.AddComponent<TicketRegister>();

            // The route surfers walk. Move these points in the scene to change where they go.
            var route = new GameObject("Surfer Route").transform;
            var entrance = Point("Entrance", route, new Vector3(30, 0, -38));
            var deskApproach = Point("Desk Approach", route, new Vector3(20, 0, -28));
            var deskSpot = Point("Desk Spot", route, new Vector3(20, 0, -24.4f));
            var deskExit = Point("Desk Exit", route, new Vector3(24, 0, -24.4f));
            var poolSide = Point("Pool Side", route, new Vector3(24, 0, -14));
            var poolEntry = Point("Pool Entry", route, new Vector3(14, 0, -halfW - 0.6f));
            var lineup = Point("Lineup", route, new Vector3(12, WaterLevel, -4));
            float landingX = 57f;
            var landing = Point("Beach Landing", route, new Vector3(landingX, -PoolDepth + PoolDepth * (landingX - rampStart) / run, 0));
            var beachExit = Point("Beach Exit", route, new Vector3(PoolLength + 3, 0, -6));
            var exitCorner = Point("Exit Corner", route, new Vector3(PoolLength + 6, 0, -30));
            var exit = Point("Exit", route, new Vector3(45, 0, -40));

            var spawnerGo = new GameObject("Surfer Spawner");
            var spawner = spawnerGo.AddComponent<SurferSpawner>();
            Set(spawner, "surferPrefab", BuildSurferPrefab(surferMat));
            Set(spawner, "water", water);
            SetArray(spawner, "pathToDesk", entrance, deskApproach, deskSpot);
            SetArray(spawner, "pathToPool", deskExit, poolSide, poolEntry);
            Set(spawner, "lineup", lineup);
            Set(spawner, "beachLanding", landing);
            SetArray(spawner, "pathOut", beachExit, exitCorner, exit);
            Set(register, "spawner", spawner);

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

        static void SetArray(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform Point(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = position;
            return t;
        }

        /// <summary>Placeholder surfer: capsule body, sphere head, flat box board. Colors are randomized per surfer at runtime.</summary>
        static Surfer BuildSurferPrefab(Material mat)
        {
            const string path = "Assets/_Game/Prefabs/Surfer.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var root = new GameObject("Surfer");
            Transform Part(PrimitiveType type, string name, Vector3 scale)
            {
                var go = GameObject.CreatePrimitive(type);
                go.name = name;
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(root.transform, false);
                go.transform.localScale = scale;
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                return go.transform;
            }
            var body = Part(PrimitiveType.Capsule, "Body", new Vector3(0.5f, 0.8f, 0.5f));
            var head = Part(PrimitiveType.Sphere, "Head", new Vector3(0.3f, 0.3f, 0.3f));
            var board = Part(PrimitiveType.Cube, "Board", new Vector3(0.5f, 0.07f, 2f));
            body.localPosition = new Vector3(0, 0.8f, 0);
            head.localPosition = new Vector3(0, 1.75f, 0);

            var surfer = root.AddComponent<Surfer>();
            Set(surfer, "body", body);
            Set(surfer, "head", head);
            Set(surfer, "board", board);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Surfer>();
        }
    }
}
