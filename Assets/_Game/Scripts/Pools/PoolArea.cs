using System;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// One wave pool in the park: its water, wave machine and the route surfers take into it and out of it.
    /// The first pool is open from the start; others are built (opened) from the upgrade computer.
    /// A pool can also have size stages: the main pool starts small and grows with Pool Expansion upgrades
    /// (bigger waves need a bigger pool). The machine end and the south wall stay put; it grows toward the beach
    /// and to the north.
    /// </summary>
    public class PoolArea : MonoBehaviour
    {
        [Serializable]
        public class SizeStage
        {
            public string name;
            public float length, width;
            [Tooltip("Walls, floor, beach and the deck that fills the not-yet-dug part, for this size.")]
            public GameObject visuals;
        }

        [SerializeField] string displayName = "Main Pool";
        [Tooltip("Upgrade id that builds this pool (empty for the starting pool).")]
        [SerializeField] string upgradeId;
        [SerializeField] bool openAtStart = true;
        [SerializeField] WaveWater water;
        [SerializeField] WaveMachine machine;

        [Header("Surfer route")]
        [Tooltip("From the desk (or rental counter) to the edge of this pool where surfers jump in.")]
        [SerializeField] Transform[] pathToPool;
        [Tooltip("Where surfers wait in the water for a wave.")]
        [SerializeField] Transform lineup;
        [Tooltip("Where surfers land on this pool's beach after a ride.")]
        [SerializeField] Transform beachLanding;
        [Tooltip("From the beach to the park exit.")]
        [SerializeField] Transform[] pathOut;
        [Tooltip("Like Path Out, but past the board return bin (renters). Must include the bin spot.")]
        [SerializeField] Transform[] returnPathOut;
        [Tooltip("Shown when the pool is built (deck, water, machine...).")]
        [SerializeField] GameObject buildVisuals;
        [Tooltip("Shown while the pool isn't built yet (sand lot, construction sign).")]
        [SerializeField] GameObject lotVisuals;
        [Tooltip("Light on the machine house that turns on with the Auto Wave Timer.")]
        [SerializeField] GameObject autoWaveLight;

        [Header("Price and size")]
        [Tooltip("Sessions in this pool cost this many times the base price (bigger pools, bigger prices).")]
        [SerializeField] float priceMultiplier = 1f;
        [Tooltip("Size stages from smallest to biggest. Empty = the pool is always full size.")]
        [SerializeField] SizeStage[] stages;
        [Tooltip("South edge of the pool (world z). The pool grows north from here.")]
        [SerializeField] float southEdge;
        [Tooltip("Daily water bill at full size (smaller stages pay for their smaller area).")]
        [SerializeField] float dailyWater = 25f;
        [Tooltip("Shown when Solar Panels are bought (on the machine house roof).")]
        [SerializeField] GameObject solarPanels;
        [Tooltip("Shown when Water Recycling is bought (tanks next to the machine house).")]
        [SerializeField] GameObject waterTanks;
        [Tooltip("How far the machine reaches behind the pool's deep end (m): 14.5 for the air wall, less for a Cove pier.")]
        [SerializeField] float machineDepth = 14.5f;

        public string DisplayName => displayName;
        public float SouthEdge => southEdge;
        public string UpgradeId => upgradeId;
        public GameObject AutoWaveLight => autoWaveLight;
        public WaveWater Water => water;
        public WaveMachine Machine => machine;
        public bool IsOpen { get; private set; }
        public Transform[] PathToPool => pathToPool;
        public Transform Lineup => lineup;
        public Transform BeachLanding => beachLanding;
        public Transform[] PathOut => pathOut;
        public Transform[] ReturnPathOut => returnPathOut;
        public float PriceMultiplier => priceMultiplier;
        public int Stage { get; private set; }
        public int StageCount => stages?.Length ?? 0;
        public string StageName => StageCount > 0 ? stages[Stage].name : displayName;
        public GameObject SolarPanels => solarPanels;
        public GameObject WaterTanks => waterTanks;

        /// <summary>Today's water bill for this pool: the full-size bill times how much of the full area is dug.</summary>
        public float DailyWaterCost
        {
            get
            {
                if (StageCount == 0) return dailyWater;
                var full = stages[StageCount - 1];
                var now = stages[Stage];
                return dailyWater * (now.length * now.width) / (full.length * full.width);
            }
        }

        void Awake()
        {
            IsOpen = openAtStart;
            if (buildVisuals != null) buildVisuals.SetActive(IsOpen);
            if (lotVisuals != null) lotVisuals.SetActive(!IsOpen);
            if (StageCount > 0) SetStage(0);
        }

        void OnEnable() => PoolRegistry.All.Add(this);
        void OnDisable() => PoolRegistry.All.Remove(this);

        public void Open()
        {
            IsOpen = true;
            if (buildVisuals != null) buildVisuals.SetActive(true);
            if (lotVisuals != null) lotVisuals.SetActive(false);
        }

        /// <summary>Grows (or sets) the pool to a size stage: swaps the walls and deck, resizes the water and moves the beach.</summary>
        public void SetStage(int stage)
        {
            if (StageCount == 0) return;
            Stage = Mathf.Clamp(stage, 0, StageCount - 1);
            for (int i = 0; i < stages.Length; i++)
                if (stages[i].visuals != null) stages[i].visuals.SetActive(i == Stage);

            var size = stages[Stage];
            var waterPosition = water.transform.position;
            waterPosition.z = southEdge + size.width / 2f;
            water.transform.position = waterPosition;
            water.Resize(size.length, size.width);

            // The lineup sits in the middle of the new water (surfers spread out from it across the width).
            if (lineup != null) lineup.position = new Vector3(lineup.position.x, lineup.position.y, waterPosition.z);

            // Surfers land on the new beach and walk out past its end.
            var landing = beachLanding.position;
            beachLanding.position = new Vector3(size.length * 0.95f, landing.y, waterPosition.z);
            if (pathOut.Length > 0)
            {
                var exit = pathOut[0].position;
                pathOut[0].position = new Vector3(size.length + 3f, exit.y, exit.z);
            }
        }

        /// <summary>The pool's water on the ground (x, z), grown by a margin. Walkers and litter stay out of it.</summary>
        public Rect Footprint(float margin)
        {
            float length = water.Length, width = water.Width;
            return LocalRect(0f, length, -width / 2f, width / 2f, margin);
        }

        /// <summary>
        /// A rectangle in the water's own frame (x = out from the machine, z = across the pool) as a ground rectangle
        /// (x, z) in the world. Pools face any way in 90 degree steps, so it stays axis-aligned.
        /// </summary>
        Rect LocalRect(float x0, float x1, float z0, float z1, float margin)
        {
            var w = water.transform;
            Vector3 a = w.TransformPoint(new Vector3(x0, 0f, z0)), b = w.TransformPoint(new Vector3(x1, 0f, z1));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x) - margin, Mathf.Min(a.z, b.z) - margin, Mathf.Max(a.x, b.x) + margin, Mathf.Max(a.z, b.z) + margin);
        }

        /// <summary>Across-the-pool (local z) range of the pool at full size, from the fixed south wall.</summary>
        (float z0, float z1) FullAcross()
        {
            if (StageCount == 0) return (-water.Width / 2f, water.Width / 2f);
            float south = water.transform.InverseTransformPoint(new Vector3(water.transform.position.x, 0f, southEdge)).z;
            return (south, south + stages[StageCount - 1].width);
        }

        /// <summary>The pool at its biggest size (all stages), grown by a margin. Nothing gets built there.</summary>
        public Rect FullFootprint(float margin)
        {
            float length = StageCount > 0 ? stages[StageCount - 1].length : water.Length;
            var (z0, z1) = FullAcross();
            return LocalRect(0f, length, z0, z1, margin);
        }

        /// <summary>The wave machine house next to the pool (x, z), grown by a margin.</summary>
        public Rect MachineHouseFootprint(float margin)
        {
            // The open "air wall" machine: chambers along the deep end (x -6..0) and the blowers behind (to x -14).
            // A Cove's pier is narrower: just its machine depth.
            var (z0, z1) = FullAcross();
            return LocalRect(-machineDepth, -0.2f, z0, z1, margin);
        }


        /// <summary>Surfers waiting in this pool's water right now.</summary>
        public int WaitingSurfers
        {
            get
            {
                int count = 0;
                foreach (var surfer in Surfer.All)
                    if (surfer.Pool == this && surfer.WaitingForWave) count++;
                return count;
            }
        }
    }
}
