using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Plays all the game's sound: the beach ambience (surf and a crowd that gets louder as the park fills up),
    /// shuffled chill music, seagulls, and one-shot effects from anywhere via <see cref="Play"/>.
    /// Volumes (music / effects / ambience) are saved in PlayerPrefs and set from the pause menu.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx instance;

        [SerializeField] SoundBank bank;
        [SerializeField] PriceBoard pricing;
        [Range(0f, 1f)] [SerializeField] float musicLevel = 0.32f;
        [Range(0f, 1f)] [SerializeField] float surfLevel = 0.35f;
        [Range(0f, 1f)] [SerializeField] float crowdLevel = 0.22f;
        [Tooltip("Seconds between seagull calls (random in this range).")]
        [SerializeField] Vector2 seagullGap = new(8f, 25f);
        [SerializeField] int voices = 20;

        public static float MusicVolume { get; private set; } = 0.8f;
        public static float EffectsVolume { get; private set; } = 0.9f;
        public static float AmbienceVolume { get; private set; } = 0.8f;

        readonly List<AudioSource> pool = new();
        readonly Dictionary<Sound, float> lastPlayed = new();
        readonly Dictionary<Sound, int> lastVariant = new();
        AudioSource music, surf, crowd;
        int nextVoice, musicIndex = -1;
        float duckUntil, duck = 1f, nextSeagull, pendingMoney;

        /// <summary>How many one-shots have played (for tests).</summary>
        public static int PlayedCount { get; private set; }

        void Awake()
        {
            instance = this;
            MusicVolume = PlayerPrefs.GetFloat("vol_music", 0.8f);
            EffectsVolume = PlayerPrefs.GetFloat("vol_effects", 0.9f);
            AmbienceVolume = PlayerPrefs.GetFloat("vol_ambience", 0.8f);

            for (int i = 0; i < voices; i++)
            {
                var go = new GameObject("Voice " + i);
                go.transform.SetParent(transform, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.dopplerLevel = 0f;
                pool.Add(source);
            }
            surf = Loop(bank.surfLoop);
            crowd = Loop(bank.crowdLoop);
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.ignoreListenerPause = true;
            nextSeagull = Random.Range(3f, 8f);
        }

        AudioSource Loop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            if (clip != null) source.Play();
            return source;
        }

        void Start()
        {
            // A soft click on every button in every screen.
            foreach (var doc in FindObjectsByType<UIDocument>())
                doc.rootVisualElement?.RegisterCallback<ClickEvent>(OnUiClick, TrickleDown.TrickleDown);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        static void OnUiClick(ClickEvent evt)
        {
            if (evt.target is VisualElement element && (element is Button || element.GetFirstAncestorOfType<Button>() != null))
                Play(Sound.Click);
        }

        // ---------- one-shots ----------

        /// <summary>Plays a sound. Pass a position for 3D sounds (waves, splashes); UI and money sounds play everywhere.</summary>
        public static void Play(Sound id, Vector3? position = null, float volume = 1f)
        {
            if (instance == null) return;
            instance.PlayInternal(id, position, volume);
        }

        /// <summary>Money came in. Several payments in the same frame (tip + merch + lessons) make one sound.</summary>
        public static void Money(int amount)
        {
            if (instance != null) instance.pendingMoney += amount;
        }

        /// <summary>Lowers music and ambience for a moment so a big sound (mission complete, new pool) stands out.</summary>
        public static void Duck(float seconds = 1.6f)
        {
            if (instance != null) instance.duckUntil = Time.unscaledTime + seconds;
        }

        public static void SetVolumes(float musicVolume, float effects, float ambience)
        {
            MusicVolume = Mathf.Clamp01(musicVolume);
            EffectsVolume = Mathf.Clamp01(effects);
            AmbienceVolume = Mathf.Clamp01(ambience);
            PlayerPrefs.SetFloat("vol_music", MusicVolume);
            PlayerPrefs.SetFloat("vol_effects", EffectsVolume);
            PlayerPrefs.SetFloat("vol_ambience", AmbienceVolume);
        }

        void PlayInternal(Sound id, Vector3? position, float volume)
        {
            var entry = bank != null ? bank.Get(id) : null;
            if (entry == null || entry.clips == null || entry.clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < entry.minGap) return;
            lastPlayed[id] = now;

            // Never the same variant twice in a row.
            int variant = Random.Range(0, entry.clips.Length);
            if (entry.clips.Length > 1 && lastVariant.TryGetValue(id, out int previous) && variant == previous)
                variant = (variant + 1) % entry.clips.Length;
            lastVariant[id] = variant;

            var source = NextVoice();
            bool spatial = entry.spatial && position.HasValue;
            source.transform.position = spatial ? position.Value : transform.position;
            source.spatialBlend = spatial ? 1f : 0f;
            source.minDistance = entry.minDistance;
            source.maxDistance = entry.maxDistance;
            source.pitch = 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);
            source.clip = entry.clips[variant];
            source.volume = entry.volume * volume * EffectsVolume * Random.Range(0.9f, 1f);
            source.Play();
            PlayedCount++;
        }

        AudioSource NextVoice()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var candidate = pool[(nextVoice + i) % pool.Count];
                if (!candidate.isPlaying)
                {
                    nextVoice = (nextVoice + i + 1) % pool.Count;
                    return candidate;
                }
            }
            var oldest = pool[nextVoice]; // all busy: steal the oldest
            nextVoice = (nextVoice + 1) % pool.Count;
            return oldest;
        }

        // ---------- beds, music, seagulls ----------

        void Update()
        {
            float now = Time.unscaledTime;
            bool menuOpen = Time.timeScale == 0f;
            float target = now < duckUntil ? 0.45f : menuOpen ? 0.6f : 1f;
            duck = Mathf.MoveTowards(duck, target, Time.unscaledDeltaTime * (target < duck ? 4f : 1.2f));

            int surfers = Surfer.All.Count;
            float busy = Mathf.Clamp01(0.25f + surfers / 12f);
            surf.volume = surfLevel * AmbienceVolume * duck;
            crowd.volume = Mathf.MoveTowards(crowd.volume, crowdLevel * busy * AmbienceVolume * duck, Time.unscaledDeltaTime * 0.2f);

            music.volume = musicLevel * MusicVolume * duck;
            if (!music.isPlaying && bank.music != null && bank.music.Length > 0)
            {
                musicIndex = (musicIndex + 1 + Random.Range(0, bank.music.Length - 1)) % bank.music.Length;
                music.clip = bank.music[musicIndex];
                music.Play();
            }

            if (!menuOpen && (nextSeagull -= Time.deltaTime) <= 0f)
            {
                nextSeagull = Random.Range(seagullGap.x, seagullGap.y);
                var listener = Camera.main != null ? Camera.main.transform.position : transform.position;
                var offset = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(12f, 30f);
                Play(Sound.Seagull, listener + offset + Vector3.up * 10f, Random.Range(0.6f, 1f));
            }
        }

        void LateUpdate()
        {
            if (pendingMoney <= 0f) return;
            int basePrice = pricing != null ? pricing.Price : 20;
            Play(pendingMoney >= basePrice * 4 ? Sound.CashBig : Sound.Coin);
            pendingMoney = 0f;
        }
    }
}
