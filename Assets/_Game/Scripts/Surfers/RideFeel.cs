using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The feel of a board on a wave, shared by NPC surfers and the player: tilts the board to lie on the water
    /// (the wave face under it), leans it into turns by how fast the rider turns at speed, and throws spray off
    /// the rail on hard carves plus a thin wake behind the tail. Call <see cref="Begin"/> when a ride starts and
    /// <see cref="Step"/> every frame while riding with the rider's yaw-only heading.
    /// </summary>
    [System.Serializable]
    public class RideFeel
    {
        [Tooltip("Most the board leans into a turn (degrees).")]
        public float maxLean = 32f;
        [Tooltip("Lean per (degrees/second of turning x meters/second of speed).")]
        public float leanGain = 0.07f;
        [Tooltip("Steepest the board follows the wave face (degrees); the lip is near-vertical, so this is clamped.")]
        public float maxSlope = 32f;
        [Tooltip("Spray blobs per second off the rail at full lean.")]
        public float railSpray = 70f;
        [Tooltip("Spray blobs per second of thin wake behind the tail.")]
        public float wakeSpray = 8f;

        const float SampleDistance = 0.35f;

        float lastYaw, leanDeg, pitchDeg, rollDeg;
        Vector3 lastPosition;
        bool started;

        /// <summary>The board's tilt in the rider's heading frame (face slope + lean into the turn).</summary>
        public Quaternion Tilt => Quaternion.Euler(pitchDeg, 0f, rollDeg + leanDeg);

        /// <summary>
        /// What the rider's body follows: all of the lean into a turn but under half the face's slope (real surfers stay
        /// upright over a board that's tilted on a steep face; ankles and knees take the rest).
        /// </summary>
        public Quaternion BodyTilt => Quaternion.Euler(pitchDeg * BodySlope, 0f, rollDeg * BodySlope + leanDeg);

        const float BodySlope = 0.25f;

        /// <summary>-1..1 how hard the rider is turning: positive = turning right.</summary>
        public float Turn { get; private set; }

        /// <summary>Horizontal speed (m/s).</summary>
        public float Speed { get; private set; }

        /// <summary>Extra roll (degrees) added on top, e.g. a snap or a beginner's wobble.</summary>
        public float ExtraRoll { get; set; }

        public void Begin(Vector3 position, Quaternion heading)
        {
            lastPosition = position;
            lastYaw = heading.eulerAngles.y;
            leanDeg = pitchDeg = rollDeg = 0f;
            Turn = 0f;
            Speed = 0f;
            started = true;
        }

        public void Step(WaveWater water, Vector3 position, Quaternion heading, float dt)
        {
            if (!started) Begin(position, heading);
            dt = Mathf.Max(dt, 1e-4f);
            float yaw = heading.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(lastYaw, yaw) / dt;
            Vector3 moved = position - lastPosition;
            moved.y = 0f;
            Speed = Mathf.Lerp(Speed, moved.magnitude / dt, 1f - Mathf.Exp(-8f * dt));
            lastYaw = yaw;
            lastPosition = position;

            float k = 1f - Mathf.Exp(-9f * dt);
            // Turning right rolls the board right (negative z).
            float targetLean = Mathf.Clamp(-yawRate * Speed * leanGain, -maxLean, maxLean);
            leanDeg = Mathf.Lerp(leanDeg, targetLean + ExtraRoll, k);
            Turn = Mathf.Clamp(-leanDeg / maxLean, -1f, 1f);

            // Lie on the water: sample the surface around the board.
            Vector3 right = heading * Vector3.right, forward = heading * Vector3.forward;
            float d = SampleDistance;
            float side = Mathf.Atan2(water.SurfaceHeightAt(position + right * d) - water.SurfaceHeightAt(position - right * d), 2f * d) * Mathf.Rad2Deg;
            float slope = Mathf.Atan2(water.SurfaceHeightAt(position + forward * d) - water.SurfaceHeightAt(position - forward * d), 2f * d) * Mathf.Rad2Deg;
            rollDeg = Mathf.Lerp(rollDeg, Mathf.Clamp(side, -maxSlope, maxSlope), k);
            pitchDeg = Mathf.Lerp(pitchDeg, Mathf.Clamp(-slope, -maxSlope, maxSlope), k);
        }

        /// <summary>Spray off the outside rail when carving, plus a thin wake behind the tail.</summary>
        public void Spray(WaveWater water, Vector3 boardPosition, Quaternion heading, float boost = 0f)
        {
            Vector3 right = heading * Vector3.right, forward = heading * Vector3.forward;
            Vector3 tail = boardPosition - forward * 0.7f + Vector3.up * 0.05f;
            float carve = Mathf.Clamp01(Mathf.Abs(Turn) * 1.3f + boost);
            if (carve > 0.15f)
            {
                float outside = Turn >= 0f ? -1f : 1f;
                Vector3 velocity = right * (outside * (2.5f + carve * 2.5f)) + Vector3.up * (1.8f + carve * 2.2f) - forward * 1.2f;
                water.EmitRiderSpray(tail + right * (outside * 0.25f), velocity, railSpray * carve * carve, 0.18f + carve * 0.18f);
            }
            if (Speed > 1.5f)
                water.EmitRiderSpray(tail, Vector3.up * 0.8f - forward * 0.8f, wakeSpray, 0.12f);
        }

        /// <summary>A ring of splash (landing an air, wiping out).</summary>
        public static void Splash(WaveWater water, Vector3 position, int blobs, float power)
        {
            for (int i = 0; i < blobs; i++)
            {
                Vector2 ring = Random.insideUnitCircle.normalized;
                water.SprayBurst(position + Vector3.up * 0.1f,
                    new Vector3(ring.x * power, power * Random.Range(0.8f, 1.6f), ring.y * power), 0.2f + power * 0.05f);
            }
        }
    }
}
