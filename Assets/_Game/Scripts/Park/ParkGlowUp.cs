using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The park's glow-up: it starts as the run-down pool you inherited (grimy deck, weeds, trash bags, a crooked
    /// hand-painted sign) and gets nicer at each stage of the park level: fresh paint, flags, bunting, flowers, string
    /// lights, the big entrance sign. Every <see cref="GlowItem"/> in the scene is shown or hidden from the current park
    /// level and owned upgrades, so a loaded save looks right without saving anything new. On a real level-up the new
    /// things pop in one by one (a loaded save just snaps to the right look).
    /// On top of that, the richer you get the more luxurious the park is (Ido: "the richer you are, the luxurier the
    /// place"; docs/research/park-layout-and-pool-shapes.md section 4): luxury tiers by total money earned (never
    /// the wallet, so spending doesn't make the park shabby again) swap the deck paint (concrete -> wooden boardwalk ->
    /// sandstone -> marble) and bring in loungers, cabanas, fire pits and a fountain.
    /// </summary>
    public class ParkGlowUp : MonoBehaviour
    {
        public static ParkGlowUp Instance { get; private set; }

        /// <summary>Park level where each glow-up stage starts.</summary>
        public static readonly int[] StageLevels = { 1, 3, 5, 8, 11, 15 };
        public static readonly string[] StageNames = { "Run-Down", "Cleaned Up", "Festive", "Lit Up", "Landmark", "Famous" };

        static readonly string[] StageNews =
        {
            "",
            "Park glow-up: the weeds and junk are gone, the deck has fresh paint, and the flags are up!",
            "Park glow-up: bunting, flower boxes and park speakers. Music and announcements now play around the park!",
            "Park glow-up: string lights over the decks!",
            "Park glow-up: the big entrance sign is up. Kai's Surf Park is a landmark now!",
            "Park glow-up: flag towers at the gate. The park is famous!",
        };

        /// <summary>Total money earned where each luxury tier starts, and the tiers' names.</summary>
        public static readonly long[] TierEarnings = { 0, 25_000, 150_000, 500_000 };
        public static readonly string[] TierNames = { "Surf Pool", "Beach Club", "Resort", "Luxury Resort" };

        static readonly string[] TierNews =
        {
            "",
            "Business is good, so the park got a makeover: wooden boardwalks, loungers and shade. It's a beach club now!",
            "The park is a resort now: sandstone decks, cabanas, palms in big planters and lights under the water.",
            "Pure luxury: marble decks, a fountain at the gate, fire pits and VIP cabanas. Uncle Kai won't believe it.",
        };

        [Serializable]
        public class PaintTier
        {
            [Tooltip("Same order as Fresh; empty slots keep the tier below.")]
            public Material[] materials;
        }

        [SerializeField] UpgradeShop shop;
        [Header("Fresh paint")]
        [Tooltip("Materials as they look once the park is fixed up...")]
        [SerializeField] Material[] fresh;
        [Tooltip("...and the grimy run-down version of each (same order).")]
        [SerializeField] Material[] worn;
        [Tooltip("Paint for luxury tiers 1, 2, 3...")]
        [SerializeField] PaintTier[] tierPaint;

        readonly List<GlowItem> items = new();
        readonly List<(Renderer renderer, int slot, int pair)> painted = new();
        int shownLevel = -1, shownOwned = -1, shownTier = -1, celebrateFrom = int.MaxValue;
        int paintNow = int.MinValue;
        bool earnedLive;
        float nextCheck;

        /// <summary>The glow-up stage (0 = run-down) for a park level.</summary>
        public static int StageFor(int level)
        {
            int stage = 0;
            for (int i = 0; i < StageLevels.Length; i++)
                if (level >= StageLevels[i]) stage = i;
            return stage;
        }

        public int Stage => StageFor(Mathf.Max(1, shownLevel));

        /// <summary>The luxury tier (0 = plain surf pool) for a total of money earned.</summary>
        public static int TierFor(long earned)
        {
            int tier = 0;
            for (int i = 0; i < TierEarnings.Length; i++)
                if (earned >= TierEarnings[i]) tier = i;
            return tier;
        }

        /// <summary>The park's luxury tier right now.</summary>
        public int Tier => Mathf.Max(0, shownTier);

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ParkLevel.Instance != null) ParkLevel.Instance.LeveledUp -= OnLeveledUp;
            if (Wallet.Instance != null) Wallet.Instance.Changed -= OnMoney;
        }

        void Start()
        {
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            items.AddRange(FindObjectsByType<GlowItem>(FindObjectsInactive.Include));
            CollectPaintedRenderers();
            if (ParkLevel.Instance != null) ParkLevel.Instance.LeveledUp += OnLeveledUp;
            if (Wallet.Instance != null) Wallet.Instance.Changed += OnMoney;
            Refresh(false);
        }

        // Money really coming in (a loaded save sets the total without this), so a new tier now is worth a show.
        void OnMoney(int amount, string reason)
        {
            if (amount > 0) earnedLive = true;
        }

        void CollectPaintedRenderers()
        {
            if (fresh == null || worn == null) return;
            foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                var mats = renderer.sharedMaterials;
                for (int slot = 0; slot < mats.Length; slot++)
                {
                    int pair = System.Array.IndexOf(fresh, mats[slot]);
                    if (pair >= 0 && pair < worn.Length) painted.Add((renderer, slot, pair));
                }
            }
        }

        // A real level-up (not a loaded save): the next refresh animates what changed.
        void OnLeveledUp(int level) => celebrateFrom = Mathf.Min(celebrateFrom, level);

        void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.4f;
            Refresh(true);
        }

        void Refresh(bool allowAnimation)
        {
            int level = ParkLevel.Instance != null ? ParkLevel.Instance.Level : 1;
            int owned = shop != null ? shop.OwnedCount : 0;
            int tier = TierFor(GameStats.TotalEarned);
            bool live = earnedLive;
            earnedLive = false;
            if (level == shownLevel && owned == shownOwned && tier == shownTier) return;

            // A tier reached by earning one more step (a loaded save can jump several, and earns nothing first).
            bool newTier = allowAnimation && shownTier >= 0 && tier == shownTier + 1 && live;
            bool animate = allowAnimation && shownLevel > 0 && (level >= celebrateFrom || (level == shownLevel && owned > shownOwned) || newTier);
            int oldStage = StageFor(Mathf.Max(1, shownLevel));
            celebrateFrom = int.MaxValue;
            shownLevel = level;
            shownOwned = owned;
            shownTier = tier;

            // The run-down look wins until the park is cleaned up; after that the paint follows the luxury tier.
            SetPaint(level < StageLevels[1] ? -1 : tier);

            int popIndex = 0;
            foreach (var item in items)
            {
                if (item == null) continue;
                bool show = item.ShouldShow(level, shop, tier);
                if (show == item.gameObject.activeSelf) continue;
                var baseScale = item.BaseScale;
                if (!animate)
                {
                    item.transform.localScale = baseScale;
                    item.gameObject.SetActive(show);
                }
                else if (show)
                {
                    item.transform.localScale = baseScale * 0.01f;
                    item.gameObject.SetActive(true);
                    StartCoroutine(Pop(item, baseScale, Mathf.Min(popIndex++ * 0.06f, 2.5f)));
                }
                else StartCoroutine(Shrink(item, baseScale, UnityEngine.Random.Range(0f, 0.6f)));
            }

            int newStage = StageFor(level);
            if (animate && newStage > oldStage && newStage < StageNews.Length)
                Notifications.Post(StageNews[newStage], Notice.Reward);
            if (newTier && tier < TierNews.Length)
            {
                Notifications.Post(TierNews[tier], Notice.Reward);
                Sfx.Play(Sound.UiStamp);
            }
        }

        /// <summary>-1 = the worn run-down paint, 0 = fresh, 1+ = a luxury tier's paint.</summary>
        void SetPaint(int look)
        {
            if (look == paintNow) return;
            paintNow = look;
            foreach (var (renderer, slot, pair) in painted)
            {
                if (renderer == null) continue;
                var mats = renderer.sharedMaterials;
                mats[slot] = PaintFor(look, pair);
                renderer.sharedMaterials = mats;
            }
        }

        Material PaintFor(int look, int pair)
        {
            if (look < 0) return worn[pair];
            for (int t = Mathf.Min(look, tierPaint?.Length ?? 0); t >= 1; t--)
            {
                var set = tierPaint[t - 1].materials;
                if (set != null && pair < set.Length && set[pair] != null) return set[pair];
            }
            return fresh[pair];
        }

        static IEnumerator Pop(GlowItem item, Vector3 baseScale, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.55f)
            {
                if (item == null) yield break;
                // Overshoot a little, then settle: a bouncy "boing".
                float s = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
                item.transform.localScale = baseScale * Mathf.Max(0.01f, s);
                yield return null;
            }
            if (item != null) item.transform.localScale = baseScale;
        }

        static IEnumerator Shrink(GlowItem item, Vector3 baseScale, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
            {
                if (item == null) yield break;
                item.transform.localScale = baseScale * Mathf.Max(0.01f, 1f - t);
                yield return null;
            }
            if (item == null) yield break;
            item.gameObject.SetActive(false);
            item.transform.localScale = baseScale;
        }
    }
}
