using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A low-poly person built from separate parts (Blender: tools/blender/build_models.py) and animated in code.
    /// Locomotion switches between idle and walking by how fast the character moves; Paddle lies on the board and
    /// strokes, Ride stands sideways on the board (crouch, lean, airs driven by the surfer), Flail tumbles in a
    /// wipeout, Sit sits with legs forward, Lie sunbathes. Switching mode crossfades from the old pose, and going
    /// from Paddle to Ride plays a pop-up (push up, tuck, stand).
    /// Joint directions (character faces +z): an arm or leg swings forward with negative x and out to the side
    /// with negative z on the left, positive z on the right.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        public enum Mode { Locomotion, Paddle, Ride, Sit, Lie, Flail, Straddle }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] Transform model;
        [SerializeField] Transform hips;
        [SerializeField] Transform torso;
        [SerializeField] Transform head;
        [SerializeField] Transform shoulderL, shoulderR;
        [SerializeField] Transform hipL, hipR;
        [Tooltip("Optional elbow/knee joints (people built with forearms and shins). Positive bend = more bent.")]
        [SerializeField] Transform elbowL, elbowR;
        [SerializeField] Transform kneeL, kneeR;
        [SerializeField] Renderer[] skinRenderers;
        [SerializeField] Renderer shirtRenderer;
        [SerializeField] Renderer shortsRenderer;
        [SerializeField] Renderer[] hairRenderers;

        [Header("Colors used when not painted by code (staff)")]
        [SerializeField] bool paintOnStart;
        [SerializeField] Color defaultSkin = new(0.85f, 0.65f, 0.5f);
        [SerializeField] Color defaultShirt = Color.white;
        [SerializeField] Color defaultShorts = new(0.1f, 0.18f, 0.3f);
        [SerializeField] Color defaultHair = new(0.25f, 0.16f, 0.1f);
        [SerializeField] int defaultHairStyle;

        [Header("Feel")]
        [Tooltip("Seconds to blend from one pose to the next when the mode changes.")]
        [SerializeField] float blendTime = 0.3f;
        [Tooltip("Seconds for a pro's pop-up from lying on the board to standing (measured: about 1 s).")]
        [SerializeField] float popUpTimePro = 1f;
        [Tooltip("Seconds for a beginner's pop-up (real beginners take 1.5 to 2.5 s, often via the knees).")]
        [SerializeField] float popUpTimeBeginner = 2f;

        public Mode CurrentMode { get; set; } = Mode.Locomotion;

        /// <summary>Walking with a board under the right arm: that arm stays still, hugging the board.</summary>
        public bool CarryingBoard { get; set; }

        /// <summary>Throw both arms up and wave (cheering spectators, sitting or standing still).</summary>
        public bool CheerArms { get; set; }

        // ---- riding inputs, set by whoever is surfing ----

        /// <summary>Regular stance (left foot forward) or goofy (right foot forward).</summary>
        public bool Goofy { get; set; }

        /// <summary>0 = stiff, wobbly beginner, 1 = low, loose pro.</summary>
        public float Style { get; set; } = 0.5f;

        /// <summary>0..1 how low the rider crouches (on top of their style).</summary>
        public float Crouch { get; set; }

        /// <summary>-1..1 leaning into a turn (positive = toward the rider's toes).</summary>
        public float Lean { get; set; }

        /// <summary>0..1 in the air: knees tuck and the lead hand grabs the rail.</summary>
        public float Air { get; set; }

        /// <summary>Degrees the head and shoulders turn back toward the tail (cutbacks); the head leads.</summary>
        public float Twist { get; set; }

        /// <summary>0..1 arms up and forward (floaters, unweighting the nose).</summary>
        public float ArmsUp { get; set; }

        /// <summary>Paddling hard for a wave: faster strokes, head glancing back over the shoulder.</summary>
        public bool Sprint { get; set; }

        /// <summary>Tilt of the board (face slope, carve, spins). The body tilts with it around the board.</summary>
        public Quaternion BoardTilt { get; set; } = Quaternion.identity;

        /// <summary>Where the board sits under the rider (rig space); the tilt pivots here.</summary>
        public static readonly Vector3 BoardPivot = new(0f, 0.05f, 0f);

        struct PoseData
        {
            public Vector3 modelPos;
            public Quaternion modelRot;
            public float hipsY;
            public Quaternion torso, head, shoulderL, shoulderR, hipL, hipR;
            public Quaternion elbowL, elbowR, kneeL, kneeR;

            public static PoseData Lerp(in PoseData a, in PoseData b, float t) => new()
            {
                modelPos = Vector3.LerpUnclamped(a.modelPos, b.modelPos, t),
                modelRot = Quaternion.Slerp(a.modelRot, b.modelRot, t),
                hipsY = Mathf.LerpUnclamped(a.hipsY, b.hipsY, t),
                torso = Quaternion.Slerp(a.torso, b.torso, t),
                head = Quaternion.Slerp(a.head, b.head, t),
                shoulderL = Quaternion.Slerp(a.shoulderL, b.shoulderL, t),
                shoulderR = Quaternion.Slerp(a.shoulderR, b.shoulderR, t),
                hipL = Quaternion.Slerp(a.hipL, b.hipL, t),
                hipR = Quaternion.Slerp(a.hipR, b.hipR, t),
                elbowL = Quaternion.Slerp(a.elbowL, b.elbowL, t),
                elbowR = Quaternion.Slerp(a.elbowR, b.elbowR, t),
                kneeL = Quaternion.Slerp(a.kneeL, b.kneeL, t),
                kneeR = Quaternion.Slerp(a.kneeR, b.kneeR, t),
            };

            /// <summary>Bends elbows (forearm forward/up) and knees (shin back), in degrees; returns the pose.</summary>
            public PoseData Bend(float elbowLeft, float elbowRight, float kneeLeft, float kneeRight)
            {
                var pose = this;
                pose.elbowL = Quaternion.Euler(-elbowLeft, 0f, 0f);
                pose.elbowR = Quaternion.Euler(-elbowRight, 0f, 0f);
                pose.kneeL = Quaternion.Euler(kneeLeft, 0f, 0f);
                pose.kneeR = Quaternion.Euler(kneeRight, 0f, 0f);
                return pose;
            }
        }

        /// <summary>Hip joint to sole, standing straight (thigh 0.41 + shin 0.38 + foot 0.07).</summary>
        const float LegLength = 0.86f;
        const float Thigh = 0.41f, Shin = 0.38f, FootHeight = 0.07f;

        /// <summary>Number of hair styles this person can wear (index for Paint).</summary>
        public int HairStyles => hairRenderers.Length;

        float phase;
        float sprint;
        float blend; // 0 = idle, 1 = walking
        Vector3 lastPosition;
        float hipsHeight;
        Mode lastMode;
        float modeStart;
        bool popUp, started;
        PoseData current, snapshot;

        void Awake()
        {
            lastPosition = transform.position;
            hipsHeight = hips.localPosition.y;
        }

        void Start()
        {
            if (paintOnStart) Paint(defaultSkin, defaultShirt, defaultShorts, defaultHair, defaultHairStyle);
        }

        /// <summary>
        /// Colors the character. Every part is painted by material name: Skin, Shirt (also sleeves and caps),
        /// Shorts (also the legs of the shorts on the thighs) and Hair; eyes, brows and mouth keep their colors.
        /// Only the chosen hair style is shown.
        /// </summary>
        public void Paint(Color skin, Color shirt, Color shorts, Color hair, int hairStyle)
        {
            var block = new MaterialPropertyBlock();
            hairStyle = Mathf.Clamp(hairStyle, 0, hairRenderers.Length - 1);
            for (int i = 0; i < hairRenderers.Length; i++) hairRenderers[i].gameObject.SetActive(i == hairStyle);
            foreach (var r in skinRenderers) PaintRenderer(r, skin, shirt, shorts, hair, block);
            PaintRenderer(shirtRenderer, skin, shirt, shorts, hair, block);
            PaintRenderer(shortsRenderer, skin, shirt, shorts, hair, block);
            foreach (var r in hairRenderers) PaintRenderer(r, skin, shirt, shorts, hair, block);
        }

        static void PaintRenderer(Renderer renderer, Color skin, Color shirt, Color shorts, Color hair, MaterialPropertyBlock block)
        {
            if (renderer == null) return;
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                string name = mats[i].name;
                Color? color = name.StartsWith("Skin") ? skin : name.StartsWith("Shirt") ? shirt
                    : name.StartsWith("Shorts") ? shorts : name.StartsWith("Hair") ? hair : null;
                if (color == null) continue;
                block.Clear();
                block.SetColor(BaseColor, color.Value);
                renderer.SetPropertyBlock(block, i);
            }
        }

        void LateUpdate()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 moved = transform.position - lastPosition;
            moved.y = 0f;
            float speed = moved.magnitude / dt;
            lastPosition = transform.position;

            var target = CurrentMode switch
            {
                Mode.Paddle => Paddle(dt),
                Mode.Ride => Ride(),
                Mode.Sit => Sit(),
                Mode.Lie => Lie(),
                Mode.Flail => Flail(),
                Mode.Straddle => Straddle(),
                _ => Locomotion(speed, dt),
            };

            if (!started)
            {
                started = true;
                lastMode = CurrentMode;
                current = target;
            }
            else if (CurrentMode != lastMode)
            {
                // Crossfade from wherever the body is now, so nothing snaps.
                popUp = lastMode == Mode.Paddle && CurrentMode == Mode.Ride;
                lastMode = CurrentMode;
                snapshot = current;
                modeStart = Time.time;
            }

            float elapsed = Time.time - modeStart;
            float popUpTime = Mathf.Lerp(popUpTimeBeginner, popUpTimePro, Style);
            if (popUp && elapsed < popUpTime) current = PopUp(target, elapsed / popUpTime);
            else
            {
                popUp = false;
                float w = blendTime <= 0f ? 1f : Mathf.Clamp01(elapsed / blendTime);
                current = w >= 1f ? target : PoseData.Lerp(snapshot, target, w * w * (3f - 2f * w));
            }
            Apply(current);
        }

        void Apply(in PoseData pose)
        {
            model.localPosition = pose.modelPos;
            model.localRotation = pose.modelRot;
            hips.localPosition = new Vector3(0f, pose.hipsY, 0f);
            torso.localRotation = pose.torso;
            head.localRotation = pose.head;
            shoulderL.localRotation = pose.shoulderL;
            shoulderR.localRotation = pose.shoulderR;
            hipL.localRotation = pose.hipL;
            hipR.localRotation = pose.hipR;
            if (elbowL != null) elbowL.localRotation = pose.elbowL;
            if (elbowR != null) elbowR.localRotation = pose.elbowR;
            if (kneeL != null) kneeL.localRotation = pose.kneeL;
            if (kneeR != null) kneeR.localRotation = pose.kneeR;
        }

        static Quaternion E(float x, float y, float z) => Quaternion.Euler(x, y, z);

        PoseData Pose(Vector3 modelPos, Vector3 modelEuler, float hipsY, Vector3 torsoE, Vector3 headE,
            Vector3 shoulderLE, Vector3 shoulderRE, Vector3 hipLE, Vector3 hipRE) => new()
        {
            modelPos = modelPos,
            modelRot = Quaternion.Euler(modelEuler),
            hipsY = hipsY,
            torso = Quaternion.Euler(torsoE),
            head = Quaternion.Euler(headE),
            shoulderL = Quaternion.Euler(shoulderLE),
            shoulderR = Quaternion.Euler(shoulderRE),
            hipL = Quaternion.Euler(hipLE),
            hipR = Quaternion.Euler(hipRE),
            elbowL = Quaternion.identity,
            elbowR = Quaternion.identity,
            kneeL = Quaternion.identity,
            kneeR = Quaternion.identity,
        };

        PoseData Locomotion(float speed, float dt)
        {
            blend = Mathf.MoveTowards(blend, speed > 0.3f ? 1f : 0f, dt * 5f);
            phase += dt * Mathf.Lerp(1.2f, 7.5f, blend);
            float swing = Mathf.Sin(phase);
            float idle = Mathf.Sin(phase) * 2f * (1 - blend);
            if (CheerArms && blend < 0.5f)
            {
                // Standing and cheering: both arms up, waving, a little bounce.
                float wave = Mathf.Sin(Time.time * 9f) * 15f;
                return Pose(Vector3.zero, Vector3.zero, hipsHeight + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.03f,
                    new Vector3(-4f, 0, 0), new Vector3(-12f, 0, 0),
                    new Vector3(0, 0, -150f - wave), new Vector3(0, 0, 150f + wave),
                    Vector3.zero, Vector3.zero).Bend(20f, 20f, 6f, 6f);
            }
            return Pose(Vector3.zero, Vector3.zero,
                hipsHeight + Mathf.Abs(Mathf.Cos(phase)) * 0.04f * blend,
                new Vector3(3f * blend, 0, 0),
                new Vector3(0, Mathf.Sin(phase * 0.3f) * 4f * (1 - blend), 0),
                new Vector3(-swing * 28f * blend, 0, -5f - idle),
                CarryingBoard ? new Vector3(-6f, 0, CarryArmAngle) : new Vector3(swing * 28f * blend, 0, 5f + idle),
                new Vector3(swing * 32f * blend, 0, 0),
                new Vector3(-swing * 32f * blend, 0, 0))
                // Knees fold as each leg swings through; elbows a little bent, more while walking.
                .Bend(12f + Mathf.Abs(swing) * 15f * blend, CarryingBoard ? 25f : 12f + Mathf.Abs(swing) * 15f * blend,
                    4f + Mathf.Max(0f, -Mathf.Cos(phase)) * 45f * blend, 4f + Mathf.Max(0f, Mathf.Cos(phase)) * 45f * blend);
        }

        /// <summary>Right-arm roll that holds the arm out against a carried board.</summary>
        const float CarryArmAngle = 16f;

        /// <summary>How far the paddling arms angle out, so they pull through the water beside the board, not through it.</summary>
        const float PaddleArmOut = 22f;

        PoseData Paddle(float dt)
        {
            // Strokes per arm: cruising about 0.4 a second, sprinting into a wave about 0.9.
            sprint = Mathf.MoveTowards(sprint, Sprint ? 1f : 0f, dt * 2f);
            phase += dt * Mathf.Lerp(2.5f, 5.7f, sprint);
            // Lying face down on the board, chest arched up, head up. Sprinting: glance back over the shoulder.
            // Alternating crawl strokes: each arm reaches forward past the head, pulls back through the water
            // beside the board to the hip, then swings forward through the air. (Lying down, the arm's positive x
            // rotation goes hip -> up in the air -> past the head -> down in the water -> hip.)
            float stroke = phase * Mathf.Rad2Deg;
            float lookBack = sprint * Mathf.Clamp01(Mathf.Sin(Time.time * 2.4f) * 3f - 2f) * 70f;
            return Pose(new Vector3(0, 0.22f, -0.35f), new Vector3(90f, 0, 0), hipsHeight, new Vector3(-10f, 0, Mathf.Sin(phase) * 3f),
                new Vector3(-35f + lookBack * 0.2f, lookBack, 0),
                new Vector3(stroke % 360f, 0, -PaddleArmOut),
                new Vector3((stroke + 180f) % 360f, 0, PaddleArmOut),
                new Vector3(Mathf.Sin(phase * 2f) * 8f, 0, -4f),
                new Vector3(-Mathf.Sin(phase * 2f) * 8f, 0, 4f))
                // Elbows fold as each arm swings over through the air, nearly straight on the pull.
                .Bend(PaddleElbow(stroke), PaddleElbow(stroke + 180f), 12f + Mathf.Sin(phase * 2f) * 6f, 12f - Mathf.Sin(phase * 2f) * 6f);
        }

        static float PaddleElbow(float strokeDegrees)
        {
            float a = Mathf.Repeat(strokeDegrees, 360f);
            return a < 180f ? 15f + Mathf.Sin(a * Mathf.Deg2Rad) * 75f : 15f;
        }

        /// <summary>
        /// Standing sideways on the board, knees bent "like sitting in a low chair", eyes down the line. The hips
        /// drop and the knees fold to match (thigh forward, shin back the same angle, so the feet stay under the
        /// hips about shoulder width apart). Front arm leads toward the nose at
        /// waist-chest height, back arm low behind. Beginners bend at the waist ("poo stance"), stand tall and
        /// wave their arms; better surfers pump about once a second. Lean tips the body over the rail, Twist turns
        /// head then shoulders back (cutbacks), ArmsUp lifts the arms (floaters), Air tucks and grabs the rail.
        /// </summary>
        PoseData Ride()
        {
            float t = Time.time;
            float side = Goofy ? -1f : 1f;              // regular: left foot forward
            float rookie = 1f - Style;
            float crouch = Mathf.Clamp01(Mathf.Lerp(0.25f, 0.65f, Style) + Crouch + Air * 0.35f);
            // Pump: compress and extend about once a second (not beginners), torso quiet.
            float pump = Mathf.Sin(t * Mathf.PI * 2f) * Style * (1f - Air);
            float drop = Mathf.Min(MaxHipsDrop, Mathf.Lerp(0.05f, MaxHipsDrop, crouch) + pump * 0.04f);
            float hipsY = hipsHeight - drop;
            float knees, kneeBend, splay;
            if (kneeL != null)
            {
                // Feet about shoulder width apart (beginners a bit wider); the knees fold to reach the deck.
                splay = 13f + rookie * 4f;
                float legDown = hipsY / Mathf.Cos(splay * Mathf.Deg2Rad) - FootHeight;
                knees = Mathf.Acos(Mathf.Clamp(legDown / (Thigh + Shin), 0.2f, 1f)) * Mathf.Rad2Deg;
                kneeBend = knees * 2f;
            }
            else
            {
                // Knee-less legs: angle them forward a little and splay them so the feet reach the deck.
                knees = 4f + crouch * 12f;
                kneeBend = 0f;
                float reach = LegLength * Mathf.Cos(knees * Mathf.Deg2Rad);
                splay = Mathf.Acos(Mathf.Clamp(hipsY / reach, 0.3f, 1f)) * Mathf.Rad2Deg + 3f;
            }
            float wobble = (Mathf.Sin(t * 5.3f) * 0.6f + Mathf.Sin(t * 8.7f) * 0.4f) * rookie;
            float sway = Mathf.Sin(t * 2.3f);
            float lean = Mathf.Clamp(Lean, -1f, 1f);
            // Beginners look down at their feet now and then (a classic mistake).
            float lookDown = rookie * Mathf.Clamp01((Mathf.PerlinNoise(t * 0.4f, Goofy ? 3.1f : 7.7f) - 0.6f) * 5f) * 25f;

            // Front arm (the one on the nose side) leads toward travel; back arm low behind. Both rise as the
            // body leans; beginners hold them wide and windmill.
            float frontOut = 70f + rookie * 12f + wobble * 25f + sway * 4f;
            float backOut = 28f + rookie * 42f - wobble * 20f;
            var front = new Vector3(-15f - lean * 25f, 0f, frontOut + Mathf.Max(0f, -lean) * 15f);
            var back = new Vector3(-25f - lean * 20f, 0f, backOut + Mathf.Max(0f, lean) * 20f);
            // Floater: both arms up and forward to unweight the nose.
            front = Vector3.Lerp(front, new Vector3(-40f, 0f, 60f), ArmsUp);
            back = Vector3.Lerp(back, new Vector3(-30f, 0f, 50f), ArmsUp);
            // Grab: the lead hand reaches down past the toes to the rail.
            front = Vector3.Lerp(front, new Vector3(-70f, 0f, 12f), Air);
            var left = Goofy ? back : front;
            var right = Goofy ? front : back;

            float tuck = Air * 30f;
            // Twist (cutback): the head turns first and furthest, the shoulders follow.
            float headYaw = Mathf.Lerp(70f, 90f, Style) - Twist;
            float torsoYaw = Mathf.Lerp(12f, 25f, Style) - Twist * 0.6f;
            float torsoPitch = (kneeL != null ? Mathf.Lerp(30f, 12f, Style) + crouch * 8f : Mathf.Lerp(35f, 18f, Style) + crouch * 15f)
                + lean * 15f + pump * 3f;
            var pose = Pose(new Vector3(0f, 0.08f, 0f), new Vector3(0f, 90f * side, wobble * 5f),
                hipsY,
                new Vector3(torsoPitch, -torsoYaw * side, lean * -6f),
                new Vector3(-8f - crouch * 12f + lookDown, -headYaw * side, 0f),
                new Vector3(left.x, 0f, -left.z),
                new Vector3(right.x, 0f, right.z),
                new Vector3(-knees - tuck - lean * 10f, 0f, -splay),
                new Vector3(-knees - tuck - lean * 10f, 0f, splay));
            // Elbows soft, the lead arm reaching; knees fold with the crouch (and tuck up in the air).
            float frontElbow = Mathf.Lerp(20f + rookie * 10f, 35f, Air), backElbow = 35f - ArmsUp * 15f;
            pose = pose.Bend(Goofy ? backElbow : frontElbow, Goofy ? frontElbow : backElbow,
                kneeBend + tuck * 1.5f, kneeBend + tuck * 1.5f);

            // The whole rider tilts with the board around the deck, so the feet stay planted.
            pose.modelPos = BoardPivot + BoardTilt * (pose.modelPos - BoardPivot);
            pose.modelRot = BoardTilt * pose.modelRot;
            return pose;
        }

        /// <summary>Deepest the hips drop when riding (m).</summary>
        const float MaxHipsDrop = 0.32f;

        /// <summary>
        /// Pop-up: from lying down, press the chest up (hands beside the ribs), bring the feet through into a
        /// low tuck (back foot first), then rise into the stance. Beginners take about twice as long.
        /// </summary>
        PoseData PopUp(in PoseData ride, float t)
        {
            float side = Goofy ? -1f : 1f;
            var pushUp = Pose(new Vector3(0, 0.3f, -0.35f), new Vector3(62f, 0, 0), hipsHeight, new Vector3(-10f, 0, 0),
                new Vector3(-40f, 0, 0), new Vector3(-85f, 0, -12f), new Vector3(-85f, 0, 12f),
                new Vector3(-10f, 0, -4f), new Vector3(-10f, 0, 4f)).Bend(10f, 10f, 15f, 15f);
            var tuck = Pose(new Vector3(0, 0.08f, 0), new Vector3(0, 90f * side, 0), hipsHeight - 0.45f,
                new Vector3(50f, -10f * side, 0), new Vector3(-30f, -50f * side, 0),
                new Vector3(-60f, 0, -25f), new Vector3(-60f, 0, 25f),
                kneeL != null ? new Vector3(-75f, 0, -28f) : new Vector3(-25f, 0, -45f),
                kneeL != null ? new Vector3(-75f, 0, 28f) : new Vector3(-25f, 0, 45f)).Bend(35f, 35f, 125f, 125f);
            tuck.modelPos = BoardPivot + BoardTilt * (tuck.modelPos - BoardPivot);
            tuck.modelRot = BoardTilt * tuck.modelRot;

            static float S(float x) => x * x * (3f - 2f * x);
            // Measured pop-ups spend about 60% of the time pressing up, then the feet come through fast.
            if (t < 0.5f) return PoseData.Lerp(snapshot, pushUp, S(t / 0.5f));
            if (t < 0.7f) return PoseData.Lerp(pushUp, tuck, S((t - 0.5f) / 0.2f));
            return PoseData.Lerp(tuck, ride, S((t - 0.7f) / 0.3f));
        }

        /// <summary>
        /// Wipeout: a moment of flailing as they get thrown, then (like real surfers are taught) arms up over the
        /// head, forearms crossed, curled loosely while tumbling and coming back up.
        /// </summary>
        PoseData Flail()
        {
            float t = Time.time * 14f;
            var flail = Pose(new Vector3(0f, 0.4f, 0f), Vector3.zero, hipsHeight,
                new Vector3(Mathf.Sin(t * 0.5f) * 20f, 0, 0), new Vector3(-20f, Mathf.Sin(t * 0.7f) * 30f, 0),
                new Vector3(-(t * Mathf.Rad2Deg) % 360f, 0, -60f),
                new Vector3(-(t * Mathf.Rad2Deg + 120f) % 360f, 0, 60f),
                new Vector3(Mathf.Sin(t) * 40f, 0, -30f), new Vector3(-Mathf.Sin(t) * 40f, 0, 30f))
                .Bend(40f + Mathf.Sin(t * 1.3f) * 40f, 40f + Mathf.Cos(t * 1.1f) * 40f, 30f + Mathf.Sin(t) * 30f, 30f - Mathf.Sin(t) * 30f);
            // Forearms folded over the head, knees curled.
            var cover = Pose(new Vector3(0f, 0.4f, 0f), Vector3.zero, hipsHeight,
                new Vector3(20f, 0, 0), new Vector3(30f, 0, 0),
                new Vector3(-160f, 0, 20f), new Vector3(-160f, 0, -20f),
                new Vector3(-35f, 0, -10f), new Vector3(-35f, 0, 10f)).Bend(110f, 110f, 60f, 60f);
            float covered = Mathf.Clamp01((Time.time - modeStart - 0.45f) / 0.25f);
            return PoseData.Lerp(flail, cover, covered);
        }

        /// <summary>
        /// Waiting in the lineup: sitting up on the board, legs dangling in the water either side, hands resting
        /// on the thighs, looking around at the horizon for the next wave.
        /// </summary>
        PoseData Straddle()
        {
            float t = Time.time;
            float kick = Mathf.Sin(t * 1.3f) * 8f;
            return Pose(new Vector3(0f, 0.12f - hipsHeight, 0f), Vector3.zero, hipsHeight, new Vector3(-4f, 0, 0),
                new Vector3(Mathf.Sin(t * 0.5f) * 6f - 4f, Mathf.Sin(t * 0.35f) * 35f, 0),
                new Vector3(-35f, 0, -12f), new Vector3(-35f, 0, 12f),
                new Vector3(-35f + kick, 0, -32f), new Vector3(-35f - kick, 0, 32f))
                // Hands resting on the thighs, shins hanging down in the water, gently kicking.
                .Bend(50f, 50f, 75f - kick, 75f + kick);
        }

        PoseData Sit()
        {
            var shoulders = CheerArms
                ? new Vector2(150f + Mathf.Sin(Time.time * 9f) * 15f, 0f)
                : new Vector2(8f, -20f);
            return Pose(new Vector3(0, -0.35f, 0), Vector3.zero, hipsHeight, new Vector3(-5f, 0, 0),
                new Vector3(0, Mathf.Sin(Time.time * 0.4f) * 25f, 0),
                new Vector3(shoulders.y, 0, -shoulders.x),
                new Vector3(shoulders.y, 0, shoulders.x),
                new Vector3(-85f, 0, 4f), new Vector3(-85f, 0, -4f))
                // Shins hang down from the seat; hands in the lap (or waving when cheering).
                .Bend(CheerArms ? 20f : 35f, CheerArms ? 20f : 35f, 85f, 85f);
        }

        /// <summary>Lying on the back (sunbathing on a lounger), hands behind the head now and then.</summary>
        PoseData Lie()
        {
            return Pose(new Vector3(0, 0.12f, 1.25f), new Vector3(-90f, 0, 0), hipsHeight, new Vector3(-8f, 0, 0),
                new Vector3(-20f, Mathf.Sin(Time.time * 0.3f) * 10f, 0),
                new Vector3(-170f, 0, 25f), new Vector3(-170f, 0, -25f),
                new Vector3(0f, 0, 3f), new Vector3(-12f, 0, -3f))
                // Hands folded behind the head, one knee up.
                .Bend(120f, 120f, 0f, 30f);
        }
    }
}
