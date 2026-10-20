using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Something that cruises around the park on a loop: boats and jet skis on the ocean, the blimp and the helicopter
    /// in the sky. Follows a smooth closed path, faces where it's going, bobs and banks into turns, can spin a rotor,
    /// and can carry a looping engine sound (volume follows the ambience slider).
    /// </summary>
    public class Cruiser : MonoBehaviour
    {
        [Tooltip("World positions of the loop, in order (the path is smoothed between them).")]
        [SerializeField] Vector3[] points;
        [SerializeField] float speed = 6f;
        [Tooltip("Where on the loop it starts (0..1).")]
        [SerializeField] float startAt;
        [Header("Feel")]
        [SerializeField] float bobHeight = 0.15f;
        [SerializeField] float bobSpeed = 1.2f;
        [SerializeField] float rock = 3f;
        [Tooltip("Degrees of roll per unit of turn (positive leans into turns like a jet ski or helicopter).")]
        [SerializeField] float bank = 8f;
        [SerializeField] Transform spinner;
        [SerializeField] Vector3 spinAxis = Vector3.up;
        [SerializeField] float spinSpeed = 900f;
        [Header("Sound")]
        [SerializeField] AudioSource engine;
        [Range(0f, 1f)] [SerializeField] float engineVolume = 0.5f;

        float[] lengths;
        float total, distance, roll;

        void Start()
        {
            if (points == null || points.Length < 3) { enabled = false; return; }
            lengths = new float[points.Length];
            total = 0f;
            for (int i = 0; i < points.Length; i++)
            {
                lengths[i] = Vector3.Distance(points[i], points[(i + 1) % points.Length]);
                total += lengths[i];
            }
            distance = Mathf.Repeat(startAt, 1f) * total;
            Place(0f);
        }

        void Update()
        {
            distance = Mathf.Repeat(distance + speed * Time.deltaTime, total);
            Place(Time.deltaTime);
            if (spinner != null) spinner.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
            if (engine != null) engine.volume = engineVolume * Sfx.AmbienceVolume * (Time.timeScale == 0f ? 0f : 1f);
        }

        void Place(float dt)
        {
            var position = PointAt(distance);
            var ahead = PointAt(distance + 4f);
            var further = PointAt(distance + 12f);
            var dir = ahead - position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            var dir2 = further - ahead;
            dir2.y = 0f;
            float turn = Vector3.SignedAngle(dir, dir2, Vector3.up);
            roll = dt > 0f ? Mathf.Lerp(roll, -turn * bank / 10f, dt * 1.5f) : -turn * bank / 10f;

            float time = Time.time + startAt * 17f;
            position.y += Mathf.Sin(time * bobSpeed) * bobHeight;
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(dir.normalized) *
                                 Quaternion.Euler(Mathf.Sin(time * bobSpeed * 0.8f) * rock * 0.5f, 0f, roll + Mathf.Sin(time * bobSpeed * 0.6f) * rock);
        }

        /// <summary>Point on the smoothed loop at a distance along it (Catmull-Rom through the points).</summary>
        Vector3 PointAt(float d)
        {
            d = Mathf.Repeat(d, total);
            int i = 0;
            while (i < lengths.Length - 1 && d > lengths[i])
            {
                d -= lengths[i];
                i++;
            }
            float t = lengths[i] > 0f ? Mathf.Clamp01(d / lengths[i]) : 0f;
            int n = points.Length;
            Vector3 p0 = points[(i - 1 + n) % n], p1 = points[i], p2 = points[(i + 1) % n], p3 = points[(i + 2) % n];
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
        }
    }
}
