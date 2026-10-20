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
            if (keyboard == null || mouse == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame) LockCursor(false);
            else if (!CursorLocked && mouse.leftButton.wasPressedThisFrame) LockCursor(true);

            if (CursorLocked) Look(mouse.delta.ReadValue());
            Move(keyboard);
        }

        void Look(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * mouseSensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -85f, 85f);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }

        void Move(Keyboard keyboard)
        {
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
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
