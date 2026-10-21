using UnityEngine;

namespace SurfPool
{
    /// <summary>Scales an object in from nothing with a little overshoot (a topping landing, a fresh item appearing).</summary>
    public class PopIn : MonoBehaviour
    {
        Vector3 target;
        float start;
        const float Duration = 0.28f;

        public static T On<T>(T component) where T : Component
        {
            component.gameObject.AddComponent<PopIn>();
            return component;
        }

        void Awake()
        {
            target = transform.localScale;
            start = Time.time;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            float t = (Time.time - start) / Duration;
            if (t >= 1f)
            {
                transform.localScale = target;
                Destroy(this);
                return;
            }
            // Ease out with a small overshoot.
            float s = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f);
            transform.localScale = target * s;
        }
    }
}
