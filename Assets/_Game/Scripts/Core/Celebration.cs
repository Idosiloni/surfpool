using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The party for big moments: a park level-up, or a pool opening or growing. Fireworks over the pool, confetti in
    /// front of you, the whole crowd cheering, a fanfare, and a big banner on the HUD (<see cref="Shown"/>).
    /// Only real moments celebrate: loading a save sets the level and upgrades without firing these events.
    /// Several moments at once (a purchase that also levels you up) queue up one after another, and they wait while a
    /// menu is open (buying a pool happens at the upgrade computer).
    /// </summary>
    public class Celebration : MonoBehaviour
    {
        public static Celebration Instance { get; private set; }

        /// <summary>A banner to show: title, subtitle, and whether it's a big one (new pool).</summary>
        public static event Action<string, string, bool> Shown;

        [SerializeField] ParticleSystem rockets;
        [SerializeField] ParticleSystem confetti;
        [SerializeField] Transform mainPoolCenter;
        [Tooltip("Seconds each banner stays before the next queued one.")]
        [SerializeField] float bannerTime = 3.6f;

        class Moment
        {
            public string title, subtitle;
            public bool big;
            public Vector3 where;
        }

        readonly Queue<Moment> queue = new();
        int pendingLevel;
        float busyUntil;
        UpgradeShop shop;
        ParkLevel subscribed;

        /// <summary>How many celebrations have played (for tests).</summary>
        public static int Count { get; private set; }

        void Awake() => Instance = this;

        void OnEnable() => UpgradeShop.Bought += OnBought;

        void OnDisable()
        {
            UpgradeShop.Bought -= OnBought;
            if (subscribed != null) subscribed.LeveledUp -= OnLeveledUp;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (subscribed == null && ParkLevel.Instance != null)
            {
                subscribed = ParkLevel.Instance;
                subscribed.LeveledUp += OnLeveledUp;
            }
        }

        // Several level-ups in one go (a big XP reward) become one celebration for the highest level.
        void OnLeveledUp(int level) => pendingLevel = Mathf.Max(pendingLevel, level);

        void OnBought(Upgrade upgrade)
        {
            if (upgrade.category != UpgradeCategory.Pools) return;
            bool newPool = upgrade.id.StartsWith("pool_") && !upgrade.id.StartsWith("pool_expand");
            var where = mainPoolCenter != null ? mainPoolCenter.position : transform.position;
            foreach (var pool in PoolRegistry.All)
                if (newPool ? pool.UpgradeId == upgrade.id : string.IsNullOrEmpty(pool.UpgradeId))
                {
                    var center = pool.Footprint(0f).center;
                    where = new Vector3(center.x, 0f, center.y);
                }
            string title = newPool ? "NEW POOL OPEN!" : "POOL EXPANDED!";
            string subtitle = newPool ? upgrade.title.Replace("Build the ", "") : upgrade.title;
            queue.Enqueue(new Moment { title = title, subtitle = subtitle, big = true, where = where });
        }

        void LateUpdate()
        {
            if (pendingLevel > 0)
            {
                if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
                var unlocked = shop != null ? shop.UnlockedAt(pendingLevel) : new List<string>();
                queue.Enqueue(new Moment
                {
                    title = $"PARK LEVEL {pendingLevel}!",
                    subtitle = unlocked.Count > 0 ? "New: " + string.Join(", ", unlocked) : "Your park is growing!",
                    where = mainPoolCenter != null ? mainPoolCenter.position : transform.position,
                });
                pendingLevel = 0;
            }
            // Wait until the player is back in the world: a purchase at the upgrade computer (or a level-up while any
            // menu is open) celebrates the moment the menu closes, not hidden behind it.
            bool menuOpen = InputBlocker.Blocked || Time.timeScale == 0f;
            if (queue.Count > 0 && Time.unscaledTime >= busyUntil && !menuOpen)
            {
                var moment = queue.Dequeue();
                busyUntil = Time.unscaledTime + bannerTime;
                Celebrate(moment);
            }
        }

        /// <summary>Fires a celebration now (also handy for testing from the console).</summary>
        public void Celebrate(string title, string subtitle, bool big) =>
            Celebrate(new Moment { title = title, subtitle = subtitle, big = big, where = mainPoolCenter != null ? mainPoolCenter.position : transform.position });

        void Celebrate(Moment moment)
        {
            Count++;
            Shown?.Invoke(moment.title, moment.subtitle, moment.big);
            Sfx.Play(moment.big ? Sound.FanfarePool : Sound.FanfareLevel);
            Sfx.Duck(moment.big ? 4.5f : 3f);
            Sfx.Play(Sound.Cheer, null, 0.9f);
            Spectator.CheerAll(moment.big ? 6f : 4f);
            Confetti(moment.big ? 260 : 160);
            StartCoroutine(Fireworks(moment.where, moment.big ? 14 : 7));
        }

        void Confetti(int count)
        {
            var cam = Camera.main;
            if (confetti == null || cam == null) return;
            confetti.transform.position = cam.transform.position + cam.transform.forward * 3.5f + Vector3.up * 2.2f;
            confetti.Emit(count);
        }

        IEnumerator Fireworks(Vector3 where, int count)
        {
            if (rockets == null) yield break;
            float lifetime = rockets.main.startLifetime.constantMax;
            for (int i = 0; i < count; i++)
            {
                var at = where + new Vector3(UnityEngine.Random.Range(-14f, 14f), 0.5f, UnityEngine.Random.Range(-5f, 5f));
                rockets.transform.position = at;
                rockets.Emit(1);
                Sfx.Play(Sound.FireworkLaunch, at, 0.8f);
                StartCoroutine(Bang(at + Vector3.up * 26f, lifetime));
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.25f, 0.55f));
            }
        }

        static IEnumerator Bang(Vector3 at, float delay)
        {
            yield return new WaitForSeconds(delay);
            Sfx.Play(Sound.FireworkBang, at);
        }
    }
}
