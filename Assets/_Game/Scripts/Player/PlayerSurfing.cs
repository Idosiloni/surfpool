using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SurfPool
{
    /// <summary>
    /// Lets the player surf their own pool. Paddle with W (steer by looking), Space to pop up when a wave reaches
    /// you, A/D to carve along the wave, W/S to move up or down the face, Space for an air, F to send a wave
    /// from the water, Q to get out. Rides earn style points, a small sponsor bonus and a better park rating.
    /// Feel: carving builds up speed (no instant sideways sliding), the board lies on the wave face and leans into
    /// turns and throws spray, the camera leans with you, widens and shakes with speed (with rushing wind), rises
    /// as you pop up, squashes on landings and tumbles under the water in a wipeout (a hit-pause, muffled sound).
    /// Game: the pop-up is graded EARLY / PERFECT / LATE by where the wave is when you stand. Flow (0..1) builds by
    /// trimming high and linking carves and drains in the flats; it raises carve speed and points, and airs need
    /// some. Carves, bottom turns and airs chain into a combo (x1 to x3) that is banked after a pause or at the end
    /// of the ride and lost in a wipeout. The ride ends with a score card. SurfHud shows all of this.
    /// Barrels: high on the face just ahead of the break point of a barreling wave you're in the tube: the world
    /// goes muffled and green-dark at the edges, tube time scores fast, and coming out is a combo trick.
    /// See docs/research/surf-feel.md.
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
        [Tooltip("Camera shake at full speed: position (m) and roll (degrees).")]
        [SerializeField] Vector2 speedShake = new(0.012f, 0.6f);

        [Header("Flow and combos")]
        [Tooltip("Extra carve speed at full flow (x carveSpeed): 7 m/s becomes about 9.5, like a pro's peak.")]
        [SerializeField] float flowSpeedBonus = 0.35f;
        [Tooltip("Flow gained per second trimming high on the face, and lost per second down in the flats.")]
        [SerializeField] Vector2 flowTrimAndFlats = new(0.06f, 0.15f);
        [Tooltip("Seconds without a new trick before the combo is banked.")]
        [SerializeField] float comboWindow = 4f;
        [Tooltip("Flow needed to launch an air.")]
        [Range(0f, 1f)] [SerializeField] float airFlow = 0.25f;

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
        float eye, eyeVelocity, roll, tumble, landSquash, popUpStart = -10f, kick;
        float flow, topSpeed, carveSign, carveSince, lowSince = -10f, sprayDropsAt;
        int comboCount, comboPoints, bestComboPoints;
        float comboUntil, bestMultiplier = 1f, hitPauseUntil = -1f;
        float tube, tubeTime;
        bool inTube;
        AudioSource wind;
        AudioLowPassFilter underwater;
        Volume tubeVolume;

        public bool IsSurfing => state != State.Off;
        public bool IsRiding => state == State.Riding;
        public bool IsWipingOut => state == State.Wipeout;

        // ---- read by the instant replay (SurfReplay) ----
        public PoolArea Pool => pool;
        /// <summary>-1..1 how hard you're turning.</summary>
        public float Turn => feel.Turn;
        /// <summary>0..1 height of an air right now.</summary>
        public float AirLift { get; private set; }
        /// <summary>The board's tilt under you (heading frame).</summary>
        public Quaternion BoardTilt => surfBoard.transform.localRotation;

        /// <summary>You popped up and are riding a wave.</summary>
        public event Action RideStarted;
        /// <summary>A wipeout began.</summary>
        public event Action WipedOut;

        /// <summary>0..1 how deep in the tube you are right now.</summary>
        public float Tube => tube;

        /// <summary>0..1 how much speed and style you've built up this ride.</summary>
        public float Flow => flow;

        /// <summary>This ride's score (including banked combos).</summary>
        public int Score => score;

        public int ComboCount => comboCount;
        public int ComboPoints => comboPoints;
        public float ComboMultiplier => Mathf.Min(3f, 1f + 0.5f * Mathf.Max(0, comboCount - 1));

        /// <summary>1 right after a trick, 0 when the combo is about to be banked.</summary>
        public float ComboTimeLeft => comboCount == 0 ? 0f : Mathf.Clamp01((comboUntil - Time.time) / comboWindow);

        /// <summary>Big centered call-outs: text and mood (Good / Reward / Bad).</summary>
        public event Action<string, Notice> Called;

        /// <summary>Water hits the lens: how many drops.</summary>
        public event Action<int> Splashed;

        /// <summary>A ride finished: score, rating word, best combo multiplier, top speed (km/h), airs.</summary>
        public event Action<int, string, float, float, int> RideFinished;

        /// <summary>For tests: carve input (-1..1, positive = A) used when no key is held.</summary>
        public float SteerOverride { get; set; }

        /// <summary>Controls hint for the HUD while surfing.</summary>
        public string Hint => state switch
        {
            State.Paddling => "W paddle  ·  mouse steer  ·  SPACE stand up when a wave reaches you  ·  F send a wave  ·  Q get out",
            State.Riding => "A/D carve (link turns for flow)  ·  W/S up and down the face  ·  SPACE air",
            State.Wipeout => "Wipeout! Hold on...",
            _ => null,
        };

        void Awake()
        {
            Instance = this;
            cam = cameraPivot.GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
            boardRest = surfBoard.transform.localPosition;

            // Rushing wind that follows your speed, and a filter on the listener for muffled underwater sound.
            wind = gameObject.AddComponent<AudioSource>();
            wind.loop = true;
            wind.playOnAwake = false;
            wind.spatialBlend = 0f;
            wind.volume = 0f;
            underwater = cameraPivot.GetComponent<AudioLowPassFilter>();
            if (underwater == null) underwater = cameraPivot.gameObject.AddComponent<AudioLowPassFilter>();
            underwater.cutoffFrequency = 22000f;
            underwater.enabled = false;

            // Inside the barrel: darker teal-green edges and a cooler, dimmer world (weight follows Tube).
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.5f);
            vignette.smoothness.Override(0.6f);
            vignette.color.Override(new Color(0.02f, 0.2f, 0.22f));
            var grade = profile.Add<ColorAdjustments>(true);
            grade.colorFilter.Override(new Color(0.78f, 1f, 0.95f));
            grade.postExposure.Override(-0.35f);
            tubeVolume = cameraPivot.gameObject.AddComponent<Volume>();
            tubeVolume.isGlobal = true;
            tubeVolume.priority = 50f;
            tubeVolume.sharedProfile = profile;
            tubeVolume.weight = 0f;
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
            if (wind.isPlaying) wind.Stop();
            underwater.enabled = false;
            tube = 0f;
            tubeVolume.weight = 0f;
            body.enabled = true;
            walker.enabled = true;
        }

        void Update()
        {
            // End a wipeout hit-pause (unscaled time), unless a menu has paused the game meanwhile.
            if (hitPauseUntil > 0f && Time.unscaledTime >= hitPauseUntil)
            {
                if (Mathf.Approximately(Time.timeScale, HitPauseScale)) Time.timeScale = 1f;
                hitPauseUntil = -1f;
            }
            if (state == State.Off || InputBlocker.Blocked)
            {
                if (wind.isPlaying) wind.volume = 0f;
                return;
            }
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
                State.Riding => Mathf.Lerp(PaddleEye + 0.1f, StandEye - 0.45f * Mathf.Clamp01(Mathf.Abs(feel.Turn) * 0.6f + landSquash) - 0.3f * tube,
                    Mathf.SmoothStep(0f, 1f, (Time.time - popUpStart) / PopUpRise)),
                State.Wipeout => WipeoutEye,
                _ => PaddleEye + Mathf.Sin(Time.time * 4f) * 0.03f,
            };
            eye = Mathf.SmoothDamp(eye, targetEye, ref eyeVelocity, 0.12f);

            float targetRoll = state == State.Riding ? -feel.Turn * maxCameraRoll
                : state == State.Paddling ? Mathf.Sin(Time.time * 2.5f) * 1.5f : 0f; // rocks with each stroke
            roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-6f * dt));
            if (state != State.Wipeout) tumble = Mathf.LerpAngle(tumble, 0f, 1f - Mathf.Exp(-5f * dt));

            // Speed: a fine shake and a wider view; flow keeps them up between bursts of speed.
            float speed = state == State.Riding ? Mathf.Max(Mathf.InverseLerp(2f, 9f, feel.Speed), flow * 0.8f) : 0f;
            float t = Time.time * 9f;
            Vector3 shake = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * (2f * speedShake.x * speed);
            float shakeRoll = (Mathf.PerlinNoise(t, 5.1f) - 0.5f) * 2f * speedShake.y * speed;
            kick = Mathf.MoveTowards(kick, 0f, 30f * dt);

            cameraPivot.localPosition = new Vector3(0f, eye, 0f) + shake;
            cameraPivot.localRotation = Quaternion.Euler(pitch + kick, 0f, roll + tumble + shakeRoll);

            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFov + speedFovKick * speed, 1f - Mathf.Exp(-3f * dt));

            // In the tube: muffled (unless a wipeout is already muffling), vignette by depth.
            tubeVolume.weight = state == State.Riding ? tube : 0f;
            if (state == State.Riding)
            {
                underwater.enabled = tube > 0.01f;
                underwater.cutoffFrequency = Mathf.Lerp(22000f, 1600f, tube);
            }
            else if (state == State.Paddling)
            {
                tube = 0f;
                underwater.enabled = false;
            }

            if (wind.clip == null) wind.clip = Sfx.WindClip;
            if (wind.clip != null)
            {
                if (!wind.isPlaying) wind.Play();
                wind.volume = Mathf.Lerp(wind.volume, Mathf.Lerp(0.03f, 0.55f, speed) * Sfx.AmbienceVolume, 1f - Mathf.Exp(-4f * dt));
                wind.pitch = Mathf.Lerp(0.8f, 1.3f, speed);
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

        /// <summary>
        /// Stands up on a wave that's reaching you. Graded by where its crest is: well behind you = EARLY (the wave
        /// isn't under you yet), just behind = PERFECT, already passing = LATE.
        /// </summary>
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
                    float d = crest - x;
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
                    comboCount = comboPoints = bestComboPoints = 0;
                    tube = tubeTime = 0f;
                    inTube = false;
                    bestMultiplier = 1f;
                    topSpeed = 0f;
                    carveSign = 0f;
                    feel.Begin(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
                    state = State.Riding;
                    RideStarted?.Invoke();
                    Sfx.Play(Sound.Splash, transform.position, 0.4f);
                    if (d >= -1.6f && d <= -0.4f)
                    {
                        flow = 0.4f;
                        score += 100;
                        RideFeel.Splash(water, transform.position, 12, 2f);
                        Splashed?.Invoke(3);
                        Called?.Invoke("PERFECT TAKEOFF! +100", Notice.Reward);
                    }
                    else if (d > -0.4f)
                    {
                        flow = 0f;
                        popUpStart = Time.time + 0.3f; // scrambling up late
                        Called?.Invoke("LATE! Stand up as the wave lifts you", Notice.Bad);
                    }
                    else
                    {
                        flow = 0.1f;
                        Called?.Invoke("EARLY - wait for the wave to reach you", Notice.Info);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>Jump off the lip (Space while riding high on the face).</summary>
        public void TryAir()
        {
            if (state != State.Riding || Time.time - airStart <= 1f) return;
            if (rideOffset >= 2f)
            {
                Called?.Invoke("Get up high on the face to launch an air", Notice.Info);
                return;
            }
            if (flow < airFlow)
            {
                Called?.Invoke("Need more speed! Link some carves first", Notice.Info);
                return;
            }
            airStart = Time.time;
            tricks++;
            GameStats.TricksLanded++;
            Sfx.Play(Sound.Cheer, transform.position);
            Trick("AIR", 150, 0.15f);
        }

        /// <summary>A move in the combo: adds points and flow, keeps the combo alive.</summary>
        void Trick(string name, int points, float flowGain)
        {
            comboCount++;
            comboPoints += points;
            comboUntil = Time.time + comboWindow;
            flow = Mathf.Clamp01(flow + flowGain);
            bestMultiplier = Mathf.Max(bestMultiplier, ComboMultiplier);
            Called?.Invoke(comboCount > 1 ? $"{name}  x{ComboMultiplier:0.#}" : $"{name} +{points}", Notice.Good);
        }

        /// <summary>Adds the combo (times its multiplier) to the score.</summary>
        void BankCombo()
        {
            if (comboCount == 0) return;
            int banked = Mathf.RoundToInt(comboPoints * ComboMultiplier);
            score += banked;
            bestComboPoints = Mathf.Max(bestComboPoints, banked);
            if (comboCount > 1) Called?.Invoke($"x{ComboMultiplier:0.#} COMBO  +{banked}", Notice.Reward);
            comboCount = comboPoints = 0;
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
            rideVelocity = Mathf.MoveTowards(rideVelocity, steer * carveSpeed * (1f + flowSpeedBonus * flow), carveAcceleration * dt);

            // A carve is a turn held long enough to build speed one way, then switched: linking them builds flow.
            // (Tapping A/D quickly doesn't count: the turn must have been held and fast.)
            if (steer != 0f && Mathf.Sign(steer) != carveSign)
            {
                float held = Time.time - carveSince;
                if (carveSign != 0f && held >= 0.6f && held <= 3f && Mathf.Abs(rideVelocity) >= 0.6f * carveSpeed)
                    Trick("CARVE", 50, 0.25f);
                carveSign = Mathf.Sign(steer);
                carveSince = Time.time;
            }
            rideZ += rideVelocity * dt;
            if (Mathf.Abs(rideZ) > limit)
            {
                rideZ = Mathf.Clamp(rideZ, -limit, limit);
                rideVelocity = 0f;
            }
            if (keyboard.wKey.isPressed) rideOffset -= 1.5f * dt;
            if (keyboard.sKey.isPressed) rideOffset += 1.5f * dt;
            rideOffset = Mathf.Clamp(rideOffset, 0.3f, 5f);

            // Bottom turn: drop down the face, then drive back up to the top quickly.
            if (rideOffset > 2.5f) lowSince = Time.time;
            else if (rideOffset < 1.2f && lowSince > 0f)
            {
                if (Time.time - lowSince < 1.5f) Trick("BOTTOM TURN", 120, 0.2f);
                lowSince = -10f;
            }

            // Flow: trimming high on the face builds it, the flats drain it, and it fades a little anyway.
            flow += (rideOffset < 1.5f ? flowTrimAndFlats.x : 0f) * dt - (rideOffset > 3f ? flowTrimAndFlats.y : 0.02f) * dt;
            flow = Mathf.Clamp01(flow);
            if (comboCount > 0 && Time.time > comboUntil) BankCombo();

            // Too far down in the flats: the wave leaves you behind.
            if (rideOffset > 4.2f)
            {
                StartWipeout("Too far down the face - the wave left you behind. Stay high with W!");
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame) TryAir();

            Vector3 local = new(crest + rideOffset, 0f, rideZ);
            Vector3 target = water.transform.TransformPoint(local);
            float air = Mathf.Clamp01((Time.time - airStart) / 0.8f);
            bool airborne = air < 1f;
            float lift = airborne ? Mathf.Sin(air * Mathf.PI) : 0f;
            AirLift = lift;
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
            topSpeed = Mathf.Max(topSpeed, feel.Speed);
            // Hard carves throw a little spray onto the lens.
            if (Mathf.Abs(feel.Turn) > 0.75f && Time.time > sprayDropsAt)
            {
                sprayDropsAt = Time.time + 0.8f;
                Splashed?.Invoke(2);
            }

            if (wasAirborne && !airborne)
            {
                landSquash = 1f;
                RideFeel.Splash(water, transform.position, 18, 2.6f);
                Sfx.Play(Sound.Splash, transform.position, 0.6f);
                Splashed?.Invoke(6);
            }
            wasAirborne = airborne;
            landSquash = Mathf.MoveTowards(landSquash, 0f, dt * 2.5f);

            UpdateTube(water, dt);

            // Style: riding high on the face and carving fast both score, more with flow.
            score += Mathf.RoundToInt((Mathf.Abs(steer) * 20f + Mathf.Clamp(3f - rideOffset, 0f, 3f) * 10f) * dt * 10f * (1f + flow));
        }

        /// <summary>
        /// Are you in the barrel? On a barreling wave, high on the face and just ahead of the break point, the lip is
        /// curling over you (the lip mesh covers a few meters ahead of the break, see WaveWater.AddLipStrip).
        /// </summary>
        void UpdateTube(WaveWater water, float dt)
        {
            float target = 0f;
            int side = rideVelocity >= 0f ? 1 : -1;
            if (water.TryGetPocket(rideWave, side, out float pocketZ, out int direction, out _, out _, out bool barrel) && barrel)
            {
                float ahead = (rideZ - pocketZ) * direction; // meters in front of the break point, under the lip
                if (ahead > 0f && ahead < 4.5f && rideOffset < 1.3f)
                    target = Mathf.Clamp01(1.4f - ahead / 4.5f) * Mathf.Clamp01((1.3f - rideOffset) / 0.5f);
            }
            tube = Mathf.MoveTowards(tube, target, dt * 3f);

            bool nowIn = tube > 0.5f;
            if (nowIn)
            {
                tubeTime += dt;
                score += Mathf.RoundToInt(120f * dt * (1f + flow));
                if (!inTube) Called?.Invoke("GETTING BARRELED!", Notice.Reward);
            }
            else if (inTube)
            {
                // Coming out: spit in your face, and the tube counts as a trick if it lasted.
                if (tubeTime >= 0.6f)
                {
                    GameStats.TubeRides++;
                    Trick($"TUBE {tubeTime:0.0}s", Mathf.RoundToInt(150f + 150f * tubeTime), 0.3f);
                    RideFeel.Splash(water, transform.position + transform.forward * 1.5f, 10, 2.2f);
                    Sfx.Play(Sound.Splash, transform.position, 0.5f);
                    Splashed?.Invoke(8);
                }
                tubeTime = 0f;
            }
            inTube = nowIn;
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
            BankCombo();
            float seconds = Time.time - rideStart;
            score += Mathf.RoundToInt(seconds * 40f);
            int bonus = Mathf.Clamp(Mathf.RoundToInt(score / (float)SponsorScorePerDollar * ParkPerks.SponsorMultiplier), 1, MaxSponsorBonus);
            Wallet.Instance.Add(bonus, "sponsor bonus");
            if (ParkReputation.Instance != null) ParkReputation.Instance.Record(1f);
            GameStats.PlayerRides++;
            ParkLevel.Add(ParkLevel.XpPlayerRide);
            string rating = score < 250 ? "Nice ride!" : score < 600 ? "Sick ride!" : score < 1000 ? "EPIC RIDE!" : "LEGENDARY!";
            // The crowd's cheer follows how good it was.
            Sfx.Play(Sound.Cheer, transform.position, Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(100f, 900f, score)));
            RideFinished?.Invoke(score, rating, bestMultiplier, topSpeed * 3.6f, tricks);
            Notifications.Post($"{rating} Score {score}. Sponsors paid ${bonus}.", Notice.Reward);
            flow = 0f;
        }

        const float HitPauseScale = 0.08f;

        /// <summary>Sponsor bonus: $1 per this many points, capped. A good ride (~2,500 with flow and combos) pays about
        /// $20, like before flow and combos raised scores; see docs/BALANCE.md.</summary>
        const int SponsorScorePerDollar = 120, MaxSponsorBonus = 40;

        /// <summary>Wipeout: a split-second freeze and a jolt, then a muffled tumble under the water.</summary>
        void StartWipeout(string why)
        {
            state = State.Wipeout;
            AirLift = 0f;
            WipedOut?.Invoke();
            wipeoutUntil = Time.time + 1.5f;
            GameStats.Wipeouts++;
            Sfx.Play(Sound.SplashBig);
            RideFeel.Splash(pool.Water, transform.position, 30, 3.2f);
            if (comboCount > 1) Called?.Invoke("COMBO LOST!", Notice.Bad);
            comboCount = comboPoints = 0;
            flow = 0f;
            kick = 8f;
            Splashed?.Invoke(14);
            if (Mathf.Approximately(Time.timeScale, 1f))
            {
                Time.timeScale = HitPauseScale;
                hitPauseUntil = Time.unscaledTime + 0.12f;
            }
            underwater.enabled = true;
            Notifications.Post("WIPEOUT! " + why, Notice.Bad);
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
            // Muffled underwater, clearing as you come back up.
            underwater.cutoffFrequency = Mathf.Lerp(22000f, 650f, Mathf.Pow(left, 0.5f));
            if (Time.time >= wipeoutUntil)
            {
                state = State.Paddling;
                underwater.enabled = false;
                Sfx.Play(Sound.Splash, transform.position, 0.35f); // breaking the surface
            }
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
