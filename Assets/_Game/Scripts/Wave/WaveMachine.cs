using UnityEngine;

namespace SurfPool
{
    /// <summary>Sends waves down the pool, with a cooldown between waves. Can send waves on its own once upgraded.</summary>
    public class WaveMachine : MonoBehaviour
    {
        [SerializeField] WaveWater water;
        [Tooltip("Seconds before the machine can send another wave.")]
        [SerializeField] float cooldown = 4f;
        [Tooltip("Send a wave automatically every this many seconds. 0 = off (only the button).")]
        [SerializeField] float autoInterval;

        float readyAt;
        float nextAutoWave;

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

        void Update()
        {
            if (autoInterval <= 0f || Time.time < nextAutoWave) return;
            if (TryTriggerWave()) nextAutoWave = Time.time + autoInterval;
        }

        public bool TryTriggerWave()
        {
            if (!Ready) return false;
            water.SpawnWave();
            readyAt = Time.time + cooldown;
            return true;
        }
    }
}
