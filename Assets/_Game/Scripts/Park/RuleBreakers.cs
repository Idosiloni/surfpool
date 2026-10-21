using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Bad customers. Sneak-ins (from day 2): now and then a surfer skips the ticket desk and walks straight into the
    /// water, whistling innocently and glancing back at the desk. From day 3 also wave hogs (drop in on everyone,
    /// "Out of my way!") and litterbugs ("*tosses cup*"), who pay like anyone else. Look at them (from up to 30 m, even in the water) and
    /// press E to throw them out for a little XP. If nobody catches them, they surf for free and brag about it.
    /// One at a time.
    /// </summary>
    public class RuleBreakers : MonoBehaviour
    {
        [SerializeField] SurferSpawner spawner;
        [SerializeField] Surfer surferPrefab;
        [SerializeField] int firstDay = 2;
        [Tooltip("Seconds between sneak-ins (random in this range).")]
        [SerializeField] Vector2 gap = new(150f, 300f);
        [SerializeField] int xpForCatch = 20;

        static RuleBreakers instance;
        Surfer current;
        float next = -1f;
        bool hinted;

        void OnEnable() => instance = this;

        void OnDisable()
        {
            if (instance == this) instance = null;
        }

        void Update()
        {
            var day = DayCycle.Instance;
            if (day == null || !DayCycle.Open || day.Day < firstDay || current != null) return;
            if (next < 0f) next = Time.time + Random.Range(gap.x, gap.y) * 0.5f;
            if (Time.time < next) return;
            next = Time.time + Random.Range(gap.x, gap.y);
            // From day 3 the troublemakers get more varied: wave hogs and litterbugs (they pay, then misbehave).
            float roll = Random.value;
            if (day.Day >= 3 && roll < 0.3f) SendRude(Surfer.Rudeness.WaveHog);
            else if (day.Day >= 3 && roll < 0.55f) SendRude(Surfer.Rudeness.Litterbug);
            else SneakIn();
        }

        /// <summary>Sends a paying customer with bad manners (a wave hog or a litterbug) right now (also for testing).</summary>
        public Surfer SendRude(Surfer.Rudeness kind)
        {
            current = Instantiate(surferPrefab, spawner.Entrance, Quaternion.identity);
            current.Init(spawner, false);
            current.MakeRude(kind);
            return current;
        }

        /// <summary>Sends a sneak-in right now (also for testing).</summary>
        public Surfer SneakIn()
        {
            current = Instantiate(surferPrefab, spawner.Entrance, Quaternion.identity);
            current.Init(spawner, false);
            current.MakeSneaker();
            Notifications.Post(hinted
                ? "Psst! Somebody just skipped the ticket desk..."
                : "Psst! Somebody just skipped the ticket desk... Look at them and press E to kick them out!", Notice.Info);
            hinted = true;
            return current;
        }

        public static void Caught(Surfer surfer)
        {
            int xp = instance != null ? instance.xpForCatch : 20;
            ParkLevel.Add(xp);
            ParkReviews.SneaksCaught++;
            string what = surfer.BadKind switch
            {
                Surfer.Rudeness.WaveHog => $"Kicked out {surfer.FirstName} the wave hog! The lineup cheers.",
                Surfer.Rudeness.Litterbug => $"Kicked out {surfer.FirstName} the litterbug!",
                _ => $"Caught {surfer.FirstName} sneaking in!",
            };
            Notifications.Post($"{what} +{xp} XP", Notice.Good);
        }

        public static void GotAway(Surfer surfer)
        {
            ParkReviews.SneaksEscaped++;
            Notifications.Post($"{surfer.FirstName} surfed for free and got away with it!", Notice.Bad);
        }
    }
}
