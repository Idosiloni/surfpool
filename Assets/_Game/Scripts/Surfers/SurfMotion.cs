using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// How a board moves on a wave, shared by the player and NPC surfers. The rider has a heading on the wave face and a
    /// speed: dropping down the face turns height into speed, climbing turns speed back into height (so a drop and a
    /// bottom turn draw a real curve), and the wave's push keeps a rider going in the clean, steep part just ahead of the
    /// break (so letting go of the keys trims along the line instead of falling off). The flats in front of the wave and
    /// the whitewater behind the break bog you down. Speeds land around 4-9 m/s, a pro's peak ~11 (docs/research/
    /// surf-feel.md). Everything is in the water's own frame (x = machine to beach, z = across), so pools can face any way.
    /// Height on the face is a fraction (0 = the trough, 1 = the lip, below 0 = out in the flats); the real face under the
    /// rider (WaveWater surface) is measured every step, so steep Pro faces and soft swells both work.
    /// Beginner whitewater has no green face: the foam pushes you in, and steering only angles you left or right.
    /// </summary>
    public class SurfMotion
    {
        /// <summary>0 = the bottom of the face (trough) .. 1 = the lip; below 0 = out in the flats in front of the wave.</summary>
        public float Face;
        /// <summary>Across the pool (water frame z).</summary>
        public float Z;
        /// <summary>Degrees on the face: 0 = along +z, 90 = straight up the face, 180 = along -z, 270 = straight down.</summary>
        public float Heading;
        /// <summary>Meters per second along the heading.</summary>
        public float Speed;
        /// <summary>Riding the foam of a broken wave (no green face to drop down).</summary>
        public bool Whitewater;

        /// <summary>Meters in front of the crest where the rider is (water frame x = crest + U).</summary>
        public float U { get; private set; } = 1f;
        /// <summary>The face under the rider: its height (m) and length along the slope (m).</summary>
        public float FaceHeight { get; private set; } = 1f;
        public float FaceLength { get; private set; } = 2f;
        /// <summary>Meters in front of the break point on the side you're riding (+ = clean face, - = in the whitewater behind it).</summary>
        public float Ahead { get; private set; } = 5f;
        public bool Breaking { get; private set; }
        public bool Barrel { get; private set; }
        public bool Finished { get; private set; }
        /// <summary>Which way the wave peels on your side: +1 toward +z, -1 toward -z.</summary>
        public int PeelDirection { get; private set; } = 1;
        /// <summary>The rider drove into the lip this step (it turns you back down) or bottomed out in the flats.</summary>
        public bool HitLip { get; private set; }
        public bool HitBottom { get; private set; }

        /// <summary>Along the wave (m/s): + toward +z.</summary>
        public float AlongSpeed => Speed * Mathf.Cos(Heading * Mathf.Deg2Rad);
        /// <summary>-1 straight down the face .. +1 straight up it.</summary>
        public float Climb => Mathf.Sin(Heading * Mathf.Deg2Rad);
        /// <summary>Which way you're going along the wave: +1 toward +z, -1 toward -z.</summary>
        public int Travel => Mathf.Cos(Heading * Mathf.Deg2Rad) >= 0f ? 1 : -1;

        /// <summary>The board's pointing direction in the water frame (flat).</summary>
        public Vector3 LocalDirection
        {
            get
            {
                float h = Heading * Mathf.Deg2Rad;
                // Up the face is toward the crest (-x); down is toward the beach (+x). Squashed a little so lines across a
                // steep face still look like lines across it from above.
                return new Vector3(-Mathf.Sin(h) * 0.8f, 0f, Mathf.Cos(h)).normalized;
            }
        }

        const float Gravity = 9.81f;
        const float GravityScale = 0.75f; // how much of real gravity the face slope gives (game feel)
        const float FlatsDepth = 0.8f; // lowest Face (out in the flats)
        const float FlatsRun = 3f;     // meters in front of the trough at Face = -1
        const float WhitewaterU = 2.2f;

        /// <summary>
        /// Starts a ride where the rider is (u = meters in front of the crest), angled down toward the open side
        /// (direction +1 = toward +z).
        /// </summary>
        public void Begin(WaveWater water, float waveId, float z, float u, int direction, bool whitewater, float speed = 3.5f)
        {
            Z = z;
            Whitewater = whitewater;
            Heading = whitewater ? (direction > 0 ? 300f : 240f) : (direction > 0 ? -45f : 225f);
            Speed = speed;
            HitLip = HitBottom = false;
            measured = false;
            if (whitewater)
            {
                Face = 0.35f;
                U = WhitewaterU;
                return;
            }
            float crest = water.CurrentWaveCenter(waveId);
            MeasureFace(water, crest, 0f);
            // How high up the face that spot is: a late take-off starts near the top; an early one (the face hasn't
            // reached you yet) gets picked up and starts halfway up.
            float y = HeightAt(water, crest + u);
            Face = u > troughU ? 0.45f : Mathf.Clamp((y - troughHeight) / Mathf.Max(0.3f, crestHeight - troughHeight), 0.45f, 0.9f);
            U = UAt(water, crest, Face);
        }

        /// <summary>
        /// One step. turnRate in degrees/second (+ = turn left = toward a bigger Heading). extraPush adds to the speed the
        /// wave carries you at (pumping, leaning forward); stall drags the tail (slows you, to wait for the barrel).
        /// </summary>
        public void Step(WaveWater water, float waveId, float turnRate, float extraPush, bool stall, float dt)
        {
            HitLip = HitBottom = false;
            float crest = water.CurrentWaveCenter(waveId);
            int preferred = Travel;
            if (water.TryGetPocket(waveId, preferred, out float pocketZ, out int direction, out bool breaking, out bool finished, out bool barrel))
            {
                PeelDirection = direction;
                Ahead = (Z - pocketZ) * direction;
                Breaking = breaking;
                Barrel = barrel;
                Finished = finished;
            }
            else
            {
                // Closeouts and gone waves: no pocket, it's all one face.
                Ahead = 6f;
                Breaking = water.IsBroken(waveId);
                Barrel = Finished = false;
            }
            Heading = Mathf.Repeat(Heading + turnRate * dt, 360f);

            if (Whitewater)
            {
                StepWhitewater(dt, extraPush, stall);
                return;
            }

            MeasureFace(water, crest, dt);
            float h = Heading * Mathf.Deg2Rad;
            float climb = Mathf.Sin(h);

            // Gravity along the face: going down speeds you up, going up slows you.
            if (Face >= 0f && Face <= 1f) Speed -= Gravity * GravityScale * FaceHeight / FaceLength * climb * dt;

            // The wave's push: the speed it carries you at here. Strongest high on a clean face near the break.
            float trim;
            if (Face < 0f) trim = 2.5f;                                  // out in the flats: the wave runs away
            else if (Breaking && Ahead < -1.5f) trim = 2.5f;             // in the whitewater behind the break
            else if (!Breaking) trim = 4.5f + 1.5f * Face;               // a swell that hasn't broken yet
            else if (Ahead > 12f) trim = 4.5f + Face;                    // far out on the shoulder
            else trim = Mathf.Lerp(5f, 7.5f, Face);                      // the pocket
            trim += extraPush;
            if (stall) trim = Mathf.Min(trim, 2.5f);
            float rate = Speed < trim ? 0.9f : stall ? 3f : 0.35f;
            Speed = Mathf.MoveTowards(Speed, trim, Mathf.Abs(trim - Speed) * rate * dt + (stall ? 3f * dt : 0f));
            bool cleanFace = Face >= 0.1f && (!Breaking || Ahead > -1.5f);
            Speed = Mathf.Clamp(Speed, cleanFace ? 3f : 1f, 13f);

            // Move.
            Face += Speed * climb / FaceLength * dt;
            Z += Speed * Mathf.Cos(h) * dt;
            if (Face > 1f)
            {
                // Into the lip: it swings you round and back down (a forced top turn), with a little speed lost.
                Face = 1f;
                if (climb > 0f) Heading = Mathf.MoveTowardsAngle(Heading, Mathf.Repeat(-Heading, 360f), 420f * dt);
                Speed -= 1.5f * dt;
                HitLip = true;
            }
            else if (Face < -FlatsDepth)
            {
                Face = -FlatsDepth;
                if (climb < 0f) Heading = Mathf.MoveTowardsAngle(Heading, Mathf.Repeat(-Heading, 360f), 300f * dt);
                HitBottom = true;
            }
            // Never faster than the board could really move across the face (the measured face can wobble in foam).
            U = Mathf.MoveTowards(U, UAt(water, crest, Face), (Speed * 1.2f + 3f) * dt);
        }

        void StepWhitewater(float dt, float extraPush, bool stall)
        {
            // Pointed at the beach, give or take 60 degrees: the foam pushes you in at its own speed.
            float down = Mathf.DeltaAngle(270f, Heading);
            Heading = 270f + Mathf.Clamp(down, -60f, 60f);
            float trim = 4f + extraPush * 0.5f - (stall ? 1.5f : 0f);
            Speed = Mathf.MoveTowards(Speed, trim, 2f * dt);
            Z += Speed * Mathf.Cos(Heading * Mathf.Deg2Rad) * dt;
            Face = 0.35f;
            U = WhitewaterU;
            FaceHeight = 0.6f;
            FaceLength = 1.5f;
        }

        /// <summary>
        /// Keeps the rider inside the pool's side walls: near a wall they ease back in (the pool narrows toward the
        /// beach), slow down and get turned back along it, never snapped.
        /// </summary>
        public void KeepInside(float halfWidth, float dt)
        {
            if (Mathf.Abs(Z) <= halfWidth) return;
            float side = Mathf.Sign(Z);
            Z = Mathf.MoveTowards(Z, side * halfWidth, 5f * dt);
            if (Mathf.Sign(AlongSpeed) == side)
            {
                Speed = Mathf.Max(1.5f, Speed - 6f * dt);
                float away = side > 0f ? 180f + 20f : -20f; // back along the wave, a little down the face
                Heading = Mathf.MoveTowardsAngle(Heading, away, 200f * dt);
            }
        }

        // ---- the face under the rider ----
        float crestU, troughU, crestHeight, troughHeight;
        bool measured;

        /// <summary>
        /// Finds the crest (highest point around the wave's center line) and the trough (lowest point in front of it)
        /// at the rider's z. Smoothed over a few frames (dt > 0), so the sample spacing never makes the rider hop.
        /// </summary>
        void MeasureFace(WaveWater water, float crest, float dt)
        {
            float cu = 0f, ch = float.MinValue;
            for (float u = -0.8f; u <= 0.81f; u += 0.4f)
            {
                float y = HeightAt(water, crest + u);
                if (y > ch) { ch = y; cu = u; }
            }
            // Big waves have longer faces: look further out for the trough (10 samples either way).
            float reach = Mathf.Max(5f, 2.2f * (ch - HeightAt(water, crest + cu + 6f)));
            float tu = cu + 2.5f, th = float.MaxValue;
            for (int i = 0; i <= 10; i++)
            {
                float u = cu + 1f + (reach - 1f) * i / 10f;
                float y = HeightAt(water, crest + u);
                if (y < th) { th = y; tu = u; }
            }
            float k = measured && dt > 0f ? 1f - Mathf.Exp(-10f * dt) : 1f;
            measured = true;
            crestU = Mathf.Lerp(crestU, cu, k);
            crestHeight = Mathf.Lerp(crestHeight, ch, k);
            troughU = Mathf.Lerp(troughU, tu, k);
            troughHeight = Mathf.Lerp(troughHeight, th, k);
            FaceHeight = Mathf.Max(0.4f, crestHeight - troughHeight);
            float run = Mathf.Max(0.5f, troughU - crestU);
            FaceLength = Mathf.Sqrt(FaceHeight * FaceHeight + run * run);
        }

        /// <summary>Where on x (in front of the crest) the face is this high (found walking down the face).</summary>
        float UAt(WaveWater water, float crest, float face)
        {
            if (face <= 0f) return troughU + -face * FlatsRun;
            float target = troughHeight + face * (crestHeight - troughHeight);
            float prevU = crestU, prevY = crestHeight;
            const int steps = 8;
            for (int i = 1; i <= steps; i++)
            {
                float u = Mathf.Lerp(crestU, troughU, i / (float)steps);
                float y = i == steps ? troughHeight : HeightAt(water, crest + u);
                if (y <= target)
                    return Mathf.Lerp(prevU, u, Mathf.InverseLerp(prevY, y, target));
                prevU = u;
                prevY = y;
            }
            return troughU;
        }

        Vector3 local;

        float HeightAt(WaveWater water, float x)
        {
            local.x = x;
            local.z = Z;
            var world = water.transform.TransformPoint(local);
            return water.transform.InverseTransformPoint(new Vector3(world.x, water.SurfaceHeightAt(world), world.z)).y;
        }
    }
}
