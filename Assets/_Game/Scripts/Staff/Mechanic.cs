using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired mechanic: keeps every open pool's wave machine running, servicing worn ones and fixing broken ones.</summary>
    public class Mechanic : MonoBehaviour
    {
        [Tooltip("Seconds the mechanic needs to get to a machine and fix it.")]
        [SerializeField] float fixTime = 8f;
        [Tooltip("Services machines once their wear passes this.")]
        [Range(0f, 1f)] [SerializeField] float serviceAt = 0.6f;

        MachineService job;
        float doneAt;

        void Update()
        {
            if (job == null)
            {
                foreach (var pool in PoolRegistry.All)
                {
                    if (!pool.IsOpen || pool.Machine == null) continue;
                    var service = pool.Machine.Service;
                    if (service != null && (service.Broken || service.Wear >= serviceAt))
                    {
                        job = service;
                        doneAt = Time.time + fixTime * StaffMember.TimeFactor(gameObject);
                        break;
                    }
                }
                return;
            }
            if (Time.time < doneAt) return;
            job.Repair();
            StaffMember.RecordWork(gameObject);
            job = null;
        }
    }
}
