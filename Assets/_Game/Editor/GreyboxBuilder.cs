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
        const float PoolLength = 60f, PoolWidth = 24f, PoolDepth = 5f, WaterLevel = -3f;
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
            var greenMat = Mat("ButtonGreen", new Color(0.2f, 0.85f, 0.3f), 0.6f);
            var redMat = Mat("ButtonRed", new Color(0.9f, 0.2f, 0.15f), 0.6f);
            var screenMat = Mat("Screen", new Color(0.05f, 0.1f, 0.15f), 0.9f, new Color(0.2f, 0.5f, 0.9f));
            var rentalBoardMat = Mat("RentalBoard", new Color(1f, 0.85f, 0.15f), 0.4f);
            var beaconMat = Mat("Beacon", new Color(1f, 0.85f, 0.1f), 0.5f, new Color(2f, 1.6f, 0.2f));

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
            Box("Pedestal", console, console.position + new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1f, 0.7f), consoleMat);
            var buttonGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonGo.name = "Button";
            buttonGo.transform.SetParent(console, false);
            buttonGo.transform.localPosition = new Vector3(0, 1.04f, 0.12f);
            buttonGo.transform.localScale = new Vector3(0.32f, 0.04f, 0.32f);
            Object.DestroyImmediate(buttonGo.GetComponent<Collider>());
            buttonGo.AddComponent<BoxCollider>();
            buttonGo.GetComponent<MeshRenderer>().sharedMaterial = buttonMat;
            var button = console.gameObject.AddComponent<WaveButton>();
            Set(button, "machine", machine);
            Set(button, "buttonRenderer", buttonGo.GetComponent<MeshRenderer>());

            // Wave type buttons in front of the big button: Beginner, Intermediate, Pro (left to right).
            var levels = new[] { WaveLevel.Beginner, WaveLevel.Intermediate, WaveLevel.Pro };
            var levelColors = new[] { new Color(0.35f, 0.85f, 1f), new Color(0.2f, 0.4f, 1f), new Color(0.85f, 0.2f, 0.95f) };
            for (int i = 0; i < levels.Length; i++)
            {
                var select = Box($"Select {levels[i]}", console, console.position + new Vector3((i - 1) * 0.35f, 1.03f, -0.2f), new Vector3(0.24f, 0.06f, 0.16f), buttonMat);
                var selectButton = select.AddComponent<WaveSelectButton>();
                Set(selectButton, "machine", machine);
                Set(selectButton, "buttonRenderer", select.GetComponent<MeshRenderer>());
                SetInt(selectButton, "level", (int)levels[i]);
                SetColor(selectButton, "color", levelColors[i]);
            }

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

            // The board the player carries, held low on the right side of the view.
            var held = GameObject.CreatePrimitive(PrimitiveType.Cube);
            held.name = "Held Board";
            Object.DestroyImmediate(held.GetComponent<Collider>());
            held.transform.SetParent(camGo.transform, false);
            held.transform.localPosition = new Vector3(0.45f, -0.42f, 0.75f);
            held.transform.localRotation = Quaternion.Euler(-10f, -20f, 75f);
            held.transform.localScale = new Vector3(0.4f, 0.05f, 1.4f);
            var heldRenderer = held.GetComponent<MeshRenderer>();
            heldRenderer.sharedMaterial = rentalBoardMat;
            heldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var carry = player.AddComponent<PlayerCarry>();
            Set(carry, "heldBoard", held);

            // Money.
            var game = new GameObject("Game");
            game.AddComponent<Wallet>();
            var pricing = game.AddComponent<PriceBoard>();

            // Ticket desk on the south deck. The player stands on the pool side, customers on the far side.
            var desk = new GameObject("Ticket Desk").transform;
            desk.position = new Vector3(20, 0, -22.5f);
            Box("Counter", desk, desk.position + new Vector3(0, 0.55f, 0), new Vector3(3, 1.1f, 1), deskMat);
            var registerGo = Box("Register", desk, desk.position + new Vector3(0.6f, 1.25f, 0), new Vector3(0.5f, 0.3f, 0.4f), consoleMat);
            var register = registerGo.AddComponent<TicketRegister>();
            Set(register, "pricing", pricing);

            // Upgrade computer on the other end of the counter.
            var computer = new GameObject("Upgrade Computer").transform;
            computer.SetParent(desk, false);
            computer.position = desk.position + new Vector3(-0.8f, 1.1f, -0.2f);
            Box("Monitor Stand", computer, computer.position + new Vector3(0, 0.12f, 0), new Vector3(0.12f, 0.24f, 0.12f), consoleMat);
            Box("Monitor", computer, computer.position + new Vector3(0, 0.45f, 0), new Vector3(0.7f, 0.45f, 0.06f), screenMat);
            var shop = computer.gameObject.AddComponent<UpgradeShop>();

            // Price panel next to the desk: − on the left and + on the right, as seen from behind the desk.
            var pricePanel = new GameObject("Price Panel").transform;
            pricePanel.position = new Vector3(17.6f, 0, -22.3f);
            Box("Pedestal", pricePanel, pricePanel.position + new Vector3(0, 0.5f, 0), new Vector3(0.7f, 1f, 0.5f), consoleMat);
            var priceUp = Box("Price Up (+)", pricePanel, pricePanel.position + new Vector3(-0.17f, 1.04f, 0), new Vector3(0.22f, 0.08f, 0.22f), greenMat);
            Set(priceUp.AddComponent<PriceButton>(), "pricing", pricing);
            var priceDown = Box("Price Down (-)", pricePanel, pricePanel.position + new Vector3(0.17f, 1.04f, 0), new Vector3(0.22f, 0.08f, 0.22f), redMat);
            var downButton = priceDown.AddComponent<PriceButton>();
            Set(downButton, "pricing", pricing);
            SetInt(downButton, "direction", -1);

            // Upgrade visuals, hidden until bought.
            var autoLight = Box("Auto Wave Light", null, new Vector3(-6, 5.25f, -9), new Vector3(0.6f, 0.5f, 0.6f), beaconMat);
            autoLight.SetActive(false);
            var bench = Box("Surfer Bench", env, new Vector3(30, 0.25f, -15), new Vector3(3, 0.5f, 0.8f), deskMat);
            bench.SetActive(false);

            // Board rental stand east of the ticket desk, hidden until bought.
            var stand = new GameObject("Rental Stand").transform;
            stand.position = new Vector3(30, 0, -22.5f);
            Box("Rental Counter", stand, stand.position + new Vector3(0, 0.55f, 0), new Vector3(3, 1.1f, 1), deskMat);
            Box("Rental Sign", stand, stand.position + new Vector3(0, 2.45f, -0.45f), new Vector3(2.4f, 0.5f, 0.08f), rentalBoardMat);
            Box("Sign Post L", stand, stand.position + new Vector3(-1.1f, 1.2f, -0.45f), new Vector3(0.08f, 2.4f, 0.08f), consoleMat);
            Box("Sign Post R", stand, stand.position + new Vector3(1.1f, 1.2f, -0.45f), new Vector3(0.08f, 2.4f, 0.08f), consoleMat);

            const int boardSlots = 10;
            var rackRoot = new GameObject("Board Rack").transform;
            rackRoot.SetParent(stand, false);
            rackRoot.position = new Vector3(32.6f, 0, -20.6f);
            Box("Rack Frame", rackRoot, rackRoot.position + new Vector3(0, 0.6f, 0), new Vector3(0.4f, 1.2f, 2.4f), consoleMat);
            var rackSlots = new Object[boardSlots];
            for (int i = 0; i < boardSlots; i++)
            {
                float z = -1.1f + i * (2.2f / (boardSlots - 1));
                var slot = Box($"Rack Board {i + 1}", rackRoot, rackRoot.position + new Vector3(-0.3f, 0.95f, z), new Vector3(0.45f, 1.8f, 0.06f), rentalBoardMat);
                Object.DestroyImmediate(slot.GetComponent<Collider>());
                rackSlots[i] = slot;
            }
            var rack = rackRoot.gameObject.AddComponent<RentalRack>();
            SetArray(rack, "slots", rackSlots);

            var binRoot = new GameObject("Return Bin").transform;
            binRoot.SetParent(stand, false);
            binRoot.position = new Vector3(34.5f, 0, -24.6f);
            Box("Bin", binRoot, binRoot.position + new Vector3(0, 0.3f, 0), new Vector3(2.0f, 0.6f, 1.0f), consoleMat);
            var binPile = new Object[boardSlots];
            for (int i = 0; i < boardSlots; i++)
            {
                var pileBoard = Box($"Returned Board {i + 1}", binRoot, binRoot.position + new Vector3((i % 2) * 0.1f - 0.05f, 0.64f + i * 0.07f, 0), new Vector3(1.9f, 0.06f, 0.45f), rentalBoardMat);
                Object.DestroyImmediate(pileBoard.GetComponent<Collider>());
                binPile[i] = pileBoard;
            }
            var bin = binRoot.gameObject.AddComponent<ReturnBin>();
            SetArray(bin, "pile", binPile);
            stand.gameObject.SetActive(false);

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
            var rentalApproach = Point("Rental Approach", route, new Vector3(30, 0, -27.5f));
            var rentalSpot = Point("Rental Spot", route, new Vector3(30, 0, -24.4f));
            var rentalExit = Point("Rental Exit", route, new Vector3(27.3f, 0, -24.4f));
            var rentalSide = Point("Rental Side", route, new Vector3(27.3f, 0, -19));
            var binSpot = Point("Return Bin Spot", route, new Vector3(35.8f, 0, -25.8f));

            var spawnerGo = new GameObject("Surfer Spawner");
            var spawner = spawnerGo.AddComponent<SurferSpawner>();
            Set(spawner, "surferPrefab", BuildSurferPrefab(surferMat));
            Set(spawner, "water", water);
            Set(spawner, "pricing", pricing);
            SetArray(spawner, "pathIn", entrance, deskApproach);
            Set(spawner, "deskSpot", deskSpot);
            SetArray(spawner, "pathToPool", deskExit, poolSide, poolEntry);
            Set(spawner, "lineup", lineup);
            Set(spawner, "beachLanding", landing);
            SetArray(spawner, "pathOut", beachExit, exitCorner, exit);
            SetArray(spawner, "rentalPathIn", deskExit, rentalApproach);
            Set(spawner, "rentalSpot", rentalSpot);
            SetArray(spawner, "rentalPathToPool", rentalExit, rentalSide, poolSide, poolEntry);
            SetArray(spawner, "returnPathOut", beachExit, binSpot, exit);
            Set(spawner, "returnBinSpot", binSpot);
            Set(spawner, "returnBin", bin);
            Set(register, "spawner", spawner);

            Set(shop, "machine", machine);
            Set(shop, "water", water);
            Set(shop, "spawner", spawner);
            Set(shop, "pricing", pricing);
            Set(shop, "autoWaveLight", autoLight);
            Set(shop, "extraBench", bench);
            Set(shop, "rentalStand", stand.gameObject);
            Set(shop, "rentalRack", rack);

            Set(hud, "pricing", pricing);
            Set(hud, "spawner", spawner);
            Set(hud, "machine", machine);

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

        static Material Mat(string name, Color color, float smoothness = 0.2f, Color? emission = null)
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
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop.propertyType == SerializedPropertyType.Enum) prop.enumValueIndex = value;
            else prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetColor(Object target, string field, Color value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).colorValue = value;
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

            // Collider so the player can look at a surfer and hand them a board.
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 0.9f, 0);
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            root.AddComponent<Rigidbody>().isKinematic = true;

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
