using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfPool
{
    /// <summary>
    /// Finds what you're looking at and lets E (or left click) use it. Forgiving aim: if the exact center of the
    /// screen misses, the closest usable thing near the crosshair counts (a fat "sphere" ray), as long as no wall
    /// is in front of it.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera playerCamera;
        [SerializeField] float reach = 3.5f;
        [Tooltip("How far off the crosshair (meters) something can be and still be picked.")]
        [SerializeField] float aimAssist = 0.4f;

        readonly RaycastHit[] hits = new RaycastHit[24];

        public IInteractable Target { get; private set; }

        void Update()
        {
            Target = null;
            if (InputBlocker.Blocked) return;
            if (PlayerSurfing.Instance != null && PlayerSurfing.Instance.IsSurfing) return;
            Target = FindTarget();

            if (Target == null) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            bool pressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                        || (mouse != null && Cursor.lockState == CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame);
            if (pressed) Target.Interact();
            bool held = (keyboard != null && keyboard.eKey.isPressed)
                     || (mouse != null && Cursor.lockState == CursorLockMode.Locked && mouse.leftButton.isPressed);
            if (held && Target is IHoldInteractable holdable) holdable.Hold(Time.deltaTime);
        }

        IInteractable FindTarget()
        {
            var origin = playerCamera.transform.position;
            var forward = playerCamera.transform.forward;
            var ray = new Ray(origin, forward);

            // Exact aim first.
            float blockedAt = reach + aimAssist;
            if (Physics.Raycast(ray, out var direct, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                var exact = Usable(direct.collider);
                if (exact != null) return exact;
                blockedAt = direct.distance; // a wall or counter: nothing behind it counts
            }

            // Then anything usable close to the crosshair, nearest to the aim line wins.
            int count = Physics.SphereCastNonAlloc(ray, aimAssist, hits, reach, ~0, QueryTriggerInteraction.Ignore);
            IInteractable best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.distance <= 0f || hit.distance > blockedAt + 0.5f) continue;
                if (hit.collider.GetComponentInParent<FirstPersonController>() != null) continue;
                var usable = Usable(hit.collider);
                if (usable == null) continue;
                Vector3 toPoint = hit.collider.bounds.ClosestPoint(origin + forward * hit.distance) - origin;
                float offAim = Vector3.Cross(forward, toPoint).magnitude; // distance from the aim line
                float score = offAim + hit.distance * 0.05f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = usable;
                }
            }
            return best;
        }

        /// <summary>The usable thing on this collider, if it has something to say right now.</summary>
        static IInteractable Usable(Collider collider)
        {
            var usable = collider.GetComponentInParent<IInteractable>();
            return usable != null && !string.IsNullOrEmpty(usable.Prompt) ? usable : null;
        }
    }
}
