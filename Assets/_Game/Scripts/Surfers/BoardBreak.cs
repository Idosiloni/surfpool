using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Snapped boards: a hard wipeout can break a board in two. The board's mesh is cut across the middle (cached per
    /// mesh), and the nose and tail halves fly up spinning, splash down, drift on the water and sink away.
    /// </summary>
    public static class BoardBreak
    {
        static readonly Dictionary<Mesh, (Mesh nose, Mesh tail)> halves = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            halves.Clear();
            ForceNext = false;
        }

        /// <summary>
        /// Chance a wipeout snaps the board, by the wave it happened on (whitewater never breaks a board) and whether
        /// it was off an air.
        /// </summary>
        public static float Chance(WaveLevel wave, bool fromAir)
        {
            if (ForceNext)
            {
                ForceNext = false;
                return 1f;
            }
            return (wave switch { WaveLevel.Beginner => 0f, WaveLevel.Intermediate => 0.04f, _ => 0.1f }) + (fromAir ? 0.12f : 0f);
        }

        /// <summary>For tests: the next wipeout (anyone's) snaps the board.</summary>
        public static bool ForceNext { get; set; }

        /// <summary>What the park loses when a rental board snaps.</summary>
        public const int RentalLoss = 30;

        /// <summary>A surfer who snapped their own board buys a new one at your gear shop (if it's open).</summary>
        public static int NewBoardPrice(WaveLevel skill) => skill switch { WaveLevel.Beginner => 60, WaveLevel.Intermediate => 110, _ => 180 };

        /// <summary>Breaks this board: hides it and throws two halves. <paramref name="water"/> is where they float.</summary>
        public static void Snap(Transform board, WaveWater water)
        {
            var filter = board.GetComponentInChildren<MeshFilter>();
            var renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (filter == null || renderer == null || filter.sharedMesh == null) return;
            // A mesh we can't read (import Read/Write off) can't be cut: throw two squashed copies instead.
            bool cut = filter.sharedMesh.isReadable;
            var (nose, tail) = cut ? Halves(filter.sharedMesh) : (filter.sharedMesh, filter.sharedMesh);
            var materials = renderer.sharedMaterials;
            var t = filter.transform;
            Throw(nose, materials, t, water, t.forward * 1.8f, !cut);
            Throw(tail, materials, t, water, -t.forward * 1.4f, !cut);
            board.gameObject.SetActive(false);
            Sfx.Play(Sound.CrashSmall, t.position, 0.9f, 1.4f); // the crack
            Sfx.Play(Sound.Splash, t.position, 0.8f);
        }

        static void Throw(Mesh mesh, Material[] materials, Transform from, WaveWater water, Vector3 push, bool squash)
        {
            var go = new GameObject("Board Half");
            go.transform.SetPositionAndRotation(from.position, from.rotation);
            var scale = from.lossyScale;
            if (squash) scale.z *= 0.5f;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            go.AddComponent<BrokenHalf>().Launch(push + Vector3.up * Random.Range(3.5f, 5f), water);
        }

        /// <summary>The mesh cut across its length (z): triangles go to the half their middle is in.</summary>
        static (Mesh, Mesh) Halves(Mesh source)
        {
            if (halves.TryGetValue(source, out var cached)) return cached;
            var result = (Cut(source, true), Cut(source, false));
            halves[source] = result;
            return result;
        }

        static Mesh Cut(Mesh source, bool nose)
        {
            var vertices = source.vertices;
            var normals = source.normals;
            var uv = source.uv;
            float middle = source.bounds.center.z;
            var mesh = new Mesh { name = source.name + (nose ? " Nose" : " Tail") };
            mesh.vertices = vertices;
            if (normals.Length == vertices.Length) mesh.normals = normals;
            if (uv.Length == vertices.Length) mesh.uv = uv;
            mesh.subMeshCount = source.subMeshCount;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var tris = source.GetTriangles(sub);
                var kept = new List<int>(tris.Length / 2);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    float z = (vertices[tris[i]].z + vertices[tris[i + 1]].z + vertices[tris[i + 2]].z) / 3f;
                    // A jagged break: the cut wobbles across the board instead of being a clean line.
                    float x = vertices[tris[i]].x;
                    float cut = middle + Mathf.Sin(x * 23f) * 0.06f;
                    if (z > cut == nose) kept.AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
                }
                mesh.SetTriangles(kept, sub);
            }
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>One half of a snapped board: flies, splashes down, bobs on the water, then sinks and is gone.</summary>
    public class BrokenHalf : MonoBehaviour
    {
        Vector3 velocity, spinAxis;
        float spinSpeed, born;
        bool landed;
        WaveWater water;

        public void Launch(Vector3 startVelocity, WaveWater onWater)
        {
            velocity = startVelocity;
            water = onWater;
            spinAxis = Random.onUnitSphere;
            spinSpeed = Random.Range(400f, 800f);
            born = Time.time;
        }

        void Update()
        {
            float age = Time.time - born;
            float surface = water != null ? water.SurfaceHeightAt(transform.position) : transform.position.y - 10f;
            if (!landed)
            {
                velocity += Physics.gravity * Time.deltaTime;
                transform.position += velocity * Time.deltaTime;
                transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);
                if (velocity.y < 0f && transform.position.y <= surface)
                {
                    landed = true;
                    if (water != null) RideFeel.Splash(water, transform.position, 8, 1.6f);
                }
            }
            else
            {
                // Floating, rocking, drifting toward the beach a little; after a while it sinks out of sight.
                float sink = Mathf.Clamp01((age - 6f) / 2f);
                var p = transform.position;
                p += Vector3.right * 0.3f * Time.deltaTime;
                p.y = surface + 0.02f + Mathf.Sin(age * 3f) * 0.03f - sink * 0.6f;
                transform.position = p;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, Mathf.Sin(age * 2f) * 8f), Time.deltaTime * 3f);
            }
            if (age > 8.5f) Destroy(gameObject);
        }
    }
}
