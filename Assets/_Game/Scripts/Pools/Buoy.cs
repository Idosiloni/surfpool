using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A floating marker buoy in a pool (lineup and inside markers): rides up and down on the water surface and tips
    /// with the slope of passing waves, swaying a little on its own.
    /// </summary>
    public class Buoy : MonoBehaviour
    {
        [SerializeField] float floatHeight = 0.08f;
        [SerializeField] float maxTilt = 28f;

        WaveWater water;
        Vector3 home;
        float seed;

        void Start()
        {
            var pool = GetComponentInParent<PoolArea>(true);
            water = pool != null ? pool.Water : null;
            home = transform.position;
            seed = Random.value * 10f;
        }

        void LateUpdate()
        {
            if (water == null) return;
            const float d = 0.3f;
            float h = water.SurfaceHeightAt(home);
            float sx = water.SurfaceHeightAt(home + Vector3.right * d) - water.SurfaceHeightAt(home - Vector3.right * d);
            float sz = water.SurfaceHeightAt(home + Vector3.forward * d) - water.SurfaceHeightAt(home - Vector3.forward * d);
            float tiltX = Mathf.Clamp(Mathf.Atan2(sz, 2f * d) * Mathf.Rad2Deg, -maxTilt, maxTilt);
            float tiltZ = Mathf.Clamp(-Mathf.Atan2(sx, 2f * d) * Mathf.Rad2Deg, -maxTilt, maxTilt);
            float t = Time.time + seed;
            var target = Quaternion.Euler(tiltX + Mathf.Sin(t * 1.7f) * 3f, t * 6f, tiltZ + Mathf.Sin(t * 1.3f) * 3f);
            transform.SetPositionAndRotation(new Vector3(home.x, h + floatHeight, home.z),
                Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-6f * Time.deltaTime)));
        }
    }
}
