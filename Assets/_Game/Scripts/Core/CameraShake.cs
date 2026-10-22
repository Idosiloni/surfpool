using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Short camera shakes from big moments (a Giant wave crashing nearby). Shakes add "trauma" that fades out; the
    /// shake grows with trauma squared, so small bumps stay subtle and big ones hit. The walking and surfing cameras
    /// both add <see cref="Sample"/> to their own pose. Scaled by the Camera shake setting (0 = off).
    /// </summary>
    public static class CameraShake
    {
        const float MaxOffset = 0.2f; // m
        const float MaxRoll = 3f; // degrees
        const float Recovery = 0.8f; // trauma lost per second

        static float trauma;

        public static float Trauma => trauma;

        /// <summary>Adds a shake (0..1; 0.3 = a thump, 1 = the ground moves).</summary>
        public static void Add(float amount) => trauma = Mathf.Clamp01(trauma + amount * GameSettings.Shake);

        /// <summary>A shake from something at a point: full strength within <paramref name="near"/> m, gone at <paramref name="far"/> m.</summary>
        public static void From(Vector3 source, float amount, float near, float far)
        {
            var cam = Camera.main;
            if (cam == null) return;
            float d = Vector3.Distance(cam.transform.position, source);
            Add(amount * (1f - Mathf.InverseLerp(near, far, d)));
        }

        /// <summary>This frame's shake (call once per frame from the active camera script).</summary>
        public static void Sample(float dt, out Vector3 offset, out float roll)
        {
            trauma = Mathf.MoveTowards(trauma, 0f, Recovery * dt);
            float k = trauma * trauma;
            if (k <= 0f)
            {
                offset = Vector3.zero;
                roll = 0f;
                return;
            }
            float t = Time.time * 18f;
            offset = new Vector3(Mathf.PerlinNoise(t, 1.7f) - 0.5f, Mathf.PerlinNoise(3.1f, t) - 0.5f, 0f) * (2f * MaxOffset * k);
            roll = (Mathf.PerlinNoise(t, 8.3f) - 0.5f) * 2f * MaxRoll * k;
        }
    }
}
