using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Sends surfers into the park, runs the line at the ticket desk, counts happy and lost customers,
    /// and holds the route surfers walk. Move the route points in the scene to change it.
    /// </summary>
    public class SurferSpawner : MonoBehaviour
    {
        [SerializeField] Surfer surferPrefab;
        [SerializeField] WaveWater water;
        [SerializeField] PriceBoard pricing;

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

        [Header("Spawning")]
        [Tooltip("Most surfers in the park at once.")]
        [SerializeField] int maxSurfers = 2;
        [Tooltip("Seconds between new surfers arriving.")]
        [SerializeField] float spawnDelay = 6f;

        readonly List<Surfer> queue = new();
        float nextSpawnTime;

        public WaveWater Water => water;
        public PriceBoard Pricing => pricing;
        public Vector3[] PathIn => Points(pathIn);
        public Vector3[] PathBackOut => Points(pathIn).Reverse().ToArray();
        public Vector3[] PathToPool => Points(pathToPool);
        public Vector3[] PathOut => Points(pathOut);
        public Vector3 Lineup => lineup.position;
        public Vector3 BeachLanding => beachLanding.position;
        public Vector3 DeskSpot => deskSpot.position;

        /// <summary>Direction a surfer in line faces: toward the register.</summary>
        public Vector3 DeskFacing => (deskSpot.position - pathIn[pathIn.Length - 1].position).normalized;

        public int Served { get; private set; }
        public int Lost { get; private set; }

        public int MaxSurfers
        {
            get => maxSurfers;
            set => maxSurfers = Mathf.Max(1, value);
        }

        /// <summary>The surfer at the front of the line, if they're ready to pay.</summary>
        public Surfer NextCustomer => queue.Count > 0 && queue[0].WaitingToPay ? queue[0] : null;

        void Start()
        {
            nextSpawnTime = Time.time + 1f;
        }

        void Update()
        {
            if (Surfer.All.Count >= maxSurfers || Time.time < nextSpawnTime) return;
            var surfer = Instantiate(surferPrefab, pathIn[0].position, pathIn[0].rotation);
            surfer.Init(this);
            nextSpawnTime = Time.time + spawnDelay;
        }

        public void JoinQueue(Surfer surfer)
        {
            if (!queue.Contains(surfer)) queue.Add(surfer);
        }

        public void LeaveQueue(Surfer surfer) => queue.Remove(surfer);

        public int QueueIndex(Surfer surfer) => queue.IndexOf(surfer);

        /// <summary>Where this surfer should stand in the desk line.</summary>
        public Vector3 QueueSlot(Surfer surfer)
        {
            int index = Mathf.Max(0, queue.IndexOf(surfer));
            Vector3 back = (pathIn[pathIn.Length - 1].position - deskSpot.position).normalized;
            return deskSpot.position + back * (queueSpacing * index);
        }

        public void ReportServed() => Served++;
        public void ReportLost() => Lost++;

        static Vector3[] Points(Transform[] transforms) => transforms.Select(t => t.position).ToArray();
    }
}
