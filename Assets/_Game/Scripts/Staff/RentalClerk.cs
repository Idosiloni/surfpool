using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired rental clerk: hands a board from the rack to the renter at the front of the rental line.</summary>
    public class RentalClerk : MonoBehaviour
    {
        [SerializeField] SurferSpawner spawner;
        [SerializeField] RentalRack rack;
        [Tooltip("Seconds the clerk takes to serve a renter.")]
        [SerializeField] float serveTime = 2.5f;

        Surfer serving;
        float serveDoneAt;

        void Update()
        {
            var renter = spawner.NextRenter;
            if (renter != serving)
            {
                serving = renter;
                serveDoneAt = Time.time + serveTime;
            }
            if (serving == null || Time.time < serveDoneAt || rack.Count == 0) return;
            if (rack.TryTakeBoard()) serving.GiveBoard();
            serving = null;
        }
    }
}
