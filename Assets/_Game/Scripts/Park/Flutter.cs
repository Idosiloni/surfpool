using UnityEngine;

namespace SurfPool
{
    /// <summary>Flags and bunting moving in the sea breeze: a small swing around their hanging point.</summary>
    public class Flutter : MonoBehaviour
    {
        [SerializeField] Vector3 axis = Vector3.up;
        [SerializeField] float degrees = 12f;
        [SerializeField] float speed = 2.2f;

        Quaternion rest;
        float phase;

        void Start()
        {
            rest = transform.localRotation;
            phase = Random.Range(0f, 10f);
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            float swing = Mathf.Sin(t) * 0.7f + Mathf.Sin(t * 2.3f) * 0.3f;
            transform.localRotation = rest * Quaternion.AngleAxis(swing * degrees, axis);
        }
    }
}
