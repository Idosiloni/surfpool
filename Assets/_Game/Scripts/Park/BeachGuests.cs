using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Sends beach guests to the park once Beach Passes are bought: whenever there's a free lounger or bench seat,
    /// a guest arrives every so often. The beach pass price grows with park beauty (up to 3x).
    /// </summary>
    public class BeachGuests : MonoBehaviour
    {
        [SerializeField] BeachGuest guestPrefab;
        [SerializeField] SurferSpawner spawner;
        [SerializeField] int basePass = 10;
        [Tooltip("Seconds between arrivals while seats are free.")]
        [SerializeField] Vector2 arrivalGap = new(8f, 18f);
        [Tooltip("How long a guest relaxes (seconds).")]
        [SerializeField] Vector2 relaxTime = new(70f, 140f);

        float nextArrival;

        public int PassPrice => Mathf.RoundToInt(basePass * Mathf.Min(3f, 1f + Decoration.Beauty / 40f));

        void Update()
        {
            if (!ParkEffects.BeachPassesOpen || !DayCycle.Open || Time.time < nextArrival) return;
            nextArrival = Time.time + Random.Range(arrivalGap.x, arrivalGap.y) / DailyEvents.ArrivalMultiplier;
            if (Decoration.FreeSeats <= 0) return;
            var guest = Instantiate(guestPrefab);
            guest.Begin(spawner.Entrance, PassPrice, Random.Range(relaxTime.x, relaxTime.y));
        }
    }
}
