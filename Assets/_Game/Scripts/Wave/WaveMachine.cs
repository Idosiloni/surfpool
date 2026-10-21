using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Sends waves down the pool. The player picks the wave type on the console; Pro waves are locked until
    /// upgraded. Has a cooldown between presses, can send sets of several waves, and can run on a timer.
    /// </summary>
    public class WaveMachine : MonoBehaviour
    {
        [SerializeField] WaveWater water;
        [Tooltip("Seconds before the machine can send another wave.")]
        [SerializeField] float cooldown = 4f;
        [Tooltip("Send a wave automatically every this many seconds. 0 = off (only the button).")]
        [SerializeField] float autoInterval;
        [Tooltip("Waves sent per button press (a 'set').")]
        [SerializeField] int wavesPerSet = 1;
        [Tooltip("Seconds between waves in a set (real pools: about 5).")]
        [SerializeField] float setSpacing = 4f;
        [Tooltip("Seconds the machine charges after a press before the first wave leaves (\"SET INCOMING\").")]
        [SerializeField] float setLead = 1.4f;
        [Tooltip("Auto waves pick the type most waiting surfers want, instead of the selected one.")]
        [SerializeField] bool smartAuto;

        readonly HashSet<WaveLevel> unlocked = new() { WaveLevel.Beginner };
        bool directionsUnlocked;
        readonly List<(float time, WaveLevel level, WaveDirection direction, FamousWave preset, WaveShape shape)> pending = new();
        bool nextAlternateRight;
        float readyAt;
        float chargeDuration;
        float nextAutoWave;

        PoolArea pool;

        /// <summary>Fires the moment each wave leaves the machine (the visible machine plays its chambers on this).</summary>
        public event Action<WaveLevel, WaveDirection> WaveFired;

        /// <summary>Fires when a press is accepted (a set starts charging). Arg = waves in the set.</summary>
        public event Action<int> SetStarted;

        /// <summary>Waves in the set being sent now (or the last one).</summary>
        public int SetCount { get; private set; }

        /// <summary>Waves of that set that have left the machine so far.</summary>
        public int SetFired { get; private set; }

        /// <summary>A set is on its way: charging, or some of its waves are still to come.</summary>
        public bool SetRunning => pending.Count > 0;

        /// <summary>When the last wave left the machine (the lull starts then).</summary>
        public float LastWaveAt { get; private set; } = -100f;

        /// <summary>Seconds until the timer sends the next set, or -1 if the timer is off.</summary>
        public float NextAutoIn => autoInterval > 0f ? Mathf.Max(0f, nextAutoWave - Time.time) : -1f;

        /// <summary>The pool this machine belongs to.</summary>
        public PoolArea Pool
        {
            get
            {
                if (pool == null)
                    foreach (var area in PoolRegistry.All)
                        if (area.Machine == this) pool = area;
                return pool;
            }
        }

        MachineService service;

        /// <summary>Wear and breakdowns (null if the machine has no service component).</summary>
        public MachineService Service => service != null ? service : service = GetComponent<MachineService>();

        public bool Broken => Service != null && Service.Broken;

        /// <summary>Power cost of one press with the selected wave (a whole set if upgraded).</summary>
        public int PressCost => OperatingCosts.Instance != null ? OperatingCosts.Instance.PowerCost(Selected, Pool) * SetSize : 0;

        public WaveLevel Selected { get; private set; } = WaveLevel.Beginner;
        public WaveDirection SelectedDirection { get; private set; } = WaveDirection.Peak;
        public bool Ready => Time.time >= readyAt;
        public float SecondsUntilReady => Mathf.Max(0f, readyAt - Time.time);

        /// <summary>0 right after sending, 1 when ready again.</summary>
        public float ReadyProgress => chargeDuration <= 0f ? 1f : 1f - Mathf.Clamp01(SecondsUntilReady / chargeDuration);

        public float Cooldown
        {
            get => cooldown;
            set => cooldown = Mathf.Max(0.5f, value);
        }

        public float AutoInterval
        {
            get => autoInterval;
            set => autoInterval = Mathf.Max(0f, value);
        }

        public int WavesPerSet
        {
            get => wavesPerSet;
            set => wavesPerSet = Mathf.Max(1, value);
        }

        public bool SmartAuto
        {
            get => smartAuto;
            set => smartAuto = value;
        }

        public bool IsUnlocked(WaveLevel level) => unlocked.Contains(level);
        public void Unlock(WaveLevel level) => unlocked.Add(level);

        /// <summary>Peaks are always available; lefts and rights need the Reef Shaper upgrade.</summary>
        public bool IsUnlocked(WaveDirection direction) => direction == WaveDirection.Peak || directionsUnlocked;
        public void UnlockDirections() => directionsUnlocked = true;

        public bool Select(WaveDirection direction)
        {
            if (!IsUnlocked(direction)) return false;
            SelectedDirection = direction;
            Alternate = false;
            Preset = null;
            return true;
        }

        /// <summary>The wave's shape (Mellow / Standard / Hollow). Hollow needs the Reef Shaper, like lefts and rights.</summary>
        public WaveShape SelectedShape { get; private set; } = WaveShape.Standard;

        public bool IsUnlocked(WaveShape shape) => shape != WaveShape.Hollow || directionsUnlocked;

        public bool Select(WaveShape shape)
        {
            if (!IsUnlocked(shape)) return false;
            SelectedShape = shape;
            return true;
        }

        /// <summary>Lefts and rights in turn, one wave each (like PerfectSwell pools run their sessions).</summary>
        public bool Alternate { get; private set; }

        public void SetAlternate(bool on)
        {
            if (on && !directionsUnlocked) return;
            Alternate = on;
            if (on) Preset = null;
        }

        /// <summary>Waves per press chosen on the console (1 up to what the upgrades allow; 0 = the most).</summary>
        int chosenSetSize;

        public int SetSize => chosenSetSize > 0 ? Mathf.Min(chosenSetSize, wavesPerSet) : wavesPerSet;

        public void ChooseSetSize(int size) => chosenSetSize = Mathf.Clamp(size, 1, wavesPerSet);

        /// <summary>The famous wave picked on the console (null = a plain wave).</summary>
        public FamousWave Preset { get; private set; }

        /// <summary>Picks a famous wave: its type and direction, plus its own size and peel.</summary>
        public bool SelectPreset(FamousWave wave)
        {
            if (wave == null || !FamousWaves.Available(wave, this)) return false;
            Selected = wave.level;
            SelectedDirection = wave.direction;
            Alternate = false;
            SelectedShape = WaveShape.Standard;
            Preset = wave;
            return true;
        }

        public bool Select(WaveLevel level)
        {
            if (!IsUnlocked(level)) return false;
            Selected = level;
            Preset = null;
            return true;
        }

        void Update()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].time) continue;
                water.SpawnWave(pending[i].level, pending[i].direction, pending[i].preset, pending[i].shape);
                WaveFired?.Invoke(pending[i].level, pending[i].direction);
                pending.RemoveAt(i);
                SetFired++;
                LastWaveAt = Time.time;
            }

            if (autoInterval <= 0f || Time.time < nextAutoWave) return;
            var level = smartAuto ? MostWantedLevel() : Selected;
            var direction = smartAuto ? MostWantedDirection(level) : SelectedDirection;
            if (TrySendSet(level, direction, smartAuto ? null : Preset)) nextAutoWave = Time.time + autoInterval;
        }

        /// <summary>For the wave operator: send the wave most of this pool's waiting surfers want, if any are waiting.</summary>
        public bool SendForWaiting()
        {
            bool anyoneWaiting = false;
            foreach (var surfer in Surfer.All)
                if (surfer.WaitingForWave && IsMine(surfer)) anyoneWaiting = true;
            if (!anyoneWaiting) return false;
            var level = MostWantedLevel();
            return TrySendSet(level, MostWantedDirection(level));
        }

        /// <summary>Sends the selected wave type and direction (a whole set if upgraded).</summary>
        public bool TryTriggerWave() => TrySendSet(Selected, SelectedDirection, Preset);

        bool TrySendSet(WaveLevel level, WaveDirection direction, FamousWave preset = null)
        {
            if (!Ready || Broken || !IsUnlocked(level) || !IsUnlocked(direction)) return false;
            int count = SetSize;
            for (int i = 0; i < count; i++)
            {
                var dir = direction;
                if (Alternate && level != WaveLevel.Beginner && preset == null)
                {
                    dir = nextAlternateRight ? WaveDirection.Right : WaveDirection.Left;
                    nextAlternateRight = !nextAlternateRight;
                }
                pending.Add((Time.time + setLead + i * setSpacing, level, dir, preset, preset != null ? WaveShape.Standard : SelectedShape));
            }
            GameStats.WavesSent += count;
            if (OperatingCosts.Instance != null) OperatingCosts.Instance.MeterWaves(level, Pool, count);
            Sfx.Play(Sound.WaveMachine, transform.position);
            if (Service != null) Service.AddWear(level, count);
            chargeDuration = cooldown + setLead + (count - 1) * setSpacing;
            readyAt = Time.time + chargeDuration;
            SetCount = count;
            SetFired = 0;
            SetStarted?.Invoke(count);
            // The lineup sees it coming: someone calls "Outside!", everyone looks out to the machine.
            if (Pool != null) Surfer.SeeSet(Pool, transform.position, count);
            return true;
        }

        /// <summary>
        /// For smart auto waves: a left or right if everyone waiting for this wave type prefers the same way,
        /// otherwise a peak (which goes both ways).
        /// </summary>
        WaveDirection MostWantedDirection(WaveLevel level)
        {
            if (!directionsUnlocked || level == WaveLevel.Beginner) return WaveDirection.Peak;
            int lefts = 0, rights = 0;
            foreach (var surfer in Surfer.All)
            {
                if (!surfer.WaitingForWave || surfer.Skill != level || !IsMine(surfer)) continue;
                if (surfer.PreferredDirection == WaveDirection.Left) lefts++;
                else rights++;
            }
            if (lefts > 0 && rights == 0) return WaveDirection.Left;
            if (rights > 0 && lefts == 0) return WaveDirection.Right;
            return WaveDirection.Peak;
        }

        bool IsMine(Surfer surfer) => surfer.Pool != null && surfer.Pool.Machine == this;

        /// <summary>The unlocked wave type wanted by the most surfers waiting in the water.</summary>
        /// <summary>
        /// The wave type to send for the surfers waiting in this pool. Beginners ride the whitewater of any wave, so a
        /// bigger wave serves them too: send the type most of the better surfers want, or a Beginner wave if only
        /// beginners are waiting.
        /// </summary>
        WaveLevel MostWantedLevel()
        {
            var counts = new int[3];
            foreach (var surfer in Surfer.All)
                if (surfer.WaitingForWave && IsMine(surfer)) counts[(int)surfer.Skill]++;
            WaveLevel best = counts[0] > 0 || (counts[1] == 0 && counts[2] == 0) ? WaveLevel.Beginner : Selected;
            int bestCount = 0;
            for (int i = 1; i < counts.Length; i++)
            {
                var level = (WaveLevel)i;
                if (counts[i] > bestCount && IsUnlocked(level))
                {
                    best = level;
                    bestCount = counts[i];
                }
            }
            return best;
        }
    }
}
