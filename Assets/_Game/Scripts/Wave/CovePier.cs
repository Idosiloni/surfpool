using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The Cove's pier (like a Wavegarden Cove): a row of wave modules down the middle of the water. When a set fires,
    /// the modules kick their paddles out one after another from the north end to the south, a ripple running down
    /// the pier, and the wave peels the same way on both sides: a right on the east side, a left on the west side.
    /// Purely visual: the two <see cref="WaveMachine"/>s (leader + twin) do the work.
    /// </summary>
    public class CovePier : MonoBehaviour
    {
        [SerializeField] WaveMachine eastMachine, westMachine;
        [Tooltip("Paddle hinges on the east face, from north to south.")]
        [SerializeField] Transform[] eastPaddles;
        [Tooltip("Paddle hinges on the west face, from north to south.")]
        [SerializeField] Transform[] westPaddles;
        [Tooltip("Lamps on top of each module (lit while it fires).")]
        [SerializeField] Renderer[] lamps;
        [SerializeField] Material lampOff, lampOn;
        [Tooltip("Seconds between neighbouring modules firing (the ripple down the pier).")]
        [SerializeField] float stagger = 0.12f;
        [SerializeField] float swing = 38f;
        [SerializeField] ParticleSystem spray;

        float[] firedAt;
        Quaternion[] eastRest, westRest;

        void Awake()
        {
            int n = Mathf.Max(eastPaddles.Length, westPaddles.Length);
            firedAt = new float[n];
            for (int i = 0; i < n; i++) firedAt[i] = -10f;
            eastRest = Rest(eastPaddles);
            westRest = Rest(westPaddles);
        }

        static Quaternion[] Rest(Transform[] paddles)
        {
            var rest = new Quaternion[paddles.Length];
            for (int i = 0; i < paddles.Length; i++) rest[i] = paddles[i] != null ? paddles[i].localRotation : Quaternion.identity;
            return rest;
        }

        void OnEnable()
        {
            // Both sides fire together, so listening to one is enough (the east side leads).
            if (eastMachine != null) eastMachine.WaveFired += OnFired;
        }

        void OnDisable()
        {
            if (eastMachine != null) eastMachine.WaveFired -= OnFired;
        }

        void OnFired(WaveLevel level, WaveDirection direction)
        {
            for (int i = 0; i < firedAt.Length; i++) firedAt[i] = Time.time + i * stagger;
            Sfx.Play(Sound.AirBlast, transform.position);
        }

        void Update()
        {
            for (int i = 0; i < firedAt.Length; i++)
            {
                float t = Time.time - firedAt[i];
                // Kick out fast, ease back slowly.
                float push = t < 0f ? 0f : t < 0.2f ? Mathf.SmoothStep(0f, 1f, t / 0.2f) : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 1.4f);
                if (i < eastPaddles.Length && eastPaddles[i] != null) eastPaddles[i].localRotation = eastRest[i] * Quaternion.Euler(0f, 0f, swing * push);
                if (i < westPaddles.Length && westPaddles[i] != null) westPaddles[i].localRotation = westRest[i] * Quaternion.Euler(0f, 0f, -swing * push);
                if (lamps != null && i < lamps.Length && lamps[i] != null)
                {
                    var mat = push > 0.3f ? lampOn : lampOff;
                    if (lamps[i].sharedMaterial != mat) lamps[i].sharedMaterial = mat;
                }
                if (spray != null && t >= 0.12f && t - Time.deltaTime < 0.12f) Splash(i);
            }
        }

        void Splash(int i)
        {
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            foreach (var paddles in new[] { eastPaddles, westPaddles })
            {
                if (i >= paddles.Length || paddles[i] == null) continue;
                Vector3 outward = (paddles[i].position - transform.position);
                outward.y = 0f;
                outward = new Vector3(Mathf.Sign(outward.x), 0f, 0f);
                Vector3 at = paddles[i].position + outward * 0.8f + Vector3.down * 1.6f;
                for (int k = 0; k < 10; k++)
                {
                    emit.position = at + new Vector3(Random.Range(0f, 0.6f) * outward.x, Random.Range(-0.2f, 0.3f), Random.Range(-1.8f, 1.8f));
                    emit.velocity = outward * Random.Range(2f, 5f) + Vector3.up * Random.Range(1.5f, 4f);
                    emit.startSize = Random.Range(0.3f, 0.7f);
                    spray.Emit(emit, 1);
                }
            }
        }
    }
}
