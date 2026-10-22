using UnityEngine;

namespace SurfPool.EditorTools
{
    public static partial class GreyboxBuilder
    {
        // The Surf Ranch (like Kelly Slater's): the lagoon's last stage, one very long, narrow strip along the east side,
        // past the Cove's east beach. A foil on a rail along its west side makes one long right per pass: the wave
        // crawls slowly away from the rail while the break races 150 m down the line (~22 s rides). Few surfers, very
        // high price. Same wave code as every pool: a very low travel speed and a fast peel (docs/research/long-rides.md).
        internal const float RanchLength = 45f, RanchWidth = 150f, RanchRail = 6f, RanchX = CoveEastBeach + CoveEastDeck + RanchRail;
        internal const float RanchZ = MainSouth + RanchWidth / 2f;

        /// <summary>The Ranch's water (basin) on the ground, for the island's sand (its decks cover the rest).</summary>
        internal static readonly Rect RanchBasin = Rect.MinMaxRect(RanchX, RanchZ - RanchWidth / 2f, RanchX + RanchLength, RanchZ + RanchWidth / 2f);

        static PoolBuild BuildRanch()
        {
            float south = RanchZ - RanchWidth / 2f;
            var ranch = BuildPool(new PoolSpec
            {
                name = "Surf Ranch", display = "Surf Ranch", upgradeId = "pool_ranch", z = RanchZ, open = false,
                depth = PoolDepth + 2f, waterLevel = WaterLevel - 1.5f, size = 1.15f, peel = 2.2f, price = 10f, water = 150f,
                length = RanchLength, width = RanchWidth, travel = 0.2f, peakFromWall = 4f, gridDensity = 2f,
                x0 = RanchX, machineDepth = RanchRail, backDeck = RanchRail, airWall = false,
                beachCorner = new Vector2(7, 18), look = PoolLook.Ranch,
                // East along the south decks (past the Reef's machine) to the Ranch's south end.
                pathToPool = new[] { new Vector3(40, 0, SpineZ), new Vector3(RanchX + 14f, 0, SpineZ), new Vector3(RanchX + 14f, 0, south - 0.6f) },
                // Off the east beach, back south and west along the south decks to the exit.
                pathOut = new[]
                {
                    new Vector3(RanchX + RanchLength + 3f, 0, RanchZ - 6f), new Vector3(RanchX + RanchLength + 4f, 0, SpineZ - 3f),
                    new Vector3(OutLane, 0, SpineZ - 3f), new Vector3(OutLane, 0, -30), new Vector3(45, 0, -40),
                },
            });

            // One long wave per pass of the foil (no sets), and the water needs time to settle between passes.
            SetEnum(ranch.machine, "fixedDirection", (int)WaveDirection.Right);
            SetFloat(ranch.machine, "minCooldown", 20f);
            SetInt(ranch.machine, "maxSetSize", 1);
            SetBool(ranch.machine, "bigWaves", true);
            // Made by the foil right by the rail: it starts breaking almost at once, then crawls out for ~25 s.
            SetFloat(ranch.water, "headStart", 5f);
            SetFloat(ranch.water, "breakAtMultiplier", 0.4f);

            TakeoffLineup(ranch, RanchWidth / 2f - 4f - 6f, 6f);
            BuildRanchFoil(ranch);
            return ranch;
        }

        /// <summary>The rail along the west side, a winch house at each end, and the foil carriage that rides it.</summary>
        static void BuildRanchFoil(PoolBuild ranch)
        {
            var rig = new GameObject("Ranch Foil").transform;
            rig.SetParent(ranch.built, true);
            float x = RanchX - 2.5f, z0 = RanchZ - RanchWidth / 2f, z1 = RanchZ + RanchWidth / 2f;
            var steel = Mat("RanchSteel", new Color(0.55f, 0.6f, 0.64f), 0.55f);
            var navy = Mat("CoveModule", new Color(0.12f, 0.24f, 0.32f), 0.35f);
            var yellow = Mat("CovePaddle", new Color(1f, 0.78f, 0.25f), 0.45f);
            var teal = Mat("CoveStripe", new Color(0.08f, 0.72f, 0.65f), 0.4f);

            foreach (float dx in new[] { -0.7f, 0.7f })
            {
                var rail = Box("Rail", rig, new Vector3(x + dx, 0.12f, RanchZ), new Vector3(0.22f, 0.24f, RanchWidth + 8f), steel);
                Object.DestroyImmediate(rail.GetComponent<Collider>());
            }
            var drums = new Object[2];
            int d = 0;
            foreach (float z in new[] { z0 - 6f, z1 + 6f }) // on the end decks, clear of the walkway
            {
                Box("Winch House", rig, new Vector3(x - 3f, 2f, z), new Vector3(8f, 4f, 5f), navy);
                var band = Box("Winch Stripe", rig, new Vector3(x - 3f, 3.2f, z), new Vector3(8.1f, 0.4f, 5.1f), teal);
                Object.DestroyImmediate(band.GetComponent<Collider>());
                var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                drum.name = "Winch Drum";
                Object.DestroyImmediate(drum.GetComponent<Collider>());
                drum.transform.SetParent(rig, true);
                drum.transform.position = new Vector3(x + 1.6f, 1.2f, z);
                drum.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                drum.transform.localScale = new Vector3(1.6f, 0.9f, 1.6f);
                drum.GetComponent<MeshRenderer>().sharedMaterial = yellow;
                drums[d++] = drum.transform;
            }

            // The carriage: a cab on the rail with an arm reaching over the wall and the foil blade down in the water.
            var carriage = new GameObject("Foil Carriage").transform;
            carriage.SetParent(rig, true);
            carriage.position = new Vector3(x, 0f, z1 - 4f);
            void Part(string name, Vector3 local, Vector3 size, Material mat)
            {
                var go = Box(name, carriage, carriage.position + local, size, mat);
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            Part("Cab", new Vector3(-0.3f, 1.4f, 0f), new Vector3(2.6f, 2.2f, 4.5f), yellow);
            Part("Cab Window", new Vector3(-0.3f, 1.9f, 0f), new Vector3(2.65f, 0.6f, 3.6f), navy);
            Part("Arm", new Vector3(1.6f, 1.6f, 0f), new Vector3(3.4f, 0.5f, 1.2f), steel);
            Part("Strut", new Vector3(3.1f, -1.5f, 0f), new Vector3(0.4f, 6.2f, 0.8f), steel);
            Part("Foil", new Vector3(3.3f, -4.2f, 0f), new Vector3(1.4f, 1.6f, 7f), yellow);
            Signs.Text("SURF RANCH", carriage, carriage.position + new Vector3(-1.64f, 1.5f, 0f), 90f, 0.45f, Color.white, 4f);

            var wake = BuildSpray(mats.foam);
            wake.transform.SetParent(rig, true);
            var foil = rig.gameObject.AddComponent<RanchFoil>();
            Set(foil, "water", ranch.water);
            Set(foil, "carriage", carriage);
            SetArray(foil, "drums", drums);
            Set(foil, "wake", wake);
        }
    }
}
