using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A low-poly person built from separate parts (Blender: tools/blender/build_models.py) and animated in code.
    /// Locomotion switches between idle and walking by how fast the character moves; Paddle lies on the board and
    /// strokes, Ride crouches sideways with arms out, Sit sits with legs forward.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        public enum Mode { Locomotion, Paddle, Ride, Sit, Lie }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] Transform model;
        [SerializeField] Transform hips;
        [SerializeField] Transform torso;
        [SerializeField] Transform head;
        [SerializeField] Transform shoulderL, shoulderR;
        [SerializeField] Transform hipL, hipR;
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

        public Mode CurrentMode { get; set; } = Mode.Locomotion;

        /// <summary>Walking with a board under the right arm: that arm stays still, hugging the board.</summary>
        public bool CarryingBoard { get; set; }

        /// <summary>While sitting: throw both arms up and wave (grandstand cheering).</summary>
        public bool CheerArms { get; set; }

        float phase;
        float blend; // 0 = idle, 1 = walking
        Vector3 lastPosition;
        float hipsHeight;

        void Awake()
        {
            lastPosition = transform.position;
            hipsHeight = hips.localPosition.y;
        }

        void Start()
        {
            if (paintOnStart) Paint(defaultSkin, defaultShirt, defaultShorts, defaultHair, defaultHairStyle);
        }

        /// <summary>Colors the character: skin, shirt, shorts and hair (hair -1 alpha hides it).</summary>
        public void Paint(Color skin, Color shirt, Color shorts, Color hair, int hairStyle)
        {
            var block = new MaterialPropertyBlock();
            foreach (var r in skinRenderers) PaintRenderer(r, skin, "Skin", block);
            PaintRenderer(shirtRenderer, shirt, null, block);
            PaintRenderer(shortsRenderer, shorts, null, block);
            for (int i = 0; i < hairRenderers.Length; i++)
            {
                hairRenderers[i].gameObject.SetActive(i == hairStyle);
                PaintRenderer(hairRenderers[i], hair, null, block);
            }
        }

        /// <summary>Tints one renderer; with onlyMaterial set, only material slots whose name contains it (keeps eyes dark).</summary>
        static void PaintRenderer(Renderer renderer, Color color, string onlyMaterial, MaterialPropertyBlock block)
        {
            if (renderer == null) return;
            var mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (onlyMaterial != null && (mats[i] == null || !mats[i].name.Contains(onlyMaterial))) continue;
                block.Clear();
                block.SetColor(BaseColor, color);
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

            switch (CurrentMode)
            {
                case Mode.Locomotion: Locomotion(speed, dt); break;
                case Mode.Paddle: Paddle(dt); break;
                case Mode.Ride: Ride(); break;
                case Mode.Sit: Sit(); break;
                case Mode.Lie: Lie(); break;
            }
        }

        void Locomotion(float speed, float dt)
        {
            blend = Mathf.MoveTowards(blend, speed > 0.3f ? 1f : 0f, dt * 5f);
            phase += dt * Mathf.Lerp(1.2f, 7.5f, blend);
            float swing = Mathf.Sin(phase);
            SetModel(Vector3.zero, Vector3.zero);
            hips.localPosition = new Vector3(0, hipsHeight + Mathf.Abs(Mathf.Cos(phase)) * 0.04f * blend, 0);
            torso.localEulerAngles = new Vector3(3f * blend, 0, 0);
            head.localEulerAngles = new Vector3(0, Mathf.Sin(phase * 0.3f) * 4f * (1 - blend), 0);
            hipL.localEulerAngles = new Vector3(swing * 32f * blend, 0, 0);
            hipR.localEulerAngles = new Vector3(-swing * 32f * blend, 0, 0);
            shoulderL.localEulerAngles = new Vector3(-swing * 28f * blend, 0, 6f + Mathf.Sin(phase) * 2f * (1 - blend));
            shoulderR.localEulerAngles = CarryingBoard
                ? new Vector3(-6f, 0, CarryArmAngle)
                : new Vector3(swing * 28f * blend, 0, -6f - Mathf.Sin(phase) * 2f * (1 - blend));
        }

        /// <summary>Right-arm roll that holds the arm out against a carried board.</summary>
        const float CarryArmAngle = 16f;

        /// <summary>How far the paddling arms angle out, so they pull through the water beside the board, not through it.</summary>
        const float PaddleArmOut = 22f;

        void Paddle(float dt)
        {
            phase += dt * 4f;
            // Lying face down on the board: the whole body pitched forward, head up a little.
            SetModel(new Vector3(0, 0.22f, -0.35f), new Vector3(90f, 0, 0));
            hips.localPosition = new Vector3(0, hipsHeight, 0);
            torso.localEulerAngles = Vector3.zero;
            head.localEulerAngles = new Vector3(-35f, 0, 0);
            // Alternating crawl strokes: each arm reaches forward, pulls back beside the board, then swings over.
            float stroke = phase * Mathf.Rad2Deg;
            shoulderL.localEulerAngles = new Vector3(-stroke % 360f, 0, -PaddleArmOut);
            shoulderR.localEulerAngles = new Vector3(-(stroke + 180f) % 360f, 0, PaddleArmOut);
            hipL.localEulerAngles = new Vector3(Mathf.Sin(phase * 2f) * 8f, 0, 4f);
            hipR.localEulerAngles = new Vector3(-Mathf.Sin(phase * 2f) * 8f, 0, -4f);
        }

        void Ride()
        {
            float sway = Mathf.Sin(Time.time * 2.3f);
            // Sideways on the board, knees bent (hips low, legs spread), arms out for balance.
            SetModel(new Vector3(0, 0.08f, 0), new Vector3(0, -90f, sway * 4f));
            hips.localPosition = new Vector3(0, hipsHeight - 0.22f, 0);
            torso.localEulerAngles = new Vector3(18f, 0, 0);
            head.localEulerAngles = new Vector3(-10f, 70f, 0);
            hipL.localEulerAngles = new Vector3(-25f, 0, 22f);
            hipR.localEulerAngles = new Vector3(25f, 0, -22f);
            shoulderL.localEulerAngles = new Vector3(0, 0, 70f + sway * 10f);
            shoulderR.localEulerAngles = new Vector3(0, 0, -70f + sway * 10f);
        }

        void Sit()
        {
            SetModel(new Vector3(0, -0.35f, 0), Vector3.zero);
            hips.localPosition = new Vector3(0, hipsHeight, 0);
            torso.localEulerAngles = new Vector3(-5f, 0, 0);
            head.localEulerAngles = new Vector3(0, Mathf.Sin(Time.time * 0.4f) * 25f, 0);
            hipL.localEulerAngles = new Vector3(-85f, 0, 4f);
            hipR.localEulerAngles = new Vector3(-85f, 0, -4f);
            if (CheerArms)
            {
                float wave = Mathf.Sin(Time.time * 9f) * 15f;
                shoulderL.localEulerAngles = new Vector3(0, 0, 150f + wave);
                shoulderR.localEulerAngles = new Vector3(0, 0, -150f - wave);
                return;
            }
            shoulderL.localEulerAngles = new Vector3(-20f, 0, 8f);
            shoulderR.localEulerAngles = new Vector3(-20f, 0, -8f);
        }

        /// <summary>Lying on the back (sunbathing on a lounger), hands behind the head now and then.</summary>
        void Lie()
        {
            SetModel(new Vector3(0, 0.12f, 1.25f), new Vector3(-90f, 0, 0));
            hips.localPosition = new Vector3(0, hipsHeight, 0);
            torso.localEulerAngles = new Vector3(-8f, 0, 0);
            head.localEulerAngles = new Vector3(-20f, Mathf.Sin(Time.time * 0.3f) * 10f, 0);
            hipL.localEulerAngles = new Vector3(0f, 0, 3f);
            hipR.localEulerAngles = new Vector3(0f, 0, -3f);
            shoulderL.localEulerAngles = new Vector3(-170f, 0, 25f);
            shoulderR.localEulerAngles = new Vector3(-170f, 0, -25f);
        }

        void SetModel(Vector3 position, Vector3 euler)
        {
            model.localPosition = position;
            model.localEulerAngles = euler;
        }
    }
}
