using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Wear and repairs for a wave machine. Every wave wears the machine a little (bigger waves more). Past 70%
    /// it needs a service and may break down: broken machines smoke, flash a red light and send no waves until
    /// someone repairs them. Hold E at the machine house to service or repair it (free, takes a few seconds),
    /// or hire the Mechanic to do it automatically. Heavy-Duty Pumps halve the wear.
    /// </summary>
    [RequireComponent(typeof(WaveMachine))]
    public class MachineService : MonoBehaviour, IInteractable, IHoldInteractable
    {
        public static float WearMultiplier = 1f;

        [Tooltip("Wear added per wave sent, by wave type (Beginner, Intermediate, Pro). 1 = worn out.")]
        [SerializeField] float[] wearPerWave = { 0.004f, 0.006f, 0.009f };
        [Tooltip("Above this wear the machine needs a service and can break down.")]
        [Range(0f, 1f)] [SerializeField] float warnAt = 0.7f;
        [Tooltip("Seconds of holding E to fix a broken machine (a service of a working one takes half).")]
        [SerializeField] float repairSeconds = 3f;
        [SerializeField] ParticleSystem smoke;
        [SerializeField] GameObject brokenLight;

        WaveMachine machine;
        float repairProgress, lastHoldTime;

        public float Wear { get; private set; }
        public bool Broken { get; private set; }
        public bool NeedsService => Broken || Wear >= warnAt;
        public float RepairProgress => repairProgress;
        string PoolName => machine.Pool != null ? machine.Pool.DisplayName : "the";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatics() => WearMultiplier = 1f;

        void Awake()
        {
            machine = GetComponent<WaveMachine>();
            ShowBroken(false);
        }

        public string Prompt
        {
            get
            {
                if (Broken) return $"Hold E to repair the machine ({Mathf.RoundToInt(repairProgress / repairSeconds * 100f)}%)";
                if (Wear >= 0.3f) return $"Hold E to service the machine (wear {Mathf.RoundToInt(Wear * 100f)}%)";
                return $"Wave machine (wear {Mathf.RoundToInt(Wear * 100f)}%)";
            }
        }

        public void Interact() { }

        public void Hold(float deltaTime)
        {
            if (!Broken && Wear < 0.3f) return;
            if (Time.time - lastHoldTime > 0.3f) repairProgress = 0f; // let go: start over
            lastHoldTime = Time.time;
            repairProgress += deltaTime;
            if (repairProgress >= (Broken ? repairSeconds : repairSeconds * 0.5f)) Repair();
        }

        /// <summary>A wave was sent: add wear, and maybe break down once the machine is worn.</summary>
        public void AddWear(WaveLevel level, int waves)
        {
            if (Broken) return;
            bool wasWarned = Wear >= warnAt;
            Wear = Mathf.Clamp01(Wear + wearPerWave[Mathf.Clamp((int)level, 0, wearPerWave.Length - 1)] * waves * WearMultiplier);
            if (!wasWarned && Wear >= warnAt)
                Notifications.Post($"The {PoolName} machine needs a service! Hold E at the machine house.", true);
            // Past the warning, each wave has a growing chance to break it; at 100% it always breaks.
            if (Wear >= 1f || (Wear >= warnAt && Random.value < (Wear - warnAt) * 0.12f)) Break();
        }

        public void Break()
        {
            Broken = true;
            repairProgress = 0f;
            ShowBroken(true);
            Sfx.Play(Sound.Error);
            Sfx.Play(Sound.Splash, transform.position);
            Notifications.Post($"The {PoolName} machine broke down! Hold E at the machine house to fix it.", true);
        }

        /// <summary>Fixes the machine and resets its wear (player, or the Mechanic).</summary>
        public void Repair()
        {
            bool wasBroken = Broken;
            Broken = false;
            Wear = 0f;
            repairProgress = 0f;
            ShowBroken(false);
            Sfx.Play(Sound.Buy);
            Notifications.Post(wasBroken ? $"{PoolName} machine fixed. Back to surfing!" : $"{PoolName} machine serviced. Good as new.");
        }

        public void Restore(float wear, bool broken)
        {
            Wear = Mathf.Clamp01(wear);
            Broken = broken;
            ShowBroken(broken);
        }

        void ShowBroken(bool broken)
        {
            if (brokenLight != null) brokenLight.SetActive(broken);
            if (smoke == null) return;
            if (broken) smoke.Play();
            else smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void Update()
        {
            if (brokenLight != null && Broken) brokenLight.SetActive(Mathf.Repeat(Time.time, 0.8f) < 0.45f);
        }
    }
}
