using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Sneak-ins (from day 2): now and then a surfer skips the ticket desk and walks straight into the water,
    /// whistling innocently and glancing back at the desk. Look at them (from up to 30 m, even in the water) and
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
            SneakIn();
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
            Notifications.Post($"Caught {surfer.FirstName} sneaking in! +{xp} XP", Notice.Good);
        }

        public static void GotAway(Surfer surfer)
        {
            ParkReviews.SneaksEscaped++;
            Notifications.Post($"{surfer.FirstName} surfed for free and got away with it!", Notice.Bad);
        }
    }
}
