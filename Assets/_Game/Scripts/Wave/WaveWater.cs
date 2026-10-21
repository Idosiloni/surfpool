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
            new() { level = WaveLevel.Beginner, height = 1.0f, speed = 5f, frontWidth = 3.2f, backWidth = 3f,
                breakAt = 0.22f, closeout = true, whitewashHeight = 0.5f },
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
        [Range(0f, 1f)] [SerializeField] float beachBreakStart = 0.85f;
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
        [SerializeField] float foamLinger = 3f;
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
            public float size; // XL multiplier (1 = normal)
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
            public float closeoutAge;    // closeouts: seconds since they started breaking (-1 = not yet)
            public float seedA, seedB;   // closeouts: the two spots along the crest where they start to spill
            public float steepen;        // 0 = just launched (a fat, soft swell) .. 1 = breaking (the face at its steepest)
            public WaveShape shape;
            public float peel;           // 0..1 how far along its line the break has run (the sections)
            public float hollow;         // 0..1 in the barrel section: the pocket stands up and the lip throws
            public float born;           // seconds since it left the machine (for the boil at the wall)
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

        // ---- backwash: each wave that hits the beach sends a low ripple back across the pool, fading as it goes ----
        const float BackwashHeight = 0.07f, BackwashSpeed = 3.2f, BackwashLife = 9f, BackwashWidth = 2.5f;
        readonly List<(float start, float height)> backwash = new();

        /// <summary>Height of the reflected ripples at x (same for every z).</summary>
        float BackwashAt(float x, float t)
        {
            float h = 0f;
            foreach (var (start, height) in backwash)
            {
                float age = t - start;
                if (age < 0f || age > BackwashLife) continue;
                float cx = length * 0.92f - age * BackwashSpeed;
                float d = (x - cx) / BackwashWidth;
                if (d * d > 9f) continue;
                h += height * (1f - age / BackwashLife) * Mathf.Exp(-d * d);
            }
            return h;
        }
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
                barrel = (state.type.barrel || state.hollow > 0.5f) && state.breaking;
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
            WaveShape shape = WaveShape.Standard, float size = 1f)
        {
            var type = Scaled(TypeOf(level));
            // XL: a taller wave with a proportionally bigger face (its lip, foam ball and spray all scale with height).
            type.height *= size;
            type.frontWidth *= Mathf.Lerp(1f, size, 0.7f);
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
            var spawned = new ActiveWave { start = Now - headStart, type = type, direction = direction, famous = preset?.name, shape = shape, size = size };
            waves.Add(spawned);
            if (recording) recorded.Add(spawned);
        }

        /// <summary>
        /// A board's wake: paints foam into the water around a world point; it then fades into lace like any foam left
        /// behind (riders call this every frame). amount 0..1.
        /// </summary>
        public void PaintFoam(Vector3 worldPosition, float radius, float amount)
        {
            if (foamMemory == null || gridX < 2) return;
            var local = transform.InverseTransformPoint(worldPosition);
            float dx = length / (gridX - 1), dz = width / (gridZ - 1);
            int cx = Mathf.RoundToInt(local.x / dx), cz = Mathf.RoundToInt((local.z + width * 0.5f) / dz);
            int rx = Mathf.CeilToInt(radius / dx), rz = Mathf.CeilToInt(radius / dz);
            for (int iz = Mathf.Max(0, cz - rz); iz <= Mathf.Min(gridZ - 1, cz + rz); iz++)
            for (int ix = Mathf.Max(0, cx - rx); ix <= Mathf.Min(gridX - 1, cx + rx); ix++)
            {
                float ddx = ix * dx - local.x, ddz = iz * dz - width * 0.5f - local.z;
                float k = 1f - (ddx * ddx + ddz * ddz) / (radius * radius);
                if (k <= 0f) continue;
                int i = iz * gridX + ix;
                foamMemory[i] = Mathf.Max(foamMemory[i], amount * k);
            }
        }

        /// <summary>How much bigger than normal this wave is (1 = normal, 1.15 Big, 1.3 Giant).</summary>
        public float SizeOf(float waveId)
        {
            foreach (var wave in waves)
                if (wave.start == waveId) return wave.size > 0f ? wave.size : 1f;
            return 1f;
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
                    if (WaveCenter(waves[i], t) <= RemoveAt(waves[i].type)) continue;
                    added += chopPerWave * (waves[i].type.level == WaveLevel.Beginner ? 0.4f : waves[i].type.level == WaveLevel.Pro ? 1.3f : 1f);
                    // It bounces off the beach: a low ripple rolls back toward the machine.
                    backwash.Add((t, waves[i].type.height * BackwashHeight));
                    if (backwash.Count > 8) backwash.RemoveAt(0);
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
            mesh.uv2 = tubeShade; // tube shadow for the shader (TEXCOORD1)
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
        float[] rowEdge = new float[0], rowSin = new float[0];
        float[] colSin = new float[0], colShallow = new float[0], colBackwash = new float[0];
        int[] colStates = new int[0], colCount = new int[0];
        RowTerms[] rowTerms = new RowTerms[0];
        Vector2[] tubeShade = new Vector2[0];
        ColTerms[] colTerms = new ColTerms[0];

        void UpdateSurface(float t, float fade)
        {
            int nx = gridX, nz = gridZ, count = states.Count;
            if (stateArray.Length < count) stateArray = new WaveState[count * 2];
            states.CopyTo(stateArray);
            if (rowEdge.Length != nz) { rowEdge = new float[nz]; rowSin = new float[nz]; }
            if (colSin.Length != nx) { colSin = new float[nx]; colShallow = new float[nx]; colCount = new int[nx]; colBackwash = new float[nx]; }
            if (rowTerms.Length < count * nz) rowTerms = new RowTerms[count * nz * 2];
            if (tubeShade.Length != vertices.Length) tubeShade = new Vector2[vertices.Length];
            if (colStates.Length < count * nx)
            {
                colStates = new int[count * nx * 2];
                colTerms = new ColTerms[count * nx * 2];
            }

            for (int iz = 0; iz < nz; iz++)
            {
                float z = baseVertices[iz * nx].z, edge = z / HalfWidth;
                rowEdge[iz] = 1f - edgeDrop * edge * edge;
                rowSin[iz] = Mathf.Sin(z * 0.9f + t * 1.1f);
                for (int s = 0; s < count; s++) rowTerms[s * nz + iz] = Row(stateArray[s], z);
            }
            for (int ix = 0; ix < nx; ix++)
            {
                float x = baseVertices[ix].x;
                colSin[ix] = Ripple * Mathf.Sin(x * 0.7f + t * 1.3f);
                colBackwash[ix] = backwash.Count > 0 ? BackwashAt(x, t) : 0f;
                colShallow[ix] = Smooth(0.55f, 1f, x / length);
                int n = 0;
                for (int s = 0; s < count; s++)
                {
                    ref readonly var st = ref stateArray[s];
                    float d = x - st.center;
                    // Past these distances every term (shape, trough, foam, lip spray) is ~0.
                    if (d < -Mathf.Max(3f * st.back, 32f) || d > 12f * st.type.frontWidth + 2f) continue;
                    int c = ix * count + n++;
                    colStates[c] = s;
                    colTerms[c] = Col(st, x);
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
                    float h = colSin[ix] * zSin + colBackwash[ix], foam = 0f, face = 0f, shade = 0f;
                    for (int k = 0; k < colCount[ix]; k++)
                    {
                        int c = ix * count + k, s = colStates[c];
                        Sample(stateArray[s], rowTerms[s * nz + iz], colTerms[c], v.x, v.z, t, edgeScale, out float height, out float f, out float g, out float sh);
                        h += height;
                        foam = Mathf.Max(foam, f);
                        face = Mathf.Max(face, g);
                        shade = Mathf.Max(shade, sh);
                    }
                    v.y = h;
                    vertices[i] = v;
                    float fresh = Mathf.Clamp01(foam);
                    float memory = foamMemory[i] = Mathf.Max(fresh, foamMemory[i] - fade);
                    colors[i] = new Color(memory, face, colShallow[ix], memory > 0.001f ? fresh / memory : 1f);
                    tubeShade[i] = new Vector2(shade, 0f);
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
                    breakDistance = breaking ? PeelDistance(wave, t - breakTime) : 0f,
                    closeoutAge = type.closeout && breaking ? t - breakTime : -1f,
                    steepen = breaking ? 1f : Mathf.Clamp01((t - wave.start) / Mathf.Max(0.1f, breakTime - wave.start)),
                    born = t - wave.start - headStart,
                    shape = wave.shape,
                    peel = breaking && !type.closeout ? Mathf.Clamp01(PeelDistance(wave, t - breakTime) / Mathf.Max(1f, PeelLength(wave.direction))) : 0f,
                    hollow = breaking ? Hollowness(type, wave.shape, true,
                        Mathf.Clamp01(PeelDistance(wave, t - breakTime) / Mathf.Max(1f, PeelLength(wave.direction)))) : 0f,
                    seedA = -HalfWidth * (0.5f + 0.3f * (Hash(wave.start) - 0.5f)),
                    seedB = HalfWidth * (0.5f + 0.3f * (Hash(wave.start + 17.3f) - 0.5f)),
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

        // ---- how a wave breaks (docs/research/wave-breaking.md): broken water has an AGE. Just broken = a fat foam
        // ball nearly as tall as the wave; then it settles into a low, steep-fronted bore (whitewater) with a short tail,
        // and after a few seconds into flat soup with lace foam. The unbroken shoulder is tallest by the pocket and
        // mellows far away. Closeouts (beginner whitewater) spill from two spots and unzip sideways. ----

        const float CloseoutUnzip = 6f; // m/s a closeout's break runs along the crest

        static float Hash(float v)
        {
            float x = Mathf.Sin(v * 12.9898f) * 43758.5453f;
            return x - Mathf.Floor(x);
        }

        /// <summary>Seconds since this spot along the crest broke (below 0 = not broken yet; -1 before the wave breaks).</summary>
        float Age(in WaveState state, float z)
        {
            if (!state.breaking) return -1f;
            float tau;
            if (state.type.closeout)
                tau = state.closeoutAge - Mathf.Min(Mathf.Abs(z - state.seedA), Mathf.Abs(z - state.seedB)) / CloseoutUnzip;
            else
            {
                float peel = Mathf.Max(0.1f, state.type.peelSpeed);
                tau = state.direction switch
                {
                    WaveDirection.Right => (z - BreakPoint(state, -1)) / peel, // break travels toward -z; higher z broke first
                    WaveDirection.Left => (BreakPoint(state, 1) - z) / peel,
                    _ => (state.breakDistance - Mathf.Abs(z)) / peel,
                };
            }
            return Mathf.Clamp(tau, -1f, 20f);
        }

        /// <summary>0 = clean wave, 1 = broken whitewash, at position z along the crest (tau = its Age there).</summary>
        float Broken(in WaveState state, float z, float tau)
        {
            if (!state.breaking) return state.type.closeout ? state.beachBreak : 0f;
            float soft = state.type.closeout ? 0.1f : 1f / Mathf.Max(0.1f, state.type.peelSpeed); // about 1 m either side
            float broken = Smooth(-soft, soft, tau);
            // Beginner whitewater rides all the way in; bigger waves reform into a shoulder near the beach instead.
            return state.type.closeout ? Mathf.Max(broken, state.beachBreak) : broken;
        }

        /// <summary>Meters from the break point along the unbroken side (above 0 = not broken yet).</summary>
        float Ahead(in WaveState state, float z) => state.direction switch
        {
            WaveDirection.Right => BreakPoint(state, -1) - z,
            WaveDirection.Left => z - BreakPoint(state, 1),
            _ => Mathf.Abs(z) - state.breakDistance,
        };

        /// <summary>What one wave does at one row (z) this frame.</summary>
        struct RowTerms
        {
            public float broken, hb, boreMult, lumps, ball, frontFoam, trailFoam, shoulder, frontG, pocket, mask;
            public float tube, tubeReach; // under a thrown lip here (0..1) and how far out the lip reaches (m)
        }

        /// <summary>What one wave does at one column (x) this frame: the shapes that only need the distance to the crest.</summary>
        struct ColTerms
        {
            public float d, backG, tail, boreFront, ballShape, bandShape, spillShape, trailShape, lip, faceShape;
        }

        RowTerms Row(in WaveState state, float z)
        {
            var r = new RowTerms();
            float tau = Age(state, z), age = Mathf.Max(0f, tau);
            r.broken = Broken(state, z, tau);
            // Height follows more slowly than the foam: the face slumps over ~1 s of crest instead of a 2 m cliff.
            r.hb = r.broken * (state.breaking ? Smooth(0f, 1.2f, tau) : 1f);
            float ww = state.type.whitewashHeight, ball = state.type.closeout ? 0.6f : 0.75f;
            r.boreMult = 0.6f * ww + 0.4f * ww * Mathf.Exp(-age / 6f) + Mathf.Max(0f, ball - ww) * Mathf.Exp(-age / 1.2f);
            r.lumps = Mathf.Exp(-age / 4f);
            r.ball = 1f - Smooth(0.8f, 1.6f, tau);
            r.frontFoam = 0.85f * (0.4f + 0.6f * Mathf.Exp(-age / 4f));
            r.trailFoam = 0.45f * Mathf.Exp(-age / 3f);
            float far = 0f;
            if (state.breaking && !state.type.closeout)
            {
                float ahead = Ahead(state, z);
                if (ahead > 0f) far = (1f - Mathf.Exp(-(ahead / 12f) * (ahead / 12f))) * Smooth(0f, 8f, state.breakDistance);
            }
            r.shoulder = 1f - 0.3f * far;
            // A wave leaves the machine as a fat, soft swell and its face sharpens until it breaks.
            r.frontG = state.type.frontWidth * (1f + 0.5f * far) * Mathf.Lerp(1.8f, 1f, Mathf.SmoothStep(0f, 1f, state.steepen));
            r.mask = state.breaking && !state.type.closeout ? PocketMask(state, z) : 0f;
            if (HasLip(state) && state.breaking && state.beachBreak <= 0.5f)
            {
                if (state.direction != WaveDirection.Left) TubeAt(state, -1, z, ref r);
                if (state.direction != WaveDirection.Right) TubeAt(state, 1, z, ref r);
            }
            r.pocket = r.mask * (1f - r.broken) * (0.25f + 0.2f * state.hollow);
            // In the barrel section the face by the pocket stands up even steeper.
            r.frontG *= Mathf.Lerp(1f, 0.7f, state.hollow * r.mask);
            return r;
        }

        /// <summary>The lip strip on one side (as AddLipStrip builds it): is z under it, and how far does it reach.</summary>
        void TubeAt(in WaveState state, int direction, float z, ref RowTerms r)
        {
            float length = LipLengthOf(state);
            float s = (z - BreakPoint(state, direction)) * direction / length;
            if (s <= 0f || s >= 1f) return;
            float tau = 0.8f * (1f - s) * Mathf.Clamp01(state.breakDistance / 1.5f);
            LipShape(tau, out float lengthA, out _);
            lengthA *= LipScale(state);
            // Only where the lip is out far enough to roof the face, fading toward the far end where it barely pitches.
            float covered = Smooth(0.3f, 0.75f, lengthA) * (1f - Smooth(0.8f, 1f, s));
            if (covered <= r.tube) return;
            r.tube = covered;
            r.tubeReach = lengthA * state.amplitude;
        }

        /// <summary>Barreling waves throw a lip everywhere; others only in the barrel section.</summary>
        static bool HasLip(in WaveState state) => state.type.barrel || state.hollow > 0.15f;

        /// <summary>How big the thrown lip is: barreling waves throw more in the barrel section, others only there.</summary>
        static float LipScale(in WaveState state) => state.type.barrel ? 0.8f + 0.35f * state.hollow : state.hollow;

        /// <summary>How far ahead of the break the lip starts to pitch (about 1.5 s of peel; docs/research/wave-look-2.md).</summary>
        float LipLengthOf(in WaveState state) => Mathf.Max(lipLength, state.type.peelSpeed * 1.5f);

        static ColTerms Col(in WaveState state, float x)
        {
            var c = new ColTerms();
            float d = c.d = x - state.center;
            float tail = (state.type.closeout ? 2.5f : 3f) * (1f - 0.5f * state.beachBreak);
            float boreFront = state.type.closeout ? 1.2f : Mathf.Clamp(0.45f * state.type.frontWidth, 0.9f, 1.4f);
            if (d <= 0f)
            {
                c.backG = Mathf.Exp(-(d / state.back) * (d / state.back));
                c.tail = Mathf.Exp(d / tail);
                c.trailShape = Mathf.Exp(d / 1.8f);
            }
            else c.boreFront = Mathf.Exp(-(d / boreFront) * (d / boreFront));
            float bd = (d - 0.4f) / 1.4f, fd = (d - 0.2f) / 0.9f, face = (d - 0.9f) / 1.2f;
            c.ballShape = Mathf.Exp(-bd * bd);
            c.bandShape = Mathf.Exp(-fd * fd);
            float sd = (d + 0.3f) / 0.95f; // spilling foam sits on the crest and just behind it, the face stays green
            c.spillShape = Mathf.Exp(-sd * sd);
            c.lip = Mathf.Exp(-(d / 1.2f) * (d / 1.2f)) * 0.7f;
            c.faceShape = Mathf.Exp(-face * face) * 0.35f;
            return c;
        }

        /// <summary>
        /// One wave's height, foam and face brightness at a point: the green wave blended into the bore by how broken it
        /// is there. LocalHeight/Foam and the fast UpdateSurface both use this, so riders and the mesh always agree.
        /// </summary>
        void Sample(in WaveState state, in RowTerms r, in ColTerms c, float x, float z, float t, float edgeScale,
            out float height, out float foam, out float face, out float shade)
        {
            float d = c.d;
            float shapeG = d <= 0f ? c.backG : Mathf.Exp(-(d / r.frontG) * (d / r.frontG));
            float shapeB = d <= 0f ? c.tail : c.boreFront;
            float ampG = state.amplitude * r.shoulder * (1f + r.pocket);
            float ampB = state.amplitude * r.boreMult;
            height = edgeScale * Mathf.Lerp(ampG * shapeG, ampB * shapeB, r.hb);
            // A shallow dip in front of the clean face: the wave draws water up into itself, so it stands taller.
            if (troughDepth > 0f && d > 0f)
            {
                float ahead = (d - 1.6f * r.frontG) / r.frontG;
                height -= troughDepth * ampG * (1f - r.hb) * edgeScale * Mathf.Exp(-ahead * ahead);
            }
            // Fresh whitewash is lumpy; old soup goes flat.
            if (r.broken > 0.01f && shapeB > 0.01f)
                height += r.broken * r.lumps * shapeB * state.amplitude * 0.12f * (Mathf.PerlinNoise(x * 0.9f - t * 2f, z * 0.9f) - 0.5f);

            // Foam: a fat ball where it just broke, a thin band on the bore's front, a short tail. The lace left behind
            // comes from foam memory (it stays where it was made while the bore moves on).
            float strength = Mathf.Clamp01(state.amplitude / 2f) + (state.type.closeout ? 0.3f : 0.2f);
            // Small (beginner) waves spill: foam tumbles over the top while a little green face stays below it.
            float ballShape = state.type.closeout ? c.spillShape : c.ballShape;
            float bandShape = state.type.closeout ? c.spillShape : c.bandShape;
            float fresh = r.broken * Mathf.Max(r.ball * ballShape, Mathf.Max(r.frontFoam * bandShape, r.trailFoam * c.trailShape)) * strength;
            fresh = Mathf.Max(fresh, state.beachBreak * 0.6f * c.bandShape); // swash: it whitens as it washes up the beach
            // Launch: the water boils white against the machine wall for a moment as the wave is pushed out.
            if (state.born < 1.8f)
            {
                float boil = (1f - Smooth(0.6f, 1.8f, state.born)) * Mathf.Exp(-((x - 0.8f) / 1.6f) * ((x - 0.8f) / 1.6f));
                fresh = Mathf.Max(fresh, boil * 0.9f);
                height += boil * 0.22f * state.amplitude * edgeScale * (0.6f + 0.4f * Mathf.PerlinNoise(z * 0.6f, t * 3f));
            }
            foam = Mathf.Max(fresh, r.mask * c.lip);
            // Height up the wave (vertex G, 0 = trough, 1 = crest): the shader colors the face by it, dark low down
            // (deep water seen through) to bright aqua at the thin top lit from behind (docs/research/wave-look-2.md).
            // Tiny swells stay plain water.
            // The tube: the face under a thrown lip is in its shadow, from the crest out to where the lip lands.
            shade = r.tube > 0f
                ? r.tube * Smooth(-0.8f, 0.2f, d) * (1f - Smooth(r.tubeReach * 0.75f, r.tubeReach * 1.1f, d)) * (1f - r.broken)
                : 0f;
            face = Mathf.Lerp(shapeG * r.shoulder, shapeB * r.boreMult / Mathf.Max(0.3f, state.type.whitewashHeight + 0.3f), r.hb)
                * Mathf.Clamp01(state.amplitude * 0.8f);
        }

        float LocalHeight(float x, float z, float t)
        {
            float h = Ripple * Mathf.Sin(x * 0.7f + t * 1.3f) * Mathf.Sin(z * 0.9f + t * 1.1f) + BackwashAt(x, t);
            float edge = z / HalfWidth;
            float edgeScale = 1f - edgeDrop * edge * edge;
            foreach (var state in states)
            {
                Sample(state, Row(state, z), Col(state, x), x, z, t, edgeScale, out float height, out _, out _, out _);
                h += height;
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
            float edge = z / HalfWidth, edgeScale = 1f - edgeDrop * edge * edge;
            foreach (var state in states)
            {
                Sample(state, Row(state, z), Col(state, x), x, z, Now, edgeScale, out _, out float f, out float g, out _);
                foam = Mathf.Max(foam, f);
                face = Mathf.Max(face, g);
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
            lipShade.Clear();
            innerShade.Clear();

            foreach (var state in states)
            {
                if (!HasLip(state) || !state.breaking || state.beachBreak > 0.5f) continue;
                if (state.direction != WaveDirection.Left) AddLipStrip(state, -1, t);
                if (state.direction != WaveDirection.Right) AddLipStrip(state, 1, t);
            }

            lip.Clear();
            if (lipTriangles.Count == 0) return;
            lip.SetVertices(lipVertices);
            lip.SetColors(lipColors);
            lip.SetUVs(1, lipShade);
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
        readonly List<Vector3> lipInnerVerts = new();
        readonly List<Vector2> lipShade = new(), innerShade = new();
        readonly List<Color> lipInnerColors = new();

        void AddLipStrip(in WaveState state, int direction, float t)
        {
            const int along = 20, across = 10;
            float bp = BreakPoint(state, direction);
            if (Mathf.Abs(bp) > HalfWidth - 0.5f) return;
            float limit = HalfWidth - 0.3f;
            float grown = Mathf.Clamp01(state.breakDistance / 1.5f); // the first lip grows in as the wave starts to break
            // The lip starts to pitch about 1.5 s of peel ahead of where it lands (docs/research/wave-look-2.md).
            float length = LipLengthOf(state);
            int row = across + 1;
            int outer = lipVertices.Count, inner = outer + (along + 1) * row;
            var innerVerts = lipInnerVerts;
            var innerColors = lipInnerColors;
            innerVerts.Clear();
            innerColors.Clear();

            for (int i = 0; i <= along; i++)
            {
                float s = i / (float)along;
                float z = Mathf.Clamp(bp + direction * s * length, -limit, limit);
                // Section age: 0.8 = landing at the break point, 0 = just starting to pitch at the far end.
                float tau = 0.8f * (1f - s) * grown;
                LipShape(tau, out float lengthA, out float hookDeg);
                lengthA *= LipScale(state);
                float reach = lengthA * state.amplitude;
                float top = LocalHeight(state.center, z, t);
                var p = new Vector3(state.center - 0.3f, top, z);
                float theta = 0f;
                for (int k = 0; k <= across; k++)
                {
                    float u = k / (float)across;
                    if (k > 0)
                    {
                        // Bends down more toward the tip (power > 1: straight throw, then it hooks).
                        theta = Mathf.Deg2Rad * hookDeg * Mathf.Pow(u, 1.4f);
                        p += new Vector3(Mathf.Cos(theta), -Mathf.Sin(theta), 0f) * (reach / across);
                    }
                    // The thrown sheet has thickness: thick at the root, thin at the tip. Its inner skin (the roof of
                    // the tube) sits under it toward the curl's center and stays darker.
                    float thick = 0.14f * state.amplitude * (1f - 0.75f * u) * Mathf.Clamp01(lengthA * 3f);
                    var under = p + new Vector3(-Mathf.Sin(theta), -Mathf.Cos(theta), 0f) * thick;
                    lipVertices.Add(p);
                    innerVerts.Add(under);
                    lipShade.Add(new Vector2(0.15f * u, 1f)); // y = 1: lip (the shader can see through it near the chase cam)
                    innerShade.Add(new Vector2(0.9f - 0.4f * u, 1f)); // the roof of the tube, in its own shadow
                    // Foam feathers off the thrown tip; the outer sheet is the thinnest, brightest water on the wave.
                    float foam = k == across ? 0.95f : k == across - 1 ? 0.45f * tau : 0.1f * tau;
                    lipColors.Add(new Color(foam, 0.92f + 0.08f * u, 0f, 1f));
                    innerColors.Add(new Color(foam * 0.8f, 0.15f + 0.3f * u, 0f, 1f));
                }
            }
            lipVertices.AddRange(innerVerts);
            lipColors.AddRange(innerColors);
            lipShade.AddRange(innerShade);
            innerShade.Clear();

            for (int i = 0; i < along; i++)
            for (int k = 0; k < across; k++)
            {
                int a = outer + i * row + k;
                lipTriangles.Add(a); lipTriangles.Add(a + row); lipTriangles.Add(a + 1);
                lipTriangles.Add(a + 1); lipTriangles.Add(a + row); lipTriangles.Add(a + row + 1);
                int b = inner + i * row + k; // the inner skin faces the other way
                lipTriangles.Add(b); lipTriangles.Add(b + 1); lipTriangles.Add(b + row);
                lipTriangles.Add(b + 1); lipTriangles.Add(b + row + 1); lipTriangles.Add(b + row);
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
                if (state.type.closeout)
                {
                    // Spray only where the spill is fresh (it unzips along the crest from two spots).
                    int count = RandomCount(sprayRate * 1.2f * strength);
                    for (int i = 0; i < count; i++)
                    {
                        float z = Random.Range(-HalfWidth + 1f, HalfWidth - 1f), age = Age(state, z);
                        if (age >= 0f && age < 1f) EmitBlob(state, z, 0, strength);
                    }
                }
                else if (state.beachBreak > 0.3f)
                {
                    // A little swash as the reformed shoulder washes up the beach.
                    int count = RandomCount(sprayRate * 0.3f * strength);
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

            // Feathering: just ahead of the break the crest is so steep that a little mist streams off its top.
            int feather = RandomCount(sprayRate * 0.25f * strength);
            for (int i = 0; i < feather; i++)
            {
                float z = bp + direction * Random.Range(0.8f, 5f);
                if (Mathf.Abs(z) > HalfWidth - 0.5f) continue;
                float x = state.center + 0.2f;
                var local = new Vector3(x, LocalHeight(x, z, Now) + 0.1f, z);
                var velocity = new Vector3(-Random.Range(0.5f, 1.5f), Random.Range(1.2f, 2.4f), direction * Random.Range(0f, 0.6f));
                spray.Emit(new ParticleSystem.EmitParams
                {
                    position = transform.TransformPoint(local),
                    velocity = transform.TransformDirection(velocity),
                    startSize = Random.Range(0.14f, 0.3f),
                    startLifetime = Random.Range(0.35f, 0.65f),
                }, 1);
            }

            if (HasLip(state) && state.breakDistance > 1.5f)
            {
                // The lip lands: a white burst thrown up off the flat water out in front of the face.
                LipShape(0.8f, out float landA, out _);
                float landX = state.center + landA * state.amplitude * 0.75f;
                int splash = RandomCount(sprayRate * 0.45f * strength);
                for (int i = 0; i < splash; i++)
                {
                    float z = bp + direction * Random.Range(-0.5f, 1.5f);
                    float x = landX + Random.Range(-0.6f, 0.6f);
                    var local = new Vector3(x, LocalHeight(x, z, Now) + 0.1f, z);
                    var velocity = new Vector3(Random.Range(0.5f, 2.5f), Random.Range(3f, 6.5f) * strength, direction * Random.Range(-1f, 1f));
                    spray.Emit(new ParticleSystem.EmitParams
                    {
                        position = transform.TransformPoint(local),
                        velocity = transform.TransformDirection(velocity),
                        startSize = Random.Range(0.25f, 0.6f) * strength,
                        startLifetime = Random.Range(0.6f, 1.1f),
                    }, 1);
                }
            }

            // Spit: air squeezed out of the barrel blows mist out of the tube mouth, ahead of the break, in puffs.
            if (HasLip(state) && state.breakDistance > 1.5f)
            {
                float pulse = Mathf.Max(0f, Mathf.Sin(Now * 4.2f + state.id)) * 2f;
                int puffs = RandomCount(spitRate * strength * pulse);
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

        /// <summary>
        /// Where the crest is (local x). Full speed over the deep water, then past the beach-break line it feels the
        /// bottom and slows, to about 55% of its speed at the beach: v = c (end - x) / (end - xs), solved exactly so
        /// replays (which rewind the clock) land on the same spot.
        /// </summary>
        float WaveCenter(ActiveWave wave, float t)
        {
            float c = wave.type.speed, x = -wave.type.backWidth + (t - wave.start) * c;
            float xs = beachBreakStart * length;
            if (x <= xs) return x;
            float end = ShoalEnd;
            return xs + (end - xs) * (1f - Mathf.Exp(-(x - xs) / Mathf.Max(0.1f, end - xs)));
        }

        /// <summary>Where a slowing wave would stop (beyond the beach: it gets there at 55% speed).</summary>
        float ShoalEnd => (length - 0.55f * beachBreakStart * length) / 0.45f;

        /// <summary>Gone: well up the beach (the crest never reaches ShoalEnd).</summary>
        float RemoveAt(in WaveType type) => length + Mathf.Min(type.backWidth * 3f, (ShoalEnd - length) * 0.85f);

        /// <summary>The break's length of run: from where it starts to the far wall (a peak: from the middle out).</summary>
        float PeelLength(WaveDirection direction) => direction == WaveDirection.Peak ? HalfWidth : PeakOffset + HalfWidth;

        /// <summary>
        /// How far the break has peeled after `seconds`: steady along the line, then over the last fifth the rest of
        /// the wave goes all at once (the closeout section, three times as fast).
        /// </summary>
        float PeelDistance(ActiveWave wave, float seconds)
        {
            float d = seconds * wave.type.peelSpeed;
            if (wave.type.closeout) return d;
            float end = 0.8f * PeelLength(wave.direction);
            return d <= end ? d : end + (d - end) * 3f;
        }

        /// <summary>The barrel section: about 35-80% along the line the pocket stands up and throws (Mellow waves barely).</summary>
        static float Hollowness(WaveType type, WaveShape shape, bool breaking, float peel)
        {
            if (type.closeout || !breaking) return 0f;
            float h = Smooth(0.35f, 0.48f, peel) * (1f - Smooth(0.7f, 0.8f, peel));
            return shape == WaveShape.Mellow ? h * 0.4f : h;
        }

        /// <summary>Which section this wave's break is in right now (the takeoff for whitewater and missing waves).</summary>
        public WaveSection SectionOf(float waveId)
        {
            foreach (var state in states)
            {
                if (state.id != waveId) continue;
                if (state.type.closeout || !state.breaking) return WaveSection.Takeoff;
                if (state.peel >= 0.8f) return WaveSection.Closeout;
                if (state.hollow > 0.5f || (state.type.barrel && state.peel >= 0.4f)) return WaveSection.Barrel;
                return state.peel < 0.15f ? WaveSection.Takeoff : WaveSection.Wall;
            }
            return WaveSection.Takeoff;
        }

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
