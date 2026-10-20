using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Session prices. The player sets the base price (a beginner session in the main pool). Better surfers and
    /// fancier pools pay a multiple of it: Intermediate x2.5, Pro x6, and each pool has its own multiplier
    /// (Reef x2.5, Lagoon x5). The "fair" price is what an average surfer expects for a base session; each surfer
    /// will pay up to 80%-150% of fair (times the same multipliers). Upgrades raise the fair price.
    /// </summary>
    public class PriceBoard : MonoBehaviour
    {
        [Tooltip("Base price: a beginner session in the main pool.")]
        [SerializeField] int price = 20;
        [Tooltip("What an average surfer thinks a base session is worth. Upgrades raise it.")]
        [SerializeField] int fairPrice = 25;
        [SerializeField] int step = 5;
        [SerializeField] int minPrice = 5;
        [SerializeField] int maxPrice = 200;

        [Header("Board rental")]
        [SerializeField] int rentalPrice = 10;

        public int Price => price;
        public int RentalPrice
        {
            get => rentalPrice;
            set => rentalPrice = Mathf.Max(0, value);
        }

        public int FairPrice
        {
            get => fairPrice;
            set => fairPrice = Mathf.Max(1, value);
        }

        public static float SkillMultiplier(WaveLevel skill) => skill switch
        {
            WaveLevel.Beginner => 1f,
            WaveLevel.Intermediate => 2.5f,
            _ => 6f,
        };

        static float PoolMultiplier(PoolArea pool) => pool != null ? pool.PriceMultiplier : 1f;

        /// <summary>Daily events (a surf contest) can make skilled sessions worth more.</summary>
        static float EventMultiplier(WaveLevel skill) => skill == WaveLevel.Beginner ? 1f : DailyEvents.SkilledPriceMultiplier;

        /// <summary>What this surfer is charged at the desk.</summary>
        public int PriceFor(WaveLevel skill, PoolArea pool) =>
            Mathf.RoundToInt(price * SkillMultiplier(skill) * PoolMultiplier(pool) * EventMultiplier(skill));

        /// <summary>The most a surfer of this skill, going to this pool, expects to pay (before their own mood roll).</summary>
        public float FairFor(WaveLevel skill, PoolArea pool) => fairPrice * SkillMultiplier(skill) * PoolMultiplier(pool) * EventMultiplier(skill);

        /// <summary>Restores the player's base price from a save. Fair and rental prices come from owned upgrades.</summary>
        public void RestorePrice(int ticket)
        {
            if (ticket > 0) price = Mathf.Clamp(ticket, minPrice, maxPrice);
        }

        public void Change(int direction)
        {
            price = Mathf.Clamp(price + step * direction, minPrice, maxPrice);
        }
    }
}
