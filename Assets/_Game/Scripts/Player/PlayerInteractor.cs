using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfPool
{
    /// <summary>Raycasts from the center of the screen; E (or left click) uses what you're looking at.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera playerCamera;
        [SerializeField] float reach = 3f;

        public IInteractable Target { get; private set; }

        void Update()
        {
            Target = null;
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit, reach, ~0, QueryTriggerInteraction.Ignore))
                Target = hit.collider.GetComponentInParent<IInteractable>();

            if (Target == null) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            bool pressed = (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                        || (mouse != null && Cursor.lockState == CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame);
            if (pressed) Target.Interact();
        }
    }
}
