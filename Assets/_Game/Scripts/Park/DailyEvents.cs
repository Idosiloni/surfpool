using UnityEngine;

namespace SurfPool
{
    public enum DayEvent { None, HeatWave, RainyDay, SurfContest, Influencer, PowerPrices, Inspector }

    /// <summary>
    /// Something different most days (never on day 1): a heat wave, a rainy day, a surf contest, an influencer
    /// visit, expensive power, or a health inspector who checks how clean the park is in the afternoon.
    /// Each event only changes its own multipliers (read where they're used), so upgrades stay untouched.
    /// </summary>
    public class DailyEvents : MonoBehaviour
    {
        public static DailyEvents Instance { get; private set; }

        [Tooltip("Chance of a normal day with no event (0 to 1).")]
        [Range(0f, 1f)] [SerializeField] float quietDayChance = 0.35f;
        [Tooltip("Rain that falls around the player on rainy days.")]
        [SerializeField] ParticleSystem rain;

        // Read by the systems they affect. All 1 on a normal day.
        public static float ArrivalMultiplier = 1f;
        public static float LitterMultiplier = 1f;
        public static float ElectricityMultiplier = 1f;
        public static float WaterMultiplier = 1f;
        public static float SunMultiplier = 1f;
        public static float XpMultiplier = 1f;
        public static float RatingSpeedMultiplier = 1f;
        public static float SkilledPriceMultiplier = 1f; // Intermediate and Pro sessions
        public static bool ContestDay;

        public DayEvent Today { get; private set; }
        bool inspected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatics()
        {
            ArrivalMultiplier = LitterMultiplier = ElectricityMultiplier = WaterMultiplier = SunMultiplier = 1f;
            XpMultiplier = RatingSpeedMultiplier = SkilledPriceMultiplier = 1f;
            ContestDay = false;
        }

        void Awake() => Instance = this;

        public static string Title(DayEvent e) => e switch
        {
            DayEvent.HeatWave => "Heat Wave",
            DayEvent.RainyDay => "Rainy Day",
            DayEvent.SurfContest => "Surf Contest",
            DayEvent.Influencer => "Influencer Visit",
            DayEvent.PowerPrices => "Power Price Spike",
            DayEvent.Inspector => "Health Inspector",
            _ => "",
        };

        public static string Description(DayEvent e) => e switch
        {
            DayEvent.HeatWave => "Scorching hot! Surfers come 40% more often, but they drop more trash.",
            DayEvent.RainyDay => "Rain all day: 40% fewer surfers, but the pools top themselves up (half the water bill).",
            DayEvent.SurfContest => "A surf contest is in town! Intermediate and Pro sessions pay 50% more, and more pros come.",
            DayEvent.Influencer => "A famous surf influencer is filming here. Your rating moves twice as fast and XP is +50%.",
            DayEvent.PowerPrices => "Power prices spiked: every wave costs double electricity today.",
            DayEvent.Inspector => "A health inspector visits at 3 PM. A clean park earns a bonus, trash earns a fine!",
            _ => "",
        };

        /// <summary>Morning: picks today's event (none on day 1) and announces it.</summary>
        public void StartDay(int day, bool announce = true)
        {
            var pick = DayEvent.None;
            if (day > 1 && Random.value > quietDayChance)
            {
                pick = (DayEvent)Random.Range(1, 7);
                // Contests need skilled surfers; the inspector needs litter to matter.
                if (pick == DayEvent.SurfContest && UnityEngine.Object.FindAnyObjectByType<SurferSpawner>()?.BestSkillComing == WaveLevel.Beginner)
                    pick = DayEvent.HeatWave;
            }
            Apply(pick, announce);
        }

        public void Apply(DayEvent pick, bool announce)
        {
            ResetStatics();
            Today = pick;
            inspected = false;
            switch (pick)
            {
                case DayEvent.HeatWave: ArrivalMultiplier = 1.4f; LitterMultiplier = 1.8f; SunMultiplier = 1.15f; break;
                case DayEvent.RainyDay: ArrivalMultiplier = 0.6f; WaterMultiplier = 0.5f; SunMultiplier = 0.5f; break;
                case DayEvent.SurfContest: SkilledPriceMultiplier = 1.5f; ContestDay = true; break;
                case DayEvent.Influencer: RatingSpeedMultiplier = 2f; XpMultiplier = 1.5f; break;
                case DayEvent.PowerPrices: ElectricityMultiplier = 2f; break;
            }
            if (rain != null)
            {
                if (pick == DayEvent.RainyDay) rain.Play();
                else rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (announce && pick != DayEvent.None)
            {
                Notifications.Post($"Today: {Title(pick)}! {Description(pick)}");
                Sfx.Play(Sound.Whistle);
            }
        }

        /// <summary>Called by the day cycle every frame with how far through the day it is (0 to 1).</summary>
        public void Tick(float progress)
        {
            if (Today != DayEvent.Inspector || inspected || progress < 7f / 12f) return; // 3 PM
            inspected = true;
            Inspect();
        }

        /// <summary>The inspector's verdict: a clean park (3 pieces of trash or fewer) gets a bonus, a dirty one a fine.</summary>
        public void Inspect()
        {
            int trash = Trash.All.Count;
            var wallet = Wallet.Instance;
            if (wallet == null) return;
            int income = DayCycle.Instance != null ? DayCycle.Instance.IncomeToday : 0;
            if (trash <= 3)
            {
                int bonus = Mathf.Max(100, Mathf.RoundToInt(income * 0.1f));
                wallet.Add(bonus, "inspection bonus");
                if (ParkReputation.Instance != null) ParkReputation.Instance.Record(1f);
                Notifications.Post($"Inspector: \"Spotless!\" Clean park bonus {MoneyText.Format(bonus)}.");
            }
            else
            {
                // Never more than a fifth of what you have, so a fine can't sink the park.
                int fine = Mathf.Min(50 + trash * 15, Mathf.Max(0, wallet.Money / 5));
                wallet.Charge(fine, "inspection fine");
                Notifications.Post($"Inspector: \"{trash} pieces of trash!\" Fined {MoneyText.Format(fine)}. Hire a janitor or pick it up!", true);
            }
        }
    }
}
