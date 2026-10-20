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
        [Tooltip("Seconds between waves in a set.")]
        [SerializeField] float setSpacing = 2.5f;
        [Tooltip("Auto waves pick the type most waiting surfers want, instead of the selected one.")]
        [SerializeField] bool smartAuto;

        readonly HashSet<WaveLevel> unlocked = new() { WaveLevel.Beginner };
        bool directionsUnlocked;
        readonly List<(float time, WaveLevel level, WaveDirection direction)> pending = new();
        float readyAt;
        float chargeDuration;
        float nextAutoWave;

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
            return true;
        }

        public bool Select(WaveLevel level)
        {
            if (!IsUnlocked(level)) return false;
            Selected = level;
            return true;
        }

        void Update()
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].time) continue;
                water.SpawnWave(pending[i].level, pending[i].direction);
                pending.RemoveAt(i);
            }

            if (autoInterval <= 0f || Time.time < nextAutoWave) return;
            var level = smartAuto ? MostWantedLevel() : Selected;
            var direction = smartAuto ? MostWantedDirection(level) : SelectedDirection;
            if (TrySendSet(level, direction)) nextAutoWave = Time.time + autoInterval;
        }

        /// <summary>Sends the selected wave type and direction (a whole set if upgraded).</summary>
        public bool TryTriggerWave() => TrySendSet(Selected, SelectedDirection);

        bool TrySendSet(WaveLevel level, WaveDirection direction)
        {
            if (!Ready || !IsUnlocked(level) || !IsUnlocked(direction)) return false;
            for (int i = 0; i < wavesPerSet; i++)
                pending.Add((Time.time + i * setSpacing, level, direction));
            GameStats.WavesSent += wavesPerSet;
            chargeDuration = cooldown + (wavesPerSet - 1) * setSpacing;
            readyAt = Time.time + chargeDuration;
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
        WaveLevel MostWantedLevel()
        {
            var counts = new int[3];
            foreach (var surfer in Surfer.All)
                if (surfer.WaitingForWave && IsMine(surfer)) counts[(int)surfer.Skill]++;
            WaveLevel best = Selected;
            int bestCount = 0;
            for (int i = 0; i < counts.Length; i++)
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
