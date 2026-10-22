using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The surf photographer (docs/briefs/surf-photographer.md). E at the camera on the photo tower: look through the
    /// long lens (mouse aims, scroll zooms, click shoots, E/Esc leaves). Every shot is graded C / B / A / S from what's
    /// in it: a surfer riding, near the middle, big enough in frame, at a great moment (a trick, a tube, a
    /// celebration), better surfers and legends worth more. Surfers you shoot at B or better buy a print when they
    /// leave happy; S shots go up on the Wall of Fame. People hanging around pose with a shaka when you aim at them.
    /// </summary>
    public class PhotoTower : MonoBehaviour, IInteractable
    {
        [SerializeField] Camera lens;
        [SerializeField] PhotoWall wall;
        [SerializeField] float minFov = 10f, maxFov = 48f;
        [SerializeField] Vector2 yawLimit = new(-100f, 100f), pitchLimit = new(-35f, 12f);
        [SerializeField] float sensitivity = 0.08f;

        public static readonly string[] Grades = { "C", "B", "A", "S" };
        static readonly int[] PrintPrices = { 0, 15, 30, 80 };

        bool active;
        int openedFrame;
        float yaw, pitch, fov = 30f, nextShot;
        Quaternion baseRotation;
        Camera playerCam;
        readonly List<Behaviour> paused = new();
        readonly Dictionary<Surfer, float> posedAt = new();
        VisualElement hudRoot, viewfinder, flash, photo, photoImage;
        Label zoomLabel, gradeLabel, reasonLabel;
        float photoUntil, flashUntil;
        RenderTexture shotTarget;

        public static int PhotosTaken { get; set; }
        public static int PrintsSold { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => PhotosTaken = PrintsSold = 0;

        public string Prompt => active ? null : "Take surf photos";

        void Awake()
        {
            if (lens != null)
            {
                baseRotation = lens.transform.localRotation;
                lens.enabled = false;
            }
        }

        public void Interact()
        {
            if (active || lens == null) return;
            active = true;
            openedFrame = Time.frameCount;
            yaw = pitch = 0f;
            fov = 30f;
            InputBlocker.Block();
            FirstPersonController.LockCursor(true);
            playerCam = Camera.main;
            var player = FindAnyObjectByType<FirstPersonController>();
            paused.Clear();
            if (player != null)
                foreach (var b in new Behaviour[] { player, player.GetComponent<PlayerInteractor>() })
                    if (b != null && b.enabled)
                    {
                        b.enabled = false;
                        paused.Add(b);
                    }
            if (playerCam != null) playerCam.enabled = false;
            lens.enabled = true;
            FindHud();
            hudRoot?.AddToClassList("hud-root--photo");
            viewfinder?.EnableInClassList("hidden", false);
            Sfx.Play(Sound.Click);
        }

        void Leave()
        {
            if (!active) return;
            active = false;
            lens.enabled = false;
            if (playerCam != null) playerCam.enabled = true;
            foreach (var b in paused) if (b != null) b.enabled = true;
            paused.Clear();
            hudRoot?.RemoveFromClassList("hud-root--photo");
            viewfinder?.EnableInClassList("hidden", true);
            InputBlocker.Release();
        }

        void Update()
        {
            if (photo != null && Time.unscaledTime > photoUntil) photo.EnableInClassList("hidden", true);
            if (flash != null && Time.unscaledTime > flashUntil) flash.EnableInClassList("hidden", true);
            if (!active) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (Time.frameCount != openedFrame
                && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame))
            {
                Leave();
                return;
            }
            // Aim: slower when zoomed in, so you can frame a far surfer.
            var delta = mouse.delta.ReadValue() * sensitivity * GameSettings.MouseSensitivity * (fov / maxFov + 0.15f);
            yaw = Mathf.Clamp(yaw + delta.x, yawLimit.x, yawLimit.y);
            pitch = Mathf.Clamp(pitch - delta.y * (GameSettings.InvertY ? -1f : 1f), pitchLimit.x, pitchLimit.y);
            lens.transform.localRotation = baseRotation * Quaternion.Euler(-pitch, yaw, 0f);
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f) fov = Mathf.Clamp(fov - Mathf.Sign(scroll) * 3f, minFov, maxFov);
            lens.fieldOfView = Mathf.Lerp(lens.fieldOfView, fov, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f));
            if (zoomLabel != null) zoomLabel.text = $"{maxFov / lens.fieldOfView:0.0}x";
            Pose();
            if (mouse.leftButton.wasPressedThisFrame && Time.unscaledTime >= nextShot) Shoot();
        }

        /// <summary>Someone in the middle of the viewfinder who isn't busy surfing looks at the camera and throws a shaka.</summary>
        void Pose()
        {
            foreach (var surfer in Surfer.All)
            {
                if (surfer.IsRidingWave) continue;
                Vector3 v = lens.WorldToViewportPoint(surfer.transform.position + Vector3.up * 1.2f);
                if (v.z <= 0f || Mathf.Abs(v.x - 0.5f) > 0.12f || Mathf.Abs(v.y - 0.5f) > 0.15f) continue;
                if (posedAt.TryGetValue(surfer, out float t) && Time.time - t < 12f) continue;
                posedAt[surfer] = Time.time;
                surfer.Rig.LookTarget = lens.transform.position;
                surfer.Rig.PlayGesture(CharacterRig.Gesture.Shaka, 2f);
                surfer.Rig.ShowFace(Face.Stoked, 2f);
            }
        }

        void Shoot()
        {
            nextShot = Time.unscaledTime + 0.6f;
            PhotosTaken++;
            Sfx.Play(Sound.ScanBeep, null, 0.8f, 0.55f); // the shutter: a short low click
            flashUntil = Time.unscaledTime + 0.12f;
            flash?.EnableInClassList("hidden", false);

            var (subject, grade, reason) = Grade();
            var picture = Capture();
            if (subject != null && grade > subject.PhotoGrade) subject.PhotoGrade = grade;
            if (grade == 3 && wall != null) wall.Add(picture);
            ShowPhoto(picture, grade, reason);
            if (grade == 3)
            {
                Sfx.Play(Sound.Mission);
                Notifications.Post("S-grade shot! It's going on the Wall of Fame.", Notice.Reward);
            }
        }

        /// <summary>Scores the best subject in frame: (surfer, grade 0..3, reason).</summary>
        (Surfer, int, string) Grade()
        {
            Surfer best = null;
            float bestScore = -1f;
            string bestReason = "Nobody in the shot";
            foreach (var surfer in Surfer.All)
            {
                Vector3 feet = surfer.transform.position, head = feet + Vector3.up * 1.8f;
                Vector3 vf = lens.WorldToViewportPoint(feet), vh = lens.WorldToViewportPoint(head);
                if (vf.z <= 0f) continue;
                Vector2 middle = (new Vector2(vf.x, vf.y) + new Vector2(vh.x, vh.y)) / 2f;
                if (middle.x < 0f || middle.x > 1f || middle.y < 0f || middle.y > 1f) continue;
                // Blocked by something (a wall, the machine)? Surfers in a sunken pool only need their head to show.
                if (Blocked(feet + Vector3.up * 1.1f, surfer) && Blocked(feet + Vector3.up * 1.6f, surfer)) continue;

                float centered = 1f - Mathf.Clamp01(Vector2.Distance(middle, new Vector2(0.5f, 0.5f)) / 0.5f);
                float size = Mathf.Abs(vh.y - vf.y); // share of the frame height
                float sizeScore = size < 0.12f ? size / 0.12f * 0.6f : size > 0.85f ? 0.6f : 1f;
                string reason;
                float moment = 0f;
                if (surfer.InBarrelNow) { moment = 1f; reason = "IN THE BARREL!"; }
                else if (Time.time - surfer.LastTrickAt < 0.8f) { moment = 0.85f; reason = "Caught the trick!"; }
                else if (surfer.IsCelebrating) { moment = 0.6f; reason = "Celebration shot"; }
                else if (surfer.IsRidingWave) { moment = 0.4f; reason = "Riding shot"; }
                else { moment = 0.05f; reason = "Not surfing right now"; }
                if (size < 0.12f) reason = "Too far: zoom in!";
                else if (centered < 0.4f) reason = "Off-center";
                float skill = surfer.Famous != null ? 1f : surfer.Skill switch { WaveLevel.Pro => 0.8f, WaveLevel.Intermediate => 0.55f, _ => 0.35f };
                float score = moment * 0.45f + centered * 0.2f + sizeScore * 0.2f + skill * 0.15f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = surfer;
                    bestReason = surfer.Famous != null && moment >= 0.85f ? $"{(string.IsNullOrEmpty(surfer.Famous.nickname) ? surfer.Famous.FullName : surfer.Famous.nickname)} in action!" : reason;
                }
            }
            int grade = bestScore >= 0.82f ? 3 : bestScore >= 0.62f ? 2 : bestScore >= 0.45f ? 1 : 0;
            return (best, grade, bestReason);
        }

        /// <summary>True when something you can see stands between the lens and this point (invisible blockers don't count).</summary>
        bool Blocked(Vector3 point, Surfer surfer)
        {
            Vector3 from = lens.transform.position, to = point - from;
            foreach (var hit in Physics.RaycastAll(from, to.normalized, to.magnitude - 0.4f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(surfer.transform)) continue;
                if (hit.collider.GetComponentInChildren<Renderer>() == null) continue;
                return true;
            }
            return false;
        }

        Texture2D Capture()
        {
            const int w = 480, h = 300;
            if (shotTarget == null) shotTarget = new RenderTexture(w, h, 24);
            var previous = lens.targetTexture;
            lens.targetTexture = shotTarget;
            lens.Render();
            lens.targetTexture = previous;
            var previousTarget = RenderTexture.active;
            RenderTexture.active = shotTarget;
            var picture = new Texture2D(w, h, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            picture.Apply();
            RenderTexture.active = previousTarget;
            return picture;
        }

        void ShowPhoto(Texture2D picture, int grade, string reason)
        {
            if (photo == null) return;
            photoImage.style.backgroundImage = new StyleBackground(picture);
            gradeLabel.text = Grades[grade];
            for (int i = 0; i < Grades.Length; i++) gradeLabel.EnableInClassList("vf-grade--" + Grades[i].ToLowerInvariant(), i == grade);
            reasonLabel.text = reason;
            photo.EnableInClassList("hidden", false);
            UiMotion.Enter(photo, "vf-photo--enter");
            photoUntil = Time.unscaledTime + 2.6f;
        }

        /// <summary>A surfer leaving happy buys the best print you took of them.</summary>
        public static void SellPrint(Surfer surfer)
        {
            int grade = Mathf.Clamp(surfer.PhotoGrade, 0, 3);
            float skill = surfer.Famous != null ? 3f : surfer.Skill switch { WaveLevel.Pro => 2.5f, WaveLevel.Intermediate => 1.5f, _ => 1f };
            int price = Mathf.RoundToInt(PrintPrices[grade] * skill * DailyEvents.PhotoPrintMultiplier);
            if (price <= 0) return;
            Wallet.Instance.Add(price, "photo prints");
            PrintsSold++;
            Notifications.Post($"A surfer bought your {Grades[grade]} photo! +{MoneyText.Format(price)}", Notice.Good);
        }

        void FindHud()
        {
            if (hudRoot != null) return;
            var hud = FindAnyObjectByType<GameHud>();
            if (hud == null) return;
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            hudRoot = root.Q("hudRoot");
            viewfinder = root.Q("viewfinder");
            flash = root.Q("vfFlash");
            photo = root.Q("vfPhoto");
            photoImage = root.Q("vfPhotoImage");
            zoomLabel = root.Q<Label>("vfZoom");
            gradeLabel = root.Q<Label>("vfGrade");
            reasonLabel = root.Q<Label>("vfReason");
        }
    }
}
