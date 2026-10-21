using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Instant replay of your own rides. While you surf, a stand-in surfer (your look; visible in the chase cam,
    /// hidden in first person) copies what you do and
    /// its pose is recorded 30 times a second, together with the waves in the pool. When a ride ends (or you wipe
    /// out) press R: the park freezes, the water rewinds, and the ride plays back from a TV-style camera on the
    /// deck, cutting to a low chase cam, with slow motion on your tricks and the wipeout. Space or Esc skips.
    /// </summary>
    public class SurfReplay : MonoBehaviour
    {
        [SerializeField] PlayerSurfing surfing;
        [Tooltip("A character rig with a child named \"Board\" (built by the scene builder).")]
        [SerializeField] CharacterRig avatarPrefab;
        [SerializeField] float sampleRate = 30f;
        [Tooltip("Seconds kept before the pop-up and after the ride ends.")]
        [SerializeField] float keepBefore = 1.2f, keepAfter = 1.6f;
        [Tooltip("How long the 'R: watch replay' offer stays up.")]
        [SerializeField] float offerSeconds = 10f;
        [SerializeField] float slowMo = 0.35f;

        static readonly string[] JointNames =
            { "Model", "Hips", "Torso", "Head", "Shoulder L", "Shoulder R", "Elbow L", "Elbow R", "Hip L", "Hip R", "Knee L", "Knee R" };

        struct Frame
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation, board;
            public Vector3 modelPosition, hipsPosition;
            public Quaternion[] joints;
        }

        public static SurfReplay Instance { get; private set; }

        /// <summary>The stand-in's board (the one you see in the chase cam).</summary>
        public Transform AvatarBoard => avatarBoard;

        /// <summary>A replay is playing (the park is frozen).</summary>
        public static bool Playing { get; private set; }

        CharacterRig avatar;
        Transform avatarBoard;
        Transform[] joints;
        readonly List<Frame> buffer = new();
        readonly List<Frame> clip = new();
        readonly List<float> highlights = new(), clipHighlights = new();
        float nextSample, rideStart = -1f, rideEnd = -1f, offerUntil;
        bool rideLive, wipedOut, liveVisible;
        WaveWater recordWater, clipWater;
        PoolArea clipPool;

        // playback
        float playhead, clipStart, clipEnd;
        Camera replayCam, playerCam;
        readonly List<Renderer> hidden = new();
        readonly List<Behaviour> paused = new();
        float savedTimeScale;
        Vector3 camVelocity;
        VisualElement hudRoot, overlay, offer;
        Label slowLabel;

        void Awake() => Instance = this;

        void OnEnable()
        {
            surfing.RideStarted += OnRideStarted;
            surfing.RideFinished += OnRideFinished;
            surfing.WipedOut += OnWipedOut;
            surfing.Called += OnCalled;
        }

        void OnDisable()
        {
            surfing.RideStarted -= OnRideStarted;
            surfing.RideFinished -= OnRideFinished;
            surfing.WipedOut -= OnWipedOut;
            surfing.Called -= OnCalled;
            if (Playing) Stop();
        }

        void OnRideStarted()
        {
            rideLive = true;
            wipedOut = false;
            rideStart = Time.time;
            rideEnd = -1f;
            highlights.Clear();
            recordWater = surfing.Pool != null ? surfing.Pool.Water : null;
            if (recordWater != null) recordWater.BeginRecording();
        }

        void OnRideFinished(int score, string rating, float multiplier, float speed, int tricks) => EndOfRide();

        void OnWipedOut()
        {
            if (!rideLive) return;
            wipedOut = true;
            highlights.Add(Time.time);
            EndOfRide();
        }

        void OnCalled(string text, Notice kind)
        {
            if (rideLive && (kind == Notice.Good || kind == Notice.Reward)) highlights.Add(Time.time);
        }

        void EndOfRide()
        {
            if (!rideLive || rideEnd > 0f) return;
            rideEnd = Time.time + (wipedOut ? 1.2f : 0f);
        }

        void LateUpdate()
        {
            if (Playing)
            {
                Play();
                return;
            }
            DriveAvatar();
            Record();
            UpdateOffer();
        }

        // ---------------- live: the hidden stand-in and the recording ----------------

        void EnsureAvatar()
        {
            if (avatar != null || avatarPrefab == null) return;
            avatar = Instantiate(avatarPrefab);
            avatar.name = "Replay Stand-In";
            avatar.Wear(PlayerLook());
            avatarBoard = avatar.transform.Find("Board");
            joints = new Transform[JointNames.Length];
            var all = avatar.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < JointNames.Length; i++)
                joints[i] = System.Array.Find(all, t => t.name == JointNames[i]);
            SetVisible(avatar.gameObject, false);
        }

        /// <summary>You, as the replay shows you: a teal rash guard and hibiscus boardshorts.</summary>
        public static CharacterLook PlayerLook() => new()
        {
            skin = CharacterLooks.Skins[2], hair = new Color(0.3f, 0.2f, 0.12f), hairStyle = CharacterLooks.Short, outfit = Outfit.RashGuard,
            top = CharacterLooks.Teal, bottom = CharacterLooks.Coral, bottomPattern = Pattern.Hibiscus, mood = Face.Focused, voice = 1f,
        };

        void DriveAvatar()
        {
            if (!surfing.IsSurfing)
            {
                if (avatar != null && avatar.gameObject.activeSelf) avatar.gameObject.SetActive(false);
                return;
            }
            EnsureAvatar();
            if (avatar == null) return;
            if (!avatar.gameObject.activeSelf) avatar.gameObject.SetActive(true);
            // In the chase cam you see the stand-in: it IS you.
            if (liveVisible != GameSettings.ChaseCam)
            {
                liveVisible = GameSettings.ChaseCam;
                SetVisible(avatar.gameObject, liveVisible);
            }
            var t = surfing.transform;
            var heading = Quaternion.Euler(0f, t.eulerAngles.y, 0f);
            if (surfing.IsWipingOut)
            {
                // Rolled over in the foam: tumbling head over heels, mostly under the surface.
                float roll = surfing.Tumble;
                var tumbling = heading * Quaternion.Euler(roll * 0.6f, 0f, roll);
                Vector3 center = t.position + Vector3.up * 0.15f; // spin around the middle of the body, not the feet
                avatar.transform.SetPositionAndRotation(center - tumbling * (Vector3.up * 0.7f), tumbling);
            }
            else avatar.transform.SetPositionAndRotation(new Vector3(t.position.x, t.position.y - 0.05f, t.position.z), heading);
            avatar.CurrentMode = surfing.IsWipingOut ? CharacterRig.Mode.Flail : surfing.IsRiding ? CharacterRig.Mode.Ride : CharacterRig.Mode.Paddle;
            avatar.Lean = -surfing.Turn;
            avatar.Crouch = Mathf.Clamp01(Mathf.Abs(surfing.Turn) * 0.5f + surfing.Tube * 0.7f + Mathf.Sin(surfing.PumpGlow * Mathf.PI) * 0.45f);
            avatar.Air = Mathf.Max(surfing.AirLift * 0.5f, surfing.Grab);
            avatar.Twist = surfing.Twist;
            avatar.ArmsUp = surfing.ArmsUp;
            avatar.Style = 0.6f + surfing.Flow * 0.4f;
            avatar.BoardTilt = surfing.BoardTilt;
            if (avatarBoard != null)
            {
                avatarBoard.localPosition = CharacterRig.BoardPivot;
                avatarBoard.localRotation = surfing.BoardTilt;
            }
        }

        void Record()
        {
            if (avatar == null || !avatar.gameObject.activeSelf || Time.time < nextSample) return;
            nextSample = Time.time + 1f / sampleRate;
            var frame = new Frame
            {
                time = Time.time,
                position = avatar.transform.position,
                rotation = avatar.transform.rotation,
                board = avatarBoard != null ? avatarBoard.localRotation : Quaternion.identity,
                modelPosition = joints[0] != null ? joints[0].localPosition : Vector3.zero,
                hipsPosition = joints[1] != null ? joints[1].localPosition : Vector3.zero,
                joints = new Quaternion[joints.Length],
            };
            for (int i = 0; i < joints.Length; i++) frame.joints[i] = joints[i] != null ? joints[i].localRotation : Quaternion.identity;
            buffer.Add(frame);
            while (buffer.Count > 0 && Time.time - buffer[0].time > 40f) buffer.RemoveAt(0);

            // The ride is over (plus a moment after): keep it as the clip you can replay.
            if (rideLive && rideEnd > 0f && Time.time >= rideEnd + keepAfter) Keep();
        }

        void Keep()
        {
            rideLive = false;
            if (recordWater != null) recordWater.EndRecording();
            clip.Clear();
            foreach (var f in buffer)
                if (f.time >= rideStart - keepBefore && f.time <= rideEnd + keepAfter) clip.Add(f);
            if (clip.Count < 10) return;
            clipHighlights.Clear();
            clipHighlights.AddRange(highlights);
            clipWater = recordWater;
            clipPool = surfing.Pool;
            offerUntil = Time.unscaledTime + offerSeconds;
        }

        void UpdateOffer()
        {
            FindHud();
            bool show = clip.Count > 0 && Time.unscaledTime < offerUntil && !InputBlocker.Blocked;
            offer?.EnableInClassList("hidden", !show);
            var keyboard = Keyboard.current;
            if (show && keyboard != null && keyboard.rKey.wasPressedThisFrame) Begin();
        }

        // ---------------- playback ----------------

        public void Begin()
        {
            if (Playing || clip.Count < 2 || clipWater == null) return;
            Playing = true;
            offerUntil = 0f;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            InputBlocker.Block();

            // Freeze the player and look through a replay camera instead.
            playerCam = Camera.main;
            hidden.Clear();
            paused.Clear();
            foreach (var b in new Behaviour[] { surfing, GetComponent<FirstPersonController>(), GetComponent<PlayerInteractor>() })
                if (b != null && b.enabled)
                {
                    b.enabled = false;
                    paused.Add(b);
                }
            foreach (var r in surfing.GetComponentsInChildren<Renderer>())
                if (r.enabled && (avatar == null || !r.transform.IsChildOf(avatar.transform)))
                {
                    r.enabled = false;
                    hidden.Add(r);
                }
            // The other surfers in that pool would be standing on the wrong water: hide them.
            foreach (var s in Surfer.All)
                if (s.Pool == clipPool)
                    foreach (var r in s.GetComponentsInChildren<Renderer>())
                        if (r.enabled)
                        {
                            r.enabled = false;
                            hidden.Add(r);
                        }
            if (replayCam == null)
            {
                replayCam = new GameObject("Replay Camera").AddComponent<Camera>();
                if (playerCam != null) replayCam.CopyFrom(playerCam);
                replayCam.fieldOfView = 50f;
                replayCam.gameObject.AddComponent<AudioListener>().enabled = false;
            }
            replayCam.gameObject.SetActive(true);
            if (playerCam != null) playerCam.enabled = false;
            var listener = playerCam != null ? playerCam.GetComponent<AudioListener>() : null;
            if (listener != null) listener.enabled = false;
            replayCam.GetComponent<AudioListener>().enabled = true;

            EnsureAvatar();
            avatar.gameObject.SetActive(true);
            avatar.enabled = false; // the recording drives the joints now
            SetVisible(avatar.gameObject, true);

            clipStart = clip[0].time;
            clipEnd = clip[clip.Count - 1].time;
            playhead = clipStart;
            clipWater.StartReplay(playhead);
            Apply(playhead);
            PlaceCamera(true);
            FindHud();
            hudRoot?.AddToClassList("hud-root--replay");
            overlay?.EnableInClassList("hidden", false);
            UiMotion.Show(overlay);
        }

        void Play()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                Stop();
                return;
            }
            // Slow motion around tricks and the wipeout.
            float speed = 1f;
            foreach (float h in clipHighlights)
                if (playhead > h - 0.35f && playhead < h + 0.9f) speed = slowMo;
            slowLabel?.EnableInClassList("hidden", speed >= 1f);
            playhead += Time.unscaledDeltaTime * speed;
            if (playhead >= clipEnd)
            {
                Stop();
                offerUntil = Time.unscaledTime + 6f; // "watch again?"
                return;
            }
            clipWater.SetReplayTime(playhead);
            Apply(playhead);
            PlaceCamera(false);
        }

        void Apply(float time)
        {
            int i = 0;
            while (i < clip.Count - 2 && clip[i + 1].time < time) i++;
            var a = clip[i];
            var b = clip[Mathf.Min(i + 1, clip.Count - 1)];
            float k = b.time > a.time ? Mathf.Clamp01((time - a.time) / (b.time - a.time)) : 0f;
            avatar.transform.SetPositionAndRotation(Vector3.Lerp(a.position, b.position, k), Quaternion.Slerp(a.rotation, b.rotation, k));
            if (avatarBoard != null) avatarBoard.localRotation = Quaternion.Slerp(a.board, b.board, k);
            if (joints[0] != null) joints[0].localPosition = Vector3.Lerp(a.modelPosition, b.modelPosition, k);
            if (joints[1] != null) joints[1].localPosition = Vector3.Lerp(a.hipsPosition, b.hipsPosition, k);
            for (int j = 0; j < joints.Length; j++)
                if (joints[j] != null) joints[j].localRotation = Quaternion.Slerp(a.joints[j], b.joints[j], k);
        }

        /// <summary>A drone over the pool's side wall for the first part, then a low chase cam behind the rider.</summary>
        void PlaceCamera(bool snap)
        {
            var water = clipWater.transform;
            Vector3 rider = avatar.transform.position;
            Vector3 local = water.InverseTransformPoint(rider);
            float progress = Mathf.InverseLerp(clipStart, clipEnd, playhead);
            Vector3 want;
            Vector3 look = rider + Vector3.up * 1.0f;
            if (progress < 0.55f)
            {
                // A drone hanging just inside the pool wall, high enough to look down over the coping.
                float side = local.z > 0f ? -(clipWater.Width * 0.5f - 0.5f) : clipWater.Width * 0.5f - 0.5f;
                want = water.TransformPoint(new Vector3(local.x + 3.5f, 0f, side)) + Vector3.up * 5.2f;
            }
            else
            {
                Vector3 back = -avatar.transform.forward;
                want = rider + back * 3.2f + Vector3.up * 1.4f + avatar.transform.right * 1.2f;
                look += avatar.transform.forward * 2f;
            }
            var t = replayCam.transform;
            t.position = snap ? want : Vector3.SmoothDamp(t.position, want, ref camVelocity, 0.25f, 60f, Time.unscaledDeltaTime);
            var aim = Quaternion.LookRotation(look - t.position);
            t.rotation = snap ? aim : Quaternion.Slerp(t.rotation, aim, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 8f));
        }

        public void Stop()
        {
            if (!Playing) return;
            Playing = false;
            clipWater.StopReplay();
            foreach (var r in hidden) if (r != null) r.enabled = true;
            hidden.Clear();
            foreach (var b in paused) if (b != null) b.enabled = true;
            paused.Clear();
            if (avatar != null)
            {
                avatar.enabled = true;
                SetVisible(avatar.gameObject, false);
                liveVisible = false;
            }
            if (replayCam != null)
            {
                replayCam.GetComponent<AudioListener>().enabled = false;
                replayCam.gameObject.SetActive(false);
            }
            if (playerCam != null)
            {
                playerCam.enabled = true;
                var listener = playerCam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
            hudRoot?.RemoveFromClassList("hud-root--replay");
            overlay?.EnableInClassList("hidden", true);
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            InputBlocker.Release();
        }

        void FindHud()
        {
            if (hudRoot != null) return;
            var hud = FindAnyObjectByType<GameHud>();
            if (hud == null) return;
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            hudRoot = root.Q("hudRoot");
            overlay = root.Q("replayOverlay");
            offer = root.Q("replayOffer");
            slowLabel = root.Q<Label>("replaySlow");
        }

        static void SetVisible(GameObject go, bool visible)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
        }
    }
}
