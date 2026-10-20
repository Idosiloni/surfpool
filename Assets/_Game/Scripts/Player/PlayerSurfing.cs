using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfPool
{
    /// <summary>
    /// Lets the player surf their own pool. Paddle with W (steer by looking), Space to pop up when a wave reaches
    /// you, A/D to carve along the wave, W/S to move up or down the face, Space for an air, F to send a wave
    /// from the water, Q to get out. Rides earn style points, a small sponsor bonus and a better park rating.
    /// </summary>
    public class PlayerSurfing : MonoBehaviour
    {
        public static PlayerSurfing Instance { get; private set; }

        enum State { Off, Paddling, Riding, Wipeout }

        [SerializeField] FirstPersonController walker;
        [SerializeField] CharacterController body;
        [SerializeField] Transform cameraPivot;
        [SerializeField] GameObject surfBoard;
        [SerializeField] float paddleSpeed = 3f;
        [SerializeField] float carveSpeed = 7f;
        [SerializeField] float mouseSensitivity = 0.12f;

        State state;
        PoolArea pool;
        float rideWave;
        float rideOffset;
        float rideZ;
        float rideStart;
        float airStart = -10f;
        float wipeoutUntil;
        int score;
        int tricks;
        float pitch;
        float catchUntil = -1f;

        public bool IsSurfing => state != State.Off;

        /// <summary>Controls hint for the HUD while surfing.</summary>
        public string Hint => state switch
        {
            State.Paddling => "W paddle  ·  mouse steer  ·  SPACE stand up when a wave reaches you  ·  F send a wave  ·  Q get out",
            State.Riding => $"A/D carve  ·  W/S up and down the face  ·  SPACE air      Score: {score}",
            State.Wipeout => "Wipeout! Hold on...",
            _ => null,
        };

        void Awake() => Instance = this;

        /// <summary>Jump into the water of the given pool with your own board.</summary>
        public void Enter(PoolArea target)
        {
            if (target == null || state != State.Off) return;
            pool = target;
            state = State.Paddling;
            walker.enabled = false;
            body.enabled = false;
            surfBoard.SetActive(true);
            Vector3 start = pool.Lineup.position + Vector3.forward * 2f;
            start.y = pool.Water.SurfaceHeightAt(start);
            transform.position = start;
            transform.rotation = Quaternion.LookRotation(Vector3.right);
            pitch = 10f;
            Notifications.Post($"Surf's up! You're in the {pool.DisplayName}.");
        }

        public void Exit()
        {
            if (state == State.Off) return;
            state = State.Off;
            surfBoard.SetActive(false);
            var path = pool.PathToPool;
            transform.position = path[path.Length - 1].position + Vector3.up * 0.05f;
            transform.rotation = Quaternion.LookRotation(Vector3.right);
            cameraPivot.localPosition = new Vector3(0, 1.65f, 0);
            body.enabled = true;
            walker.enabled = true;
        }

        void Update()
        {
            if (state == State.Off || InputBlocker.Blocked) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;

            if (Cursor.lockState == CursorLockMode.Locked) Look(mouse.delta.ReadValue());
            else if (mouse.leftButton.wasPressedThisFrame) FirstPersonController.LockCursor(true);
            if (keyboard.escapeKey.wasPressedThisFrame) FirstPersonController.LockCursor(false);
            if (keyboard.qKey.wasPressedThisFrame) { Exit(); return; }
            if (keyboard.fKey.wasPressedThisFrame) pool.Machine.TryTriggerWave();

            switch (state)
            {
                case State.Paddling: Paddle(keyboard); break;
                case State.Riding: Ride(keyboard); break;
                case State.Wipeout: Wipeout(); break;
            }
        }

        void Look(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * mouseSensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -60f, 70f);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        void Paddle(Keyboard keyboard)
        {
            cameraPivot.localPosition = new Vector3(0, 0.6f, 0);
            Vector3 forward = transform.forward;
            forward.y = 0;
            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += forward.normalized;
            if (keyboard.sKey.isPressed) move -= forward.normalized * 0.5f;
            Vector3 next = ClampToPool(transform.position + move * (paddleSpeed * Time.deltaTime));
            next.y = pool.Water.SurfaceHeightAt(next) + 0.05f;
            transform.position = next;

            // Pressing Space early keeps trying for a moment, so you don't need perfect timing.
            if (keyboard.spaceKey.wasPressedThisFrame) catchUntil = Time.time + 2f;
            if (catchUntil > 0f)
            {
                if (TryCatchWave()) catchUntil = -1f;
                else if (Time.time > catchUntil)
                {
                    catchUntil = -1f;
                    Notifications.Post("No wave reached you. Wait for one (or press F to send one), then press Space.");
                }
            }
        }

        /// <summary>Asks to catch the next wave that reaches you within 2 seconds.</summary>
        public void RequestCatch() => catchUntil = Time.time + 2f;

        bool TryCatchWave()
        {
            var water = pool.Water;
            float x = water.transform.InverseTransformPoint(transform.position).x;
            for (int i = 0; i < water.ActiveWaves; i++)
            {
                var (wave, _) = water.GetWave(i);
                float crest = water.CurrentWaveCenter(wave);
                if (crest > x - 4f && crest < x + 2f)
                {
                    rideWave = wave;
                    rideOffset = 1.5f;
                    rideZ = water.transform.InverseTransformPoint(transform.position).z;
                    rideStart = Time.time;
                    score = 0;
                    tricks = 0;
                    state = State.Riding;
                    Notifications.Post("You're up! Carve with A/D.");
                    return true;
                }
            }
            return false;
        }

        void Ride(Keyboard keyboard)
        {
            var water = pool.Water;
            float crest = water.CurrentWaveCenter(rideWave);
            if (crest / water.Length > 0.9f)
            {
                EndRide();
                return;
            }

            float limit = water.Width * 0.5f - 1.2f;
            float steer = (keyboard.dKey.isPressed ? -1f : 0f) + (keyboard.aKey.isPressed ? 1f : 0f);
            rideZ = Mathf.Clamp(rideZ + steer * carveSpeed * Time.deltaTime, -limit, limit);
            if (keyboard.wKey.isPressed) rideOffset -= 1.5f * Time.deltaTime;
            if (keyboard.sKey.isPressed) rideOffset += 1.5f * Time.deltaTime;
            rideOffset = Mathf.Clamp(rideOffset, 0.3f, 5f);

            // Too far down in the flats: the wave leaves you behind.
            if (rideOffset > 4.2f)
            {
                StartWipeout();
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame && Time.time - airStart > 1f && rideOffset < 2f)
            {
                airStart = Time.time;
                tricks++;
                score += 150;
                GameStats.TricksLanded++;
                Notifications.Post("AIR! +150");
            }

            Vector3 local = new(crest + rideOffset, 0f, rideZ);
            Vector3 target = water.transform.TransformPoint(local);
            float air = Mathf.Clamp01((Time.time - airStart) / 0.8f);
            target.y = water.SurfaceHeightAt(target) + 0.05f + (air < 1f ? Mathf.Sin(air * Mathf.PI) * 1.1f : 0f);
            Vector3 delta = target - transform.position;
            transform.position = target;
            delta.y = 0;
            if (delta.sqrMagnitude > 1e-5f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), 6f * Time.deltaTime);
            cameraPivot.localPosition = new Vector3(0, 1.35f, 0);

            // Style: riding high on the face and carving fast both score.
            score += Mathf.RoundToInt((Mathf.Abs(steer) * 20f + Mathf.Clamp(3f - rideOffset, 0f, 3f) * 10f) * Time.deltaTime * 10f);
        }

        void EndRide()
        {
            state = State.Paddling;
            float seconds = Time.time - rideStart;
            score += Mathf.RoundToInt(seconds * 40f);
            int bonus = Mathf.Clamp(score / 60, 1, 50);
            Wallet.Instance.Add(bonus, "sponsor bonus");
            if (ParkReputation.Instance != null) ParkReputation.Instance.Record(1f);
            GameStats.PlayerRides++;
            Notifications.Post($"Great ride! Score {score} ({tricks} airs). Sponsors paid ${bonus}.");
        }

        void StartWipeout()
        {
            state = State.Wipeout;
            wipeoutUntil = Time.time + 1.5f;
            GameStats.Wipeouts++;
            Notifications.Post("WIPEOUT! You fell off the back of the wave.", true);
        }

        void Wipeout()
        {
            Vector3 p = transform.position;
            p.y = pool.Water.SurfaceHeightAt(p) - 0.4f;
            transform.position = p;
            cameraPivot.localPosition = new Vector3(0, 0.3f, 0);
            if (Time.time >= wipeoutUntil) state = State.Paddling;
        }

        Vector3 ClampToPool(Vector3 world)
        {
            var water = pool.Water;
            Vector3 local = water.transform.InverseTransformPoint(world);
            local.x = Mathf.Clamp(local.x, 2f, water.Length - 10f);
            float limit = water.Width * 0.5f - 1f;
            local.z = Mathf.Clamp(local.z, -limit, limit);
            return water.transform.TransformPoint(local);
        }
    }
}
