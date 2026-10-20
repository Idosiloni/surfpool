using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Sends surfers into the park, runs the lines at the ticket desk and the rental counter, counts happy and
    /// lost customers, and holds the routes surfers walk. Move the route points in the scene to change them.
    /// </summary>
    public class SurferSpawner : MonoBehaviour
    {
        [SerializeField] Surfer surferPrefab;
        [SerializeField] WaveWater water;
        [SerializeField] PriceBoard pricing;
        [SerializeField] ReturnBin returnBin;

        [Header("Route")]
        [Tooltip("From the entrance to the back of the desk line. The first point is where surfers appear.")]
        [SerializeField] Transform[] pathIn;
        [Tooltip("The spot right in front of the register. The line forms behind it, toward the last point of Path In.")]
        [SerializeField] Transform deskSpot;
        [SerializeField] float queueSpacing = 1.3f;
        [Tooltip("From the desk to the pool edge where they jump in.")]
        [SerializeField] Transform[] pathToPool;
        [Tooltip("Where surfers wait in the water for a wave.")]
        [SerializeField] Transform lineup;
        [Tooltip("Where surfers land on the beach after a ride.")]
        [SerializeField] Transform beachLanding;
        [Tooltip("From the beach to the exit, where they disappear.")]
        [SerializeField] Transform[] pathOut;

        [Header("Rental route")]
        [Tooltip("From the ticket desk to the back of the rental line.")]
        [SerializeField] Transform[] rentalPathIn;
        [Tooltip("The spot in front of the rental counter. The line forms behind it.")]
        [SerializeField] Transform rentalSpot;
        [Tooltip("From the rental counter to the pool edge.")]
        [SerializeField] Transform[] rentalPathToPool;
        [Tooltip("From the beach past the return bin to the exit. Must include the Return Bin Spot.")]
        [SerializeField] Transform[] returnPathOut;
        [Tooltip("The point on Return Path Out where renters drop their board in the bin.")]
        [SerializeField] Transform returnBinSpot;

        [Header("Spawning")]
        [Tooltip("Most surfers in the park at once.")]
        [SerializeField] int maxSurfers = 2;
        [Tooltip("Seconds between new surfers arriving.")]
        [SerializeField] float spawnDelay = 6f;
        [Tooltip("Share of new surfers who come without a board and need to rent one (0 until the rental stand is bought).")]
        [Range(0f, 1f)] [SerializeField] float renterChance;

        readonly List<Surfer> deskLine = new();
        readonly List<Surfer> rentalLine = new();
        float nextSpawnTime;

        public WaveWater Water => water;
        public PriceBoard Pricing => pricing;
        public ReturnBin ReturnBin => returnBin;
        public Vector3[] PathIn => Points(pathIn);
        public Vector3[] PathBackOut => Points(pathIn).Reverse().ToArray();
        public Vector3[] PathToPool => Points(pathToPool);
        public Vector3[] PathOut => Points(pathOut);
        public Vector3[] RentalPathIn => Points(rentalPathIn);
        public Vector3[] RentalPathToPool => Points(rentalPathToPool);
        public Vector3[] ReturnPathOut => Points(returnPathOut);
        public Vector3 ReturnBinSpot => returnBinSpot.position;
        public Vector3 Entrance => pathIn[0].position;
        public Vector3 Lineup => lineup.position;
        public Vector3 BeachLanding => beachLanding.position;

        public int Served { get; private set; }
        public int Lost { get; private set; }

        public int MaxSurfers
        {
            get => maxSurfers;
            set => maxSurfers = Mathf.Max(1, value);
        }

        public float RenterChance
        {
            get => renterChance;
            set => renterChance = Mathf.Clamp01(value);
        }

        /// <summary>Multiplies the time between arrivals. Marketing upgrades lower it.</summary>
        public float SpawnDelayMultiplier { get; set; } = 1f;

        /// <summary>Seconds between arrivals right now: shorter with a better park rating and marketing.</summary>
        public float CurrentSpawnDelay
        {
            get
            {
                float rating = ParkReputation.Instance != null ? ParkReputation.Instance.Score : 0.6f;
                return spawnDelay * Mathf.Lerp(1.8f, 0.6f, rating) * SpawnDelayMultiplier;
            }
        }

        /// <summary>The renter at the front of the rental line, if they're waiting for a board.</summary>
        public Surfer NextRenter => rentalLine.Count > 0 && rentalLine[0].WaitingForBoard ? rentalLine[0] : null;

        /// <summary>The surfer at the front of the desk line, if they're ready to pay.</summary>
        public Surfer NextCustomer => deskLine.Count > 0 && deskLine[0].WaitingToPay ? deskLine[0] : null;

        void Start()
        {
            nextSpawnTime = Time.time + 1f;
        }

        void Update()
        {
            if (Surfer.All.Count >= maxSurfers || Time.time < nextSpawnTime) return;
            var surfer = Instantiate(surferPrefab, pathIn[0].position, pathIn[0].rotation);
            surfer.Init(this, Random.value < renterChance);
            nextSpawnTime = Time.time + CurrentSpawnDelay;
        }

        public void JoinLine(Surfer surfer, bool rental)
        {
            var line = rental ? rentalLine : deskLine;
            if (!line.Contains(surfer)) line.Add(surfer);
        }

        public void LeaveLines(Surfer surfer)
        {
            deskLine.Remove(surfer);
            rentalLine.Remove(surfer);
        }

        /// <summary>Where this surfer should stand in its line, and whether that's the front.</summary>
        public (Vector3 slot, bool front, Vector3 facing) LineSlot(Surfer surfer)
        {
            bool rental = rentalLine.Contains(surfer);
            var line = rental ? rentalLine : deskLine;
            var spot = rental ? rentalSpot : deskSpot;
            var path = rental ? rentalPathIn : pathIn;
            int index = Mathf.Max(0, line.IndexOf(surfer));
            Vector3 back = (path[path.Length - 1].position - spot.position).normalized;
            return (spot.position + back * (queueSpacing * index), index == 0, -back);
        }

        public void ReportServed(float happiness)
        {
            Served++;
            if (ParkReputation.Instance != null) ParkReputation.Instance.Record(happiness);
        }

        public void ReportLost()
        {
            Lost++;
            if (ParkReputation.Instance != null) ParkReputation.Instance.Record(0f);
        }

        static Vector3[] Points(Transform[] transforms) => transforms.Select(t => t.position).ToArray();
    }
}
