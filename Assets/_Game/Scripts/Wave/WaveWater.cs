using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The pool's water surface. A grid mesh whose vertices are moved every frame:
    /// small ambient ripples plus any waves currently rolling down the pool.
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
            new() { level = WaveLevel.Beginner, height = 1.0f, speed = 5f, frontWidth = 3.2f, backWidth = 6f },
            new() { level = WaveLevel.Intermediate, height = 2.0f, speed = 7f, frontWidth = 2.0f, backWidth = 8f },
            new() { level = WaveLevel.Pro, height = 2.8f, speed = 8.5f, frontWidth = 1.4f, backWidth = 9f },
        };
        [Tooltip("How much smaller the wave is at the pool walls than in the middle (0 to 1).")]
        [Range(0f, 1f)] [SerializeField] float edgeDrop = 0.2f;

        [Header("Breaking near the beach")]
        [Tooltip("How far down the pool (0 to 1) the wave starts to break and shrink.")]
        [Range(0f, 1f)] [SerializeField] float breakStart = 0.75f;
        [Tooltip("Size of the wave when it reaches the beach, compared to full size.")]
        [Range(0f, 1f)] [SerializeField] float breakEndScale = 0.3f;

        [Header("Calm water")]
        [SerializeField] float rippleHeight = 0.04f;

        struct ActiveWave
        {
            public float start;
            public WaveType type;
        }

        readonly List<ActiveWave> waves = new();
        Mesh mesh;
        Vector3[] baseVertices;
        Vector3[] vertices;

        public int ActiveWaves => waves.Count;
        public float Length => length;
        public float Width => width;

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

        void Awake()
        {
            BuildMesh();
        }

        public void SpawnWave(WaveLevel level)
        {
            waves.Add(new ActiveWave { start = Time.time, type = TypeOf(level) });
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

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = baseVertices[i];
                v.y = LocalHeight(v.x, v.z, t);
                vertices[i] = v;
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        float LocalHeight(float x, float z, float t)
        {
            float h = rippleHeight * Mathf.Sin(x * 0.7f + t * 1.3f) * Mathf.Sin(z * 0.9f + t * 1.1f);

            float edge = z / (width * 0.5f);
            float edgeScale = 1f - edgeDrop * edge * edge;

            foreach (var wave in waves)
            {
                var type = wave.type;
                float center = WaveCenter(wave, t);
                float progress = center / length;

                float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.05f, 0.15f, progress));
                float breaking = Mathf.InverseLerp(breakStart, 1f, progress);
                float amplitude = type.height * rise * Mathf.Lerp(1f, breakEndScale, breaking);
                float back = type.backWidth * (1f + breaking);

                float d = x - center;
                float w = d > 0f ? type.frontWidth : back;
                h += amplitude * edgeScale * Mathf.Exp(-(d / w) * (d / w));
            }
            return h;
        }

        static float WaveCenter(ActiveWave wave, float t) => -wave.type.backWidth + (t - wave.start) * wave.type.speed;

        void BuildMesh()
        {
            int nx = Mathf.Max(2, Mathf.CeilToInt(length * verticesPerMeter) + 1);
            int nz = Mathf.Max(2, Mathf.CeilToInt(width * verticesPerMeter) + 1);

            baseVertices = new Vector3[nx * nz];
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
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
