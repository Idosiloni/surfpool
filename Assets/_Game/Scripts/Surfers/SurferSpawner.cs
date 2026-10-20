using System.Linq;
using UnityEngine;

namespace SurfPool
{
    /// <summary>Sends surfers into the park and holds the route they walk. Move the route points in the scene to change it.</summary>
    public class SurferSpawner : MonoBehaviour
    {
        [SerializeField] Surfer surferPrefab;
        [SerializeField] WaveWater water;

        [Header("Route")]
        [Tooltip("From the entrance to the spot in front of the ticket desk. The first point is where surfers appear.")]
        [SerializeField] Transform[] pathToDesk;
        [Tooltip("From the desk to the pool edge where they jump in.")]
        [SerializeField] Transform[] pathToPool;
        [Tooltip("Where surfers wait in the water for a wave.")]
        [SerializeField] Transform lineup;
        [Tooltip("Where surfers land on the beach after a ride.")]
        [SerializeField] Transform beachLanding;
        [Tooltip("From the beach to the exit, where they disappear.")]
        [SerializeField] Transform[] pathOut;

        [Header("Spawning")]
        [SerializeField] int maxSurfers = 1;
        [Tooltip("Seconds after a surfer leaves before the next one arrives.")]
        [SerializeField] float spawnDelay = 3f;

        float nextSpawnTime;

        public WaveWater Water => water;
        public Vector3[] PathToDesk => Points(pathToDesk);
        public Vector3[] PathToPool => Points(pathToPool);
        public Vector3[] PathOut => Points(pathOut);
        public Vector3 Lineup => lineup.position;
        public Vector3 BeachLanding => beachLanding.position;

        /// <summary>The surfer waiting longest at the desk, or null.</summary>
        public Surfer NextCustomer => Surfer.All.FirstOrDefault(s => s.WaitingToPay);

        void Start()
        {
            nextSpawnTime = Time.time + 1f;
        }

        void Update()
        {
            if (Surfer.All.Count >= maxSurfers)
            {
                nextSpawnTime = Time.time + spawnDelay;
                return;
            }
            if (Time.time < nextSpawnTime) return;

            var surfer = Instantiate(surferPrefab, pathToDesk[0].position, pathToDesk[0].rotation);
            surfer.Init(this);
            nextSpawnTime = Time.time + spawnDelay;
        }

        static Vector3[] Points(Transform[] transforms) => transforms.Select(t => t.position).ToArray();
    }
}
