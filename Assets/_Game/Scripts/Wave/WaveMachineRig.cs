using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The wave machine you can see: a wall of air chambers along the pool's deep end, blowers behind them, a
    /// pressure dial and a status lamp. Blowers spin up while the machine recharges and the dial fills; when a wave
    /// leaves, the chambers fire one after another (pistons punch down, valves spin, spray bursts out of the mouths)
    /// in the direction the wave peels. Chambers past the pool's current width stay hidden until it's dug out.
    /// Upgrade parts (extra blowers, air tanks, timer, control cabin, smart screen...) show once owned, and
    /// Heavy-Duty Pumps repaint the machine safety orange. Purely visual: <see cref="WaveMachine"/> does the work.
    /// </summary>
    public class WaveMachineRig : MonoBehaviour
    {
        [System.Serializable]
        public class Chamber
        {
            public Transform root;
            public Transform piston;
            public Transform valve;
            public Renderer body;
            [HideInInspector] public Vector3 pistonRest;
            [HideInInspector] public float firedAt = -10f;
        }

        [System.Serializable]
        public class UpgradePart
        {
            public string upgrade;
            public GameObject part;
        }

        [SerializeField] WaveMachine machine;
        [SerializeField] PoolArea pool;
        [SerializeField] Chamber[] chambers;
        [SerializeField] Transform[] fans;
        [Tooltip("Blowers that only spin when their upgrade is owned (same order as fans; empty = always).")]
        [SerializeField] string[] fanUpgrades;
        [SerializeField] Renderer[] blowerBodies;
        [SerializeField] Transform dialNeedle;
        [SerializeField] Renderer lampBulb;
        [SerializeField] Material lampReady, lampCharging, lampBroken;
        [SerializeField] ParticleSystem spray;
        [SerializeField] UpgradePart[] parts;
        [SerializeField] TextMeshPro smartScreen;
        [Tooltip("Seconds between neighbouring chambers firing.")]
        [SerializeField] float stagger = 0.07f;
        [SerializeField] float pistonDrop = 0.9f;

        UpgradeShop shop;
        float fanSpeed, nextPartCheck, lastFire = -10f, nextScreen;
        Color bodyColor = new(-1, 0, 0);

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color Worn = new(0.5f, 0.56f, 0.6f), Blue = new(0.2f, 0.45f, 0.68f), DeepBlue = new(0.14f, 0.3f, 0.6f),
            SafetyOrange = new(0.98f, 0.52f, 0.16f);

        void Awake()
        {
            foreach (var chamber in chambers)
                if (chamber.piston != null) chamber.pistonRest = chamber.piston.localPosition;
        }

        void OnEnable()
        {
            if (machine != null) machine.WaveFired += OnWaveFired;
            shop = FindAnyObjectByType<UpgradeShop>();
            nextPartCheck = 0f;
        }

        void OnDisable()
        {
            if (machine != null) machine.WaveFired -= OnWaveFired;
        }

        void OnWaveFired(WaveLevel level, WaveDirection direction)
        {
            lastFire = Time.time;
            int count = 0; // only the chambers built for the pool's current width
            foreach (var chamber in chambers)
                if (chamber.root != null && chamber.root.gameObject.activeSelf) count++;
            float mid = (count - 1) / 2f;
            for (int i = 0; i < count; i++)
            {
                // Peaks fire from the middle out; lefts and rights sweep across from one side.
                float order = direction == WaveDirection.Peak ? Mathf.Abs(i - mid)
                    : direction == WaveDirection.Right ? i : count - 1 - i;
                chambers[i].firedAt = Time.time + order * stagger;
            }
            Sfx.Play(Sound.AirBlast, transform.position);
        }

        void Update()
        {
            if (Time.time >= nextPartCheck)
            {
                nextPartCheck = Time.time + 0.5f;
                RefreshParts();
            }
            bool broken = machine != null && machine.Broken;
            bool charging = machine != null && !machine.Ready;

            // Blowers: idle hum, roaring while recharging, stopped when broken.
            float target = broken ? 0f : charging ? 900f : Time.time - lastFire < 1.2f ? 700f : 90f;
            fanSpeed = Mathf.MoveTowards(fanSpeed, target, (target > fanSpeed ? 1400f : 500f) * Time.deltaTime);
            for (int i = 0; i < fans.Length; i++)
            {
                if (fans[i] == null || !fans[i].gameObject.activeInHierarchy) continue;
                fans[i].Rotate(0f, 0f, fanSpeed * (1f + i * 0.13f) * Time.deltaTime, Space.Self);
            }

            // Dial: recharge progress, sweeping from -120 (empty) to +120 degrees (full); a wobble when full.
            if (dialNeedle != null)
            {
                float progress = broken ? 0f : machine != null ? machine.ReadyProgress : 1f;
                float angle = Mathf.Lerp(120f, -120f, progress) + (progress >= 1f ? Mathf.Sin(Time.time * 9f) * 1.5f : 0f);
                dialNeedle.localRotation = Quaternion.Slerp(dialNeedle.localRotation, Quaternion.Euler(0f, 0f, angle), 10f * Time.deltaTime);
            }

            if (lampBulb != null)
            {
                var mat = broken ? (Mathf.Repeat(Time.time, 0.6f) < 0.3f ? lampBroken : lampCharging) : charging ? lampCharging : lampReady;
                if (lampBulb.sharedMaterial != mat) lampBulb.sharedMaterial = mat;
            }

            foreach (var chamber in chambers) AnimateChamber(chamber);
            UpdateScreen(broken, charging);
        }

        void AnimateChamber(Chamber chamber)
        {
            if (chamber.root == null || !chamber.root.gameObject.activeInHierarchy) return;
            float t = Time.time - chamber.firedAt;
            if (t < 0f || t > 1.6f)
            {
                if (chamber.piston != null) chamber.piston.localPosition = chamber.pistonRest;
                return;
            }
            // Piston punches down fast, then rises slowly; the valve spins; spray bursts as it hits bottom.
            float down = t < 0.18f ? Mathf.SmoothStep(0f, 1f, t / 0.18f) : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.18f) / 1.3f);
            if (chamber.piston != null) chamber.piston.localPosition = chamber.pistonRest + Vector3.down * (pistonDrop * down);
            if (chamber.valve != null) chamber.valve.Rotate(540f * Time.deltaTime * (1f - Mathf.Clamp01(t / 1.2f)), 0f, 0f, Space.Self);
            if (spray != null && t >= 0.16f && t - Time.deltaTime < 0.16f)
            {
                // Out over the water: the pool's +x (pools can face any way).
                Vector3 outward = pool != null && pool.Water != null ? pool.Water.transform.right : Vector3.right;
                Vector3 across = Vector3.Cross(Vector3.up, outward);
                Vector3 mouth = chamber.root.position + outward * 3.3f + Vector3.up * 0.2f;
                var emit = new ParticleSystem.EmitParams { applyShapeToPosition = true };
                for (int i = 0; i < 22; i++)
                {
                    // A white burst out of the mouth, thrown up and out over the water.
                    emit.position = mouth + outward * Random.Range(0f, 0.6f) + Vector3.up * Random.Range(-0.3f, 0.2f) + across * Random.Range(-1.3f, 1.3f);
                    emit.velocity = outward * Random.Range(2.5f, 6f) + Vector3.up * Random.Range(1.5f, 4.5f) + across * Random.Range(-1.2f, 1.2f);
                    emit.startSize = Random.Range(0.3f, 0.75f);
                    spray.Emit(emit, 1);
                }
            }
        }

        void RefreshParts()
        {
            // Chambers beyond the pool's current width aren't built yet.
            if (pool != null && pool.Water != null)
            {
                var w = pool.Water.transform;
                float north = w.InverseTransformPoint(new Vector3(0f, 0f, pool.SouthEdge)).z + pool.Water.Width;
                if (pool.StageCount == 0) north = pool.Water.Width; // always full size: every chamber is built
                foreach (var chamber in chambers)
                    if (chamber.root != null)
                    {
                        // Built once the pool reaches past the chamber's middle (the last one may overhang a little).
                        bool built = w.InverseTransformPoint(chamber.root.position).z < north;
                        if (chamber.root.gameObject.activeSelf != built) chamber.root.gameObject.SetActive(built);
                    }
            }
            if (shop == null) return;
            foreach (var part in parts)
            {
                if (part.part == null) continue;
                bool show = string.IsNullOrEmpty(part.upgrade) || shop.IsOwned(part.upgrade);
                if (part.part.activeSelf != show)
                {
                    part.part.SetActive(show);
                    if (show && Time.timeSinceLevelLoad > 2f && part.part.GetComponent<PopIn>() == null) part.part.AddComponent<PopIn>();
                }
            }
            // Paint: worn grey to start, blue once the Intermediate program is in, deep blue for Pro, orange for heavy-duty.
            var color = shop.IsOwned("heavy_duty_pumps") ? SafetyOrange : shop.IsOwned("pro_waves") ? DeepBlue
                : shop.IsOwned("intermediate_waves") ? Blue : Worn;
            if (color == bodyColor) return;
            bodyColor = color;
            var block = new MaterialPropertyBlock();
            foreach (var chamber in chambers) Paint(chamber.body, block, color);
            foreach (var body in blowerBodies) Paint(body, block, color);
        }

        static void Paint(Renderer renderer, MaterialPropertyBlock block, Color color)
        {
            if (renderer == null) return;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !materials[i].name.StartsWith("MachineBody")) continue;
                renderer.GetPropertyBlock(block, i);
                block.SetColor(BaseColor, color);
                renderer.SetPropertyBlock(block, i);
            }
        }

        void UpdateScreen(bool broken, bool charging)
        {
            if (smartScreen == null || !smartScreen.gameObject.activeInHierarchy || Time.time < nextScreen || machine == null) return;
            nextScreen = Time.time + 0.25f;
            string wave = machine.Preset != null ? machine.Preset.name : $"{machine.Selected} {machine.SelectedDirection}";
            smartScreen.text = broken ? "<color=#FF6B57>BROKEN - CALL A MECHANIC</color>"
                : charging ? $"CHARGING {Mathf.RoundToInt(machine.ReadyProgress * 100f)}%\n<size=70%>{wave.ToUpperInvariant()}</size>"
                : $"<color=#7EE0D2>READY</color>\n<size=70%>{wave.ToUpperInvariant()}</size>";
        }
    }
}
