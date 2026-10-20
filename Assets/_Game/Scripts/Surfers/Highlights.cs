using System;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Big moments in the water (a pro's tube ride or air): spectators near it throw their arms up, and if you are
    /// close by and looking at it (and not surfing yourself), the game drops into slow motion for a moment. The
    /// slow motion is short and has a cooldown, because it slows the whole park. See docs/research/surf-feel.md.
    /// </summary>
    public class Highlights : MonoBehaviour
    {
        static Highlights instance;

        /// <summary>Raised for every highlight: where, and how big (0..1).</summary>
        public static event Action<Vector3, float> Happened;

        [SerializeField] float slowMoScale = 0.4f;
        [Tooltip("Real seconds of slow motion (never more than about 1 s: it slows the whole park).")]
        [SerializeField] float slowMoSeconds = 0.8f;
        [SerializeField] float watchDistance = 25f;
        [Tooltip("Real seconds before slow motion can happen again.")]
        [SerializeField] float slowMoCooldown = 20f;
        [SerializeField] float cheerRadius = 40f;

        float restoreAt = -1f, nextSlowMo;

        void Awake() => instance = this;

        public static void Moment(Vector3 position, float strength)
        {
            Happened?.Invoke(position, strength);
            if (instance == null) return;
            Spectator.CheerNear(position, instance.cheerRadius, 2f + strength);
            instance.TrySlowMo(position);
        }

        void TrySlowMo(Vector3 position)
        {
            if (Time.unscaledTime < nextSlowMo || !Mathf.Approximately(Time.timeScale, 1f) || InputBlocker.Blocked) return;
            if (PlayerSurfing.Instance != null && PlayerSurfing.Instance.IsRiding) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 toMoment = position - cam.transform.position;
            if (toMoment.magnitude > watchDistance || Vector3.Dot(cam.transform.forward, toMoment.normalized) < 0.75f) return;
            Time.timeScale = slowMoScale;
            restoreAt = Time.unscaledTime + slowMoSeconds;
            nextSlowMo = Time.unscaledTime + slowMoCooldown;
        }

        void Update()
        {
            if (restoreAt < 0f || Time.unscaledTime < restoreAt) return;
            // Only undo our own slow motion (a menu may have paused the game meanwhile).
            if (Mathf.Approximately(Time.timeScale, slowMoScale)) Time.timeScale = 1f;
            restoreAt = -1f;
        }
    }
}
