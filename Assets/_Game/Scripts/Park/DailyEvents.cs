using UnityEngine;

namespace SurfPool
{
    public enum DayEvent { None, HeatWave, RainyDay, SurfContest, Influencer, PowerPrices, Inspector, GoldenHour }

    /// <summary>Today's wind (docs/research/wave-pools-and-ocean-waves.md): offshore grooms waves, onshore makes them mushy.</summary>
    public enum Wind { Calm, Offshore, Onshore }

    /// <summary>
    /// Something different most days (never on day 1): a heat wave, a rainy day, a surf contest, an influencer
    /// visit, expensive power, a health inspector who checks how clean the park is in the afternoon, or a golden-hour
    /// day when the sun hangs low all afternoon and the waves glow.
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
        public static float PhotoPrintMultiplier = 1f;
        public static bool ContestDay;
        /// <summary>The sun sinks low early and stays there, golden, all afternoon (read by the day cycle).</summary>
        public static bool GoldenHour;

        // Wind, picked separately from the event (both can happen on the same day).
        public static Wind TodayWind = Wind.Calm;
        /// <summary>Size of the small surface ripples (onshore chop is big, offshore water is glassy).</summary>
        public static float WindChop = 1f;
        /// <summary>How good rides feel, and how often surfers fall.</summary>
        public static float WindRideQuality = 1f, WindWipeouts = 1f;

        public DayEvent Today { get; private set; }
        bool inspected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatics()
        {
            ArrivalMultiplier = LitterMultiplier = ElectricityMultiplier = WaterMultiplier = SunMultiplier = 1f;
            XpMultiplier = RatingSpeedMultiplier = SkilledPriceMultiplier = PhotoPrintMultiplier = 1f;
            ContestDay = GoldenHour = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetWind() => ApplyWind(Wind.Calm, false);

        void Awake() => Instance = this;

        public static string Title(DayEvent e) => e switch
        {
            DayEvent.HeatWave => "Heat Wave",
            DayEvent.RainyDay => "Rainy Day",
            DayEvent.SurfContest => "Surf Contest",
            DayEvent.Influencer => "Influencer Visit",
            DayEvent.PowerPrices => "Power Price Spike",
            DayEvent.Inspector => "Health Inspector",
            DayEvent.GoldenHour => "Golden Hour",
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
            DayEvent.GoldenHour => "The sun hangs low and golden all afternoon: glowing waves, 15% more surfers, +25% XP, and photo prints sell for double.",
            _ => "",
        };

        public static string WindTitle(Wind w) => w switch
        {
            Wind.Offshore => "Offshore winds",
            Wind.Onshore => "Onshore winds",
            _ => "",
        };

        /// <summary>Sets today's wind and what it does to the waves and the surfers.</summary>
        public static void ApplyWind(Wind wind, bool announce)
        {
            TodayWind = wind;
            WindChop = wind switch { Wind.Offshore => 0.25f, Wind.Onshore => 3.5f, _ => 1f };
            WindRideQuality = wind switch { Wind.Offshore => 1.1f, Wind.Onshore => 0.85f, _ => 1f };
            WindWipeouts = wind switch { Wind.Offshore => 0.85f, Wind.Onshore => 1.25f, _ => 1f };
            if (!announce || wind == Wind.Calm) return;
            Notifications.Post(wind == Wind.Offshore
                ? "Offshore winds today: glassy water, spray blowing off the lips. Surfers are stoked!"
                : "Onshore winds today: choppy, mushy waves. Surfers fall more and enjoy it less.", Notice.Info);
        }

        /// <summary>Morning: picks today's event (none on day 1) and announces it.</summary>
        public void StartDay(int day, bool announce = true)
        {
            float windRoll = Random.value;
            var wind = day <= 1 || windRoll < 0.5f ? Wind.Calm : windRoll < 0.75f ? Wind.Offshore : Wind.Onshore;
            var pick = DayEvent.None;
            if (day > 1 && Random.value > quietDayChance)
            {
                pick = (DayEvent)Random.Range(1, 8);
                // Contests need skilled surfers; the inspector needs litter to matter.
                if (pick == DayEvent.SurfContest && UnityEngine.Object.FindAnyObjectByType<SurferSpawner>()?.BestSkillComing == WaveLevel.Beginner)
                    pick = DayEvent.HeatWave;
            }
            Apply(pick, announce);
            ApplyWind(wind, announce);
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
                case DayEvent.GoldenHour: GoldenHour = true; ArrivalMultiplier = 1.15f; XpMultiplier = 1.25f; PhotoPrintMultiplier = 2f; break;
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
                Notifications.Post($"Inspector: \"Spotless!\" Clean park bonus {MoneyText.Format(bonus)}.", Notice.Reward);
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
