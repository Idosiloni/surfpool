using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfPool
{
    /// <summary>WASD + mouse-look first-person movement. Click to lock the cursor, Esc to free it.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float sprintSpeed = 7.5f;
        [SerializeField] float jumpHeight = 1.0f;
        [SerializeField] float gravity = -20f;
        [SerializeField] float mouseSensitivity = 0.12f;

        CharacterController controller;
        float pitch;
        float verticalVelocity;

        public bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        void Start()
        {
            LockCursor(true);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || InputBlocker.Blocked) return;

            if (keyboard.escapeKey.wasPressedThisFrame) LockCursor(false);
            else if (!CursorLocked && mouse.leftButton.wasPressedThisFrame) LockCursor(true);

            if (CursorLocked) Look(mouse.delta.ReadValue());
            Move(keyboard);
        }

        void Look(Vector2 delta)
        {
            float sensitivity = mouseSensitivity * GameSettings.MouseSensitivity;
            transform.Rotate(0f, delta.x * sensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity * (GameSettings.InvertY ? -1f : 1f), -85f, 85f);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        void Move(Keyboard keyboard)
        {
            if (!controller.enabled) return; // e.g. while something else is moving the player
            var input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1;
            if (keyboard.sKey.isPressed) input.y -= 1;
            if (keyboard.dKey.isPressed) input.x += 1;
            if (keyboard.aKey.isPressed) input.x -= 1;
            input = Vector2.ClampMagnitude(input, 1f);

            float speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (keyboard.spaceKey.wasPressedThisFrame)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
            Footsteps(input.sqrMagnitude > 0.01f ? speed * input.magnitude : 0f, speed > walkSpeed);
        }

        float stepDistance;

        /// <summary>A footstep every stride while walking on the ground; the sound follows the surface (sand, deck, wood).</summary>
        void Footsteps(float speed, bool sprinting)
        {
            if (!controller.isGrounded || speed <= 0f)
            {
                stepDistance = Mathf.Min(stepDistance, 0.4f); // the first step comes quickly when you start walking
                return;
            }
            stepDistance += speed * Time.deltaTime;
            float stride = sprinting ? 2.3f : 1.7f;
            if (stepDistance < stride) return;
            stepDistance = 0f;
            Sfx.Play(SurfaceSound(), null, sprinting ? 1.2f : 1f);
        }

        Sound SurfaceSound()
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down, out var hit, 1f, ~0, QueryTriggerInteraction.Ignore))
                return Sound.StepSand;
            var renderer = hit.collider.GetComponent<Renderer>();
            string material = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.name : hit.collider.name;
            if (material.Contains("Sand")) return Sound.StepSand;
            if (material.Contains("Wood") || material.Contains("Plank") || hit.collider.name.Contains("Pier")) return Sound.StepWood;
            return Sound.StepDeck;
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
