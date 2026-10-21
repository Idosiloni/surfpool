using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The pool's water surface. A grid mesh whose vertices are moved every frame: small ambient ripples plus any
    /// waves rolling down the pool. Each wave breaks from a break point that peels along the crest (left, right,
    /// or both ways from a peak); behind it the wave turns into lower, bumpy whitewash. Foam is written into the
    /// vertex colors for the StylizedWater shader, Pro waves throw a barrel lip (a separate mesh), and breaking
    /// sections spray foam blobs.
    /// Local space: the pool runs from x = 0 (wave machine) to x = length (beach), z is centered.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WaveWater : MonoBehaviour
    {
        [Header("Pool size (meters)")]
        [SerializeField] float length = 60f;
        [SerializeField] float width = 24f;
        [SerializeField] float verticesPerMeter = 3f;

        [Header("Wave types")]
        [Tooltip("Shape of each wave type the machine can send. Tune these to change how each wave looks and feels.")]
        [SerializeField] WaveType[] waveTypes =
        {
            new() { level = WaveLevel.Beginner, height = 1.0f, speed = 5f, frontWidth = 3.2f, backWidth = 6f,
                breakAt = 0.22f, closeout = true, whitewashHeight = 0.55f },
            new() { level = WaveLevel.Intermediate, height = 2.0f, speed = 7f, frontWidth = 2.0f, backWidth = 8f,
                breakAt = 0.25f, peelSpeed = 2.6f, whitewashHeight = 0.45f },
            new() { level = WaveLevel.Pro, height = 2.8f, speed = 8.5f, frontWidth = 1.4f, backWidth = 9f,
                breakAt = 0.22f, peelSpeed = 3.2f, whitewashHeight = 0.4f, barrel = true },
        };
        [Tooltip("Scales every wave's height in this pool (bigger pools have bigger waves).")]
        [SerializeField] float sizeMultiplier = 1f;
        [Tooltip("Scales how fast waves peel. Below 1 = longer rides.")]
        [SerializeField] float peelSpeedMultiplier = 1f;
        [Tooltip("How fast waves travel down the pool (x the wave type's speed). Slower = longer rides in long pools.")]
        [SerializeField] float travelSpeedMultiplier = 1f;
        [Tooltip("Scales how far out waves start breaking (the Surf Ranch's foil makes it break right by the rail).")]
        [SerializeField] float breakAtMultiplier = 1f;
        [Tooltip("Seconds of travel a new wave starts with (slow waves would otherwise crawl out of the machine).")]
        [SerializeField] float headStart;
        [Tooltip("Where a peeling wave starts breaking, measured from the middle of the pool (m).")]
        [SerializeField] float peakOffset = 4f;
        [Tooltip("If above 0: peeling waves start this far from a side wall instead (follows the pool's width as it grows).")]
        [SerializeField] float peakFromWall;

        float PeakOffset => peakFromWall > 0f ? Mathf.Max(0f, HalfWidth - peakFromWall) : peakOffset;
        [Tooltip("How much smaller the wave is at the pool walls than in the middle (0 to 1).")]
        [Range(0f, 1f)] [SerializeField] float edgeDrop = 0.2f;

        [Header("Breaking near the beach")]
        [Tooltip("How far down the pool (0 to 1) every wave turns to whitewash and shrinks.")]
        [Range(0f, 1f)] [SerializeField] float beachBreakStart = 0.75f;
        [Tooltip("Size of the wave when it reaches the beach, compared to full size.")]
        [Range(0f, 1f)] [SerializeField] float breakEndScale = 0.3f;

        [Header("Calm water")]
        [SerializeField] float rippleHeight = 0.04f;
        [Tooltip("Leftover chop each wave leaves when it hits the beach and bounces back (0 to 1).")]
        [SerializeField] float chopPerWave = 0.15f;
        [Tooltip("Seconds for the leftover chop to settle to half (the lull between sets).")]
        [SerializeField] float chopHalfLife = 5f;
        [Tooltip("Ripples are this many times bigger in fully choppy water.")]
        [SerializeField] float chopRipples = 2.5f;

        [Header("Look")]
        [Tooltip("Seconds foam lingers on the water after the wave has passed (fading into lacy trails).")]
        [SerializeField] float foamLinger = 3.5f;
        [Tooltip("Depth of the dip in front of an unbroken wave face, as a share of its height (makes the face stand up).")]
        [Range(0f, 0.3f)] [SerializeField] float troughDepth = 0.1f;

        [Header("Barrel lip and spray")]
        [SerializeField] MeshFilter lipMesh;
        [SerializeField] float lipLength = 6f;
        [SerializeField] ParticleSystem spray;
        [Tooltip("Foam blobs per second at each break point.")]
        [SerializeField] float sprayRate = 45f;
        [Tooltip("Mist puffs per second blown out of a barrel's mouth (spit).")]
        [SerializeField] float spitRate = 14f;

        struct ActiveWave
        {
            public float start;
            public WaveType type;
            public WaveDirection direction;
            public string famous; // name of the famous wave it imitates, or null
            public WaveShape shape;
        }

        /// <summary>Everything about a wave that's the same for every vertex this frame.</summary>
        struct WaveState
        {
            public float id;
            public WaveType type;
            public WaveDirection direction;
            public float center, amplitude, back, beachBreak;
            public bool breaking;
            public float breakDistance; // how far the break point has peeled from where it started (m)
            public float closeoutAmount; // 0..1 for closeouts, ramps up as the whole wave crashes
        }

        readonly List<ActiveWave> waves = new();
        readonly List<WaveState> states = new();
        readonly HashSet<float> crashed = new();
        Mesh mesh;
        Mesh lip;
        Vector3[] baseVertices;
        Vector3[] vertices;
        Color[] colors;
        float[] foamMemory;
        readonly List<Vector3> lipVertices = new();
        readonly List<Color> lipColors = new();
        readonly List<int> lipTriangles = new();

        // ---- instant replay: the water can be rewound to a recorded ride (SurfReplay) ----
        bool replaying;
        float replayTime, replayDt;
        readonly List<ActiveWave> recorded = new(), liveBackup = new();
        bool recording;

        /// <summary>The water's clock: the game time, or the replay's time while a ride is replayed.</summary>
        float Now => replaying ? replayTime : Time.time;
        float Dt => replaying ? replayDt : Time.deltaTime;

        /// <summary>Starts remembering every wave in the pool now and every new one (for a replay of this ride).</summary>
        public void BeginRecording()
        {
            recorded.Clear();
            recorded.AddRange(waves);
            recording = true;
        }

        public void EndRecording() => recording = false;

        /// <summary>Swaps the live waves for the recorded ones and drives the water from the replay clock.</summary>
        public void StartReplay(float time)
        {
            if (replaying) return;
            liveBackup.Clear();
            liveBackup.AddRange(waves);
            waves.Clear();
            waves.AddRange(recorded);
            foreach (var wave in recorded) crashed.Remove(wave.start); // let them crash again on screen
            replaying = true;
            replayTime = time;
            replayDt = 0f;
        }

        /// <summary>Moves the replay clock (dt can be slow-motion small).</summary>
        public void SetReplayTime(float time)
        {
            replayDt = Mathf.Max(0f, time - replayTime);
            replayTime = time;
        }

        public void StopReplay()
        {
            if (!replaying) return;
            replaying = false;
            waves.Clear();
            waves.AddRange(liveBackup);
            foreach (var wave in recorded) crashed.Add(wave.start);
            foreach (var wave in liveBackup) crashed.Add(wave.start);
        }

        public int ActiveWaves => waves.Count;

        /// <summary>
        /// Where to sit (local x) to take off on a peeling wave of this level, `along` meters down the line from where
        /// it starts breaking: the crest is that far out when the break point reaches you.
        /// </summary>
        public float TakeoffX(WaveLevel level, float along)
        {
            var type = Scaled(TypeOf(level));
            float peel = Mathf.Max(0.1f, type.peelSpeed);
            return type.breakAt * length + type.speed * Mathf.Abs(along) / peel;
        }

        /// <summary>Where a left or a right starts breaking (local z); 0 for a peak.</summary>
        public float PeelStartZ(WaveDirection direction) =>
            direction == WaveDirection.Right ? PeakOffset : direction == WaveDirection.Left ? -PeakOffset : 0f;

        /// <summary>
        /// Leftover chop from waves that hit the beach and bounced back: 0 = glassy, 1 = a washing machine. It settles
        /// during the lull between sets, so real pools wait before the next set; rides on choppy water score less.
        /// </summary>
        public float Chop { get; private set; }

        /// <summary>Ride quality on the leftover chop (1 on glassy water, 0.8 when fully choppy).</summary>
        public float ChopRideQuality => 1f - 0.2f * Chop;

        float Ripple => rippleHeight * DailyEvents.WindChop * (1f + chopRipples * Chop);
        public float Length => length;
        public float Width => width;
        float HalfWidth => width * 0.5f;

        /// <summary>A wave in the pool right now: its id (the time it was sent) and its type.</summary>
        public (float id, WaveLevel level) GetWave(int index) => (waves[index].start, waves[index].type.level);

        /// <summary>Local x position of a wave's crest right now.</summary>
        public float CurrentWaveCenter(float waveId)
        {
            foreach (var wave in waves)
                if (wave.start == waveId) return WaveCenter(wave, Now);
            return length * 2f; // gone: treat as past the beach
        }

        public WaveType TypeOf(WaveLevel level)
        {
            foreach (var type in waveTypes)
                if (type.level == level) return type;
            return waveTypes[0];
        }

        /// <summary>The wave type as this pool makes it (size and peel multipliers applied).</summary>
        WaveType Scaled(WaveType type) => new()
        {
            level = type.level,
            height = type.height * sizeMultiplier,
            speed = type.speed * travelSpeedMultiplier,
            frontWidth = type.frontWidth,
            backWidth = type.backWidth,
            breakAt = type.breakAt * breakAtMultiplier,
            closeout = type.closeout,
            peelSpeed = type.peelSpeed * peelSpeedMultiplier,
            whitewashHeight = type.whitewashHeight,
            barrel = type.barrel,
        };

        /// <summary>
        /// Where a surfer should ride on a peeling wave: the break point ("pocket") on the side they're going.
        /// preferredDirection is -1 (toward -z, a right) or +1 (toward +z, a left); peaks let the surfer choose,
        /// single-direction waves force their own direction. Returns false for closeouts and missing waves.
        /// </summary>
        public bool TryGetPocket(float waveId, int preferredDirection, out float pocketZ, out int rideDirection,
            out bool breaking, out bool finished, out bool barrel)
        {
            pocketZ = 0f; rideDirection = preferredDirection; breaking = false; finished = false; barrel = false;
            foreach (var state in states)
            {
                if (state.id != waveId) continue;
                if (state.type.closeout) return false;
                rideDirection = state.direction switch
                {
                    WaveDirection.Right => -1,
                    WaveDirection.Left => 1,
                    _ => preferredDirection,
                };
                pocketZ = BreakPoint(state, rideDirection);
                breaking = state.breaking;
                barrel = state.type.barrel && state.breaking;
                finished = Mathf.Abs(pocketZ) > HalfWidth - 1.5f;
                return true;
            }
            return false;
        }

        /// <summary>How big a wave is (Beginner = whitewater, Intermediate = green face, Pro = steep and fast).</summary>
        public WaveLevel LevelOf(float waveId)
        {
            foreach (var wave in waves)
                if (wave.start == waveId) return wave.type.level;
            return WaveLevel.Beginner;
        }

        /// <summary>The shape a wave was sent with (Standard if it's gone).</summary>
        public WaveShape ShapeOf(float waveId)
        {
            foreach (var wave in waves)
                if (wave.start == waveId) return wave.shape;
            return WaveShape.Standard;
        }

        public WaveDirection DirectionOf(float waveId)
        {
            foreach (var wave in waves)
                if (wave.start == waveId) return wave.direction;
            return WaveDirection.Peak;
        }

        void Awake()
        {
            BuildMesh();
            if (lipMesh != null)
            {
                lip = new Mesh { name = "BarrelLip" };
                lip.MarkDynamic();
                lipMesh.sharedMesh = lip;
            }
        }

        /// <summary>Changes the pool's size (the main pool grows with Pool Expansion upgrades) and rebuilds the mesh.</summary>
        public void Resize(float newLength, float newWidth)
        {
            if (Mathf.Approximately(newLength, length) && Mathf.Approximately(newWidth, width) && mesh != null) return;
            length = newLength;
            width = newWidth;
            if (mesh == null) return;
            Destroy(mesh);
            BuildMesh();
        }

        public void SpawnWave(WaveLevel level, WaveDirection direction = WaveDirection.Peak, FamousWave preset = null,
            WaveShape shape = WaveShape.Standard)
        {
            var type = Scaled(TypeOf(level));
            if (preset != null)
            {
                type.height *= preset.size;
                type.peelSpeed *= preset.peel;
            }
            if (level != WaveLevel.Beginner) // whitewater is whitewater
            {
                if (shape == WaveShape.Mellow)
                {
                    // A soft, spilling wave: lower, with a long gentle face, peeling a bit slower; never barrels.
                    type.height *= 0.85f;
                    type.frontWidth *= 1.35f;
                    type.peelSpeed *= 0.85f;
                    type.barrel = false;
                }
                else if (shape == WaveShape.Hollow)
                {
                    // Steep and throwing: taller with a short, steep face, and the lip pitches into a barrel.
                    type.height *= 1.12f;
                    type.frontWidth *= 0.75f;
                    type.barrel = true;
                }
            }
            var spawned = new ActiveWave { start = Now - headStart, type = type, direction = direction, famous = preset?.name, shape = shape };
            waves.Add(spawned);
            if (recording) recorded.Add(spawned);
        }

        /// <summary>Has this wave broken into whitewater yet? (Beginners on the inside ride the whitewater of bigger waves.)</summary>
        public bool IsBroken(float waveId)
        {
            foreach (var state in states)
                if (state.id == waveId) return state.breaking || state.beachBreak > 0f;
            return false;
        }

        /// <summary>The famous wave this wave imitates (null for plain waves or if it's gone).</summary>
        public string FamousName(float waveId)
        {
            foreach (var wave in waves)
                if (wave.start == waveId) return wave.famous;
            return null;
        }

        /// <summary>World-space height of the water surface at a world position.</summary>
        public float SurfaceHeightAt(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return transform.TransformPoint(new Vector3(local.x, LocalHeight(local.x, local.z, Now), local.z)).y;
        }

        void Update()
        {
            float t = Now;
            if (!replaying)
            {
                // Bigger waves bounce back harder; whitewater barely stirs the pool.
                float added = 0f;
                for (int i = waves.Count - 1; i >= 0; i--)
                {
                    if (WaveCenter(waves[i], t) <= length + waves[i].type.backWidth * 3f) continue;
                    added += chopPerWave * (waves[i].type.level == WaveLevel.Beginner ? 0.4f : waves[i].type.level == WaveLevel.Pro ? 1.3f : 1f);
                    waves.RemoveAt(i);
                }
                Chop = Mathf.Clamp01(Chop * Mathf.Pow(0.5f, Time.deltaTime / Mathf.Max(0.1f, chopHalfLife)) + added);
            }
            UpdateStates(t);

            // Foam stays where it was made and fades over foamLinger seconds, leaving lacy trails behind the
            // wave. Vertex color: R = foam, G = face brightness, B = shallowness (beach end), A = foam freshness
            // (1 = just made, toward 0 = old foam the shader breaks into lace).
            // Nobody looking (another pool, or behind you): skip the mesh. Surfers ride on SurfaceHeightAt, not the mesh.
            if (surfaceRenderer == null) surfaceRenderer = GetComponent<MeshRenderer>();
            if (surfaceRenderer != null && !surfaceRenderer.isVisible && !replaying)
            {
                EmitSpray();
                return;
            }
            UpdateSurface(t, Dt / Mathf.Max(0.1f, foamLinger));
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            BuildLip(t);
            EmitSpray();
        }

        // ---- the per-frame surface, fast: the same math as LocalHeight + Foam, but what only depends on the row (z)
        // or the column (x) is worked out once, and waves too far from a column are skipped. ----
        int gridX, gridZ;
        MeshRenderer surfaceRenderer;
        WaveState[] stateArray = new WaveState[0];
        float[] rowBroken = new float[0], rowPocket = new float[0], rowEdge = new float[0], rowSin = new float[0];
        float[] colSin = new float[0], colShallow = new float[0];
        int[] colStates = new int[0], colCount = new int[0];
        // Per column and wave: the terms that only depend on the distance to the crest (behind it, mostly).
        float[] colD = new float[0], colBack = new float[0], colTrail = new float[0], colLip = new float[0], colFace = new float[0];

        void UpdateSurface(float t, float fade)
        {
            int nx = gridX, nz = gridZ, count = states.Count;
            if (stateArray.Length < count) stateArray = new WaveState[count * 2];
            states.CopyTo(stateArray);
            if (rowEdge.Length != nz) { rowEdge = new float[nz]; rowSin = new float[nz]; }
            if (colSin.Length != nx) { colSin = new float[nx]; colShallow = new float[nx]; colCount = new int[nx]; }
            if (rowBroken.Length < count * nz) { rowBroken = new float[count * nz * 2]; rowPocket = new float[count * nz * 2]; }
            if (colStates.Length < count * nx)
            {
                colStates = new int[count * nx * 2];
                colD = new float[count * nx * 2];
                colBack = new float[count * nx * 2];
                colTrail = new float[count * nx * 2];
                colLip = new float[count * nx * 2];
                colFace = new float[count * nx * 2];
            }

            for (int iz = 0; iz < nz; iz++)
            {
                float z = baseVertices[iz * nx].z, edge = z / HalfWidth;
                rowEdge[iz] = 1f - edgeDrop * edge * edge;
                rowSin[iz] = Mathf.Sin(z * 0.9f + t * 1.1f);
                for (int s = 0; s < count; s++)
                {
                    ref readonly var st = ref stateArray[s];
                    rowBroken[s * nz + iz] = Broken(st, z);
                    rowPocket[s * nz + iz] = st.breaking && !st.type.closeout ? PocketMask(st, z) : 0f;
                }
            }
            for (int ix = 0; ix < nx; ix++)
            {
                float x = baseVertices[ix].x;
                colSin[ix] = Ripple * Mathf.Sin(x * 0.7f + t * 1.3f);
                colShallow[ix] = Smooth(0.55f, 1f, x / length);
                int n = 0;
                for (int s = 0; s < count; s++)
                {
                    ref readonly var st = ref stateArray[s];
                    float d = x - st.center;
                    // Past these distances every term (shape, trough, foam crest and trail, lip spray) is ~0.
                    if (d < -Mathf.Max(3f * st.back, 32f) || d > 8f * st.type.frontWidth + 2f) continue;
                    int c = ix * count + n++;
                    colStates[c] = s;
                    colD[c] = d;
                    colBack[c] = d <= 0f ? Mathf.Exp(-(d / st.back) * (d / st.back)) : 0f;
                    colTrail[c] = d < 0f ? Mathf.Exp(d / 7f) * 0.75f : 0f;
                    colLip[c] = Mathf.Exp(-(d / 1.2f) * (d / 1.2f)) * 0.7f;
                    float fd = (d - 0.9f) / 1.2f;
                    colFace[c] = Mathf.Exp(-fd * fd) * 0.35f;
                }
                colCount[ix] = n;
            }

            for (int iz = 0; iz < nz; iz++)
            {
                float edgeScale = rowEdge[iz], zSin = rowSin[iz];
                for (int ix = 0; ix < nx; ix++)
                {
                    int i = iz * nx + ix;
                    Vector3 v = baseVertices[i];
                    float x = v.x, h = colSin[ix] * zSin, foam = 0f, face = 0f;
                    for (int k = 0; k < colCount[ix]; k++)
                    {
                        int c = ix * count + k, s = colStates[c];
                        ref readonly var st = ref stateArray[s];
                        float broken = rowBroken[s * nz + iz], mask = rowPocket[s * nz + iz];
                        float d = colD[c];

                        // Height (as LocalHeight).
                        float front = st.type.frontWidth * (1f + 0.8f * broken);
                        float shape = d <= 0f ? colBack[c] : Mathf.Exp(-(d / front) * (d / front));
                        float pocket = mask * (1f - broken) * 0.15f;
                        float amplitude = st.amplitude * Mathf.Lerp(1f, st.type.whitewashHeight, broken) * (1f + pocket);
                        h += amplitude * edgeScale * shape;
                        if (troughDepth > 0f && d > 0f)
                        {
                            float ahead = (d - 1.6f * front) / front;
                            h -= troughDepth * amplitude * (1f - broken) * edgeScale * Mathf.Exp(-ahead * ahead);
                        }
                        if (broken > 0.01f && shape > 0.01f)
                            h += broken * shape * st.amplitude * 0.12f * (Mathf.PerlinNoise(x * 0.9f - t * 2f, v.z * 0.9f) - 0.5f);

                        // Foam and face brightness (as Foam). The crest band is only a few meters wide.
                        float strength = Mathf.Clamp01(st.amplitude / 1.5f) + (st.type.closeout ? 0.7f : 0.3f);
                        float crest = 0f;
                        if (d > -8f && d < 8f)
                        {
                            float cd = (d - 0.4f) / (1.1f + 1.4f * broken);
                            crest = Mathf.Exp(-cd * cd);
                        }
                        foam = Mathf.Max(foam, Mathf.Max(broken * Mathf.Max(crest, colTrail[c]) * strength, mask * colLip[c]));
                        face = Mathf.Max(face, (1f - broken) * colFace[c]);
                    }
                    v.y = h;
                    vertices[i] = v;
                    float fresh = Mathf.Clamp01(foam);
                    float memory = foamMemory[i] = Mathf.Max(fresh, foamMemory[i] - fade);
                    colors[i] = new Color(memory, face, colShallow[ix], memory > 0.001f ? fresh / memory : 1f);
                }
            }
        }

        void UpdateStates(float t)
        {
            states.Clear();
            foreach (var wave in waves)
            {
                var type = wave.type;
                float center = WaveCenter(wave, t);
                float progress = center / length;
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.05f, 0.15f, progress));
                float beachBreak = Mathf.InverseLerp(beachBreakStart, 1f, progress);

                float breakTime = wave.start + (type.breakAt * length + type.backWidth) / type.speed;
                bool breaking = t >= breakTime;
                if (breaking && crashed.Add(wave.start)) PlayCrash(type, center, wave.direction);
                states.Add(new WaveState
                {
                    id = wave.start,
                    type = type,
                    direction = wave.direction,
                    center = center,
                    amplitude = type.height * rise * Mathf.Lerp(1f, breakEndScale, beachBreak),
                    back = type.backWidth * (1f + beachBreak),
                    beachBreak = beachBreak,
                    breaking = breaking,
                    breakDistance = breaking ? (t - breakTime) * type.peelSpeed : 0f,
                    closeoutAmount = type.closeout && breaking ? Mathf.Clamp01((t - breakTime) / 0.6f) : 0f,
                });
            }
        }

        void PlayCrash(WaveType type, float center, WaveDirection direction)
        {
            var sound = type.level switch
            {
                WaveLevel.Beginner => Sound.CrashSmall,
                WaveLevel.Intermediate => Sound.CrashMedium,
                _ => Sound.CrashBig,
            };
            float z = direction switch { WaveDirection.Left => -HalfWidth * 0.5f, WaveDirection.Right => HalfWidth * 0.5f, _ => 0f };
            Sfx.Play(sound, transform.TransformPoint(new Vector3(center, 0f, z)), Mathf.Clamp(type.height / 1.5f, 0.6f, 1f));
            if (crashed.Count > 64) crashed.Clear();
        }

        /// <summary>Where the break point is on the side a surfer rides toward (-1 = toward -z, +1 = toward +z).</summary>
        float BreakPoint(in WaveState state, int direction)
        {
            float startZ = state.direction switch
            {
                WaveDirection.Right => PeakOffset,
                WaveDirection.Left => -PeakOffset,
                _ => 0f,
            };
            return startZ + direction * state.breakDistance;
        }

        /// <summary>0 = clean wave, 1 = fully broken whitewash, at position z along the crest.</summary>
        float Broken(in WaveState state, float z)
        {
            float broken = state.closeoutAmount;
            if (!state.type.closeout && state.breaking)
            {
                const float soft = 1f;
                switch (state.direction)
                {
                    case WaveDirection.Right: // break travels toward -z; everything behind it (higher z) is broken
                        broken = Smooth(BreakPoint(state, -1) - soft, BreakPoint(state, -1) + soft, z);
                        break;
                    case WaveDirection.Left:
                        broken = 1f - Smooth(BreakPoint(state, 1) - soft, BreakPoint(state, 1) + soft, z);
                        break;
                    default:
                        broken = 1f - Smooth(state.breakDistance - soft, state.breakDistance + soft, Mathf.Abs(z));
                        break;
                }
            }
            return Mathf.Max(broken, state.beachBreak);
        }

        float LocalHeight(float x, float z, float t)
        {
            float h = Ripple * Mathf.Sin(x * 0.7f + t * 1.3f) * Mathf.Sin(z * 0.9f + t * 1.1f);
            float edge = z / HalfWidth;
            float edgeScale = 1f - edgeDrop * edge * edge;

            foreach (var state in states)
            {
                float broken = Broken(state, z);
                float d = x - state.center;
                float front = state.type.frontWidth * (1f + 0.8f * broken);
                float w = d > 0f ? front : state.back;
                float shape = Mathf.Exp(-(d / w) * (d / w));

                // The clean face peaks up a little right next to the break point, like a real pocket.
                float pocket = 0f;
                if (state.breaking && !state.type.closeout) pocket = PocketMask(state, z) * (1f - broken) * 0.15f;

                float amplitude = state.amplitude * Mathf.Lerp(1f, state.type.whitewashHeight, broken) * (1f + pocket);
                h += amplitude * edgeScale * shape;

                // A shallow dip in front of the clean face: the wave draws water up into itself, so it stands taller.
                if (troughDepth > 0f && d > 0f)
                {
                    float ahead = (d - 1.6f * front) / front;
                    h -= troughDepth * amplitude * (1f - broken) * edgeScale * Mathf.Exp(-ahead * ahead);
                }

                // Whitewash is lumpy.
                if (broken > 0.01f)
                    h += broken * shape * state.amplitude * 0.12f * (Mathf.PerlinNoise(x * 0.9f - t * 2f, z * 0.9f) - 0.5f);
            }
            return h;
        }

        float PocketMask(in WaveState state, float z)
        {
            if (state.direction == WaveDirection.Peak)
            {
                float dz = Mathf.Abs(z) - state.breakDistance;
                return Mathf.Exp(-(dz / 3f) * (dz / 3f));
            }
            float pz = BreakPoint(state, state.direction == WaveDirection.Right ? -1 : 1);
            return Mathf.Exp(-((z - pz) / 3f) * ((z - pz) / 3f));
        }

        /// <summary>Vertex color: R = foam, G = extra brightness on clean wave faces.</summary>
        Color Foam(float x, float z)
        {
            float foam = 0f, face = 0f;
            foreach (var state in states)
            {
                float d = x - state.center;
                float broken = Broken(state, z);
                // Beginner whitewater is small but should still read as a wall of white foam.
                float strength = Mathf.Clamp01(state.amplitude / 1.5f) + (state.type.closeout ? 0.7f : 0.3f);

                float crest = Mathf.Exp(-((d - 0.4f) / (1.1f + 1.4f * broken)) * ((d - 0.4f) / (1.1f + 1.4f * broken)));
                float trail = d < 0f ? Mathf.Exp(d / 7f) * 0.75f : 0f;
                float lipSpray = state.breaking && !state.type.closeout
                    ? PocketMask(state, z) * Mathf.Exp(-(d / 1.2f) * (d / 1.2f)) * 0.7f
                    : 0f;
                foam = Mathf.Max(foam, broken * Mathf.Max(crest, trail) * strength, lipSpray);
                face = Mathf.Max(face, (1f - broken) * Mathf.Exp(-((d - 0.9f) / 1.2f) * ((d - 0.9f) / 1.2f)) * 0.35f);
            }
            return new Color(Mathf.Clamp01(foam), face, 0f, 1f);
        }

        /// <summary>The curling lip of barreling waves: a curved sheet thrown forward from the crest near the break point.</summary>
        void BuildLip(float t)
        {
            if (lip == null) return;
            lipVertices.Clear();
            lipColors.Clear();
            lipTriangles.Clear();

            foreach (var state in states)
            {
                if (!state.type.barrel || !state.breaking || state.beachBreak > 0.5f) continue;
                if (state.direction != WaveDirection.Left) AddLipStrip(state, -1, t);
                if (state.direction != WaveDirection.Right) AddLipStrip(state, 1, t);
            }

            lip.Clear();
            if (lipTriangles.Count == 0) return;
            lip.SetVertices(lipVertices);
            lip.SetColors(lipColors);
            lip.SetTriangles(lipTriangles, 0);
            lip.RecalculateNormals();
            lip.RecalculateBounds();
        }

        /// <summary>
        /// One side of a barreling wave's lip, swept along the crest from the break point toward the part that is
        /// still about to break. Each cross-section is a curve walked out from the crest top: it bends down more
        /// toward its tip (a "curvature profile"), and how long and how hooked it is follows the section's age:
        /// barely pitching far ahead of the break, thrown out and hooked over (the tube) near it, landing at it.
        /// </summary>
        void AddLipStrip(in WaveState state, int direction, float t)
        {
            const int along = 20, across = 10;
            float bp = BreakPoint(state, direction);
            if (Mathf.Abs(bp) > HalfWidth - 0.5f) return;
            float limit = HalfWidth - 0.3f;
            int first = lipVertices.Count;
            float grown = Mathf.Clamp01(state.breakDistance / 1.5f); // the first lip grows in as the wave starts to break

            for (int i = 0; i <= along; i++)
            {
                float s = i / (float)along;
                float z = Mathf.Clamp(bp + direction * s * lipLength, -limit, limit);
                // Section age: 0.8 = landing at the break point, 0 = just starting to pitch at the far end.
                float tau = 0.8f * (1f - s) * grown;
                LipShape(tau, out float lengthA, out float hookDeg);
                float reach = lengthA * state.amplitude;
                float top = LocalHeight(state.center, z, t);
                var p = new Vector3(state.center - 0.3f, top, z);
                for (int k = 0; k <= across; k++)
                {
                    float u = k / (float)across;
                    if (k > 0)
                    {
                        // Bends down more toward the tip (power > 1: straight throw, then it hooks).
                        float theta = Mathf.Deg2Rad * hookDeg * Mathf.Pow(u, 1.4f);
                        p += new Vector3(Mathf.Cos(theta), -Mathf.Sin(theta), 0f) * (reach / across);
                    }
                    lipVertices.Add(p);
                    // Foam at the thrown tip, a little along the lip as it curls.
                    lipColors.Add(new Color(k == across ? 0.9f : 0.12f * tau, 0.35f + 0.25f * u, 0f, 1f));
                }
            }

            int row = across + 1;
            for (int i = 0; i < along; i++)
            for (int k = 0; k < across; k++)
            {
                int a = first + i * row + k;
                lipTriangles.Add(a); lipTriangles.Add(a + row); lipTriangles.Add(a + 1);
                lipTriangles.Add(a + 1); lipTriangles.Add(a + row); lipTriangles.Add(a + row + 1);
            }
        }

        /// <summary>Lip length (in wave heights) and how far its tip hooks over (degrees) at section age tau (0..0.8).</summary>
        static void LipShape(float tau, out float length, out float hookDegrees)
        {
            // Keyframes: tau 0 -> (0.05, 10), 0.2 -> (0.35, 40), 0.5 -> (0.8, 90), 0.8 -> (1.1, 125).
            if (tau < 0.2f) { float k = tau / 0.2f; length = Mathf.Lerp(0.05f, 0.35f, k); hookDegrees = Mathf.Lerp(10f, 40f, k); }
            else if (tau < 0.5f) { float k = (tau - 0.2f) / 0.3f; length = Mathf.Lerp(0.35f, 0.8f, k); hookDegrees = Mathf.Lerp(40f, 90f, k); }
            else { float k = Mathf.Clamp01((tau - 0.5f) / 0.3f); length = Mathf.Lerp(0.8f, 1.1f, k); hookDegrees = Mathf.Lerp(90f, 125f, k); }
        }

        /// <summary>Foam blobs thrown up where the wave is breaking.</summary>
        void EmitSpray()
        {
            if (spray == null) return;
            foreach (var state in states)
            {
                if (!state.breaking && state.beachBreak <= 0f) continue;
                float strength = Mathf.Clamp01(state.amplitude / 2f) + 0.3f;
                if (state.type.closeout || state.beachBreak > 0.3f)
                {
                    // Whitewater all along the crest.
                    int count = RandomCount(sprayRate * 1.5f * strength);
                    for (int i = 0; i < count; i++) EmitBlob(state, Random.Range(-HalfWidth + 1f, HalfWidth - 1f), 0, strength);
                }
                else
                {
                    if (state.direction != WaveDirection.Left) EmitAtBreak(state, -1, strength);
                    if (state.direction != WaveDirection.Right) EmitAtBreak(state, 1, strength);
                }
            }
        }

        void EmitAtBreak(in WaveState state, int direction, float strength)
        {
            float bp = BreakPoint(state, direction);
            if (Mathf.Abs(bp) > HalfWidth - 0.5f) return;

            // Offshore wind: mist blown back off the lip of the clean wave ahead of the break, like a mane.
            if (DailyEvents.TodayWind == Wind.Offshore)
            {
                int mist = RandomCount(sprayRate * 0.6f * strength);
                for (int i = 0; i < mist; i++)
                {
                    float z = bp + direction * Random.Range(0.5f, 12f);
                    if (Mathf.Abs(z) > HalfWidth - 0.5f) continue;
                    float x = state.center + 0.3f;
                    var local = new Vector3(x, LocalHeight(x, z, Now) + 0.15f, z);
                    var velocity = new Vector3(-Random.Range(2.5f, 5f), Random.Range(1f, 2.5f), Random.Range(-0.4f, 0.4f));
                    spray.Emit(new ParticleSystem.EmitParams
                    {
                        position = transform.TransformPoint(local),
                        velocity = transform.TransformDirection(velocity),
                        startSize = Random.Range(0.25f, 0.55f),
                        startLifetime = Random.Range(0.5f, 1f),
                    }, 1);
                }
            }
            int count = RandomCount(sprayRate * strength);
            for (int i = 0; i < count; i++) EmitBlob(state, bp - direction * Random.Range(0f, 2.5f), direction, strength);

            // Spit: air squeezed out of the barrel blows mist puffs out of the tube mouth, ahead of the break.
            if (state.type.barrel && state.breakDistance > 1.5f)
            {
                int puffs = RandomCount(spitRate * strength);
                for (int i = 0; i < puffs; i++)
                {
                    float z = bp + direction * Random.Range(0.3f, 1.4f);
                    float x = state.center + state.amplitude * 0.35f;
                    var local = new Vector3(x, LocalHeight(x, z, Now) + state.amplitude * 0.45f, z);
                    var velocity = new Vector3(Random.Range(0.5f, 2f), Random.Range(0.3f, 1.2f), direction * Random.Range(4f, 7f));
                    spray.Emit(new ParticleSystem.EmitParams
                    {
                        position = transform.TransformPoint(local),
                        velocity = transform.TransformDirection(velocity),
                        startSize = Random.Range(0.35f, 0.7f),
                        startLifetime = Random.Range(0.5f, 0.9f),
                    }, 1);
                }
            }
        }

        void EmitBlob(in WaveState state, float z, int direction, float strength)
        {
            float x = state.center + Random.Range(-0.4f, 0.8f);
            var local = new Vector3(x, LocalHeight(x, z, Now) + 0.1f, z);
            var velocity = new Vector3(Random.Range(1f, 3.5f), Random.Range(1.5f, 4f) * strength, direction * Random.Range(0f, 1.5f));
            spray.Emit(new ParticleSystem.EmitParams
            {
                position = transform.TransformPoint(local),
                velocity = transform.TransformDirection(velocity),
                startSize = Random.Range(0.18f, 0.45f) * strength,
                startLifetime = Random.Range(0.6f, 1.2f),
            }, 1);
        }

        /// <summary>Throws spray blobs from a world point (surfboard rails on carves, landings), at about perSecond.</summary>
        public void EmitRiderSpray(Vector3 position, Vector3 velocity, float perSecond, float size = 0.25f)
        {
            if (spray == null) return;
            int count = RandomCount(perSecond);
            for (int i = 0; i < count; i++) SprayBurst(position, velocity, size);
        }

        /// <summary>A single blob of spray (for one-off bursts like a landing splash).</summary>
        public void SprayBurst(Vector3 position, Vector3 velocity, float size = 0.25f)
        {
            if (spray == null) return;
            spray.Emit(new ParticleSystem.EmitParams
            {
                position = position + Random.insideUnitSphere * 0.15f,
                velocity = velocity + Random.insideUnitSphere * (velocity.magnitude * 0.3f + 0.3f),
                startSize = size * Random.Range(0.6f, 1.3f),
                startLifetime = Random.Range(0.4f, 0.8f),
            }, 1);
        }

        int RandomCount(float perSecond)
        {
            float expected = perSecond * Dt;
            int count = Mathf.FloorToInt(expected);
            return count + (Random.value < expected - count ? 1 : 0);
        }

        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float WaveCenter(ActiveWave wave, float t) => -wave.type.backWidth + (t - wave.start) * wave.type.speed;

        void BuildMesh()
        {
            int nx = Mathf.Max(2, Mathf.CeilToInt(length * verticesPerMeter) + 1);
            int nz = Mathf.Max(2, Mathf.CeilToInt(width * verticesPerMeter) + 1);

            gridX = nx;
            gridZ = nz;
            baseVertices = new Vector3[nx * nz];
            colors = new Color[nx * nz];
            foamMemory = new float[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int iz = 0; iz < nz; iz++)
            for (int ix = 0; ix < nx; ix++)
            {
                float u = ix / (nx - 1f), v = iz / (nz - 1f);
                baseVertices[iz * nx + ix] = new Vector3(u * length, 0f, (v - 0.5f) * width);
                uvs[iz * nx + ix] = new Vector2(u, v);
            }

            var triangles = new int[(nx - 1) * (nz - 1) * 6];
            int k = 0;
            for (int iz = 0; iz < nz - 1; iz++)
            for (int ix = 0; ix < nx - 1; ix++)
            {
                int i = iz * nx + ix;
                triangles[k++] = i; triangles[k++] = i + nx; triangles[k++] = i + 1;
                triangles[k++] = i + 1; triangles[k++] = i + nx; triangles[k++] = i + nx + 1;
            }

            vertices = (Vector3[])baseVertices.Clone();
            mesh = new Mesh { name = "WaveWater", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
