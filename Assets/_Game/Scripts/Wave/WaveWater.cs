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

        [Header("Barrel lip and spray")]
        [SerializeField] MeshFilter lipMesh;
        [SerializeField] float lipLength = 6f;
        [SerializeField] ParticleSystem spray;
        [Tooltip("Foam blobs per second at each break point.")]
        [SerializeField] float sprayRate = 45f;

        struct ActiveWave
        {
            public float start;
            public WaveType type;
            public WaveDirection direction;
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
        Mesh mesh;
        Mesh lip;
        Vector3[] baseVertices;
        Vector3[] vertices;
        Color[] colors;
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

        public void SpawnWave(WaveLevel level, WaveDirection direction = WaveDirection.Peak)
        {
            waves.Add(new ActiveWave { start = Time.time, type = TypeOf(level), direction = direction });
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

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = baseVertices[i];
                v.y = LocalHeight(v.x, v.z, t);
                vertices[i] = v;
                colors[i] = Foam(v.x, v.z);
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

        void AddLipStrip(in WaveState state, int direction, float t)
        {
            const int along = 18, across = 10;
            float bp = BreakPoint(state, direction);
            if (Mathf.Abs(bp) > HalfWidth - 0.5f) return;
            float limit = HalfWidth - 0.3f;
            int first = lipVertices.Count;

            for (int i = 0; i <= along; i++)
            {
                float s = i / (float)along;
                float z = Mathf.Clamp(bp + direction * s * lipLength, -limit, limit);
                float curl = Mathf.Pow(1f - s, 0.6f) * Mathf.Clamp01(state.breakDistance / 1.5f);
                float top = LocalHeight(state.center, z, t);
                float radius = state.amplitude * 0.42f;
                float maxAngle = Mathf.Deg2Rad * 145f * curl;
                for (int k = 0; k <= across; k++)
                {
                    float a = maxAngle * k / across;
                    var p = new Vector3(state.center - 0.25f + radius * Mathf.Sin(a), top - radius * (1f - Mathf.Cos(a)), z);
                    lipVertices.Add(p);
                    lipColors.Add(new Color(k == across ? 0.9f : 0.1f * curl, 0.35f, 0f, 1f));
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
