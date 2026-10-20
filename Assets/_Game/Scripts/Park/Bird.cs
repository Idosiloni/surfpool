using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A seagull circling over the park: glides in wide loops that drift up and down, with a few lazy wing beats now
    /// and then. The seagull calls (in <see cref="Sfx"/>) come from a bird you can see.
    /// </summary>
    public class Bird : MonoBehaviour
    {
        public static readonly List<Bird> All = new();

        [SerializeField] Vector3 center = new(30f, 0f, 20f);
        [SerializeField] float radius = 30f;
        [SerializeField] float height = 16f;
        [Tooltip("Degrees per second around the loop (negative = the other way).")]
        [SerializeField] float turnSpeed = 14f;
        [SerializeField] float phase;
        [SerializeField] Transform body;

        Vector3 bodyScale;
        float flapUntil, nextFlap;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            if (body == null) body = transform;
            bodyScale = body.localScale;
            nextFlap = Time.time + Random.Range(0f, 4f);
        }

        void Update()
        {
            float angle = phase + Time.time * turnSpeed;
            float rad = angle * Mathf.Deg2Rad;
            // The loop is squashed and wobbles so the birds don't look like they're on a turntable.
            float r = radius * (1f + 0.18f * Mathf.Sin(rad * 2f + phase));
            var position = center + new Vector3(Mathf.Cos(rad) * r, height + 2.5f * Mathf.Sin(rad * 1.5f + phase), Mathf.Sin(rad) * r * 0.8f);
            var forward = position - transform.position;
            transform.position = position;
            if (forward.sqrMagnitude > 1e-5f)
            {
                float roll = Mathf.Sign(turnSpeed) * 22f;
                transform.rotation = Quaternion.LookRotation(forward.normalized) * Quaternion.Euler(0f, 0f, roll);
            }

            if (Time.time >= nextFlap)
            {
                flapUntil = Time.time + Random.Range(0.8f, 1.6f);
                nextFlap = flapUntil + Random.Range(2.5f, 7f);
            }
            float flap = Time.time < flapUntil ? Mathf.Sin(Time.time * 18f) : 0f;
            // Squashing the wingspan and lifting the tips reads as a wing beat from the ground.
            body.localScale = new Vector3(bodyScale.x * (1f - 0.25f * Mathf.Abs(flap)), bodyScale.y * (1f + 0.6f * flap), bodyScale.z);
        }

        /// <summary>Position of a random visible bird, for a seagull call. False if there are none.</summary>
        public static bool TryRandom(out Vector3 position)
        {
            position = default;
            if (All.Count == 0) return false;
            position = All[Random.Range(0, All.Count)].transform.position;
            return true;
        }
    }
}
