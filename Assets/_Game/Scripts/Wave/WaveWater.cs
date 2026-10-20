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
        [Tooltip("Where a peeling wave starts breaking, measured from the middle of the pool (m).")]
        [SerializeField] float peakOffset = 4f;
        [Tooltip("How much smaller the wave is at the pool walls than in the middle (0 to 1).")]
        [Range(0f, 1f)] [SerializeField] float edgeDrop = 0.2f;

        [Header("Breaking near the beach")]
        [Tooltip("How far down the pool (0 to 1) every wave turns to whitewash and shrinks.")]
        [Range(0f, 1f)] [SerializeField] float beachBreakStart = 0.75f;
        [Tooltip("Size of the wave when it reaches the beach, compared to full size.")]
        [Range(0f, 1f)] [SerializeField] float breakEndScale = 0.3f;

        [Header("Calm water")]
        [SerializeField] float rippleHeight = 0.04f;

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

        public int ActiveWaves => waves.Count;
        public float Length => length;
        public float Width => width;
        float HalfWidth => width * 0.5f;

        /// <summary>A wave in the pool right now: its id (the time it was sent) and its type.</summary>
        public (float id, WaveLevel level) GetWave(int index) => (waves[index].start, waves[index].type.level);

        /// <summary>Local x position of a wave's crest right now.</summary>
        public float CurrentWaveCenter(float waveId)
        {
            foreach (var wave in waves)
                if (wave.start == waveId) return WaveCenter(wave, Time.time);
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
            breakAt = type.breakAt,
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

        public void SpawnWave(WaveLevel level, WaveDirection direction = WaveDirection.Peak, FamousWave preset = null)
        {
            var type = Scaled(TypeOf(level));
            if (preset != null)
            {
                type.height *= preset.size;
                type.peelSpeed *= preset.peel;
            }
            waves.Add(new ActiveWave { start = Time.time, type = type, direction = direction, famous = preset?.name });
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
            return transform.TransformPoint(new Vector3(local.x, LocalHeight(local.x, local.z, Time.time), local.z)).y;
        }

        void Update()
        {
            float t = Time.time;
            waves.RemoveAll(wave => WaveCenter(wave, t) > length + wave.type.backWidth * 3f);
            UpdateStates(t);

            // Foam stays where it was made and fades over foamLinger seconds, leaving lacy trails behind the
            // wave. Vertex color: R = foam, G = face brightness, B = shallowness (beach end), A = foam freshness
            // (1 = just made, toward 0 = old foam the shader breaks into lace).
            float fade = Time.deltaTime / Mathf.Max(0.1f, foamLinger);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = baseVertices[i];
                v.y = LocalHeight(v.x, v.z, t);
                vertices[i] = v;
                var c = Foam(v.x, v.z);
                float fresh = c.r;
                float memory = foamMemory[i] = Mathf.Max(fresh, foamMemory[i] - fade);
                c.r = memory;
                c.b = Smooth(0.55f, 1f, v.x / length);
                c.a = memory > 0.001f ? fresh / memory : 1f;
                colors[i] = c;
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            BuildLip(t);
            EmitSpray();
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
                WaveDirection.Right => peakOffset,
                WaveDirection.Left => -peakOffset,
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
            float h = rippleHeight * Mathf.Sin(x * 0.7f + t * 1.3f) * Mathf.Sin(z * 0.9f + t * 1.1f);
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
                    var local = new Vector3(x, LocalHeight(x, z, Time.time) + state.amplitude * 0.45f, z);
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
            var local = new Vector3(x, LocalHeight(x, z, Time.time) + 0.1f, z);
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

        static int RandomCount(float perSecond)
        {
            float expected = perSecond * Time.deltaTime;
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
