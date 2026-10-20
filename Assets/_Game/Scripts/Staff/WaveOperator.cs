using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired wave operator: keeps an eye on every open pool and sends the wave its waiting surfers want.</summary>
    public class WaveOperator : MonoBehaviour
    {
        [Tooltip("Seconds between checks of the lineups.")]
        [SerializeField] float checkInterval = 1f;

        float nextCheck;

        void Update()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + checkInterval * StaffMember.TimeFactor(gameObject);
            foreach (var pool in PoolRegistry.All)
                if (pool.IsOpen && pool.Machine != null && pool.Machine.SendForWaiting()) StaffMember.RecordWork(gameObject);
        }
    }
}
