using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfPool
{
    /// <summary>
    /// Lets the player surf their own pool. Paddle with W (steer by looking), Space to pop up when a wave reaches
    /// you, A/D to carve along the wave, W/S to move up or down the face, Space for an air, F to send a wave
    /// from the water, Q to get out. Rides earn style points, a small sponsor bonus and a better park rating.
    /// Feel: carving builds up speed (no instant sideways sliding), the board lies on the wave face and leans into
    /// turns and throws spray, the camera leans with you, widens with speed, rises as you pop up, squashes on
    /// landings and tumbles under the water in a wipeout.
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

        [Header("Feel")]
        [Tooltip("How fast carving speeds up and slows down (m/s per second). Lower = heavier, surfier.")]
        [SerializeField] float carveAcceleration = 14f;
        [Tooltip("Most the camera leans into a turn (degrees).")]
        [SerializeField] float maxCameraRoll = 10f;
        [Tooltip("Extra field of view at full speed (degrees).")]
        [SerializeField] float speedFovKick = 9f;
        [Tooltip("How far ahead the board slides when you stand up, so you can see it (m).")]
        [SerializeField] float standingBoardAhead = 0.9f;
        [Tooltip("Board tilt on the wave face, lean into turns and rail spray.")]
        [SerializeField] RideFeel feel = new() { leanGain = 0.06f, railSpray = 90f };

        const float PaddleEye = 0.6f, StandEye = 1.35f, WipeoutEye = 0.3f;

        /// <summary>Seconds your eyes take to rise from the board to standing (a real pop-up is about 1 s).</summary>
        const float PopUpRise = 0.8f;

        State state;
        PoolArea pool;
        Camera cam;
        float baseFov;
        Vector3 boardRest;
        float rideWave;
        float rideOffset;
        float rideZ;
        float rideVelocity;
        float rideStart;
        float airStart = -10f;
        bool wasAirborne;
        float wipeoutUntil;
        int score;
        int tricks;
        float pitch;
        float catchUntil = -1f;
        float eye, eyeVelocity, roll, tumble, landSquash, popUpStart = -10f;

        public bool IsSurfing => state != State.Off;

        /// <summary>For tests: carve input (-1..1, positive = A) used when no key is held.</summary>
        public float SteerOverride { get; set; }

        /// <summary>Controls hint for the HUD while surfing.</summary>
        public string Hint => state switch
        {
            State.Paddling => "W paddle  ·  mouse steer  ·  SPACE stand up when a wave reaches you  ·  F send a wave  ·  Q get out",
            State.Riding => $"A/D carve  ·  W/S up and down the face  ·  SPACE air      Score: {score}",
            State.Wipeout => "Wipeout! Hold on...",
            _ => null,
        };

        void Awake()
        {
            Instance = this;
            cam = cameraPivot.GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
            boardRest = surfBoard.transform.localPosition;
        }

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
            eye = PaddleEye;
            roll = tumble = 0f;
            Notifications.Post($"Surf's up! You're in the {pool.DisplayName}.");
        }

        public void Exit()
        {
            if (state == State.Off) return;
            state = State.Off;
            surfBoard.SetActive(false);
            surfBoard.transform.SetLocalPositionAndRotation(boardRest, Quaternion.identity);
            var path = pool.PathToPool;
            transform.position = path[path.Length - 1].position + Vector3.up * 0.05f;
            transform.rotation = Quaternion.LookRotation(Vector3.right);
            cameraPivot.localPosition = new Vector3(0, 1.65f, 0);
            cameraPivot.localRotation = Quaternion.identity;
            if (cam != null) cam.fieldOfView = baseFov;
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
            UpdateCamera();
        }

        void Look(Vector2 delta)
        {
            float sensitivity = mouseSensitivity * GameSettings.MouseSensitivity;
            transform.Rotate(0f, delta.x * sensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity * (GameSettings.InvertY ? -1f : 1f), -60f, 70f);
        }

        /// <summary>Eye height, lean, tumble and speed FOV, all eased so nothing snaps.</summary>
        void UpdateCamera()
        {
            float dt = Time.deltaTime;
            float targetEye = state switch
            {
                // Pop-up: press up, then rise into a crouch; carving and landings drop you lower (down to about 0.9).
                State.Riding => Mathf.Lerp(PaddleEye + 0.1f, StandEye - 0.45f * Mathf.Clamp01(Mathf.Abs(feel.Turn) * 0.6f + landSquash),
                    Mathf.SmoothStep(0f, 1f, (Time.time - popUpStart) / PopUpRise)),
                State.Wipeout => WipeoutEye,
                _ => PaddleEye + Mathf.Sin(Time.time * 4f) * 0.03f,
            };
            eye = Mathf.SmoothDamp(eye, targetEye, ref eyeVelocity, 0.12f);

            float targetRoll = state == State.Riding ? -feel.Turn * maxCameraRoll
                : state == State.Paddling ? Mathf.Sin(Time.time * 2.5f) * 1.5f : 0f; // rocks with each stroke
            roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-6f * dt));
            if (state != State.Wipeout) tumble = Mathf.LerpAngle(tumble, 0f, 1f - Mathf.Exp(-5f * dt));

            cameraPivot.localPosition = new Vector3(0f, eye, 0f);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, roll + tumble);

            if (cam != null)
            {
                float speed = state == State.Riding ? Mathf.InverseLerp(2f, 9f, feel.Speed) : 0f;
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + speedFovKick * speed, 1f - Mathf.Exp(-3f * dt));
            }
        }

        void Paddle(Keyboard keyboard)
        {
            Vector3 forward = transform.forward;
            forward.y = 0;
            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += forward.normalized;
            if (keyboard.sKey.isPressed) move -= forward.normalized * 0.5f;
            Vector3 next = ClampToPool(transform.position + move * (paddleSpeed * Time.deltaTime));
            next.y = pool.Water.SurfaceHeightAt(next) + 0.05f;
            transform.position = next;
            LayBoard(Quaternion.identity);

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
                    rideVelocity = 0f;
                    rideStart = Time.time;
                    popUpStart = Time.time;
                    score = 0;
                    tricks = 0;
                    wasAirborne = false;
                    landSquash = 0f;
                    feel.Begin(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
                    state = State.Riding;
                    Sfx.Play(Sound.Splash, transform.position, 0.4f);
                    Notifications.Post("You're up! Carve with A/D.");
                    return true;
                }
            }
            return false;
        }

        /// <summary>Jump off the lip (Space while riding high on the face).</summary>
        public void TryAir()
        {
            if (state != State.Riding || Time.time - airStart <= 1f || rideOffset >= 2f) return;
            airStart = Time.time;
            tricks++;
            score += 150;
            GameStats.TricksLanded++;
            Sfx.Play(Sound.Cheer, transform.position);
            Notifications.Post("AIR! +150");
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

            float dt = Time.deltaTime;
            float limit = water.Width * 0.5f - 1.2f;
            float steer = (keyboard.dKey.isPressed ? -1f : 0f) + (keyboard.aKey.isPressed ? 1f : 0f);
            if (steer == 0f) steer = Mathf.Clamp(SteerOverride, -1f, 1f);
            // Carving builds up and bleeds off speed, so turns feel like turns, not sliding.
            rideVelocity = Mathf.MoveTowards(rideVelocity, steer * carveSpeed, carveAcceleration * dt);
            rideZ += rideVelocity * dt;
            if (Mathf.Abs(rideZ) > limit)
            {
                rideZ = Mathf.Clamp(rideZ, -limit, limit);
                rideVelocity = 0f;
            }
            if (keyboard.wKey.isPressed) rideOffset -= 1.5f * dt;
            if (keyboard.sKey.isPressed) rideOffset += 1.5f * dt;
            rideOffset = Mathf.Clamp(rideOffset, 0.3f, 5f);

            // Too far down in the flats: the wave leaves you behind.
            if (rideOffset > 4.2f)
            {
                StartWipeout();
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame) TryAir();

            Vector3 local = new(crest + rideOffset, 0f, rideZ);
            Vector3 target = water.transform.TransformPoint(local);
            float air = Mathf.Clamp01((Time.time - airStart) / 0.8f);
            bool airborne = air < 1f;
            float lift = airborne ? Mathf.Sin(air * Mathf.PI) : 0f;
            target.y = water.SurfaceHeightAt(target) + 0.05f + lift * 1.1f;
            Vector3 delta = target - transform.position;
            transform.position = target;
            delta.y = 0;
            if (delta.sqrMagnitude > 1e-5f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), 6f * dt);

            var heading = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            feel.Step(water, transform.position, heading, dt);
            LayBoard(feel.Tilt * Quaternion.Euler(-15f * lift, 0f, 0f));
            if (!airborne) feel.Spray(water, transform.position, heading);

            if (wasAirborne && !airborne)
            {
                landSquash = 1f;
                RideFeel.Splash(water, transform.position, 18, 2.6f);
                Sfx.Play(Sound.Splash, transform.position, 0.6f);
            }
            wasAirborne = airborne;
            landSquash = Mathf.MoveTowards(landSquash, 0f, dt * 2.5f);

            // Style: riding high on the face and carving fast both score.
            score += Mathf.RoundToInt((Mathf.Abs(steer) * 20f + Mathf.Clamp(3f - rideOffset, 0f, 3f) * 10f) * dt * 10f);
        }

        /// <summary>Eases the board under you to a tilt (you only turn on the y axis, so local = your heading frame).</summary>
        void LayBoard(Quaternion tilt)
        {
            float k = 1f - Mathf.Exp(-12f * Time.deltaTime);
            // Standing up, the board slides ahead a little so its nose shows at the bottom of the view.
            Vector3 place = boardRest + (state == State.Riding ? Vector3.forward * standingBoardAhead : Vector3.zero);
            var t = surfBoard.transform;
            t.SetLocalPositionAndRotation(Vector3.Lerp(t.localPosition, place, k), Quaternion.Slerp(t.localRotation, tilt, k));
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
            ParkLevel.Add(ParkLevel.XpPlayerRide);
            Sfx.Play(Sound.Cheer, transform.position);
            Notifications.Post($"Great ride! Score {score} ({tricks} airs). Sponsors paid ${bonus}.");
        }

        void StartWipeout()
        {
            state = State.Wipeout;
            wipeoutUntil = Time.time + 1.5f;
            GameStats.Wipeouts++;
            Sfx.Play(Sound.SplashBig);
            RideFeel.Splash(pool.Water, transform.position, 30, 3.2f);
            Notifications.Post("WIPEOUT! You fell off the back of the wave.", true);
        }

        void Wipeout()
        {
            Vector3 p = transform.position;
            p.y = pool.Water.SurfaceHeightAt(p) - 0.4f;
            transform.position = p;
            // Tumbling: the world spins fast, then slows as you come up.
            float left = Mathf.Clamp01((wipeoutUntil - Time.time) / 1.5f);
            tumble += 720f * left * Time.deltaTime;
            LayBoard(Quaternion.Euler(0f, 0f, 180f));
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
