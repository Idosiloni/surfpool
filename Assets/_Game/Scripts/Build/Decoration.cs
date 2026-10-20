using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A placed decoration. Together they make up the park's beauty: every point adds $0.50 to the fair price
    /// (up to +$30 at 60 beauty) and makes surfers a little happier (up to +0.1 at 100 beauty).
    /// Seats (child transforms named "Seat") are where beach guests sit or lie down.
    /// </summary>
    public class Decoration : MonoBehaviour
    {
        public static readonly List<Decoration> All = new();

        [SerializeField] string id;
        [SerializeField] int beauty;

        readonly List<Transform> seats = new();
        readonly HashSet<Transform> taken = new();

        public string Id => id;
        public int BeautyValue => beauty;

        public static int Beauty
        {
            get
            {
                int total = 0;
                foreach (var decoration in All) total += decoration.beauty;
                return total;
            }
        }

        public static int FairBonus => Mathf.Min(Beauty, 60) / 2;
        public static float HappinessBonus => Mathf.Min(Beauty, 100) / 1000f;

        public void Setup(string itemId, int itemBeauty)
        {
            id = itemId;
            beauty = itemBeauty;
        }

        void Awake()
        {
            foreach (var child in GetComponentsInChildren<Transform>())
                if (child.name == "Seat") seats.Add(child);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Claims a free seat anywhere in the park (null if all are taken).</summary>
        public static Transform ClaimSeat(out Decoration owner)
        {
            foreach (var decoration in All)
                foreach (var seat in decoration.seats)
                    if (!decoration.taken.Contains(seat))
                    {
                        decoration.taken.Add(seat);
                        owner = decoration;
                        return seat;
                    }
            owner = null;
            return null;
        }

        public void ReleaseSeat(Transform seat) => taken.Remove(seat);

        public static int FreeSeats
        {
            get
            {
                int free = 0;
                foreach (var decoration in All) free += decoration.seats.Count - decoration.taken.Count;
                return free;
            }
        }
    }
}
