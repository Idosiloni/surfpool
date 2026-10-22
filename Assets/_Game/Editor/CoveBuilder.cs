using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    public static partial class GreyboxBuilder
    {
        // The Cove (like a Wavegarden Cove): the lagoon's north-east zone, across a pier from the Reef. A pier of wave
        // modules runs north-south down its middle; waves run away from it on both sides and peel from the north end to
        // the south: rights on the east side, lefts on the west side (their beach is the sandbar), from one button. Each
        // side is its own PoolArea (own lineup), built by BuildPool with no back deck; the west side is turned 180 degrees.
        internal const float CoveLength = 33f, CoveWidth = 50f, CovePierHalf = 4f;
        internal const float CoveX = SandbarEast + CoveLength + CovePierHalf, CoveZ = MainSouth + ReefWidth + Pier + CoveWidth / 2f;
        /// <summary>East beach of the Cove's rights, and the walk past it to the Ranch.</summary>
        internal const float CoveEastBeach = CoveX + CovePierHalf + CoveLength, CoveEastDeck = 8f;

        static (PoolBuild east, PoolBuild west) BuildCove()
        {
            // Decks in each half's own frame: the pier to the Reef on the south (world), 16 m to the north; the east half's
            // beach end has a walk past it, the west half's beach end is the sandbar.
            PoolSpec Side(string name, float x0, float yaw, float southDeck, float northDeck, float eastDeck, Vector3[] toPool, Vector3[] outPath) => new PoolSpec
            {
                name = name, display = name, upgradeId = "pool_cove", z = CoveZ, open = false, depth = PoolDepth,
                waterLevel = WaterLevel, size = 1.1f, peel = 1.7f, price = 4f, water = 50f,
                length = CoveLength, width = CoveWidth, travel = 0.45f, peakFromWall = 3f, gridDensity = 2.5f,
                x0 = x0, yaw = yaw, southDeck = southDeck, northDeck = northDeck, eastDeck = eastDeck,
                backDeck = 0f, machineDepth = CovePierHalf, lotBack = CovePierHalf, airWall = false,
                pathToPool = toPool, pathOut = outPath,
                beachCorner = new Vector2(6, 12), look = PoolLook.Cove,
            };
            // Surfers walk east along the main pool's south deck, up the sandbar, then along the pier to their side.
            float pierWalk = CoveZ - CoveWidth / 2f - 2.5f, edge = CoveZ - CoveWidth / 2f - 0.6f;
            Vector3 a = new(40, 0, SpineZ), b = new(InLane, 0, SpineZ), c = new(InLane, 0, pierWalk);
            float eastIn = CoveX + CovePierHalf + 12f, westIn = CoveX - CovePierHalf - 12f;
            var east = BuildPool(Side("The Cove (rights)", CoveX + CovePierHalf, 0f, Pier, 16f, CoveEastDeck,
                new[] { a, b, c, new Vector3(eastIn, 0, c.z), new Vector3(eastIn, 0, edge) },
                // Out over the east beach, south past the Reef's machine, and west along the south deck to the exit.
                new[] { new Vector3(CoveEastBeach + 3f, 0, CoveZ - 6f), new Vector3(CoveEastBeach + 4f, 0, SpineZ - 3f), new Vector3(OutLane, 0, SpineZ - 3f), new Vector3(OutLane, 0, -30), new Vector3(45, 0, -40) }));
            var west = BuildPool(Side("The Cove (lefts)", CoveX - CovePierHalf, 180f, 16f, Pier, HalfSandbar,
                new[] { a, b, c, new Vector3(westIn, 0, c.z), new Vector3(westIn, 0, edge) },
                new[] { new Vector3(SandbarEast - 3f, 0, CoveZ + 6f), new Vector3(OutLane, 0, CoveZ), new Vector3(OutLane, 0, -30), new Vector3(45, 0, -40) }));

            // One console runs both sides: the east machine leads, the west one fires with it.
            SetEnum(east.machine, "fixedDirection", (int)WaveDirection.Right);
            SetEnum(west.machine, "fixedDirection", (int)WaveDirection.Left);
            Set(east.machine, "twin", west.machine);
            Set(west.machine, "leader", east.machine);
            var westConsole = west.built.Find("Wave Console");
            if (westConsole != null) Object.DestroyImmediate(westConsole.gameObject);

            TakeoffLineup(east, CoveWidth / 2f - 3f - 6f, 6f);
            TakeoffLineup(west, -(CoveWidth / 2f - 3f - 6f), 6f);
            BuildCovePier(east, west);
            return (east, west);
        }

        /// <summary>Moves a one-way pool's lineup (where you jump in) to the takeoff end: localZ along it, as far out
        /// as an Intermediate wave's crest is when the break reaches there.</summary>
        static void TakeoffLineup(PoolBuild pool, float localZ, float along)
        {
            var lineup = pool.area.Lineup;
            float x = pool.water.TakeoffX(WaveLevel.Intermediate, along) + 1.5f;
            lineup.position = pool.water.transform.TransformPoint(new Vector3(x, 0f, localZ));
        }

        static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The pier down the middle: a deck between the two basins and a row of wave modules on it.</summary>
        static void BuildCovePier(PoolBuild east, PoolBuild west)
        {
            var pier = new GameObject("Cove Pier").transform;
            pier.SetParent(east.built, true); // shown when the Cove is built
            pier.position = new Vector3(CoveX, 0f, CoveZ);
            const float thickness = PoolDepth + 1f;
            // Runs past the water to the ends of the side decks (they stop at the pier's faces): the pier to the Reef on the
            // south, 16 m on the north.
            Box("Pier Deck", pier, new Vector3(CoveX, -thickness / 2f, CoveZ + (16f - Pier) / 2f), new Vector3(CovePierHalf * 2f, thickness, CoveWidth + Pier + 16f), mats.deck);

            var housing = Mat("CoveModule", new Color(0.12f, 0.24f, 0.32f), 0.35f);
            var stripe = Mat("CoveStripe", new Color(0.08f, 0.72f, 0.65f), 0.4f);
            var paddle = Mat("CovePaddle", new Color(1f, 0.78f, 0.25f), 0.45f);
            var lampOff = Mat("CoveLampOff", new Color(0.3f, 0.36f, 0.4f), 0.5f);
            var lampOn = Mat("CoveLampOn", new Color(0.4f, 1f, 0.85f), 0.6f, new Color(0.6f, 3f, 2.4f));

            const int modules = 16;
            float spacing = CoveWidth / modules;
            var eastPaddles = new Object[modules];
            var westPaddles = new Object[modules];
            var lamps = new Object[modules];
            for (int i = 0; i < modules; i++)
            {
                // North to south: the order they fire in.
                float z = CoveZ + CoveWidth / 2f - spacing * (i + 0.5f);
                var module = Box($"Module {i + 1}", pier, new Vector3(CoveX, 0.7f, z), new Vector3(CovePierHalf * 2f - 0.6f, 1.4f, spacing - 0.7f), housing);
                Object.DestroyImmediate(module.GetComponent<Collider>());
                var band = Box("Stripe", pier, new Vector3(CoveX, 1.1f, z), new Vector3(CovePierHalf * 2f - 0.5f, 0.18f, spacing - 0.6f), stripe);
                Object.DestroyImmediate(band.GetComponent<Collider>());
                var lamp = Box("Lamp", pier, new Vector3(CoveX, 1.55f, z), new Vector3(0.35f, 0.3f, 0.35f), lampOff);
                Object.DestroyImmediate(lamp.GetComponent<Collider>());
                lamps[i] = lamp.GetComponent<MeshRenderer>();

                foreach (int side in new[] { 1, -1 })
                {
                    // A paddle hangs from a hinge at the top of the pier wall and kicks out into the water.
                    var hinge = new GameObject(side > 0 ? "Paddle Hinge East" : "Paddle Hinge West").transform;
                    hinge.SetParent(pier, false);
                    hinge.position = new Vector3(CoveX + side * (CovePierHalf + 0.05f), -0.5f, z);
                    var plate = Box("Paddle", hinge, hinge.position + new Vector3(side * 0.2f, -1.5f, 0f), new Vector3(0.3f, 2.8f, spacing - 1f), paddle);
                    Object.DestroyImmediate(plate.GetComponent<Collider>());
                    if (side > 0) eastPaddles[i] = hinge;
                    else westPaddles[i] = hinge;
                }
            }
            Signs.Text("THE COVE", pier, new Vector3(CoveX, 2.6f, CoveZ - CoveWidth / 2f - 0.2f), 0f, 0.8f, Color.white, 7f);

            var spray = BuildSpray(mats.foam);
            spray.transform.SetParent(pier, true);
            var rig = pier.gameObject.AddComponent<CovePier>();
            Set(rig, "eastMachine", east.machine);
            Set(rig, "westMachine", west.machine);
            SetArray(rig, "eastPaddles", eastPaddles);
            SetArray(rig, "westPaddles", westPaddles);
            SetArray(rig, "lamps", lamps);
            Set(rig, "lampOff", lampOff);
            Set(rig, "lampOn", lampOn);
            Set(rig, "spray", spray);
        }
    }
}
