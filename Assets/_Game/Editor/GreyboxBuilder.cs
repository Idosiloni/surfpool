using System.Collections.Generic;
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
    public static partial class GreyboxBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Pool.unity";
        const string MaterialFolder = "Assets/_Game/Materials";

        // Pool interior: x 0..60 (machine -> beach), z -12..12, deck top at y = 0, floor at y = -4.
        // Water sits low so the biggest waves crest just below the deck instead of spilling over it.
        const float PoolLength = 60f, PoolWidth = 24f, PoolDepth = 5f, WaterLevel = -3f;

        // Pools stack north along z: main (24 wide, 28 m deck north) | reef (36 wide, 12 m deck south, 16 north) | lagoon (48 wide).
        const float ReefZ = 12f + 28f + 12f + 18f, LagoonZ = ReefZ + 18f + 16f + 12f + 24f;
        const float DeckThickness = PoolDepth + 1f;

        [MenuItem("Surf Pool/Delete Save Game")]
        public static void DeleteSave()
        {
            SaveSystem.DeleteSave();
            Debug.Log("[GreyboxBuilder] Save deleted: the next Play starts a new game.");
        }

        [MenuItem("Surf Pool/Build Greybox Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath)!);
            Directory.CreateDirectory(MaterialFolder);

            var deckMat = Mat("Deck", new Color(0.86f, 0.83f, 0.76f));
            var tileMat = Mat("PoolTile", new Color(0.6f, 0.86f, 0.9f));
            tileMat.SetTexture("_BaseMap", PoolTileTexture());
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
                // Starts small; Pool Expansion I and II grow it (bigger waves need a bigger pool).
                stages = new[] { new Vector2(40, 14), new Vector2(50, 19), new Vector2(PoolLength, PoolWidth) },
                stageNames = new[] { "Starter Pool", "Expanded Pool", "Full-Size Pool" },
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
            var replay = player.AddComponent<SurfReplay>();
            Set(replay, "surfing", surfing);
            Set(replay, "avatarPrefab", BuildReplayAvatarPrefab());

            // Money.
            var game = new GameObject("Game");
            game.AddComponent<Wallet>();
            var pricing = game.AddComponent<PriceBoard>();
            game.AddComponent<ParkReputation>();
            game.AddComponent<Highlights>();
            var payroll = game.AddComponent<StaffPayroll>();

            // Ticket desk on the south deck. The player stands on the pool side, customers on the far side.
            var desk = new GameObject("Ticket Desk").transform;
            desk.position = new Vector3(20, 0, -22.5f);
            WorldArt.Model("KioskTeal", desk, desk.position);
            Signs.Kiosk("TICKETS", desk, desk.position, 0f);
            Hidden(Box("Counter", desk, desk.position + new Vector3(0, 0.55f, 0), new Vector3(3, 1.1f, 1), deskMat));
            WorldArt.Model("Counter", desk, desk.position);
            var registerGo = Box("Register", desk, desk.position + new Vector3(0.6f, 1.32f, 0), new Vector3(0.5f, 0.44f, 0.42f), consoleMat);
            registerGo.GetComponent<MeshRenderer>().enabled = false; // keeps its collider; the model is the look
            WorldArt.Model("CashRegister", desk, desk.position + new Vector3(0.6f, 1.1f, 0), 180f);
            var register = registerGo.AddComponent<TicketRegister>();
            Set(register, "pricing", pricing);

            // Upgrade computer on the other end of the counter.
            var computer = new GameObject("Upgrade Computer").transform;
            computer.SetParent(desk, false);
            computer.position = desk.position + new Vector3(-0.8f, 1.1f, -0.2f);
            var monitorStand = Box("Monitor Stand", computer, computer.position + new Vector3(0, 0.12f, 0), new Vector3(0.12f, 0.24f, 0.12f), consoleMat);
            monitorStand.GetComponent<MeshRenderer>().enabled = false;
            WorldArt.Model("Computer", computer, computer.position, 180f);
            // The glowing screen sits in the model's bezel (its collider is what you look at to use the computer).
            Box("Monitor", computer, computer.position + new Vector3(0, 0.45f, -0.005f), new Vector3(0.58f, 0.36f, 0.012f), screenMat);
            var shop = computer.gameObject.AddComponent<UpgradeShop>();

            // Price panel next to the desk: − on the left and + on the right, as seen from behind the desk.
            var pricePanel = new GameObject("Price Panel").transform;
            pricePanel.position = new Vector3(17.6f, 0, -22.3f);
            var pricePedestal = Box("Pedestal", pricePanel, pricePanel.position + new Vector3(0, 0.5f, 0), new Vector3(0.7f, 1f, 0.5f), consoleMat);
            pricePedestal.GetComponent<MeshRenderer>().enabled = false;
            WorldArt.Model("PriceStand", pricePanel, pricePanel.position, 180f);
            var priceUp = Box("Price Up (+)", pricePanel, pricePanel.position + new Vector3(-0.17f, 1.04f, 0), new Vector3(0.22f, 0.08f, 0.22f), greenMat);
            Set(priceUp.AddComponent<PriceButton>(), "pricing", pricing);
            var priceDown = Box("Price Down (-)", pricePanel, pricePanel.position + new Vector3(0.17f, 1.04f, 0), new Vector3(0.22f, 0.08f, 0.22f), redMat);
            var downButton = priceDown.AddComponent<PriceButton>();
            Set(downButton, "pricing", pricing);
            SetInt(downButton, "direction", -1);

            // Upgrade visuals, hidden until bought.
            var bench = new GameObject("Surfer Bench");
            bench.transform.SetParent(env, false);
            Hidden(Box("Bench Collider", bench.transform, new Vector3(30, 0.25f, -15), new Vector3(3, 0.5f, 0.8f), deskMat));
            WorldArt.Model("Bench", bench.transform, new Vector3(30, 0, -15), 180f);
            bench.SetActive(false);

            // Board rental stand east of the ticket desk, hidden until bought.
            var stand = new GameObject("Rental Stand").transform;
            stand.position = new Vector3(30, 0, -22.5f);
            Hidden(Box("Rental Counter", stand, stand.position + new Vector3(0, 0.55f, 0), new Vector3(3, 1.1f, 1), deskMat));
            WorldArt.Model("Counter", stand, stand.position);
            WorldArt.Model("KioskCoral", stand, stand.position);
            Signs.Kiosk("BOARD RENTALS", stand, stand.position, 0f);

            const int boardSlots = 10;
            var rackRoot = new GameObject("Board Rack").transform;
            rackRoot.SetParent(stand, false);
            rackRoot.position = new Vector3(32.6f, 0, -20.6f);
            Hidden(Box("Rack Frame", rackRoot, rackRoot.position + new Vector3(0, 0.6f, 0), new Vector3(0.4f, 1.2f, 2.4f), consoleMat));
            WorldArt.Model("BoardRack", rackRoot, rackRoot.position);
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
            Hidden(Box("Bin", binRoot, binRoot.position + new Vector3(0, 0.3f, 0), new Vector3(2.0f, 0.6f, 1.0f), consoleMat));
            WorldArt.Model("ReturnCrate", binRoot, binRoot.position);
            Signs.Text("RETURNS", binRoot, binRoot.position + new Vector3(0, 0.38f, -0.53f), 0f, 0.16f, new Color(0.06f, 0.18f, 0.23f), 1.4f);
            var binPile = new Object[boardSlots];
            for (int i = 0; i < boardSlots; i++)
            {
                var pileBoard = Board($"Returned Board {i + 1}", binRoot, binRoot.position + new Vector3((i % 2) * 0.1f - 0.05f, 0.15f + i * 0.11f, 0), Quaternion.Euler(0, 90, 0), 0.9f, rentalBoardMat);
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

            // Renters return their board past the bin on the way out of the main pool (then west of the gear shop).
            var mainOut = main.area.PathOut;
            var pastShop = Point("Past Gear Shop", route, new Vector3(37f, 0, -37.8f));
            SetArray(main.area, "returnPathOut", mainOut[0], binSpot, pastShop, mainOut[mainOut.Length - 1]);

            // Two more pools to the north, built later. Surfers walk up the far west edge of the decks, behind the wave
            // machines (their chambers fill x -6..0 and the blowers, tanks and control cabins sit behind them).
            var reef = BuildPool(new PoolSpec
            {
                // A long point-break pool: 75 x 36 m, slower waves that peel from near the north wall (~10 s rides).
                name = "Reef Pool", display = "Reef Pool", upgradeId = "pool_reef", z = ReefZ, open = false, depth = PoolDepth,
                waterLevel = WaterLevel, size = 1.08f, peel = 0.85f, price = 2.5f, water = 60f,
                length = 75f, width = 36f, travel = 0.7f, peakOffset = 12f,
                pathToPool = new[] { new Vector3(-2, 0, -18), new Vector3(-19.3f, 0, -18.8f), new Vector3(-19.3f, 0, ReefZ - 26), new Vector3(14, 0, ReefZ - 26), new Vector3(14, 0, ReefZ - 18 - 0.6f) },
                pathOut = new[] { new Vector3(75 + 3, 0, ReefZ - 6), new Vector3(75 + 40, 0, ReefZ - 6) },
            });
            var lagoon = BuildPool(new PoolSpec
            {
                // The biggest pool: 75 x 48 m, heavy waves that peel the whole width.
                name = "Big Wave Lagoon", display = "Big Wave Lagoon", upgradeId = "pool_lagoon", z = LagoonZ, open = false,
                depth = PoolDepth + 1f, waterLevel = WaterLevel - 0.8f, size = 1.25f, peel = 0.9f, price = 5f, water = 150f,
                length = 75f, width = 48f, travel = 0.65f, peakOffset = 16f,
                pathToPool = new[] { new Vector3(-2, 0, -18), new Vector3(-19.3f, 0, -18.8f), new Vector3(-19.3f, 0, LagoonZ - 32), new Vector3(14, 0, LagoonZ - 32), new Vector3(14, 0, LagoonZ - 24 - 0.6f) },
                pathOut = new[] { new Vector3(75 + 3, 0, LagoonZ - 6), new Vector3(75 + 40, 0, LagoonZ - 6) },
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
            Staff(rentalClerk, "Rental Clerk", 8);

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
            Staff(runner, "Board Runner", 6);

            var tower = new GameObject("Lifeguard Tower").transform;
            tower.position = new Vector3(30, 0, halfW + 1.8f);
            WorldArt.Model("LifeguardTower", tower, tower.position);
            var guard = Person("Lifeguard", tower.position + new Vector3(0, 1.96f, 0), 180f, lifeguardMat, skinMat);
            guard.transform.SetParent(tower, true);
            tower.gameObject.AddComponent<Lifeguard>();
            Staff(tower.gameObject, "Lifeguard", 3);

            // Surf coach at the shallow end of the main pool, wave operator at the main console.
            var coachMat = Mat("CoachShirt", new Color(1f, 0.78f, 0.25f), 0.3f);
            var coach = Person("Surf Coach", new Vector3(40, 0, -halfW - 1.2f), 0f, coachMat, skinMat);
            coach.AddComponent<SurfCoach>();
            Staff(coach, "Surf Coach", 15);
            var operatorGuy = Person("Wave Operator", new Vector3(3.6f, 0, -halfW - 2.4f), 30f, staffMat, skinMat);
            operatorGuy.AddComponent<WaveOperator>();
            Staff(operatorGuy, "Wave Operator", 25);

            // Mechanic by the main machine house, hidden until hired.
            var mechanicMat = Mat("MechanicShirt", new Color(0.2f, 0.32f, 0.55f), 0.3f);
            var mechanic = Person("Mechanic", new Vector3(-2.6f, 0, -halfW - 1.2f), 60f, mechanicMat, skinMat);
            mechanic.AddComponent<Mechanic>();
            Staff(mechanic, "Mechanic", 10);
            Set(shop, "mechanic", mechanic);

            // Snack bar by the main pool's exit, hidden until bought. Customers line up on its west side.
            var snack = new GameObject("Snack Bar").transform;
            snack.position = new Vector3(57.5f, 0, -24f);
            WorldArt.Model("KioskCoral", snack, snack.position, 90f);
            Signs.Kiosk("SNACKS", snack, snack.position, 90f);
            var snackRoute = new GameObject("Snack Route").transform;
            snackRoute.SetParent(snack, false);
            Hidden(Box("Snack Counter", snack, snack.position + new Vector3(-0.9f, 0.55f, 0), new Vector3(0.9f, 1.1f, 3f), deskMat));
            WorldArt.Model("Counter", snack, snack.position + new Vector3(-0.9f, 0f, 0f), 90f);
            var grill = Box("Grill", snack, snack.position + new Vector3(-0.8f, 1.16f, 0.7f), new Vector3(0.6f, 0.12f, 0.9f), consoleMat);
            Box("Grill Top", snack, grill.transform.position + new Vector3(0, 0.07f, 0), new Vector3(0.55f, 0.02f, 0.85f), Mat("GrillHot", new Color(0.25f, 0.2f, 0.18f), 0.6f, new Color(0.6f, 0.15f, 0.05f)));
            var cupMat = Mat("SmoothieCup", new Color(1f, 0.55f, 0.65f), 0.4f);
            for (int i = 0; i < 4; i++)
            {
                var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cup.name = "Smoothie";
                Object.DestroyImmediate(cup.GetComponent<Collider>());
                cup.transform.SetParent(snack, false);
                cup.transform.position = snack.position + new Vector3(-1.1f, 1.22f, -0.9f + i * 0.22f);
                cup.transform.localScale = new Vector3(0.11f, 0.12f, 0.11f);
                cup.GetComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? cupMat : mats.sign;
            }
            // A chalkboard menu standing out front, facing the line.
            var menuAt = snack.position + new Vector3(-2.2f, 0f, 2.1f);
            var chalk = Mat("MenuBoard", new Color(0.12f, 0.16f, 0.2f), 0.3f);
            Box("Menu Board", snack, menuAt + new Vector3(0f, 1.05f, 0f), new Vector3(0.06f, 0.9f, 1.1f), chalk);
            Box("Menu Leg", snack, menuAt + new Vector3(0f, 0.3f, -0.45f), new Vector3(0.06f, 0.6f, 0.06f), mats.coping).GetComponent<Collider>().enabled = false;
            Box("Menu Leg", snack, menuAt + new Vector3(0f, 0.3f, 0.45f), new Vector3(0.06f, 0.6f, 0.06f), mats.coping).GetComponent<Collider>().enabled = false;
            Signs.Text("MENU", snack, menuAt + new Vector3(-0.04f, 1.37f, 0f), 90f, 0.16f, Color.white, 1f);
            Signs.Text("FISH TACOS", snack, menuAt + new Vector3(-0.04f, 1.12f, 0f), 90f, 0.13f, new Color(1f, 0.82f, 0.35f), 1f);
            Signs.Text("SMOOTHIES", snack, menuAt + new Vector3(-0.04f, 0.92f, 0f), 90f, 0.13f, new Color(1f, 0.6f, 0.7f), 1f);
            Signs.Text("COLD DRINKS", snack, menuAt + new Vector3(-0.04f, 0.72f, 0f), 90f, 0.13f, new Color(0.6f, 0.9f, 0.8f), 1f);
            var snackBar = snack.gameObject.AddComponent<SnackBar>();
            Set(snackBar, "lineSpot", Point("Snack Line Front", snackRoute, new Vector3(56.0f, 0, -24f)));
            Set(snackBar, "approach", Point("Snack Approach", snackRoute, new Vector3(56.0f, 0, -17.5f)));
            SetArray(snackBar, "exitPath", Point("Snack Exit", snackRoute, new Vector3(54f, 0, -29f)));
            var cookMat = Mat("CookShirt", new Color(1f, 1f, 1f), 0.3f);
            var cook = Person("Snack Cook", snack.position + new Vector3(0.1f, 0, -0.4f), -90f, cookMat, skinMat);
            cook.transform.SetParent(snack, true);
            Set(cook.AddComponent<SnackCook>(), "snackBar", snackBar);
            Staff(cook, "Snack Cook", 6);
            Set(shop, "snackBar", snack.gameObject);
            Set(shop, "snackCook", cook);
            cook.SetActive(false);
            snack.gameObject.SetActive(false);

            // Janitor's broom closet by the desk, hidden until hired.
            var janitorMat = Mat("JanitorShirt", new Color(0.55f, 0.35f, 0.75f), 0.3f);
            var janitor = Person("Janitor", new Vector3(14f, 0, -26f), 0f, janitorMat, skinMat);
            janitor.AddComponent<Janitor>();
            Staff(janitor, "Janitor", 5);
            Set(shop, "janitor", janitor);
            janitor.SetActive(false);

            // Trash cans around the decks, hidden until bought.
            var bins = new GameObject("Trash Cans");
            var binMat = Mat("TrashCan", new Color(0.12f, 0.6f, 0.55f), 0.4f);
            var lidMat = Mat("TrashLid", new Color(0.93f, 0.93f, 0.9f), 0.4f);
            Vector3[] binSpots =
            {
                new(12, 0, -16), new(28, 0, -16), new(44, 0, -16), new(26, 0, -29), new(38, 0, -31),
                new(8, 0, 15), new(30, 0, 15), new(52, 0, 15), new(64, 0, 4), new(64, 0, -10),
                new(10, 0, 79), new(40, 0, 79), new(10, 0, 131), new(40, 0, 131),
            };
            foreach (var spot in binSpots)
            {
                var can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                can.name = "Trash Can";
                can.transform.SetParent(bins.transform, false);
                can.transform.position = spot + new Vector3(0, 0.5f, 0);
                can.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
                can.GetComponent<MeshRenderer>().sharedMaterial = binMat;
                var lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lid.name = "Lid";
                lid.transform.SetParent(bins.transform, false);
                lid.transform.position = spot + new Vector3(0, 1.03f, 0);
                lid.transform.localScale = new Vector3(0.66f, 0.04f, 0.66f);
                lid.GetComponent<MeshRenderer>().sharedMaterial = lidMat;
                Object.DestroyImmediate(lid.GetComponent<Collider>());
            }
            bins.SetActive(false);
            Set(shop, "trashCans", bins);
            mechanic.SetActive(false);

            Set(shop, "surfCoach", coach);
            Set(shop, "waveOperator", operatorGuy);

            // The other pools' north decks get umbrellas and loungers when they're built.
            PoolDeckDressing(reef.built, ReefZ, 18f, 75f);
            PoolDeckDressing(lagoon.built, LagoonZ, 24f, 75f);

            // Tiki Garden (Park upgrade): torches and flower beds along every pool, hidden until bought.
            var gardens = new Object[]
            {
                TikiGarden(null, 0f, true, PoolWidth / 2f, PoolLength), TikiGarden(reef.built, ReefZ, false, 18f, 75f),
                TikiGarden(lagoon.built, LagoonZ, false, 24f, 75f),
            };
            foreach (GameObject garden in gardens) garden.SetActive(false);
            SetArray(shop, "tikiGarden", gardens);

            // Grandstand (Park upgrade): bleachers full of spectators on the main pool's north deck.
            var grandstand = Grandstand();
            grandstand.SetActive(false);
            Set(shop, "grandstand", grandstand);
            coach.SetActive(false);
            operatorGuy.SetActive(false);
            game.AddComponent<PassiveIncome>();
            game.AddComponent<OperatingCosts>();
            game.AddComponent<ParkLevel>();
            var beachGuests = game.AddComponent<BeachGuests>();
            Set(beachGuests, "guestPrefab", BuildGuestPrefab());
            Set(beachGuests, "spawner", spawner);
            Set(game.AddComponent<DailyChallenges>(), "shop", shop);
            var events = game.AddComponent<DailyEvents>();
            var rain = BuildRain();
            rain.transform.SetParent(player.transform, false);
            rain.transform.localPosition = new Vector3(0, 14f, 0);
            Set(events, "rain", rain);

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
            hudGo.AddComponent<SurfHud>();
            hudGo.AddComponent<PerkPicker>();
            hudGo.AddComponent<ParkNamer>();
            BuildShops(shop, spawner, game, hudGo, skinMat);
            BuildParkSign();
            Set(shop, "photoSpot", BuildPhotoSpot());
            BuildRescuePosts();
            var legends = game.AddComponent<FamousVisits>();
            Set(legends, "spawner", spawner);
            Set(legends, "surferPrefab", AssetDatabase.LoadAssetAtPath<Surfer>("Assets/_Game/Prefabs/Surfer.prefab"));
            var sneaks = game.AddComponent<RuleBreakers>();
            Set(sneaks, "spawner", spawner);
            Set(sneaks, "surferPrefab", AssetDatabase.LoadAssetAtPath<Surfer>("Assets/_Game/Prefabs/Surfer.prefab"));
            var rush = game.AddComponent<SurfRush>();
            Set(rush, "spawner", spawner);
            Set(rush, "surferPrefab", AssetDatabase.LoadAssetAtPath<Surfer>("Assets/_Game/Prefabs/Surfer.prefab"));
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

            // Day cycle, end-of-day report and saving.
            var dayCycle = game.AddComponent<DayCycle>();
            Set(dayCycle, "sun", GameObject.Find("Directional Light").GetComponent<Light>());
            Set(dayCycle, "spawner", spawner);
            var saveSystem = game.AddComponent<SaveSystem>();
            Set(saveSystem, "shop", shop);
            Set(saveSystem, "pricing", pricing);
            Set(saveSystem, "spawner", spawner);

            var reportGo = new GameObject("Day Report Screen");
            var reportDoc = reportGo.AddComponent<UnityEngine.UIElements.UIDocument>();
            reportDoc.panelSettings = panelSettings;
            reportDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/DayReport.uxml");
            reportDoc.sortingOrder = 15;
            Set(reportGo.AddComponent<DayReportScreen>(), "saveSystem", saveSystem);

            var pauseGo = new GameObject("Pause Menu");
            var pauseDoc = pauseGo.AddComponent<UnityEngine.UIElements.UIDocument>();
            pauseDoc.panelSettings = panelSettings;
            pauseDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/Pause.uxml");
            pauseDoc.sortingOrder = 30;
            var pause = pauseGo.AddComponent<PauseMenu>();
            Set(pause, "saveSystem", saveSystem);
            Set(pause, "spawner", spawner);

            // Title screen over everything at start, and the settings window above it (also opened from pause).
            var menuDoc = new GameObject("Main Menu").AddComponent<UnityEngine.UIElements.UIDocument>();
            menuDoc.panelSettings = panelSettings;
            menuDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/MainMenu.uxml");
            menuDoc.sortingOrder = 40;
            menuDoc.gameObject.AddComponent<MainMenu>();
            var settingsDoc = new GameObject("Settings").AddComponent<UnityEngine.UIElements.UIDocument>();
            settingsDoc.panelSettings = panelSettings;
            settingsDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/Settings.uxml");
            settingsDoc.sortingOrder = 50;
            settingsDoc.gameObject.AddComponent<SettingsScreen>();

            // Build mode (B): decorate the park. The bar sits at the bottom of the screen while building.
            var buildDoc = new GameObject("Build Bar").AddComponent<UnityEngine.UIElements.UIDocument>();
            buildDoc.panelSettings = panelSettings;
            buildDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/BuildBar.uxml");
            buildDoc.sortingOrder = 6;
            var build = player.AddComponent<BuildMode>();
            Set(build, "catalog", DecorBuilder.Catalog());
            Set(build, "playerCamera", cam);
            Set(build, "ghostOk", DecorBuilder.Ghost(true));
            Set(build, "ghostBad", DecorBuilder.Ghost(false));
            Set(build, "bar", buildDoc);

            // Sound: beach ambience, music and every effect (clips made by tools/audio/make_sounds.py).
            var sfx = new GameObject("Audio").AddComponent<Sfx>();
            Set(sfx, "bank", AudioSetup.Bank());
            Set(sfx, "pricing", pricing);

            var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            WorldArt.BuildEnvironment(sandMat, waterMat);
            TreasureBuilder.Build();
            Set(shop, "fishingPier", ParkExtras.FishingPier(panelSettings));
            ParkExtras.ParkingLot();
            GlowUpBuilder.Build(shop, reef.built, lagoon.built, deckMat, copingMat, tileMat, ReefZ, LagoonZ);
            BuildUpgradeProps();
            var wornTile = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/PoolTileWorn.mat");
            if (wornTile != null)
            {
                wornTile.SetTexture("_BaseMap", PoolTileTexture());
                EditorUtility.SetDirty(wornTile);
            }
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
            public float z, depth, waterLevel, size, peel, price = 1f, water = 25f;
            // Pool size (0 = the standard PoolLength x PoolWidth). Long, wide pools with slower waves (travel) and a
            // break that starts near the wall (peakOffset) give long rides; see docs/research/long-rides.md.
            public float length, width, travel = 1f, peakOffset = 4f;
            public float Length => length > 0f ? length : PoolLength;
            public float Width => width > 0f ? width : PoolWidth;
            public Vector2[] stages; // (length, width) from smallest to full size; null = always full size
            public string[] stageNames;
            public bool open, mainDeck;
            public Vector3[] pathToPool, pathOut;
        }

        class PoolBuild
        {
            public PoolArea area;
            public Transform built;
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
            float poolLength = spec.Length, poolWidth = spec.Width;
            float halfW = poolWidth / 2f, zc = spec.z, depth = spec.depth, thickness = depth + 1f;
            var root = new GameObject(spec.name).transform;
            var built = new GameObject("Built").transform;
            built.SetParent(root, false);

            // Deck. The main pool's deck is wide (desks and sunbathing live on it); the others have narrower strips.
            float south = spec.mainDeck ? 28 : 12, north = spec.mainDeck ? 28 : 16;
            Box("Deck North", built, new Vector3(poolLength / 2, -thickness / 2, zc + halfW + north / 2), new Vector3(poolLength + 40, thickness, north), mats.deck);
            Box("Deck South", built, new Vector3(poolLength / 2, -thickness / 2, zc - halfW - south / 2), new Vector3(poolLength + 40, thickness, south), mats.deck);
            Box("Deck West", built, new Vector3(-10, -thickness / 2, zc), new Vector3(20, thickness, poolWidth), mats.deck);
            Box("Deck East", built, new Vector3(poolLength + 10, -thickness / 2, zc), new Vector3(20, thickness, poolWidth), mats.deck);

            // The basin (walls, floor, beach, coping, edge blockers). A pool with size stages gets one basin per stage;
            // smaller stages fill the not-yet-dug part with deck and an "expansion site" sign.
            float southZ = zc - halfW;
            var stageObjects = new List<GameObject>();
            if (spec.stages == null)
                BuildBasin(built, southZ, poolLength, poolWidth, depth, thickness, spec.waterLevel);
            else
                for (int i = 0; i < spec.stages.Length; i++)
                {
                    var stage = new GameObject($"Size {i + 1} ({spec.stages[i].x:0}x{spec.stages[i].y:0})");
                    stage.transform.SetParent(built, false);
                    BuildBasin(stage.transform, southZ, spec.stages[i].x, spec.stages[i].y, depth, thickness, spec.waterLevel);
                    stage.SetActive(i == 0);
                    stageObjects.Add(stage);
                }

            var waterGo = new GameObject("Water", typeof(MeshFilter), typeof(MeshRenderer));
            waterGo.transform.SetParent(built, false);
            waterGo.transform.position = new Vector3(0, spec.waterLevel, zc);
            waterGo.GetComponent<MeshRenderer>().sharedMaterial = mats.water;
            var water = waterGo.AddComponent<WaveWater>();
            SetFloat(water, "sizeMultiplier", spec.size);
            SetFloat(water, "peelSpeedMultiplier", spec.peel);
            SetFloat(water, "travelSpeedMultiplier", spec.travel);
            SetFloat(water, "peakOffset", spec.peakOffset);
            if (spec.stages == null)
            {
                SetFloat(water, "length", poolLength);
                SetFloat(water, "width", poolWidth);
            }
            var lipGo = new GameObject("Barrel Lip", typeof(MeshFilter), typeof(MeshRenderer));
            lipGo.transform.SetParent(waterGo.transform, false);
            lipGo.GetComponent<MeshRenderer>().sharedMaterial = mats.water;
            Set(water, "lipMesh", lipGo.GetComponent<MeshFilter>());
            var spray = BuildSpray(mats.foam);
            spray.transform.SetParent(built, true);
            Set(water, "spray", spray);

            // The machine's collider (hold E to repair); the open "air wall" machine built below is the look.
            var machineGo = Box("Wave Machine", built, new Vector3(-7.25f, 2.5f, zc), new Vector3(14.5f, 5, poolWidth), mats.machine);
            machineGo.GetComponent<MeshRenderer>().enabled = false;
            var machine = machineGo.AddComponent<WaveMachine>();
            Set(machine, "water", water);
            var autoLight = Box("Auto Wave Light", built, new Vector3(-6, 4.85f, zc - 9), new Vector3(0.6f, 0.5f, 0.6f), mats.beacon);
            autoLight.SetActive(false);

            // Breakdowns: a flashing red light and black smoke from the roof when the machine is broken.
            var service = machineGo.AddComponent<MachineService>();
            var brokenLight = Box("Broken Light", built, new Vector3(-6, 4.85f, zc - 7.6f), new Vector3(0.6f, 0.5f, 0.6f),
                Mat("BrokenLight", new Color(1f, 0.15f, 0.1f), 0.5f, new Color(3f, 0.3f, 0.2f)));
            Object.DestroyImmediate(brokenLight.GetComponent<Collider>());
            var smoke = BuildSmoke();
            smoke.transform.SetParent(built, false);
            smoke.transform.position = new Vector3(-10, 5.5f, zc);
            Set(service, "smoke", smoke);
            Set(service, "brokenLight", brokenLight);

            var console = new GameObject("Wave Console").transform;
            console.SetParent(built, false);
            console.position = new Vector3(5, 0, zc - halfW - 1.5f);
            var pedestal = Box("Pedestal", console, console.position + new Vector3(0, 0.5f, 0), new Vector3(1.2f, 1f, 0.8f), mats.console);
            pedestal.GetComponent<MeshRenderer>().enabled = false;
            WorldArt.Model("WaveConsole", console, console.position);
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
                Box("Lot Sand", lot.transform, new Vector3(poolLength / 2, -0.6f, zc + (north - south) / 2), new Vector3(poolLength + 40, 1, poolWidth + north + south), mats.sand);
                for (int i = 0; i <= Mathf.CeilToInt((poolLength + 10) / 3.5f); i++)
                {
                    Box("Fence Post", lot.transform, new Vector3(-5 + i * 3.5f, 0.6f, zc - halfW - 2), new Vector3(0.15f, 1.2f, 0.15f), mats.coping);
                    Box("Fence Post", lot.transform, new Vector3(-5 + i * 3.5f, 0.6f, zc + halfW + 2), new Vector3(0.15f, 1.2f, 0.15f), mats.coping);
                }
                Box("Fence Rail South", lot.transform, new Vector3(poolLength / 2, 1.0f, zc - halfW - 2), new Vector3(poolLength + 10, 0.12f, 0.08f), mats.sign);
                Box("Fence Rail North", lot.transform, new Vector3(poolLength / 2, 1.0f, zc + halfW + 2), new Vector3(poolLength + 10, 0.12f, 0.08f), mats.sign);
                Box("Sign Post L", lot.transform, new Vector3(27.5f, 1.5f, zc - halfW - 2.3f), new Vector3(0.2f, 3f, 0.2f), mats.coping);
                Box("Sign Post R", lot.transform, new Vector3(32.5f, 1.5f, zc - halfW - 2.3f), new Vector3(0.2f, 3f, 0.2f), mats.coping);
                Box("Coming Soon Sign", lot.transform, new Vector3(30, 2.6f, zc - halfW - 2.4f), new Vector3(6f, 1.6f, 0.12f), mats.sign);
                Signs.Text(spec.display.ToUpperInvariant(), lot.transform, new Vector3(30, 2.9f, zc - halfW - 2.47f), 0f, 0.42f, Color.white, 5.6f);
                Signs.Text("COMING SOON", lot.transform, new Vector3(30, 2.3f, zc - halfW - 2.47f), 0f, 0.32f, new Color(1f, 0.92f, 0.7f), 5.6f);
            }

            var route = new GameObject("Route").transform;
            route.SetParent(root, false);
            var toPool = new Object[spec.pathToPool.Length];
            for (int i = 0; i < toPool.Length; i++) toPool[i] = Point($"To Pool {i + 1}", route, spec.pathToPool[i]);
            var outPoints = new Object[spec.pathOut.Length];
            for (int i = 0; i < outPoints.Length; i++) outPoints[i] = Point($"Out {i + 1}", route, spec.pathOut[i]);
            var lineup = Point("Lineup", route, new Vector3(12, spec.waterLevel, zc - 4));
            // On the beach slope, 95% of the way down the pool (PoolArea moves it when the pool changes size).
            var landing = Point("Beach Landing", route, new Vector3(poolLength * 0.95f, -depth * 0.2f, zc));

            var area = root.gameObject.AddComponent<PoolArea>();
            SetString(area, "displayName", spec.display);
            SetFloat(area, "priceMultiplier", spec.price);
            SetFloat(area, "southEdge", southZ);
            SetFloat(area, "dailyWater", spec.water);
            Set(area, "solarPanels", SolarPanels(built, zc));
            Set(area, "waterTanks", WaterTanks(built, zc));
            if (spec.stages != null)
            {
                var so = new SerializedObject(area);
                var stagesProp = so.FindProperty("stages");
                stagesProp.arraySize = spec.stages.Length;
                for (int i = 0; i < spec.stages.Length; i++)
                {
                    var element = stagesProp.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("name").stringValue = spec.stageNames[i];
                    element.FindPropertyRelative("length").floatValue = spec.stages[i].x;
                    element.FindPropertyRelative("width").floatValue = spec.stages[i].y;
                    element.FindPropertyRelative("visuals").objectReferenceValue = stageObjects[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
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
            BuildAirWall(built, zc, southZ, poolWidth, spec.display, machine, area, autoLight, brokenLight, smoke.transform);
            if (!spec.open) built.gameObject.SetActive(false);

            return new PoolBuild { area = area, built = built, water = water, machine = machine, panel = panel, peg = peg };
        }

        /// <summary>Rows of tilted solar panels on the blower roof behind the machine (hidden until Solar Panels are bought).</summary>
        static GameObject SolarPanels(Transform parent, float zc)
        {
            var root = new GameObject("Solar Panels");
            root.transform.SetParent(parent, false);
            var cell = Mat("SolarCell", new Color(0.12f, 0.2f, 0.42f), 0.85f);
            var frame = Mat("SolarFrame", new Color(0.85f, 0.87f, 0.9f), 0.5f);
            for (int row = 0; row < 2; row++)
            for (int i = 0; i < 4; i++)
            {
                var center = new Vector3(-12.3f + row * 3.6f, 4.95f, zc - 7.5f + i * 5f);
                var panel = Box("Solar Panel", root.transform, center, new Vector3(3f, 0.08f, 4.4f), cell);
                panel.transform.rotation = Quaternion.Euler(0, 0, 18f);
                Object.DestroyImmediate(panel.GetComponent<Collider>());
                var rim = Box("Panel Frame", root.transform, center + new Vector3(0, -0.06f, 0), new Vector3(3.2f, 0.06f, 4.6f), frame);
                rim.transform.rotation = panel.transform.rotation;
                Object.DestroyImmediate(rim.GetComponent<Collider>());
            }
            root.SetActive(false);
            return root;
        }

        /// <summary>Two big filter tanks with pipes beside the machine house (hidden until Water Recycling is bought).</summary>
        static GameObject WaterTanks(Transform parent, float zc)
        {
            var root = new GameObject("Water Recycling");
            root.transform.SetParent(parent, false);
            var tank = Mat("Tank", new Color(0.92f, 0.95f, 0.96f), 0.45f);
            var band = Mat("TankBand", new Color(0.08f, 0.72f, 0.65f), 0.4f);
            foreach (float dz in new[] { -5f, 5f })
            {
                var basePos = new Vector3(-17.2f, 0, zc + dz);
                var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Filter Tank";
                body.transform.SetParent(root.transform, false);
                body.transform.position = basePos + new Vector3(0, 1.9f, 0);
                body.transform.localScale = new Vector3(2.6f, 1.9f, 2.6f);
                body.GetComponent<MeshRenderer>().sharedMaterial = tank;
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stripe.name = "Tank Band";
                stripe.transform.SetParent(root.transform, false);
                stripe.transform.position = basePos + new Vector3(0, 2.6f, 0);
                stripe.transform.localScale = new Vector3(2.66f, 0.2f, 2.66f);
                stripe.GetComponent<MeshRenderer>().sharedMaterial = band;
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
                var pipe = Box("Pipe", root.transform, basePos + new Vector3(2.6f, 0.6f, 0), new Vector3(3f, 0.3f, 0.3f), mats.coping);
                Object.DestroyImmediate(pipe.GetComponent<Collider>());
            }
            root.SetActive(false);
            return root;
        }

        static void PoolDeckDressing(Transform parent, float zc, float halfWidth, float length)
        {
            var dressing = new GameObject("Deck Dressing").transform;
            dressing.SetParent(parent, false);
            float z = zc + halfWidth + 7f;
            for (int i = 0; i < Mathf.RoundToInt(length / 10f); i++)
            {
                float x = 6 + i * 9f;
                WorldArt.Model(i % 2 == 0 ? "BeachUmbrellaCoral" : "BeachUmbrellaTeal", dressing, new Vector3(x, 0, z + 0.8f), i * 47f);
                WorldArt.Model("LoungeChair", dressing, new Vector3(x - 0.8f, 0, z), 180f);
                WorldArt.Model("LoungeChair", dressing, new Vector3(x + 0.8f, 0, z), 180f);
            }
            for (int i = 0; i < Mathf.RoundToInt(length / 15f); i++)
                WorldArt.Model(i % 2 == 0 ? "PalmTreeTall" : "PalmTree", dressing, new Vector3(2 + i * 17f, 0, z + 5f), i * 83f);
        }

        /// <summary>Tiki torches along the pool's north edge and flower beds behind them (the main pool also gets the east edge).</summary>
        static GameObject TikiGarden(Transform parent, float zc, bool mainPool, float halfWidth, float length)
        {
            var garden = new GameObject("Tiki Garden");
            if (parent != null) garden.transform.SetParent(parent, false);
            var pole = Mat("TikiPole", new Color(0.45f, 0.3f, 0.18f));
            var flame = Mat("TikiFlame", new Color(1f, 0.55f, 0.1f), 0.2f, new Color(3f, 1.4f, 0.2f));
            var planter = Mat("Planter", new Color(0.55f, 0.38f, 0.24f));
            var leaf = Mat("Leaf", new Color(0.25f, 0.62f, 0.3f));
            Material[] flowers =
            {
                Mat("FlowerPink", new Color(1f, 0.45f, 0.65f)), Mat("FlowerYellow", new Color(1f, 0.85f, 0.25f)),
                Mat("FlowerOrange", new Color(1f, 0.55f, 0.2f)),
            };

            void Torch(Vector3 at)
            {
                var t = Box("Tiki Torch", garden.transform, at + new Vector3(0, 0.9f, 0), new Vector3(0.14f, 1.8f, 0.14f), pole);
                Object.DestroyImmediate(t.GetComponent<Collider>());
                var f = Box("Flame", t.transform.parent, at + new Vector3(0, 1.95f, 0), new Vector3(0.22f, 0.32f, 0.22f), flame);
                Object.DestroyImmediate(f.GetComponent<Collider>());
            }
            void Bed(Vector3 at, int seed)
            {
                Box("Flower Bed", garden.transform, at + new Vector3(0, 0.2f, 0), new Vector3(2.4f, 0.4f, 0.9f), planter);
                for (int i = 0; i < 5; i++)
                {
                    var bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bloom.name = "Bloom";
                    Object.DestroyImmediate(bloom.GetComponent<Collider>());
                    bloom.transform.SetParent(garden.transform, false);
                    bloom.transform.position = at + new Vector3(-0.9f + i * 0.45f, 0.55f + (i % 2) * 0.08f, ((i + seed) % 3 - 1) * 0.18f);
                    bloom.transform.localScale = Vector3.one * (i % 2 == 0 ? 0.42f : 0.34f);
                    bloom.GetComponent<MeshRenderer>().sharedMaterial = i % 2 == 0 ? leaf : flowers[(i + seed) % flowers.Length];
                }
            }

            float edge = zc + halfWidth + 1.4f;
            int torches = Mathf.RoundToInt(length / 8f);
            for (int i = 0; i < torches; i++) Torch(new Vector3(3 + i * 8f, 0, edge));
            for (int i = 0; i < torches - 1; i++) Bed(new Vector3(7 + i * 8f, 0, edge + 1.6f), i);
            if (mainPool)
                for (int i = 0; i < 4; i++)
                {
                    Torch(new Vector3(PoolLength + 1.6f, 0, -8 + i * 6f));
                    if (i < 3) Bed(new Vector3(PoolLength + 4f, 0, -5 + i * 6f), i + 3);
                }
            return garden;
        }

        /// <summary>Three rows of bleachers with a shade roof and seated spectators, facing the main pool.</summary>
        static GameObject Grandstand()
        {
            var stand = new GameObject("Grandstand");
            var seatTeal = Mat("SeatTeal", new Color(0.1f, 0.68f, 0.62f), 0.35f);
            var roof = Mat("RoofSail", new Color(1f, 0.96f, 0.88f), 0.2f);
            const float x0 = 10f, length = 36f, z0 = 24.5f, row = 1.4f, rise = 0.7f;
            for (int i = 0; i < 3; i++)
            {
                float top = (i + 1) * rise;
                Box($"Row {i + 1}", stand.transform, new Vector3(x0 + length / 2, top / 2, z0 + i * row), new Vector3(length, top, row), i % 2 == 0 ? seatTeal : mats.sign);
            }
            float back = z0 + 3 * row - row / 2;
            Box("Back Rail", stand.transform, new Vector3(x0 + length / 2, 3 * rise + 0.5f, back), new Vector3(length, 0.12f, 0.12f), mats.coping);
            for (int i = 0; i <= 4; i++)
            {
                float x = x0 + i * length / 4;
                Box("Roof Post", stand.transform, new Vector3(x, 2.4f, back + 0.2f), new Vector3(0.2f, 4.8f, 0.2f), mats.coping);
            }
            var sail = Box("Shade Roof", stand.transform, new Vector3(x0 + length / 2, 4.6f, z0 + row), new Vector3(length + 1f, 0.1f, 4.4f), roof);
            sail.transform.rotation = Quaternion.Euler(-10f, 0, 0);

            var random = new System.Random(7);
            for (int r = 0; r < 3; r++)
            for (int i = 0; i < 9; i++)
            {
                if (random.NextDouble() < 0.25) continue; // a few empty seats
                var fan = new GameObject("Spectator");
                fan.transform.SetParent(stand.transform, false);
                float seatTop = (r + 1) * rise;
                fan.transform.SetPositionAndRotation(new Vector3(x0 + 2 + i * 4f + (float)random.NextDouble(), seatTop - 0.5f, z0 + r * row + 0.1f),
                    Quaternion.Euler(0, 180f, 0));
                BuildCharacter(fan.transform);
                fan.AddComponent<Spectator>();
            }
            return stand;
        }

        /// <summary>
        /// Walls, floor, sloped beach, coping and blockers for a pool of the given size, from x = 0 (machine end) and
        /// z = southZ northward. Anything smaller than full size gets deck over the rest of the full footprint.
        /// </summary>
        static void BuildBasin(Transform parent, float southZ, float length, float width, float depth, float thickness, float waterLevel)
        {
            float halfW = width / 2f, zc = southZ + halfW, wallY = -depth / 2f;
            TiledBox("Wall North", parent, new Vector3(length / 2, wallY, zc + halfW - 0.05f), new Vector3(length, depth, 0.1f), mats.tile);
            TiledBox("Wall South", parent, new Vector3(length / 2, wallY, zc - halfW + 0.05f), new Vector3(length, depth, 0.1f), mats.tile);
            TiledBox("Wall Machine", parent, new Vector3(0.05f, wallY, zc), new Vector3(0.1f, depth, width), mats.tile);
            TiledBox("Floor", parent, new Vector3(length / 2, -depth - 0.5f, zc), new Vector3(length, 1, width), mats.tile);

            // A band of dark tiles along the waterline, like real pools have.
            const float band = 0.4f;
            float bandY = waterLevel + 0.08f;
            var bandMat = PoolBandMaterial();
            foreach (var b in new[]
                     {
                         TiledBox("Waterline North", parent, new Vector3(length / 2, bandY, zc + halfW - 0.11f), new Vector3(length, band, 0.02f), bandMat),
                         TiledBox("Waterline South", parent, new Vector3(length / 2, bandY, zc - halfW + 0.11f), new Vector3(length, band, 0.02f), bandMat),
                         TiledBox("Waterline Machine", parent, new Vector3(0.11f, bandY, zc), new Vector3(0.02f, band, width), bandMat),
                     })
                Object.DestroyImmediate(b.GetComponent<Collider>());

            // Marker buoys bobbing on the water: coral at the lineup, yellow at the beginners' inside line.
            foreach (var (x, color) in new[] { (12f, new Color(1f, 0.42f, 0.34f)), (length * 0.45f, new Color(1f, 0.78f, 0.25f)) })
            {
                if (x > length - 6f) continue;
                AddBuoy(parent, new Vector3(x, waterLevel, southZ + 0.9f), color);
                AddBuoy(parent, new Vector3(x, waterLevel, southZ + width - 0.9f), color);
            }

            float rampStart = length * 0.75f;
            float run = length - rampStart;
            float rampLength = Mathf.Sqrt(run * run + depth * depth);
            float rampAngle = Mathf.Atan2(depth, run) * Mathf.Rad2Deg;
            var ramp = Box("Beach", parent, new Vector3(rampStart + run / 2, -depth / 2 - 0.15f, zc), new Vector3(rampLength, 0.3f, width), mats.sand);
            ramp.transform.rotation = Quaternion.Euler(0, 0, rampAngle);

            Box("Coping North", parent, new Vector3(length / 2, 0.06f, zc + halfW + 0.25f), new Vector3(length + 1f, 0.12f, 0.5f), mats.coping);
            Box("Coping South", parent, new Vector3(length / 2, 0.06f, zc - halfW - 0.25f), new Vector3(length + 1f, 0.12f, 0.5f), mats.coping);
            Box("Coping Machine", parent, new Vector3(-0.25f, 0.06f, zc), new Vector3(0.5f, 0.12f, width), mats.coping);

            Blocker("Blocker North", parent, new Vector3(length / 2, 0.75f, zc + halfW + 0.1f), new Vector3(length, 1.5f, 0.2f));
            Blocker("Blocker South", parent, new Vector3(length / 2, 0.75f, zc - halfW - 0.1f), new Vector3(length, 1.5f, 0.2f));
            Blocker("Blocker Machine", parent, new Vector3(-0.1f, 0.75f, zc), new Vector3(0.2f, 1.5f, width));
            Blocker("Blocker Beach", parent, new Vector3(length + 0.1f, 0.75f, zc), new Vector3(0.2f, 1.5f, width));

            // Deck over the part of the full-size footprint that isn't dug yet, with an expansion-site sign.
            float fullNorth = southZ + PoolWidth;
            if (length < PoolLength - 0.01f)
                Box("Fill Deck East", parent, new Vector3((length + PoolLength) / 2, -thickness / 2, southZ + PoolWidth / 2),
                    new Vector3(PoolLength - length, thickness, PoolWidth), mats.deck);
            if (width < PoolWidth - 0.01f)
                Box("Fill Deck North", parent, new Vector3(length / 2, -thickness / 2, (southZ + width + fullNorth) / 2),
                    new Vector3(length, thickness, PoolWidth - width), mats.deck);
            if (length < PoolLength - 0.01f || width < PoolWidth - 0.01f)
            {
                var sign = new Vector3(length * 0.5f, 0, southZ + width + 2.2f);
                if (width >= PoolWidth - 0.01f) sign = new Vector3((length + PoolLength) / 2, 0, southZ + PoolWidth / 2);
                Box("Expansion Sign Post L", parent, sign + new Vector3(-2.2f, 1.2f, 0), new Vector3(0.18f, 2.4f, 0.18f), mats.coping);
                Box("Expansion Sign Post R", parent, sign + new Vector3(2.2f, 1.2f, 0), new Vector3(0.18f, 2.4f, 0.18f), mats.coping);
                Box("Expansion Sign", parent, sign + new Vector3(0, 2.1f, 0), new Vector3(5f, 1.2f, 0.12f), mats.sign);
                Signs.Text("POOL EXPANSION", parent, sign + new Vector3(0, 2.3f, -0.07f), 0f, 0.32f, Color.white, 4.6f);
                Signs.Text("COMING SOON", parent, sign + new Vector3(0, 1.85f, -0.07f), 0f, 0.24f, new Color(1f, 0.92f, 0.7f), 4.6f);
                for (int i = 0; i < 6; i++)
                {
                    var cone = Box("Cone", parent, sign + new Vector3(-6f + i * 2.4f, 0.3f, -1.4f), new Vector3(0.35f, 0.6f, 0.35f), mats.sign);
                    Object.DestroyImmediate(cone.GetComponent<Collider>());
                }
            }
        }

        /// <summary>A box whose UVs are in meters / tileSize on every face, so a tile texture keeps square tiles.</summary>
        static GameObject TiledBox(string name, Transform parent, Vector3 center, Vector3 size, Material mat, float tileSize = 1f)
        {
            var go = Box(name, parent, center, size, mat);
            var filter = go.GetComponent<MeshFilter>();
            var mesh = Object.Instantiate(filter.sharedMesh);
            mesh.name = name + " (tiled)";
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = Vector3.Scale(vertices[i], size);
                Vector3 n = normals[i];
                Vector2 w = Mathf.Abs(n.x) > 0.5f ? new Vector2(p.z, p.y) : Mathf.Abs(n.y) > 0.5f ? new Vector2(p.x, p.z) : new Vector2(p.x, p.y);
                uv[i] = w / tileSize;
            }
            mesh.uv = uv;
            filter.sharedMesh = mesh;
            return go;
        }

        /// <summary>A floating marker buoy: a ball with a white band and a little pole and flag.</summary>
        static void AddBuoy(Transform parent, Vector3 position, Color color)
        {
            var root = new GameObject("Buoy").transform;
            root.SetParent(parent, false);
            root.position = position;
            var colorMat = Mat("Buoy" + ColorUtility.ToHtmlStringRGB(color), color, 0.5f);
            var white = Mat("BuoyWhite", new Color(0.97f, 0.96f, 0.92f), 0.5f);
            void Part(PrimitiveType type, Vector3 local, Vector3 scale, Material mat)
            {
                var go = GameObject.CreatePrimitive(type);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(root, false);
                go.transform.localPosition = local;
                go.transform.localScale = scale;
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            Part(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.55f, colorMat);
            Part(PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.57f, 0.04f, 0.57f), white);
            Part(PrimitiveType.Cylinder, new Vector3(0, 0.45f, 0), new Vector3(0.04f, 0.3f, 0.04f), white);
            Part(PrimitiveType.Cube, new Vector3(0.09f, 0.66f, 0), new Vector3(0.18f, 0.12f, 0.01f), colorMat);
            root.gameObject.AddComponent<Buoy>();
        }

        /// <summary>Waterline band tiles: deep blue, with the same tile texture as the pool.</summary>
        static Material PoolBandMaterial()
        {
            var mat = Mat("PoolBand", new Color(0.12f, 0.36f, 0.58f), 0.45f);
            mat.SetTexture("_BaseMap", PoolTileTexture());
            return mat;
        }

        /// <summary>
        /// Pool tiles: 4 x 4 small square tiles per texture (25 cm each at 1 m per repeat), soft grout lines and a
        /// slight shade difference per tile. White, so the material color tints it (fresh blue or worn green).
        /// </summary>
        internal static Texture2D PoolTileTexture()
        {
            const string path = "Assets/_Game/Materials/PoolTile.png";
            if (!System.IO.File.Exists(path))
            {
                const int size = 256, tiles = 4, cell = size / tiles;
                var rng = new System.Random(7);
                var shade = new float[tiles, tiles];
                for (int a = 0; a < tiles; a++)
                for (int b = 0; b < tiles; b++)
                    shade[a, b] = 0.93f + (float)rng.NextDouble() * 0.07f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int cx = x % cell, cy = y % cell;
                    float edge = Mathf.Min(Mathf.Min(cx, cell - 1 - cx), Mathf.Min(cy, cell - 1 - cy));
                    float grout = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge - 1.5f) / 3f));
                    float v = Mathf.Lerp(0.72f, shade[x / cell, y / cell], grout);
                    // A soft highlight toward each tile's top-left corner gives a glazed look.
                    v += 0.04f * grout * Mathf.Clamp01(1f - (cx + (cell - cy)) / (float)cell);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
                tex.Apply();
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.anisoLevel = 4;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Hides a placeholder box but keeps its collider (a model is the look).</summary>
        static void Hidden(GameObject box) => box.GetComponent<MeshRenderer>().enabled = false;

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

        internal static Material Mat(string name, Color color, float smoothness = 0.2f, Color? emission = null)
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
        /// <summary>Rain streaks falling around the player on rainy days (played by DailyEvents).</summary>
        static ParticleSystem BuildRain()
        {
            var go = new GameObject("Rain");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.2f;
            main.startSpeed = 16f;
            main.startSize = 0.03f;
            main.startColor = new Color(0.8f, 0.88f, 1f, 0.55f);
            main.maxParticles = 4000;
            var emission = ps.emission;
            emission.rateOverTime = 2200f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, 44f, 0.5f); // flat box after the rotation below
            shape.rotation = new Vector3(90f, 0f, 0f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 1f;
            const string path = "Assets/_Game/Materials/Rain.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(0.85f, 0.9f, 1f, 0.5f));
            mat.SetFloat("_Surface", 1f); // transparent
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(mat);
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        /// <summary>Puffs of dark smoke rising from a broken wave machine (played by MachineService).</summary>
        static ParticleSystem BuildSmoke()
        {
            var go = new GameObject("Breakdown Smoke");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 3.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startColor = new Color(0.25f, 0.25f, 0.27f);
            main.gravityModifier = -0.05f;
            var emission = ps.emission;
            emission.rateOverTime = 7f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.8f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.5f, 1.2f), new Keyframe(1f, 0f)));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            renderer.mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(sphere);
            renderer.sharedMaterial = Mat("Smoke", new Color(0.3f, 0.3f, 0.32f), 0f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

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
            main.maxParticles = 2500;
            main.startColor = Color.white;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f); // every puff turned differently
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            // Puffs swell as they fly and fade out, like spray turning to mist.
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1.6f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1.15f), new Keyframe(1f, 1.5f)));
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.96f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.85f, 0.45f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = SprayMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            return ps;
        }

        /// <summary>Soft, alpha-blended spray puff (URP unlit particles), fading where it touches the water.</summary>
        static Material SprayMaterial()
        {
            const string path = "Assets/_Game/Materials/SprayPuff.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", SprayTexture());
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            // Soft particles: puffs fade where they meet the water instead of showing a hard cut.
            mat.SetFloat("_SoftParticlesEnabled", 1f);
            mat.SetFloat("_SoftParticlesNearFadeDistance", 0f);
            mat.SetFloat("_SoftParticlesFarFadeDistance", 0.6f);
            mat.SetVector("_SoftParticleFadeParams", new Vector4(0f, 1f / 0.6f, 0f, 0f));
            mat.EnableKeyword("_SOFTPARTICLES_ON");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>A cartoon cloud puff: a few overlapping soft circles, white on top, a cool shade underneath.</summary>
        static Texture2D SprayTexture()
        {
            const string path = "Assets/_Game/Materials/SprayPuff.png";
            if (!System.IO.File.Exists(path))
            {
                const int size = 128;
                var blobs = new[] { new Vector3(0.5f, 0.5f, 0.3f), new Vector3(0.34f, 0.44f, 0.2f), new Vector3(0.66f, 0.45f, 0.21f),
                                    new Vector3(0.45f, 0.64f, 0.2f), new Vector3(0.6f, 0.62f, 0.18f) };
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float a = 0f;
                    foreach (var b in blobs)
                    {
                        float d = Vector2.Distance(p, new Vector2(b.x, b.y)) / b.z;
                        a = Mathf.Max(a, Mathf.Clamp01(1f - d));
                    }
                    a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(a * 2.2f));
                    float shade = Mathf.Lerp(0.9f, 1f, Mathf.Clamp01(p.y * 1.4f - 0.1f)); // lighter on top
                    tex.SetPixel(x, y, new Color(shade * 0.97f, shade * 0.99f, Mathf.Min(1f, shade * 1.03f), a));
                }
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
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
        /// Assembles a low-poly person from the Blender character parts (tools/blender/build_characters.py) under
        /// root: Model > Hips > (Shorts, Torso > (Head > hair styles, Shoulder > upper arm, Elbow > forearm),
        /// Hip > thigh, Knee > shin). The CharacterRig animates the joints and paints parts by material name.
        /// </summary>
        internal static CharacterRig BuildCharacter(Transform root)
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
            // Hair styles, hats and glasses (CharacterLooks indexes them in this order), facial hair.
            var hair = new[] { "CharHairShort", "CharHairLong", "CharHairBun", "CharHairCurly", "CharHairBuzz", "CharHairMohawk",
                "CharHairDreads", "CharHairPonytail", "CharHairMullet", "CharHairMop", "CharHairSpiky" };
            var hairRenderers = new Renderer[hair.Length];
            for (int i = 0; i < hair.Length; i++) hairRenderers[i] = Part(hair[i], head);
            var hats = new[] { "CharHatCap", "CharHatBucket", "CharHatVisor", "CharHatBand" };
            var hatRenderers = new Renderer[hats.Length];
            for (int i = 0; i < hats.Length; i++) hatRenderers[i] = Part(hats[i], head);
            var glasses = new[] { "CharGlassesSunnies", "CharGlassesShield", "CharGlassesRound" };
            var glassesRenderers = new Renderer[glasses.Length];
            for (int i = 0; i < glasses.Length; i++) glassesRenderers[i] = Part(glasses[i], head);
            var beard = Part("CharBeard", head);
            var mustache = Part("CharMustache", head);
            // Face parts on their own joints so the rig can blink and change expressions.
            Transform FacePart(string model, string name, Vector3 at)
            {
                var joint = Joint(name, head, at);
                Part(model, joint);
                return joint;
            }
            var eyeL = FacePart("CharEye", "Eye L", new Vector3(-0.078f, 0.235f, 0.19f));
            var eyeR = FacePart("CharEye", "Eye R", new Vector3(0.078f, 0.235f, 0.19f));
            var browL = FacePart("CharBrow", "Brow L", new Vector3(-0.08f, 0.305f, 0.186f));
            var browR = FacePart("CharBrow", "Brow R", new Vector3(0.08f, 0.305f, 0.186f));
            var mouthNames = new[] { "CharMouthSmile", "CharMouthGrin", "CharMouthO", "CharMouthFlat" };
            var mouthObjects = new Object[mouthNames.Length];
            for (int i = 0; i < mouthNames.Length; i++)
                mouthObjects[i] = FacePart(mouthNames[i], mouthNames[i].Replace("Char", ""), new Vector3(0f, 0.125f, 0.172f)).gameObject;
            var shoulderL = Joint("Shoulder L", torso, new Vector3(-0.27f, 0.5f, 0));
            var shoulderR = Joint("Shoulder R", torso, new Vector3(0.27f, 0.5f, 0));
            var upperArmL = Part("CharUpperArm", shoulderL);
            var upperArmR = Part("CharUpperArm", shoulderR);
            var elbowL = Joint("Elbow L", shoulderL, new Vector3(0, -0.29f, 0));
            var elbowR = Joint("Elbow R", shoulderR, new Vector3(0, -0.29f, 0));
            var forearmL = Part("CharForearm", elbowL);
            var forearmR = Part("CharForearm", elbowR);
            var hipL = Joint("Hip L", hips, new Vector3(-0.1f, 0, 0));
            var hipR = Joint("Hip R", hips, new Vector3(0.1f, 0, 0));
            var thighL = Part("CharThigh", hipL);
            var thighR = Part("CharThigh", hipR);
            var kneeL = Joint("Knee L", hipL, new Vector3(0, -0.41f, 0));
            var kneeR = Joint("Knee R", hipR, new Vector3(0, -0.41f, 0));
            var shinL = Part("CharShin", kneeL);
            var shinR = Part("CharShin", kneeR);

            var rig = root.gameObject.AddComponent<CharacterRig>();
            Set(rig, "model", model);
            Set(rig, "hips", hips);
            Set(rig, "torso", torso);
            Set(rig, "head", head);
            Set(rig, "shoulderL", shoulderL);
            Set(rig, "shoulderR", shoulderR);
            Set(rig, "hipL", hipL);
            Set(rig, "hipR", hipR);
            Set(rig, "elbowL", elbowL);
            Set(rig, "elbowR", elbowR);
            Set(rig, "kneeL", kneeL);
            Set(rig, "kneeR", kneeR);
            SetArray(rig, "skinRenderers", headSkin, upperArmL, upperArmR, forearmL, forearmR, thighL, thighR, shinL, shinR);
            Set(rig, "shirtRenderer", shirt);
            Set(rig, "shortsRenderer", shorts);
            SetArray(rig, "hairRenderers", hairRenderers);
            SetArray(rig, "hatRenderers", hatRenderers);
            SetArray(rig, "glassesRenderers", glassesRenderers);
            Set(rig, "beardRenderer", beard);
            Set(rig, "mustacheRenderer", mustache);
            Set(rig, "eyeL", eyeL);
            Set(rig, "eyeR", eyeR);
            Set(rig, "browL", browL);
            Set(rig, "browR", browR);
            SetArray(rig, "mouths", mouthObjects);
            SetArray(rig, "patterns", PatternTextures());
            return rig;
        }

        static Object[] PatternTextures()
        {
            string[] names = { "stripes", "hibiscus", "checker", "palms", "dots" };
            var textures = new Object[names.Length];
            for (int i = 0; i < names.Length; i++)
                textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Game/Textures/Patterns/pattern_{names[i]}.png");
            return textures;
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
        /// <summary>A beach guest: a character with no board who walks to a seat and relaxes.</summary>
        static BeachGuest BuildGuestPrefab()
        {
            const string path = "Assets/_Game/Prefabs/BeachGuest.prefab";
            var root = new GameObject("Beach Guest");
            BuildCharacter(root.transform);
            var guest = root.AddComponent<BeachGuest>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<BeachGuest>();
        }

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
            SetArray(surfer, "boardModels",
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/BoardFoamie.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/BoardFun.fbx"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/BoardShort.fbx"));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Surfer>();
        }
    }
}
