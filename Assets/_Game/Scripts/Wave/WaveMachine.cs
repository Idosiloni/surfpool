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

        readonly HashSet<WaveLevel> unlocked = new() { WaveLevel.Beginner, WaveLevel.Intermediate };
        readonly List<(float time, WaveLevel level)> pending = new();
        float readyAt;
        float nextAutoWave;

        public WaveLevel Selected { get; private set; } = WaveLevel.Intermediate;
        public bool Ready => Time.time >= readyAt;
        public float SecondsUntilReady => Mathf.Max(0f, readyAt - Time.time);

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
                water.SpawnWave(pending[i].level);
                pending.RemoveAt(i);
            }

            if (autoInterval <= 0f || Time.time < nextAutoWave) return;
            if (TrySendSet(smartAuto ? MostWantedLevel() : Selected)) nextAutoWave = Time.time + autoInterval;
        }

        /// <summary>Sends the selected wave type (a whole set if upgraded).</summary>
        public bool TryTriggerWave() => TrySendSet(Selected);

        bool TrySendSet(WaveLevel level)
        {
            if (!Ready || !IsUnlocked(level)) return false;
            for (int i = 0; i < wavesPerSet; i++)
                pending.Add((Time.time + i * setSpacing, level));
            readyAt = Time.time + cooldown + (wavesPerSet - 1) * setSpacing;
            return true;
        }

        /// <summary>The unlocked wave type wanted by the most surfers waiting in the water.</summary>
        WaveLevel MostWantedLevel()
        {
            var counts = new int[3];
            foreach (var surfer in Surfer.All)
                if (surfer.WaitingForWave) counts[(int)surfer.Skill]++;
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
