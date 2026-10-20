using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// One wave pool in the park: its water, wave machine and the route surfers take into it and out of it.
    /// The first pool is open from the start; others are built (opened) from the upgrade computer.
    /// </summary>
    public class PoolArea : MonoBehaviour
    {
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

        public string DisplayName => displayName;
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

        void Awake()
        {
            IsOpen = openAtStart;
            if (buildVisuals != null) buildVisuals.SetActive(IsOpen);
            if (lotVisuals != null) lotVisuals.SetActive(!IsOpen);
        }

        void OnEnable() => PoolRegistry.All.Add(this);
        void OnDisable() => PoolRegistry.All.Remove(this);

        public void Open()
        {
            IsOpen = true;
            if (buildVisuals != null) buildVisuals.SetActive(true);
            if (lotVisuals != null) lotVisuals.SetActive(false);
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
