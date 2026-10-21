using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Kook in trouble: after a wipeout a beginner sometimes gets tired and flails ("HELP!") with a timer bar.
    /// Grab a ring buoy from a red post (<see cref="RescuePost"/>), look at them (from up to 30 m) and press E to throw
    /// it (<see cref="FlyingRing"/>). A hired lifeguard throws one from the tower after a few seconds. Saved surfers
    /// are grateful (XP and a glowing review); if nobody helps they cough their way out angry and the rating drops.
    /// Never worse than that: it's a family game. One at a time.
    /// </summary>
    public static class KookRescue
    {
        /// <summary>Chance that a beginner's wipeout turns into trouble.</summary>
        public const float Chance = 0.12f;
        /// <summary>Seconds before they give up and splutter out.</summary>
        public const float TimeLimit = 25f;
        /// <summary>A hired lifeguard throws after this many seconds.</summary>
        public const float LifeguardDelay = 6f;
        public const int RescueXp = 25;

        static float nextAllowed;

        public static Surfer Current { get; private set; }
        public static bool CanStart => Current == null && DayCycle.Open && Time.time >= nextAllowed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
            nextAllowed = 0f;
        }

        public static void Started(Surfer surfer)
        {
            Current = surfer;
            Sfx.Play(Sound.VoiceAah, surfer.transform.position);
            Notifications.Post(PlayerCarry.Instance != null && PlayerCarry.Instance.ShopItem(RescuePost.Holder) != null
                ? $"{surfer.FirstName} is in trouble! Throw the ring!"
                : $"{surfer.FirstName} is in trouble! Grab a rescue ring from a red post!", Notice.Bad);
        }

        public static void Ended(Surfer surfer)
        {
            if (Current == surfer) Current = null;
            nextAllowed = Time.time + 60f; // not two dramas in a row
        }

        /// <summary>Throws a ring from <paramref name="from"/> to the surfer; they're saved when it lands.</summary>
        public static void Throw(Surfer surfer, Vector3 from, GameObject ringModel, RescuePost post)
        {
            var ring = Object.Instantiate(ringModel, from, Quaternion.identity);
            ring.SetActive(true);
            ring.AddComponent<FlyingRing>().Launch(surfer, post);
            Sfx.Play(Sound.AirBlast, from, 0.35f);
        }
    }

    /// <summary>A thrown ring buoy: arcs to the surfer, splashes down, they grab it, then it goes back to its post.</summary>
    public class FlyingRing : MonoBehaviour
    {
        Surfer target;
        RescuePost post;
        Vector3 start;
        float startTime, flight;
        bool landed;

        public void Launch(Surfer surfer, RescuePost from)
        {
            target = surfer;
            post = from;
            start = transform.position;
            startTime = Time.time;
            flight = Mathf.Clamp(Vector3.Distance(start, surfer.transform.position) / 22f, 0.45f, 1.1f);
        }

        void Update()
        {
            if (landed)
            {
                // Held by the saved surfer for a moment, then back on its post.
                if (target != null) transform.position = target.transform.position + Vector3.up * 0.55f;
                if (Time.time - startTime > flight + 3f) Finish();
                return;
            }
            if (target == null)
            {
                Finish();
                return;
            }
            float t = Mathf.Clamp01((Time.time - startTime) / flight);
            Vector3 end = target.transform.position + Vector3.up * 0.4f;
            Vector3 p = Vector3.Lerp(start, end, t);
            p.y += Mathf.Sin(t * Mathf.PI) * Mathf.Max(2f, flight * 4f);
            transform.position = p;
            transform.Rotate(Vector3.up, 540f * Time.deltaTime, Space.World);
            if (t < 1f) return;
            landed = true;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f); // lies flat on the water
            Sfx.Play(Sound.Splash, end, 0.8f);
            target.Rescued();
        }

        void Finish()
        {
            if (post != null) post.RingBack();
            Destroy(gameObject);
        }
    }
}
