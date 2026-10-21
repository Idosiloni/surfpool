using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Plays all the game's sound: the beach ambience (surf, wind, and a crowd that gets louder as the park fills up
    /// and levels up), shuffled chill music (out of the park speakers once they're up, with upbeat songs for a busy
    /// park), seagulls from the birds overhead, PA announcements, and one-shot effects from anywhere via
    /// <see cref="Play"/>. Volumes (music / effects / ambience) are saved in PlayerPrefs and set from the pause menu.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        static Sfx instance;

        [SerializeField] SoundBank bank;
        [SerializeField] PriceBoard pricing;
        [Range(0f, 1f)] [SerializeField] float musicLevel = 0.2f;
        [Tooltip("Seconds of just beach sounds between songs (random in this range).")]
        [SerializeField] Vector2 songGap = new(45f, 110f);
        [Range(0f, 1f)] [SerializeField] float surfLevel = 0.35f;
        [Range(0f, 1f)] [SerializeField] float crowdLevel = 0.22f;
        [Range(0f, 1f)] [SerializeField] float bigCrowdLevel = 0.2f;
        [Range(0f, 1f)] [SerializeField] float windLevel = 0.12f;
        [Tooltip("Music volume out of each park speaker, relative to the music level.")]
        [SerializeField] float speakerBoost = 2.6f;
        [Tooltip("Seconds between PA announcements at a small park; it gets chattier as the park levels up.")]
        [SerializeField] Vector2 announcementGap = new(110f, 200f);
        [Tooltip("Seconds between seagull calls (random in this range).")]
        [SerializeField] Vector2 seagullGap = new(8f, 25f);
        [SerializeField] int voices = 20;

        /// <summary>The wind loop, for things that play their own wind (the player's speed rush while surfing).</summary>
        public static AudioClip WindClip => instance != null && instance.bank != null ? instance.bank.windLoop : null;

        public static float MusicVolume { get; private set; } = 0.8f;
        public static float EffectsVolume { get; private set; } = 0.9f;
        public static float AmbienceVolume { get; private set; } = 0.8f;

        readonly List<AudioSource> pool = new();
        readonly Dictionary<Sound, float> lastPlayed = new();
        readonly Dictionary<Sound, int> lastVariant = new();
        AudioSource music, surf, crowd, crowdBig, wind;
        int nextVoice, musicIndex = -1, announcementIndex = -1;
        float duckUntil, duck = 1f, nextSeagull, pendingMoney, nextSongAt = 4f, nextAnnouncement = 40f, pendingVoiceAt = -1f;
        bool songPlaying;
        Vector3 announcementAt;

        static readonly string[] Announcements =
        {
            "Attention please: a lost flip-flop is waiting for its owner at the ticket desk.",
            "Reminder: please don't feed the seagulls. They have unionized.",
            "Would the owner of the van with the surfboards on top... nice boards!",
            "Surfers, please wait your turn in the lineup. Yes, you.",
            "Fresh fish tacos at the snack bar! Probably fresh!",
            "Today's wave report: pumping. Tomorrow's wave report: also pumping. We own the machine.",
            "A gentle reminder that sunscreen is cheaper than regret.",
            "Lifeguards report the water is wet. Carry on.",
            "Smile, you might be on the blimp cam!",
            "Kai's Surf Park thanks you for riding with us. Shaka!",
        };

        static int ParkLevelNow => ParkLevel.Instance != null ? ParkLevel.Instance.Level : 1;

        /// <summary>How many one-shots have played (for tests).</summary>
        public static int PlayedCount { get; private set; }

        void Awake()
        {
            instance = this;
            MusicVolume = PlayerPrefs.GetFloat("vol_music", 0.6f);
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
            crowdBig = Loop(bank.crowdBigLoop);
            wind = Loop(bank.windLoop);
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
            if (clip != null)
            {
                source.Play();
                // Start loops at different points so they never line up the same way twice.
                source.timeSamples = Random.Range(0, clip.samples);
            }
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
        public static void Play(Sound id, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            if (instance == null) return;
            instance.PlayInternal(id, position, volume, pitch);
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

        void PlayInternal(Sound id, Vector3? position, float volume, float pitch = 1f)
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
            source.pitch = pitch * (1f + Random.Range(-entry.pitchJitter, entry.pitchJitter));
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

            // The crowd grows with the park: louder with more surfers and a higher park level, and from about level 6
            // a second, packed-park layer (whoops and clapping) fades in on top.
            int surfers = Surfer.All.Count;
            int level = ParkLevelNow;
            float growth = Mathf.Clamp01((level - 1) / 17f);
            float busy = Mathf.Clamp01(0.25f + surfers / 12f);
            surf.volume = surfLevel * AmbienceVolume * duck;
            wind.volume = windLevel * AmbienceVolume * duck;
            float crowdTarget = crowdLevel * busy * Mathf.Lerp(0.7f, 1.6f, growth) * AmbienceVolume * duck;
            crowd.volume = Mathf.MoveTowards(crowd.volume, Mathf.Min(1f, crowdTarget), Time.unscaledDeltaTime * 0.2f);
            float bigTarget = bigCrowdLevel * Mathf.SmoothStep(0f, 1f, (level - 6) / 10f) * busy * AmbienceVolume * duck;
            crowdBig.volume = Mathf.MoveTowards(crowdBig.volume, bigTarget, Time.unscaledDeltaTime * 0.2f);

            UpdateMusic(now, level, growth);
            UpdateAnnouncements(now, level, menuOpen);

            if (!menuOpen && (nextSeagull -= Time.deltaTime) <= 0f)
            {
                nextSeagull = Random.Range(seagullGap.x, seagullGap.y);
                // Calls come from one of the gulls circling overhead (or somewhere around you if none are flying).
                if (!Bird.TryRandom(out var at))
                {
                    var listener = Camera.main != null ? Camera.main.transform.position : transform.position;
                    at = listener + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(12f, 30f) + Vector3.up * 10f;
                }
                Play(Sound.Seagull, at, Random.Range(0.6f, 1f));
            }
        }

        void UpdateMusic(float now, int level, float growth)
        {
            // Once the park speakers are up, the song plays out of them (louder as you walk past one) and only
            // quietly everywhere else. A busier park plays more often, with upbeat songs in the mix.
            var speakers = ParkSpeaker.Active;
            bool fromSpeakers = speakers.Count > 0;
            float songVolume = musicLevel * MusicVolume * duck;
            music.volume = songVolume * (fromSpeakers ? 0.45f : 1f);
            foreach (var speaker in speakers)
            {
                var source = speaker.Source;
                source.volume = Mathf.Min(1f, songVolume * speakerBoost);
                if (songPlaying && music.isPlaying && music.clip != null)
                {
                    if (source.clip != music.clip || !source.isPlaying)
                    {
                        source.clip = music.clip;
                        source.Play();
                    }
                    if (Mathf.Abs(source.timeSamples - music.timeSamples) > 2048) source.timeSamples = music.timeSamples;
                }
                else if (source.isPlaying && source.clip == music.clip) source.Stop();
            }

            // Songs with breaks of just the beach in between, so the music never nags.
            if (songPlaying && !music.isPlaying)
            {
                songPlaying = false;
                nextSongAt = now + Random.Range(songGap.x, songGap.y) * Mathf.Lerp(1f, 0.35f, growth);
            }
            if (!songPlaying && now >= nextSongAt && bank.music != null && bank.music.Length > 0)
            {
                bool party = level >= 8 && bank.partyMusic != null && bank.partyMusic.Length > 0 && Random.value < 0.5f;
                if (party) music.clip = bank.partyMusic[Random.Range(0, bank.partyMusic.Length)];
                else
                {
                    musicIndex = (musicIndex + 1 + Random.Range(0, bank.music.Length - 1)) % bank.music.Length;
                    music.clip = bank.music[musicIndex];
                }
                music.Play();
                songPlaying = true;
            }
        }

        void UpdateAnnouncements(float now, int level, bool menuOpen)
        {
            if (pendingVoiceAt > 0f && now >= pendingVoiceAt)
            {
                pendingVoiceAt = -1f;
                Play(Sound.PaVoice, announcementAt);
            }
            if (menuOpen || ParkSpeaker.Active.Count == 0 || now < nextAnnouncement) return;
            float chatty = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((level - 5) / 12f));
            nextAnnouncement = now + Random.Range(announcementGap.x, announcementGap.y) * chatty;
            var listener = Camera.main != null ? Camera.main.transform.position : transform.position;
            var speaker = ParkSpeaker.Nearest(listener);
            if (speaker == null) return;
            announcementAt = speaker.transform.position + Vector3.up * 4.5f;
            Play(Sound.PaChime, announcementAt);
            pendingVoiceAt = now + 1.2f;
            announcementIndex = (announcementIndex + 1 + Random.Range(0, Announcements.Length - 1)) % Announcements.Length;
            Notifications.Post("PA: " + Announcements[announcementIndex]);
        }

        void LateUpdate()
        {
            if (pendingMoney <= 0f) return;
            int basePrice = pricing != null ? pricing.Price : 20;
            Play(pendingMoney >= basePrice * 8 ? Sound.CashBig : Sound.Coin);
            pendingMoney = 0f;
        }
    }
}
