using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Legends come to famous parks: from park level 15 with Pro waves owned, one of the unlocked legends
    /// (<see cref="FamousSurfers"/>) walks in every few minutes. The PA announces them, they wear a gold name tag,
    /// ride in the best pool, throw their signature trick (with a slow-mo highlight and cheers), and when they
    /// leave happy they pay an appearance bonus, give big XP and boost the park's rating. One at a time.
    /// </summary>
    public class FamousVisits : MonoBehaviour
    {
        [SerializeField] SurferSpawner spawner;
        [SerializeField] Surfer surferPrefab;
        [Tooltip("Minimum park level for any legend to show up.")]
        [SerializeField] int firstLevel = 15;
        [Tooltip("Seconds between visits (random in this range).")]
        [SerializeField] Vector2 gap = new(240f, 420f);
        [Tooltip("Appearance bonus as a multiple of a Pro session in their pool, paid when they leave happy.")]
        [SerializeField] float appearanceFee = 8f;
        [SerializeField] int xp = 150;

        static FamousVisits instance;
        Surfer current;
        float nextVisit = -1f;
        readonly List<string> recent = new();
        UpgradeShop shop;

        public static int Visits { get; set; }

        void OnEnable() => instance = this;

        void OnDisable()
        {
            if (instance == this) instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Visits = 0;

        void Update()
        {
            if (current != null) return;
            int level = ParkLevel.Instance != null ? ParkLevel.Instance.Level : 1;
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            if (level < firstLevel || shop == null || !shop.IsOwned("pro_waves")) return;
            if (nextVisit < 0f) nextVisit = Time.time + 60f; // the first legend comes soon after the park gets famous
            if (Time.time < nextVisit) return;
            nextVisit = Time.time + Random.Range(gap.x, gap.y) / DailyEvents.ArrivalMultiplier;
            var legend = Pick(level);
            if (legend != null) Arrive(legend);
        }

        FamousSurfer Pick(int level)
        {
            var options = new List<FamousSurfer>();
            foreach (var legend in FamousSurfers.Roster)
                if (legend.level <= level && !recent.Contains(legend.id)) options.Add(legend);
            if (options.Count == 0)
            {
                recent.Clear();
                foreach (var legend in FamousSurfers.Roster)
                    if (legend.level <= level) options.Add(legend);
            }
            return options.Count > 0 ? options[Random.Range(0, options.Count)] : null;
        }

        /// <summary>Brings a legend in right now (also for testing).</summary>
        public Surfer Arrive(FamousSurfer legend)
        {
            var surfer = Instantiate(surferPrefab, spawner.Entrance, Quaternion.identity);
            surfer.Init(spawner, false);
            surfer.MakeFamous(legend);
            current = surfer;
            recent.Add(legend.id);
            if (recent.Count > 3) recent.RemoveAt(0);
            Sfx.Play(Sound.PaChime, null, 0.8f);
            Sfx.Play(Sound.Cheer, spawner.Entrance, 0.8f);
            Notifications.Post($"★ {legend.FullName} just walked in! Sell them a Pro session.", Notice.Reward);
            return surfer;
        }

        /// <summary>A legend's session is over: pays the appearance bonus if they had a good time. Returns their goodbye line.</summary>
        public static string LegendLeaves(Surfer surfer, float happiness, int paidPrice)
        {
            var legend = surfer.Famous;
            Visits++;
            if (happiness <= 0.5f)
            {
                Notifications.Post($"{legend.FullName} left unimpressed...", Notice.Bad);
                return "Hmm. Not today.";
            }
            float fee = instance != null ? instance.appearanceFee : 8f;
            int bonus = Mathf.RoundToInt(Mathf.Max(paidPrice, 50) * fee * happiness);
            Wallet.Instance.Add(bonus, "legend visits");
            ParkLevel.Add(instance != null ? instance.xp : 150);
            if (ParkReputation.Instance != null)
                for (int i = 0; i < 3; i++) ParkReputation.Instance.Record(1f);
            Sfx.Play(Sound.CashBig);
            Notifications.Post($"{legend.FullName} loved it! +{MoneyText.Format(bonus)} and the word is out.", Notice.Reward);
            return legend.endLines[Random.Range(0, legend.endLines.Length)];
        }
    }
}
