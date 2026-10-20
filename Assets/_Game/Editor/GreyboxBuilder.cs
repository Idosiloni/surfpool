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

            var deckMat = Mat("Deck", new Color(0.86f, 0.83f, 0.76f));
            var tileMat = Mat("PoolTile", new Color(0.6f, 0.86f, 0.9f));
            var copingMat = Mat("Coping", new Color(0.97f, 0.96f, 0.92f), 0.3f);
            var sandMat = Mat("Sand", new Color(0.93f, 0.84f, 0.64f));
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

            var env = new GameObject("Environment").transform;
            float halfW = PoolWidth / 2f;
            mats = new PoolMaterials
            {
                deck = deckMat, tile = tileMat, coping = copingMat, sand = sandMat, water = waterMat, foam = foamMat,
                machine = machineMat, console = consoleMat, button = buttonMat, screen = screenMat, beacon = beaconMat,
                sign = Mat("Sign", new Color(1f, 0.42f, 0.34f), 0.4f),
            };

            // The main pool, open from the start. Two more pools are built later from the upgrade computer.
            var main = BuildPool(new PoolSpec
            {
                name = "Main Pool", display = "Main Pool", z = 0, open = true, depth = PoolDepth, waterLevel = WaterLevel,
                size = 1f, peel = 1f, mainDeck = true,
                pathToPool = new[] { new Vector3(24, 0, -14), new Vector3(14, 0, -halfW - 0.6f) },
                pathOut = new[] { new Vector3(PoolLength + 3, 0, -6), new Vector3(PoolLength + 6, 0, -30), new Vector3(45, 0, -40) },
            });
            var water = main.water;
            var machine = main.machine;
            var wavePanel = main.panel;

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
            held.GetComponent<MeshFilter>().sharedMesh = WorldArt.ModelMesh("Surfboard");
            Object.DestroyImmediate(held.GetComponent<Collider>());
            held.transform.SetParent(camGo.transform, false);
            held.transform.localPosition = new Vector3(0.45f, -0.42f, 0.75f);
            held.transform.localRotation = Quaternion.Euler(-10f, -20f, 75f);
            held.transform.localScale = Vector3.one * 0.75f;
            var heldRenderer = held.GetComponent<MeshRenderer>();
            heldRenderer.sharedMaterial = rentalBoardMat;
            heldRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var carry = player.AddComponent<PlayerCarry>();
            Set(carry, "heldBoard", held);

            // Your own surfboard under your feet while surfing.
            var myBoard = Board("Surf Board", player.transform, player.transform.position, player.transform.rotation, 1.05f, mats.sign);
            myBoard.SetActive(false);
            var surfing = player.AddComponent<PlayerSurfing>();
            Set(surfing, "walker", fps);
            Set(surfing, "body", cc);
            Set(surfing, "cameraPivot", camGo.transform);
            Set(surfing, "surfBoard", myBoard);

            // Money.
            var game = new GameObject("Game");
            game.AddComponent<Wallet>();
            var pricing = game.AddComponent<PriceBoard>();
            game.AddComponent<ParkReputation>();
            var payroll = game.AddComponent<StaffPayroll>();

            // Ticket desk on the south deck. The player stands on the pool side, customers on the far side.
            var desk = new GameObject("Ticket Desk").transform;
            desk.position = new Vector3(20, 0, -22.5f);
            WorldArt.Model("KioskTeal", desk, desk.position);
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
            var bench = Box("Surfer Bench", env, new Vector3(30, 0.25f, -15), new Vector3(3, 0.5f, 0.8f), deskMat);
            bench.SetActive(false);

            // Board rental stand east of the ticket desk, hidden until bought.
            var stand = new GameObject("Rental Stand").transform;
            stand.position = new Vector3(30, 0, -22.5f);
            Box("Rental Counter", stand, stand.position + new Vector3(0, 0.55f, 0), new Vector3(3, 1.1f, 1), deskMat);
            WorldArt.Model("KioskCoral", stand, stand.position);

            const int boardSlots = 10;
            var rackRoot = new GameObject("Board Rack").transform;
            rackRoot.SetParent(stand, false);
            rackRoot.position = new Vector3(32.6f, 0, -20.6f);
            Box("Rack Frame", rackRoot, rackRoot.position + new Vector3(0, 0.6f, 0), new Vector3(0.4f, 1.2f, 2.4f), consoleMat);
            var rackSlots = new Object[boardSlots];
            for (int i = 0; i < boardSlots; i++)
            {
                float z = -1.1f + i * (2.2f / (boardSlots - 1));
                var slot = Board($"Rack Board {i + 1}", rackRoot, rackRoot.position + new Vector3(-0.32f, 0.95f, z), Quaternion.Euler(-90, 0, 0), 0.9f, rentalBoardMat);
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
                var pileBoard = Board($"Returned Board {i + 1}", binRoot, binRoot.position + new Vector3((i % 2) * 0.1f - 0.05f, 0.62f + i * 0.11f, 0), Quaternion.Euler(0, 90, 0), 0.9f, rentalBoardMat);
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
            var rentalApproach = Point("Rental Approach", route, new Vector3(30, 0, -27.5f));
            var rentalSpot = Point("Rental Spot", route, new Vector3(30, 0, -24.4f));
            var rentalExit = Point("Rental Exit", route, new Vector3(27.3f, 0, -24.4f));
            var rentalSide = Point("Rental Side", route, new Vector3(27.3f, 0, -19));
            var binSpot = Point("Return Bin Spot", route, new Vector3(35.8f, 0, -25.8f));

            // Renters return their board past the bin on the way out of the main pool.
            var mainOut = main.area.PathOut;
            SetArray(main.area, "returnPathOut", mainOut[0], binSpot, mainOut[mainOut.Length - 1]);

            // Two more pools to the north, built later. Surfers walk up the west side past the machine houses.
            var reef = BuildPool(new PoolSpec
            {
                name = "Reef Pool", display = "Reef Pool", upgradeId = "pool_reef", z = 64, open = false, depth = PoolDepth,
                waterLevel = WaterLevel, size = 1.08f, peel = 0.8f,
                pathToPool = new[] { new Vector3(-2, 0, -18), new Vector3(-2, 0, 46), new Vector3(14, 0, 64 - halfW - 0.6f) },
                pathOut = new[] { new Vector3(PoolLength + 3, 0, 58), new Vector3(PoolLength + 40, 0, 58) },
            });
            var lagoon = BuildPool(new PoolSpec
            {
                name = "Big Wave Lagoon", display = "Big Wave Lagoon", upgradeId = "pool_lagoon", z = 116, open = false,
                depth = PoolDepth + 1f, waterLevel = WaterLevel - 0.8f, size = 1.25f, peel = 0.9f,
                pathToPool = new[] { new Vector3(-2, 0, -18), new Vector3(-2, 0, 98), new Vector3(14, 0, 116 - halfW - 0.6f) },
                pathOut = new[] { new Vector3(PoolLength + 3, 0, 110), new Vector3(PoolLength + 40, 0, 110) },
            });

            var spawnerGo = new GameObject("Surfer Spawner");
            var spawner = spawnerGo.AddComponent<SurferSpawner>();
            Set(spawner, "surferPrefab", BuildSurferPrefab(surferMat));
            Set(spawner, "pricing", pricing);
            Set(spawner, "mainPool", main.area);
            SetArray(spawner, "pathIn", entrance, deskApproach);
            Set(spawner, "deskSpot", deskSpot);
            SetArray(spawner, "deskExitPath", deskExit);
            SetArray(spawner, "rentalPathIn", deskExit, rentalApproach);
            Set(spawner, "rentalSpot", rentalSpot);
            SetArray(spawner, "rentalExitPath", rentalExit, rentalSide);
            Set(spawner, "returnBinSpot", binSpot);
            Set(spawner, "returnBin", bin);
            Set(register, "spawner", spawner);

            Set(shop, "spawner", spawner);
            Set(shop, "pricing", pricing);
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
            var carried = Board("Carried Board", runner.transform, runner.transform.position, Quaternion.identity, 0.9f, rentalBoardMat);
            carried.transform.localPosition = new Vector3(0.35f, 0.9f, 0);
            carried.transform.localRotation = Quaternion.Euler(0, 0, 90);
            var runnerJob = runner.AddComponent<BoardRunner>();
            Set(runnerJob, "rack", rack);
            Set(runnerJob, "bin", bin);
            SetArray(runnerJob, "rackToBin", runnerPoints);
            Set(runnerJob, "carriedBoard", carried);
            Staff(runner, "Board Runner", 4);

            var tower = new GameObject("Lifeguard Tower").transform;
            tower.position = new Vector3(30, 0, halfW + 1.8f);
            WorldArt.Model("LifeguardTower", tower, tower.position);
            var guard = Person("Lifeguard", tower.position + new Vector3(0, 1.96f, 0), 180f, lifeguardMat, skinMat);
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
            waveUiGo.AddComponent<WaveControlScreen>();

            // Story and missions.
            var missions = game.AddComponent<MissionManager>();
            Set(missions, "ticketRegister", registerGo.transform);
            Set(missions, "waveConsole", wavePanel.transform);
            Set(missions, "upgradeComputer", computer);
            Set(missions, "pool", water.transform);
            Set(missions, "rentalRack", rackRoot);
            Set(missions, "surfPeg", main.peg);
            Set(missions, "shop", shop);
            Set(missions, "spawner", spawner);
            Set(missions, "player", interactor);

            var storyGo = new GameObject("Story Screen");
            var storyDoc = storyGo.AddComponent<UnityEngine.UIElements.UIDocument>();
            storyDoc.panelSettings = panelSettings;
            storyDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/Story.uxml");
            storyDoc.sortingOrder = 20;
            storyGo.AddComponent<StoryScreen>();

            var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            WorldArt.BuildEnvironment(sandMat, waterMat);
            WorldArt.Lighting(GameObject.Find("Directional Light").GetComponent<Light>(), cam);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[GreyboxBuilder] Built " + ScenePath);
        }

        class PoolMaterials
        {
            public Material deck, tile, coping, sand, water, foam, machine, console, button, screen, beacon, sign;
        }

        class PoolSpec
        {
            public string name, display, upgradeId;
            public float z, depth, waterLevel, size, peel;
            public bool open, mainDeck;
            public Vector3[] pathToPool, pathOut;
        }

        class PoolBuild
        {
            public PoolArea area;
            public WaveWater water;
            public WaveMachine machine;
            public WaveControlPanel panel;
            public Transform peg;
        }

        static PoolMaterials mats;

        /// <summary>
        /// One complete pool centered on z = spec.z: deck slabs (their inner faces are the walls), tiled lining, the
        /// sloped beach, coping, invisible edge blockers, water with barrel lip and spray, the wave machine house and
        /// its console, the surfer route, and a construction lot shown until the pool is built.
        /// </summary>
        static PoolBuild BuildPool(PoolSpec spec)
        {
            float halfW = PoolWidth / 2f, zc = spec.z, depth = spec.depth, thickness = depth + 1f;
            var root = new GameObject(spec.name).transform;
            var built = new GameObject("Built").transform;
            built.SetParent(root, false);

            // Deck. The main pool's deck is wide (desks and sunbathing live on it); the others have narrower strips.
            float south = spec.mainDeck ? 28 : 12, north = spec.mainDeck ? 28 : 16;
            Box("Deck North", built, new Vector3(30, -thickness / 2, zc + halfW + north / 2), new Vector3(100, thickness, north), mats.deck);
            Box("Deck South", built, new Vector3(30, -thickness / 2, zc - halfW - south / 2), new Vector3(100, thickness, south), mats.deck);
            Box("Deck West", built, new Vector3(-10, -thickness / 2, zc), new Vector3(20, thickness, PoolWidth), mats.deck);
            Box("Deck East", built, new Vector3(PoolLength + 10, -thickness / 2, zc), new Vector3(20, thickness, PoolWidth), mats.deck);

            float wallY = -depth / 2f;
            Box("Wall North", built, new Vector3(PoolLength / 2, wallY, zc + halfW - 0.05f), new Vector3(PoolLength, depth, 0.1f), mats.tile);
            Box("Wall South", built, new Vector3(PoolLength / 2, wallY, zc - halfW + 0.05f), new Vector3(PoolLength, depth, 0.1f), mats.tile);
            Box("Wall Machine", built, new Vector3(0.05f, wallY, zc), new Vector3(0.1f, depth, PoolWidth), mats.tile);
            Box("Floor", built, new Vector3(PoolLength / 2, -depth - 0.5f, zc), new Vector3(PoolLength, 1, PoolWidth), mats.tile);

            const float rampStart = 45f;
            float run = PoolLength - rampStart;
            float rampLength = Mathf.Sqrt(run * run + depth * depth);
            float rampAngle = Mathf.Atan2(depth, run) * Mathf.Rad2Deg;
            var ramp = Box("Beach", built, new Vector3(rampStart + run / 2, -depth / 2 - 0.15f, zc), new Vector3(rampLength, 0.3f, PoolWidth), mats.sand);
            ramp.transform.rotation = Quaternion.Euler(0, 0, rampAngle);

            Box("Coping North", built, new Vector3(PoolLength / 2, 0.06f, zc + halfW + 0.25f), new Vector3(PoolLength + 1f, 0.12f, 0.5f), mats.coping);
            Box("Coping South", built, new Vector3(PoolLength / 2, 0.06f, zc - halfW - 0.25f), new Vector3(PoolLength + 1f, 0.12f, 0.5f), mats.coping);
            Box("Coping Machine", built, new Vector3(-0.25f, 0.06f, zc), new Vector3(0.5f, 0.12f, PoolWidth), mats.coping);

            Blocker("Blocker North", built, new Vector3(PoolLength / 2, 0.75f, zc + halfW + 0.1f), new Vector3(PoolLength, 1.5f, 0.2f));
            Blocker("Blocker South", built, new Vector3(PoolLength / 2, 0.75f, zc - halfW - 0.1f), new Vector3(PoolLength, 1.5f, 0.2f));
            Blocker("Blocker Machine", built, new Vector3(-0.1f, 0.75f, zc), new Vector3(0.2f, 1.5f, PoolWidth));
            Blocker("Blocker Beach", built, new Vector3(PoolLength + 0.1f, 0.75f, zc), new Vector3(0.2f, 1.5f, PoolWidth));

            var waterGo = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
            waterGo.transform.SetParent(built, false);
            waterGo.transform.position = new Vector3(0, spec.waterLevel, zc);
            waterGo.GetComponent<MeshRenderer>().sharedMaterial = mats.water;
            var water = waterGo.AddComponent<WaveWater>();
            SetFloat(water, "sizeMultiplier", spec.size);
            SetFloat(water, "peelSpeedMultiplier", spec.peel);
            var lipGo = new GameObject("Barrel Lip", typeof(MeshFilter), typeof(MeshRenderer));
            lipGo.transform.SetParent(waterGo.transform, false);
            lipGo.GetComponent<MeshRenderer>().sharedMaterial = mats.water;
            Set(water, "lipMesh", lipGo.GetComponent<MeshFilter>());
            var spray = BuildSpray(mats.foam);
            spray.transform.SetParent(built, true);
            Set(water, "spray", spray);

            var machineGo = Box("Wave Machine", built, new Vector3(-9, 2.5f, zc), new Vector3(10, 5, PoolWidth - 2), mats.machine);
            machineGo.GetComponent<MeshRenderer>().enabled = false; // keeps its collider; the model is the look
            WorldArt.Model("WaveMachineHouse", built, new Vector3(-9, 0, zc));
            var machine = machineGo.AddComponent<WaveMachine>();
            Set(machine, "water", water);
            var autoLight = Box("Auto Wave Light", built, new Vector3(-6, 4.85f, zc - 9), new Vector3(0.6f, 0.5f, 0.6f), mats.beacon);
            autoLight.SetActive(false);

            var console = new GameObject("Wave Console").transform;
            console.SetParent(built, false);
            console.position = new Vector3(5, 0, zc - halfW - 1.5f);
            Box("Pedestal", console, console.position + new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1f, 0.8f), mats.console);
            var buttonGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonGo.name = "Button";
            buttonGo.transform.SetParent(console, false);
            buttonGo.transform.localPosition = new Vector3(0, 1.04f, -0.15f);
            buttonGo.transform.localScale = new Vector3(0.32f, 0.04f, 0.32f);
            Object.DestroyImmediate(buttonGo.GetComponent<Collider>());
            buttonGo.AddComponent<BoxCollider>();
            buttonGo.GetComponent<MeshRenderer>().sharedMaterial = mats.button;
            var button = console.gameObject.AddComponent<WaveButton>();
            Set(button, "machine", machine);
            Set(button, "buttonRenderer", buttonGo.GetComponent<MeshRenderer>());
            var screen = Box("Wave Screen", console, console.position + new Vector3(0, 1.3f, 0.22f), new Vector3(0.7f, 0.42f, 0.05f), mats.screen);
            screen.transform.rotation = Quaternion.Euler(-35f, 0, 0);
            var panel = screen.AddComponent<WaveControlPanel>();
            Set(panel, "machine", machine);

            // Your own board on a peg next to the console: E to go surfing in this pool.
            var peg = new GameObject("Surf Peg").transform;
            peg.SetParent(built, false);
            peg.position = new Vector3(8, 0, zc - halfW - 1.6f);
            Box("Peg Post", peg, peg.position + new Vector3(0, 0.9f, 0.25f), new Vector3(0.14f, 1.8f, 0.14f), mats.coping);
            Board("My Board", peg, peg.position + new Vector3(0, 1.0f, 0.1f), Quaternion.Euler(-80, 0, 0), 1f, mats.sign);
            var pegCollider = peg.gameObject.AddComponent<BoxCollider>();
            pegCollider.center = new Vector3(0, 1f, 0.15f);
            pegCollider.size = new Vector3(0.7f, 2f, 0.4f);
            var surfPeg = peg.gameObject.AddComponent<SurfPeg>();

            // Construction lot: sand, a fence of posts and a big coral sign, shown until the pool is built.
            GameObject lot = null;
            if (!spec.open)
            {
                lot = new GameObject("Construction Lot");
                lot.transform.SetParent(root, false);
                Box("Lot Sand", lot.transform, new Vector3(30, -0.6f, zc + (north - south) / 2), new Vector3(100, 1, PoolWidth + north + south), mats.sand);
                for (int i = 0; i <= 20; i++)
                {
                    Box("Fence Post", lot.transform, new Vector3(-5 + i * 3.5f, 0.6f, zc - halfW - 2), new Vector3(0.15f, 1.2f, 0.15f), mats.coping);
                    Box("Fence Post", lot.transform, new Vector3(-5 + i * 3.5f, 0.6f, zc + halfW + 2), new Vector3(0.15f, 1.2f, 0.15f), mats.coping);
                }
                Box("Fence Rail South", lot.transform, new Vector3(30, 1.0f, zc - halfW - 2), new Vector3(70, 0.12f, 0.08f), mats.sign);
                Box("Fence Rail North", lot.transform, new Vector3(30, 1.0f, zc + halfW + 2), new Vector3(70, 0.12f, 0.08f), mats.sign);
                Box("Sign Post L", lot.transform, new Vector3(27.5f, 1.5f, zc - halfW - 2.3f), new Vector3(0.2f, 3f, 0.2f), mats.coping);
                Box("Sign Post R", lot.transform, new Vector3(32.5f, 1.5f, zc - halfW - 2.3f), new Vector3(0.2f, 3f, 0.2f), mats.coping);
                Box("Coming Soon Sign", lot.transform, new Vector3(30, 2.6f, zc - halfW - 2.4f), new Vector3(6f, 1.6f, 0.12f), mats.sign);
            }

            var route = new GameObject("Route").transform;
            route.SetParent(root, false);
            var toPool = new Object[spec.pathToPool.Length];
            for (int i = 0; i < toPool.Length; i++) toPool[i] = Point($"To Pool {i + 1}", route, spec.pathToPool[i]);
            var outPoints = new Object[spec.pathOut.Length];
            for (int i = 0; i < outPoints.Length; i++) outPoints[i] = Point($"Out {i + 1}", route, spec.pathOut[i]);
            var lineup = Point("Lineup", route, new Vector3(12, spec.waterLevel, zc - 4));
            const float landingX = 57f;
            var landing = Point("Beach Landing", route, new Vector3(landingX, -depth + depth * (landingX - rampStart) / run, zc));

            var area = root.gameObject.AddComponent<PoolArea>();
            SetString(area, "displayName", spec.display);
            SetString(area, "upgradeId", spec.upgradeId ?? "");
            SetBool(area, "openAtStart", spec.open);
            Set(area, "water", water);
            Set(area, "machine", machine);
            SetArray(area, "pathToPool", toPool);
            Set(area, "lineup", lineup);
            Set(area, "beachLanding", landing);
            SetArray(area, "pathOut", outPoints);
            SetArray(area, "returnPathOut", outPoints);
            Set(area, "buildVisuals", built.gameObject);
            if (lot != null) Set(area, "lotVisuals", lot);
            Set(area, "autoWaveLight", autoLight);
            Set(surfPeg, "pool", area);
            if (!spec.open) built.gameObject.SetActive(false);

            return new PoolBuild { area = area, water = water, machine = machine, panel = panel, peg = peg };
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>A surfboard model (no collider).</summary>
        static GameObject Board(string name, Transform parent, Vector3 position, Quaternion rotation, float scale, Material mat)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * scale;
            go.GetComponent<MeshFilter>().sharedMesh = WorldArt.ModelMesh("Surfboard");
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
            var rig = BuildCharacter(root.transform);
            var so = new SerializedObject(rig);
            so.FindProperty("paintOnStart").boolValue = true;
            so.FindProperty("defaultSkin").colorValue = skin.GetColor("_BaseColor");
            so.FindProperty("defaultShirt").colorValue = shirt.GetColor("_BaseColor");
            so.FindProperty("defaultShorts").colorValue = new Color(0.1f, 0.18f, 0.3f);
            so.FindProperty("defaultHair").colorValue = new Color(0.25f, 0.16f, 0.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        /// <summary>
        /// Assembles a low-poly person from the Blender character parts under root: Model > Hips > (Shorts, Torso >
        /// (Head > hair, shoulders > arms), hips > legs). The CharacterRig animates the joints.
        /// </summary>
        static CharacterRig BuildCharacter(Transform root)
        {
            Transform Joint(string name, Transform parent, Vector3 localPosition)
            {
                var joint = new GameObject(name).transform;
                joint.SetParent(parent, false);
                joint.localPosition = localPosition;
                return joint;
            }
            Renderer Part(string model, Transform parent)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Game/Models/{model}.fbx");
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                return go.GetComponentInChildren<Renderer>();
            }

            var model = Joint("Model", root, Vector3.zero);
            var hips = Joint("Hips", model, new Vector3(0, 0.85f, 0));
            var shorts = Part("CharShorts", hips);
            var torso = Joint("Torso", hips, Vector3.zero);
            var shirt = Part("CharTorso", torso);
            var head = Joint("Head", torso, new Vector3(0, 0.55f, 0));
            var headSkin = Part("CharHead", head);
            var hairShort = Part("CharHairShort", head);
            var hairLong = Part("CharHairLong", head);
            var shoulderL = Joint("Shoulder L", torso, new Vector3(-0.27f, 0.5f, 0));
            var shoulderR = Joint("Shoulder R", torso, new Vector3(0.27f, 0.5f, 0));
            var armL = Part("CharArm", shoulderL);
            var armR = Part("CharArm", shoulderR);
            var hipL = Joint("Hip L", hips, new Vector3(-0.1f, 0, 0));
            var hipR = Joint("Hip R", hips, new Vector3(0.1f, 0, 0));
            var legL = Part("CharLeg", hipL);
            var legR = Part("CharLeg", hipR);

            var rig = root.gameObject.AddComponent<CharacterRig>();
            Set(rig, "model", model);
            Set(rig, "hips", hips);
            Set(rig, "torso", torso);
            Set(rig, "head", head);
            Set(rig, "shoulderL", shoulderL);
            Set(rig, "shoulderR", shoulderR);
            Set(rig, "hipL", hipL);
            Set(rig, "hipR", hipR);
            SetArray(rig, "skinRenderers", headSkin, armL, armR, legL, legR);
            Set(rig, "shirtRenderer", shirt);
            Set(rig, "shortsRenderer", shorts);
            SetArray(rig, "hairRenderers", hairShort, hairLong);
            return rig;
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
            var rig = BuildCharacter(root.transform);
            var board = new GameObject("Board", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            board.SetParent(root.transform, false);
            board.GetComponent<MeshFilter>().sharedMesh = WorldArt.ModelMesh("Surfboard");
            board.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Collider so the player can look at a surfer and hand them a board.
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, 0.9f, 0);
            capsule.height = 1.8f;
            capsule.radius = 0.35f;
            root.AddComponent<Rigidbody>().isKinematic = true;

            var surfer = root.AddComponent<Surfer>();
            Set(surfer, "rig", rig);
            Set(surfer, "board", board);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Surfer>();
        }
    }
}
