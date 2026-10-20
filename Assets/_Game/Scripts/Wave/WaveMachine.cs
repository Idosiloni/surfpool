using UnityEngine;

namespace SurfPool
{
    /// <summary>Sends waves down the pool, with a cooldown between waves.</summary>
    public class WaveMachine : MonoBehaviour
    {
        [SerializeField] WaveWater water;
        [Tooltip("Seconds before the machine can send another wave.")]
        [SerializeField] float cooldown = 4f;

        float readyAt;

        public bool Ready => Time.time >= readyAt;
        public float SecondsUntilReady => Mathf.Max(0f, readyAt - Time.time);

        public bool TryTriggerWave()
        {
            if (!Ready) return false;
            water.SpawnWave();
            readyAt = Time.time + cooldown;
            return true;
        }
    }
}
