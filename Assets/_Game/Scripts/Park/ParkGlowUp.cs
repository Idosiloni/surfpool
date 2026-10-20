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

        [SerializeField] UpgradeShop shop;
        [Header("Fresh paint")]
        [Tooltip("Materials as they look once the park is fixed up...")]
        [SerializeField] Material[] fresh;
        [Tooltip("...and the grimy run-down version of each (same order).")]
        [SerializeField] Material[] worn;

        readonly List<GlowItem> items = new();
        readonly List<(Renderer renderer, int slot, int pair)> painted = new();
        int shownLevel = -1, shownOwned = -1, celebrateFrom = int.MaxValue;
        bool wornNow, wornApplied;
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

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ParkLevel.Instance != null) ParkLevel.Instance.LeveledUp -= OnLeveledUp;
        }

        void Start()
        {
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            items.AddRange(FindObjectsByType<GlowItem>(FindObjectsInactive.Include));
            CollectPaintedRenderers();
            if (ParkLevel.Instance != null) ParkLevel.Instance.LeveledUp += OnLeveledUp;
            Refresh(false);
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
            if (level == shownLevel && owned == shownOwned) return;

            bool animate = allowAnimation && shownLevel > 0 && (level >= celebrateFrom || (level == shownLevel && owned > shownOwned));
            int oldStage = StageFor(Mathf.Max(1, shownLevel));
            celebrateFrom = int.MaxValue;
            shownLevel = level;
            shownOwned = owned;

            SetPaint(level < StageLevels[1]);

            int popIndex = 0;
            foreach (var item in items)
            {
                if (item == null) continue;
                bool show = item.ShouldShow(level, shop);
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
                else StartCoroutine(Shrink(item, baseScale, Random.Range(0f, 0.6f)));
            }

            int newStage = StageFor(level);
            if (animate && newStage > oldStage && newStage < StageNews.Length)
                Notifications.Post(StageNews[newStage], Notice.Reward);
        }

        void SetPaint(bool wornLook)
        {
            if (wornApplied && wornLook == wornNow) return;
            wornApplied = true;
            wornNow = wornLook;
            foreach (var (renderer, slot, pair) in painted)
            {
                if (renderer == null) continue;
                var mats = renderer.sharedMaterials;
                mats[slot] = wornLook ? worn[pair] : fresh[pair];
                renderer.sharedMaterials = mats;
            }
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
