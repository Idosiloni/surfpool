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
            var waterMat = WaterMaterial();
            var foamMat = Mat("Foam", Color.white, 0.1f);
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

            // Barrel lip: a second mesh the water fills in each frame for barreling waves.
            var lipGo = new GameObject("Barrel Lip", typeof(MeshFilter), typeof(MeshRenderer));
            lipGo.transform.SetParent(waterGo.transform, false);
            lipGo.GetComponent<MeshRenderer>().sharedMaterial = waterMat;
            Set(water, "lipMesh", lipGo.GetComponent<MeshFilter>());

            // Foam spray: cartoon white blobs thrown up where waves break.
            Set(water, "spray", BuildSpray(foamMat));

            // Wave machine housing behind the deep end.
            var machineGo = Box("Wave Machine", null, new Vector3(-9, 2.5f, 0), new Vector3(10, 5, PoolWidth - 2), machineMat);
            var machine = machineGo.AddComponent<WaveMachine>();
            Set(machine, "water", water);

            // Control console with the big button, on the deck by the deep end.
            var console = new GameObject("Wave Console").transform;
            console.position = new Vector3(5, 0, -halfW - 1.5f);
            Box("Pedestal", console, console.position + new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1f, 0.8f), consoleMat);
            var buttonGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonGo.name = "Button";
            buttonGo.transform.SetParent(console, false);
            buttonGo.transform.localPosition = new Vector3(0, 1.04f, -0.15f);
            buttonGo.transform.localScale = new Vector3(0.32f, 0.04f, 0.32f);
            Object.DestroyImmediate(buttonGo.GetComponent<Collider>());
            buttonGo.AddComponent<BoxCollider>();
            buttonGo.GetComponent<MeshRenderer>().sharedMaterial = buttonMat;
            var button = console.gameObject.AddComponent<WaveButton>();
            Set(button, "machine", machine);
            Set(button, "buttonRenderer", buttonGo.GetComponent<MeshRenderer>());

            // Wave control screen behind the big button: opens the Wave Control panel.
            var waveScreen = Box("Wave Screen", console, console.position + new Vector3(0, 1.3f, 0.22f), new Vector3(0.7f, 0.42f, 0.05f), screenMat);
            waveScreen.transform.rotation = Quaternion.Euler(-35f, 0, 0);
            var wavePanel = waveScreen.AddComponent<WaveControlPanel>();
            Set(wavePanel, "machine", machine);

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
            game.AddComponent<ParkReputation>();
            var payroll = game.AddComponent<StaffPayroll>();

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
            Set(shop, "spawner", spawner);
            Set(shop, "pricing", pricing);
            Set(shop, "autoWaveLight", autoLight);
            Set(shop, "extraBench", bench);
            Set(shop, "rentalStand", stand.gameObject);
            Set(shop, "rentalRack", rack);

            // Staff, hidden until hired on the upgrade computer.
            var staffMat = Mat("StaffShirt", new Color(0.2f, 0.65f, 0.4f), 0.3f);
            var lifeguardMat = Mat("LifeguardShirt", new Color(0.95f, 0.15f, 0.15f), 0.3f);
            var skinMat = Mat("Skin", new Color(0.85f, 0.65f, 0.5f), 0.3f);

            var deskClerk = Person("Desk Clerk", new Vector3(21.6f, 0, -21.4f), 180f, staffMat, skinMat);
            var deskClerkJob = deskClerk.AddComponent<DeskClerk>();
            Set(deskClerkJob, "spawner", spawner);
            Set(deskClerkJob, "register", register);
            Staff(deskClerk, "Desk Clerk", 6);

            var rentalClerk = Person("Rental Clerk", new Vector3(29.2f, 0, -21.4f), 180f, staffMat, skinMat);
            var rentalClerkJob = rentalClerk.AddComponent<RentalClerk>();
            Set(rentalClerkJob, "spawner", spawner);
            Set(rentalClerkJob, "rack", rack);
            Staff(rentalClerk, "Rental Clerk", 5);

            var runnerRoute = new GameObject("Board Runner Route").transform;
            var runnerPoints = new Object[]
            {
                Point("Rack Side", runnerRoute, new Vector3(31.9f, 0, -21.0f)),
                Point("South Of Rack", runnerRoute, new Vector3(32.0f, 0, -22.3f)),
                Point("East Corner", runnerRoute, new Vector3(33.8f, 0, -22.3f)),
                Point("Bin Side", runnerRoute, new Vector3(34.5f, 0, -23.7f)),
            };
            var runner = Person("Board Runner", new Vector3(31.9f, 0, -21.0f), 90f, staffMat, skinMat);
            var carried = Box("Carried Board", runner.transform, runner.transform.position + new Vector3(0, 0.9f, 0.4f), new Vector3(0.06f, 0.45f, 1.8f), rentalBoardMat);
            Object.DestroyImmediate(carried.GetComponent<Collider>());
            carried.transform.localPosition = new Vector3(0.35f, 0.9f, 0);
            carried.transform.localRotation = Quaternion.identity;
            var runnerJob = runner.AddComponent<BoardRunner>();
            Set(runnerJob, "rack", rack);
            Set(runnerJob, "bin", bin);
            SetArray(runnerJob, "rackToBin", runnerPoints);
            Set(runnerJob, "carriedBoard", carried);
            Staff(runner, "Board Runner", 4);

            var tower = new GameObject("Lifeguard Tower").transform;
            tower.position = new Vector3(30, 0, halfW + 1.8f);
            for (int i = 0; i < 4; i++)
            {
                var leg = new Vector3(i % 2 == 0 ? -0.5f : 0.5f, 0.9f, i < 2 ? -0.5f : 0.5f);
                Box($"Leg {i + 1}", tower, tower.position + leg, new Vector3(0.1f, 1.8f, 0.1f), deskMat);
            }
            Box("Platform", tower, tower.position + new Vector3(0, 1.85f, 0), new Vector3(1.3f, 0.1f, 1.3f), deskMat);
            var guard = Person("Lifeguard", tower.position + new Vector3(0, 1.9f, 0), 180f, lifeguardMat, skinMat);
            guard.transform.SetParent(tower, true);
            tower.gameObject.AddComponent<Lifeguard>();
            Staff(tower.gameObject, "Lifeguard", 3);

            Set(shop, "lifeguard", tower.gameObject);
            Set(shop, "boardRunner", runner);
            Set(shop, "rentalClerk", rentalClerk);
            Set(shop, "deskClerk", deskClerk);
            foreach (var hidden in new[] { deskClerk, rentalClerk, runner, tower.gameObject }) hidden.SetActive(false);

            // UI Toolkit: HUD and the upgrade computer screen, plus an EventSystem so the mouse can click buttons.
            var panelSettings = PanelSettingsAsset();
            var hudGo = new GameObject("HUD");
            var hudDoc = hudGo.AddComponent<UnityEngine.UIElements.UIDocument>();
            hudDoc.panelSettings = panelSettings;
            hudDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/Hud.uxml");
            var hud = hudGo.AddComponent<GameHud>();
            Set(hud, "interactor", interactor);
            Set(hud, "player", fps);
            Set(hud, "pricing", pricing);
            Set(hud, "spawner", spawner);
            Set(hud, "machine", machine);
            Set(hud, "payroll", payroll);

            var screenGo = new GameObject("Upgrade Screen");
            var screenDoc = screenGo.AddComponent<UnityEngine.UIElements.UIDocument>();
            screenDoc.panelSettings = panelSettings;
            screenDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/UpgradeScreen.uxml");
            screenDoc.sortingOrder = 10;
            Set(screenGo.AddComponent<UpgradeScreen>(), "shop", shop);

            var waveUiGo = new GameObject("Wave Control Screen");
            var waveDoc = waveUiGo.AddComponent<UnityEngine.UIElements.UIDocument>();
            waveDoc.panelSettings = panelSettings;
            waveDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/WaveControl.uxml");
            waveDoc.sortingOrder = 10;
            Set(waveUiGo.AddComponent<WaveControlScreen>(), "panel", wavePanel);

            var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

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

        static Material WaterMaterial()
        {
            const string path = "Assets/_Game/Materials/StylizedWater.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("SurfPool/StylizedWater"));
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        /// <summary>Particle system for foam blobs. WaveWater emits the particles itself.</summary>
        static ParticleSystem BuildSpray(Material foamMat)
        {
            var go = new GameObject("Wave Spray");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.1f;
            main.maxParticles = 4000;
            main.startColor = Color.white;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            renderer.mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(sphere);
            renderer.sharedMaterial = foamMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        /// <summary>Shared UI Toolkit panel settings: scales with screen size from a 1920x1080 layout.</summary>
        static UnityEngine.UIElements.PanelSettings PanelSettingsAsset()
        {
            const string path = "Assets/_Game/UI/GamePanelSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>("Assets/_Game/UI/GameTheme.tss");
            settings.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>Placeholder staff character: capsule body in a shirt color and a sphere head, no colliders.</summary>
        static GameObject Person(string name, Vector3 position, float yaw, Material shirt, Material skin)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.8f, 0.5f);
            body.GetComponent<MeshRenderer>().sharedMaterial = shirt;
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0, 1.75f, 0);
            head.transform.localScale = Vector3.one * 0.3f;
            head.GetComponent<MeshRenderer>().sharedMaterial = skin;
            return root;
        }

        static void Staff(GameObject go, string title, int wage)
        {
            var staff = go.AddComponent<StaffMember>();
            var so = new SerializedObject(staff);
            so.FindProperty("jobTitle").stringValue = title;
            so.FindProperty("wagePerMinute").intValue = wage;
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
