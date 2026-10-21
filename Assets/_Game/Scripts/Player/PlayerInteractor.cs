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
        [Tooltip("How far away you can whistle at a sneak-in or throw a rescue ring.")]
        [SerializeField] float whistleRange = 45f;
        [Tooltip("How far the skimmer net reaches over the water.")]
        [SerializeField] float skimmerReach = 7.5f;

        readonly RaycastHit[] hits = new RaycastHit[24];

        public IInteractable Target { get; private set; }

        void Update()
        {
            Target = null;
            if (InputBlocker.Blocked) return;
            if (PlayerSurfing.Instance != null && PlayerSurfing.Instance.IsSurfing) return;
            if (BuildMode.Active) return;
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
            // (Skips the player's own capsule: looking steeply down at a counter, the ray can clip its top.)
            float blockedAt = reach + aimAssist;
            if (RaycastIgnoringPlayer(ray, out var direct))
            {
                var exact = Usable(direct.collider);
                if (exact != null) return exact;
                blockedAt = direct.distance; // a wall or counter: nothing behind it counts
            }

            // Aiming right at a sneak-in or a surfer in trouble (far away, in the water) beats the aim assist below.
            var far = FarSurfer() ?? FarDebris();
            if (far != null) return far;

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

        /// <summary>
        /// A sneak-in or a surfer in trouble near the crosshair, up to <see cref="whistleRange"/> away (you can't reach
        /// people in the water, so you point and whistle, or throw a ring). Something tall in between blocks it.
        /// </summary>
        IInteractable FarSurfer()
        {
            Surfer best = null;
            float bestOff = 0.07f;
            var cam = playerCamera;
            foreach (var surfer in Surfer.All)
            {
                if (!surfer.WantsFarAttention) continue;
                Vector3 point = surfer.transform.position + Vector3.up * 1.2f;
                Vector3 v = cam.WorldToViewportPoint(point);
                if (v.z <= 0f || v.z > whistleRange) continue;
                float off = new Vector2(v.x - 0.5f, (v.y - 0.5f) * 0.6f).magnitude;
                if (off >= bestOff) continue;
                if (!CanSee(point, surfer)) continue;
                best = surfer;
                bestOff = off;
            }
            if (best != null) return best;
            // Shoplifters running off with gear.
            Shopper thief = null;
            foreach (var shopper in Shopper.All)
            {
                if (!shopper.IsStealing) continue;
                Vector3 v = cam.WorldToViewportPoint(shopper.transform.position + Vector3.up * 1.1f);
                if (v.z <= 0f || v.z > whistleRange) continue;
                float off = new Vector2(v.x - 0.5f, (v.y - 0.5f) * 0.6f).magnitude;
                if (off >= bestOff) continue;
                thief = shopper;
                bestOff = off;
            }
            return thief;
        }

        /// <summary>With the skimmer net: floating junk near the crosshair, up to the net's reach.</summary>
        IInteractable FarDebris()
        {
            if (!SkimmerRack.Holding) return null;
            PoolDebris best = null;
            float bestOff = 0.08f;
            var cam = playerCamera;
            foreach (var debris in PoolDebris.All)
            {
                Vector3 v = cam.WorldToViewportPoint(debris.transform.position);
                if (v.z <= 0f || v.z > skimmerReach) continue;
                float off = new Vector2(v.x - 0.5f, (v.y - 0.5f) * 0.6f).magnitude;
                if (off >= bestOff) continue;
                best = debris;
                bestOff = off;
            }
            return best;
        }

        /// <summary>
        /// Nothing tall you can see (a kiosk, a wall, the machine) in the way. The ground, the deck, the pool rim and
        /// invisible blockers don't count:
        /// from the deck a surfer down in the pool is hidden by the edge, but their speech bubble isn't.
        /// </summary>
        bool CanSee(Vector3 point, Surfer surfer)
        {
            Vector3 from = playerCamera.transform.position, to = point - from;
            int count = Physics.RaycastNonAlloc(new Ray(from, to.normalized), hits, to.magnitude - 0.8f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var collider = hits[i].collider;
                if (collider.GetComponentInParent<FirstPersonController>() != null || collider.transform.IsChildOf(surfer.transform)) continue;
                if (hits[i].point.y < 0.4f) continue;
                if (collider.GetComponentInChildren<Renderer>() == null) continue; // invisible blockers (pool edge rails)
                return false;
            }
            return true;
        }

        bool RaycastIgnoringPlayer(Ray ray, out RaycastHit nearest, float distance = -1f)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(ray, hits, distance > 0f ? distance : reach, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].distance >= best || hits[i].collider.GetComponentInParent<FirstPersonController>() != null) continue;
                best = hits[i].distance;
                nearest = hits[i];
            }
            return best < float.MaxValue;
        }

        /// <summary>The usable thing on this collider, if it has something to say right now.</summary>
        static IInteractable Usable(Collider collider)
        {
            var usable = collider.GetComponentInParent<IInteractable>();
            return usable != null && !string.IsNullOrEmpty(usable.Prompt) ? usable : null;
        }
    }
}
