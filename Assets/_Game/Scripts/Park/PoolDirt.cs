using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Pools get dirty: leaves blow in and surfers lose cups, flip-flops and sunscreen, which float on the water and
    /// drift toward the beach with each wave (now and then a rubber duck). Every piece in a pool makes the surfers in
    /// it a little less happy, and they say so. Scoop them out with the skimmer net (<see cref="SkimmerRack"/>), or
    /// buy the Pool Robot upgrade (<see cref="PoolRobot"/>) to do it slowly for you.
    /// </summary>
    public class PoolDirt : MonoBehaviour
    {
        [SerializeField] GameObject[] debrisModels;
        [SerializeField] GameObject duckModel;
        [SerializeField] GameObject robotModel;
        [Tooltip("Pieces per minute that blow into each open pool, plus this many per surfer in it.")]
        [SerializeField] float basePerMinute = 0.5f, perSurferPerMinute = 0.3f;
        [SerializeField] int maxPerPool = 14;
        [Range(0f, 1f)] [SerializeField] float duckChance = 0.04f;

        public static PoolDirt Instance { get; private set; }

        /// <summary>The Pool Robot upgrade is owned: every open pool gets a robot.</summary>
        public bool RobotsOn { get; private set; }

        readonly Dictionary<PoolArea, PoolRobot> robots = new();
        float nextTick;

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>How much a pool's floating debris lowers its surfers' happiness (up to 0.25 at 12+ pieces).</summary>
        public static float MoodPenalty(PoolArea pool) => Mathf.Clamp01(PoolDebris.CountIn(pool) / 12f) * 0.25f;

        public void EnableRobots()
        {
            RobotsOn = true;
            nextTick = 0f;
        }

        void Update()
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + 1f;
            foreach (var pool in PoolRegistry.All)
            {
                if (!pool.IsOpen) continue;
                if (RobotsOn && robotModel != null && !robots.ContainsKey(pool)) robots[pool] = SpawnRobot(pool);
                if (!DayCycle.Open) continue;
                int surfers = 0;
                foreach (var s in Surfer.All) if (s.Pool == pool) surfers++;
                float perSecond = (basePerMinute + perSurferPerMinute * surfers) / 60f * DailyEvents.LitterMultiplier * ParkPerks.LitterMultiplier;
                if (Random.value < perSecond && PoolDebris.CountIn(pool) < maxPerPool) Spawn(pool);
            }
        }

        /// <summary>Drops a random piece into a pool (also for testing).</summary>
        public PoolDebris Spawn(PoolArea pool)
        {
            var water = pool.Water;
            bool duck = duckModel != null && Random.value < duckChance;
            var model = duck ? duckModel : debrisModels[Random.Range(0, debrisModels.Length)];
            var local = new Vector3(Random.Range(3f, water.Length - 11f), 0f, Random.Range(-1f, 1f) * (water.Width * 0.5f - 1.5f));
            var go = Instantiate(model, water.transform.TransformPoint(local), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            go.name = duck ? "Rubber Duck" : "Pool Debris";
            go.transform.localScale = Vector3.one * (duck ? 2.4f : 1.9f); // cartoon-big, so you can spot it from the deck
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var aim = go.AddComponent<SphereCollider>();
            aim.radius = 0.2f;
            var debris = go.AddComponent<PoolDebris>();
            debris.Init(pool, duck);
            return debris;
        }

        PoolRobot SpawnRobot(PoolArea pool)
        {
            var water = pool.Water;
            var go = Instantiate(robotModel, water.transform.TransformPoint(new Vector3(water.Length * 0.4f, 0f, 0f)), Quaternion.identity);
            go.name = "Pool Robot";
            var robot = go.AddComponent<PoolRobot>();
            robot.Init(pool);
            return robot;
        }
    }

    /// <summary>One floating piece of junk in a pool. Scoop it with the skimmer net (aim from the deck, up to 7 m).</summary>
    public class PoolDebris : MonoBehaviour, IInteractable
    {
        public static readonly List<PoolDebris> All = new();

        PoolArea pool;
        WaveWater water;
        bool duck;
        float lastSurface, spin;

        public PoolArea Pool => pool;
        public bool IsDuck => duck;

        public static int CountIn(PoolArea pool)
        {
            int n = 0;
            foreach (var d in All) if (d.pool == pool) n++;
            return n;
        }

        /// <summary>The nearest piece within <paramref name="radius"/> of a spot in a pool (null if none).</summary>
        public static PoolDebris Near(PoolArea pool, Vector3 where, float radius)
        {
            PoolDebris best = null;
            float bestSq = radius * radius;
            foreach (var d in All)
            {
                if (d.pool != pool) continue;
                float sq = (d.transform.position - where).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = d;
                }
            }
            return best;
        }

        public void Init(PoolArea inPool, bool isDuck)
        {
            pool = inPool;
            water = inPool.Water;
            duck = isDuck;
            spin = Random.Range(-20f, 20f);
            lastSurface = water.SurfaceHeightAt(transform.position);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public string Prompt => SkimmerRack.Holding ? (duck ? "Rescue the rubber duck!" : "Scoop it out") : "Floating junk: grab the skimmer net";

        public void Interact()
        {
            if (SkimmerRack.Holding) Collect(true);
        }

        /// <summary>Taken out of the water (by you or a robot).</summary>
        public void Collect(bool byPlayer)
        {
            GameStats.TrashCollected++;
            Sfx.Play(Sound.Plop, transform.position, 0.8f);
            if (water != null) RideFeel.Splash(water, transform.position, 5, 1.2f);
            if (duck && byPlayer)
            {
                ParkLevel.Add(10);
                Notifications.Post("You rescued a rubber duck! Quack. +10 XP", Notice.Good);
            }
            Destroy(gameObject);
        }

        void Update()
        {
            if (water == null) return;
            // Bob on the surface, turn slowly, and drift toward the beach, pushed harder as a wave lifts it.
            var p = transform.position;
            float surface = water.SurfaceHeightAt(p);
            float rise = Mathf.Max(0f, surface - lastSurface) / Mathf.Max(Time.deltaTime, 0.001f);
            lastSurface = surface;
            var local = water.transform.InverseTransformPoint(p);
            local.x += (0.05f + rise * 0.9f) * Time.deltaTime;
            local.x = Mathf.Clamp(local.x, 1.5f, water.Length - 9.5f);
            float limit = water.Width * 0.5f - 0.8f;
            local.z = Mathf.Clamp(local.z, -limit, limit);
            p = water.transform.TransformPoint(local);
            p.y = surface + 0.01f;
            transform.position = p;
            transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
        }
    }

    /// <summary>
    /// The skimmer net's rack on the main pool deck. E takes the net (or puts it back). With the net, aim at
    /// floating junk up to 7 m away and press E to scoop it.
    /// </summary>
    public class SkimmerRack : MonoBehaviour, IInteractable
    {
        [SerializeField] GameObject netOnRack;

        public static bool Holding => holder != null;
        static SkimmerRack holder;

        public string Prompt
        {
            get
            {
                var carry = PlayerCarry.Instance;
                if (carry == null) return null;
                if (holder == this) return "Put the skimmer back";
                if (Holding) return null;
                return carry.HandsFull ? "Hands full" : "Take the skimmer net";
            }
        }

        public void Interact()
        {
            var carry = PlayerCarry.Instance;
            if (carry == null) return;
            if (holder == this)
            {
                carry.DropShopItem(this);
                holder = null;
                netOnRack.SetActive(true);
                Sfx.Play(Sound.Plop);
                return;
            }
            if (Holding || carry.HandsFull) return;
            if (!carry.HoldShopItem(netOnRack, this, new Vector3(0.38f, -0.52f, 0.3f), new Vector3(10f, 170f, 0f), 0.55f)) return;
            holder = this;
            netOnRack.SetActive(false);
            Sfx.Play(Sound.Plop);
        }

        void OnDisable()
        {
            if (holder == this) holder = null;
        }
    }

    /// <summary>A cheerful floating robot that cruises its pool and gobbles up junk, one piece at a time.</summary>
    public class PoolRobot : MonoBehaviour
    {
        const float Speed = 0.7f;
        PoolArea pool;
        WaveWater water;
        Vector3 wander;
        float nextWander;

        public void Init(PoolArea inPool)
        {
            pool = inPool;
            water = inPool.Water;
        }

        void Update()
        {
            if (water == null) return;
            var target = PoolDebris.Near(pool, transform.position, 200f);
            Vector3 goal;
            if (target != null) goal = target.transform.position;
            else
            {
                if (Time.time >= nextWander)
                {
                    nextWander = Time.time + Random.Range(4f, 8f);
                    wander = water.transform.TransformPoint(new Vector3(Random.Range(4f, water.Length - 12f), 0f,
                        Random.Range(-1f, 1f) * (water.Width * 0.5f - 2f)));
                }
                goal = wander;
            }
            var p = transform.position;
            Vector3 to = goal - p;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
            {
                p += Vector3.ClampMagnitude(to, Speed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Time.deltaTime * 3f); // eyes first
            }
            p.y = water.SurfaceHeightAt(p) + Mathf.Sin(Time.time * 2f) * 0.02f;
            transform.position = p;
            if (target != null && (target.transform.position - p).sqrMagnitude < 0.6f * 0.6f) target.Collect(false);
        }
    }
}
