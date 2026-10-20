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

        [Header("Wave shape")]
        [Tooltip("How tall the wave gets above the calm water.")]
        [SerializeField] float waveHeight = 2.0f;
        [Tooltip("How fast the wave travels down the pool (m/s).")]
        [SerializeField] float waveSpeed = 7f;
        [Tooltip("Width of the steep front face. Smaller = steeper.")]
        [SerializeField] float frontWidth = 2.0f;
        [Tooltip("Width of the gentle back of the wave.")]
        [SerializeField] float backWidth = 8f;
        [Tooltip("How much smaller the wave is at the pool walls than in the middle (0 to 1).")]
        [Range(0f, 1f)] [SerializeField] float edgeDrop = 0.2f;

        [Header("Breaking near the beach")]
        [Tooltip("How far down the pool (0 to 1) the wave starts to break and shrink.")]
        [Range(0f, 1f)] [SerializeField] float breakStart = 0.75f;
        [Tooltip("Size of the wave when it reaches the beach, compared to full size.")]
        [Range(0f, 1f)] [SerializeField] float breakEndScale = 0.3f;

        [Header("Calm water")]
        [SerializeField] float rippleHeight = 0.04f;

        readonly List<float> waveStartTimes = new();
        Mesh mesh;
        Vector3[] baseVertices;
        Vector3[] vertices;

        public int ActiveWaves => waveStartTimes.Count;

        void Awake()
        {
            BuildMesh();
        }

        public void SpawnWave()
        {
            waveStartTimes.Add(Time.time);
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
            waveStartTimes.RemoveAll(start => WaveCenter(start, t) > length + backWidth * 3f);

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

            foreach (float start in waveStartTimes)
            {
                float center = WaveCenter(start, t);
                float progress = center / length;

                float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.05f, 0.15f, progress));
                float breaking = Mathf.InverseLerp(breakStart, 1f, progress);
                float amplitude = waveHeight * rise * Mathf.Lerp(1f, breakEndScale, breaking);
                float back = backWidth * (1f + breaking);

                float d = x - center;
                float w = d > 0f ? frontWidth : back;
                h += amplitude * edgeScale * Mathf.Exp(-(d / w) * (d / w));
            }
            return h;
        }

        float WaveCenter(float startTime, float t) => -backWidth + (t - startTime) * waveSpeed;

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
