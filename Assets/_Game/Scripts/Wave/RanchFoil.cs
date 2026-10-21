using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The Surf Ranch's wave machine you can see (like Kelly Slater's): a carriage on a rail along the pool's long
    /// side drags a huge foil through the water. When a wave is running the carriage races down the rail just ahead
    /// of the break, throwing a wake; between waves it rumbles back to the start. Purely visual: the pool's
    /// <see cref="WaveMachine"/> and <see cref="WaveWater"/> make the wave.
    /// </summary>
    public class RanchFoil : MonoBehaviour
    {
        [SerializeField] WaveWater water;
        [SerializeField] Transform carriage;
        [Tooltip("Spins while the carriage moves (the winch drums at the ends of the rail).")]
        [SerializeField] Transform[] drums;
        [SerializeField] ParticleSystem wake;
        [Tooltip("How far ahead of the break the foil runs (m).")]
        [SerializeField] float lead = 9f;
        [Tooltip("Rail position (water frame x) and the ends it parks at (local z).")]
        [SerializeField] float railX = -2.5f;
        [SerializeField] float returnSpeed = 9f, maxSpeed = 16f;

        float z, speed;

        void Start()
        {
            if (water != null) z = ParkZ;
        }

        /// <summary>Waves start near the far end (rights) and the foil waits there.</summary>
        float ParkZ => water.Width / 2f - 4f;

        void Update()
        {
            if (water == null || carriage == null) return;
            float target = ParkZ;
            bool running = false;
            float newest = float.MinValue;
            for (int i = 0; i < water.ActiveWaves; i++)
            {
                var (id, _) = water.GetWave(i);
                if (id <= newest) continue;
                if (!water.TryGetPocket(id, -1, out float pocket, out int dir, out bool breaking, out bool finished, out _) || finished) continue;
                newest = id;
                target = Mathf.Clamp(pocket + dir * lead, -water.Width / 2f + 3f, water.Width / 2f - 3f);
                running = breaking;
            }
            float before = z;
            z = Mathf.MoveTowards(z, target, (running ? maxSpeed : returnSpeed) * Time.deltaTime);
            speed = Mathf.Abs(z - before) / Mathf.Max(1e-4f, Time.deltaTime);
            carriage.position = water.transform.TransformPoint(new Vector3(railX, 0f, z)) + Vector3.up * (0f - water.transform.position.y);
            carriage.rotation = water.transform.rotation;
            foreach (var drum in drums)
                if (drum != null) drum.Rotate(0f, 0f, speed * 40f * Time.deltaTime, Space.Self);

            // A wake off the foil while it's pushing a wave.
            if (wake != null && running && speed > 2f && Random.value < 0.8f)
            {
                var emit = new ParticleSystem.EmitParams { applyShapeToPosition = true };
                Vector3 at = water.transform.TransformPoint(new Vector3(0.8f, 0f, z));
                for (int k = 0; k < 4; k++)
                {
                    emit.position = at + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.4f), Random.Range(-1.5f, 1.5f));
                    emit.velocity = water.transform.right * Random.Range(1f, 3f) + Vector3.up * Random.Range(1.5f, 3.5f);
                    emit.startSize = Random.Range(0.3f, 0.8f);
                    wake.Emit(emit, 1);
                }
            }
        }
    }
}
