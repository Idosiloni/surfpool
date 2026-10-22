using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>One thing a mission asks for, e.g. "Sell 3 surf sessions (1/3)".</summary>
    public class Objective
    {
        public string text;
        public Func<long> current;
        public long target = 1;
        /// <summary>The upgrade this objective asks you to buy (goal strip and computer point at it), if any.</summary>
        public string upgradeId;

        public bool Done => current() >= target;
        public string Progress => target <= 1 ? "" : $" ({Math.Min(current(), target):#,0}/{target:#,0})";
    }

    /// <summary>A story step: what Uncle Kai's notebook says, what to do, a hint, where to go, and a cash reward.</summary>
    public class Mission
    {
        /// <summary>Saved instead of the list position, so missions can be added or moved without breaking saves.</summary>
        public string id;
        public string chapter;
        public string title;
        public string story;
        public string hint;
        public Func<Transform> marker;
        public int reward;
        public List<Objective> objectives = new();

        public bool Done => objectives.TrueForAll(o => o.Done);
    }

    /// <summary>
    /// The story and tutorial: an ordered list of missions in chapters. The active mission shows in the HUD with a
    /// marker over where to go; finishing one pays its reward and starts the next. Missions guide, they never block.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Places missions point at")]
        [SerializeField] Transform ticketRegister;
        [SerializeField] Transform waveConsole;
        [SerializeField] Transform upgradeComputer;
        [SerializeField] Transform pool;
        [SerializeField] Transform rentalRack;
        [SerializeField] Transform surfPeg;

        [Header("Systems missions read")]
        [SerializeField] UpgradeShop shop;
        [SerializeField] SurferSpawner spawner;
        [SerializeField] PlayerInteractor player;

        readonly List<Mission> missions = new();
        int current;
        bool started;

        public Mission Current => started && current < missions.Count ? missions[current] : null;
        public int Index => current;
        public int MissionCount => missions.Count;
        public bool AllDone => current >= missions.Count;
        public bool Started => started;

        /// <summary>Fires when a mission is completed (for the HUD celebration).</summary>
        public event Action<Mission> Completed;

        void Awake()
        {
            Instance = this;
            BuildStory();
        }

        /// <summary>Called when the player closes the intro letter.</summary>
        public void Begin() => started = true;

        /// <summary>The current mission's id for saving ("done" once every mission is finished).</summary>
        public string CurrentId => current < missions.Count ? missions[current].id : "done";

        /// <summary>
        /// Jump to a saved mission (save/load): by id; saves from before mission ids have only the list position,
        /// which is still the right one for them (missions keep that old order until new ones are added).
        /// </summary>
        public void Restore(string id, int index, bool hasStarted)
        {
            int found = id == "done" ? missions.Count : missions.FindIndex(m => m.id == id);
            current = found >= 0 ? found : Mathf.Clamp(index, 0, missions.Count);
            started = hasStarted;
        }

        void Update()
        {
            var mission = Current;
            if (mission == null || !mission.Done) return;
            if (mission.reward > 0) Wallet.Instance.Add(mission.reward, "mission reward");
            Notifications.Post($"Mission complete: {mission.title}!", Notice.Reward);
            Sfx.Play(Sound.Mission);
            ParkLevel.Add(ParkLevel.XpMission);
            Sfx.Duck();
            Completed?.Invoke(mission);
            current++;
        }

        static Objective Count(string text, Func<long> value, long target) => new() { text = text, current = value, target = target };
        static Objective Flag(string text, Func<bool> done) => new() { text = text, current = () => done() ? 1 : 0, target = 1 };
        Objective Owns(string text, string upgradeId)
        {
            var objective = Flag(text, () => shop.IsOwned(upgradeId));
            objective.upgradeId = upgradeId;
            return objective;
        }

        bool NearRegister() =>
            player != null && ticketRegister != null && Vector3.Distance(player.transform.position, ticketRegister.position) < 4f;

        void BuildStory()
        {
            const string ch1 = "Chapter 1 · The Grand Reopening";
            const string ch2 = "Chapter 2 · Word Gets Around";
            const string ch3 = "Chapter 3 · The Pro Tour";
            const string ch4 = "Chapter 4 · World-Class Surf Park";

            missions.Add(new Mission
            {
                id = "open_doors", chapter = ch1, title = "Open the doors",
                story = "First things first: the ticket desk. It's the teal kiosk by the pool. Surfers buy their sessions there.",
                hint = "Walk to the ticket desk (follow the marker).",
                marker = () => ticketRegister,
                objectives = { Flag("Go to the ticket desk", NearRegister) },
            });
            missions.Add(new Mission
            {
                id = "first_customer", chapter = ch1, title = "Your first customer", reward = 10,
                story = "Here comes a beginner! Stand behind the counter, look at the black register and press E to sell a session.",
                hint = "Look at the register and press E when a surfer is waiting.",
                marker = () => ticketRegister,
                objectives = { Count("Sell a surf session", () => GameStats.TicketsSold, 1) },
            });
            missions.Add(new Mission
            {
                id = "make_waves", chapter = ch1, title = "Make some waves", reward = 10,
                story = "My old wave machine still works! At the console by the deep end, the screen picks the wave and the big red button sends it.",
                hint = "Go to the wave console and press E on the big red button.",
                marker = () => waveConsole,
                objectives = { Count("Send a wave", () => GameStats.WavesSent, 1) },
            });
            missions.Add(new Mission
            {
                id = "stoked", chapter = ch1, title = "Stoked!", reward = 25,
                story = "Every surfer gets 3 waves. Keep the waves coming and watch the patience bars over their heads!",
                hint = "Send a wave whenever a surfer is waiting in the water.",
                marker = () => waveConsole,
                objectives = { Count("Give a surfer a full session", () => GameStats.SurfersServed, 1) },
            });
            missions.Add(new Mission
            {
                id = "back_in_business", chapter = ch1, title = "Back in business", reward = 50,
                story = "Sell sessions, send waves, keep everyone happy. Happy surfers tip, and a good rating brings more of them. The +/- buttons at the desk set your price: when the park gets nicer, surfers will pay more, so raise it!",
                hint = "Keep serving surfers. Tips count too!",
                objectives = { Count("Earn $150", () => GameStats.TotalEarned, 150) },
            });
            missions.Add(new Mission
            {
                id = "fix_up", chapter = ch1, title = "Fix up the place",
                story = "The computer on the ticket desk runs SurfOS. That's where you buy upgrades for the park.",
                hint = "Press E on the computer screen on the desk and buy any upgrade.",
                marker = () => upgradeComputer,
                objectives = { Flag("Buy your first upgrade", () => shop.OwnedCount > 0) },
            });

            missions.Add(new Mission
            {
                id = "owners_privilege", chapter = ch2, title = "Owner's privilege", reward = 50,
                story = "You can't run a surf park without surfing it! Grab your board from the peg by the wave console.",
                hint = "E on your board by the console. W paddle, Space to stand up when a wave reaches you, F sends a wave.",
                marker = () => surfPeg,
                objectives = { Count("Ride a wave yourself", () => GameStats.PlayerRides, 1) },
            });
            missions.Add(new Mission
            {
                id = "bigger_waves", chapter = ch2, title = "Bigger waves", reward = 300,
                story = "Beginners are great, but intermediate surfers pay two and a half times as much. Bigger waves need a bigger pool, so dig it out first, then teach the machine some proper peeling waves.",
                hint = "Buy Pool Expansion I (Pools tab), then the Intermediate Wave Program (Waves tab).",
                marker = () => upgradeComputer,
                objectives =
                {
                    Owns("Expand the main pool", "pool_expand_1"),
                    Owns("Unlock Intermediate waves", "intermediate_waves"),
                },
            });
            missions.Add(new Mission
            {
                id = "regulars", chapter = ch2, title = "Regulars", reward = 300,
                story = "Word is spreading. Keep the line moving and the lineup happy.",
                hint = "Serve surfers and keep your park rating high.",
                objectives =
                {
                    Count("Give 10 full sessions", () => GameStats.SurfersServed, 10),
                    Flag("Reach a 4-star rating", () => ParkReputation.Instance != null && ParkReputation.Instance.Stars >= 4f),
                },
            });
            missions.Add(new Mission
            {
                id = "board_rentals", chapter = ch2, title = "Board rentals", reward = 300,
                story = "Lots of people show up without a board. Open the rental stand and hand them one from the rack.",
                hint = "Buy the Board Rental Stand (Rentals tab), then rent boards.",
                marker = () => shop.IsOwned("rental_stand") ? rentalRack : upgradeComputer,
                objectives =
                {
                    Owns("Open the Board Rental Stand", "rental_stand"),
                    Count("Rent out 5 boards", () => GameStats.BoardsRented, 5),
                },
            });
            missions.Add(new Mission
            {
                id = "hire_help", chapter = ch2, title = "Hire some help", reward = 400,
                story = "You can't do everything yourself. Start with the chore nobody loves: hauling boards back to the rack. Hire a Board Runner.",
                hint = "The Board Runner is in the Staff tab (after you've rented out 20 boards).",
                marker = () => upgradeComputer,
                objectives = { Owns("Hire a Board Runner", "board_runner") },
            });

            missions.Add(new Mission
            {
                id = "go_pro", chapter = ch3, title = "Go pro", reward = 1500,
                story = "The pros are calling. Steep, fast, barreling waves, and they pay six times the base price. This is what I always dreamed of. Pro waves need the pool at full size.",
                hint = "Buy Pool Expansion II (Pools tab), then the Pro Wave Program (Waves tab).",
                marker = () => upgradeComputer,
                objectives =
                {
                    Owns("Expand the pool to full size", "pool_expand_2"),
                    Owns("Unlock Pro waves", "pro_waves"),
                },
            });
            missions.Add(new Mission
            {
                id = "get_barreled", chapter = ch3, title = "Get barreled", reward = 2000,
                story = "Nothing beats watching a pro disappear into the tube. Send them Pro waves!",
                hint = "Pro surfers ride inside the barrel on Pro waves.",
                marker = () => waveConsole,
                objectives = { Count("See 5 tube rides", () => GameStats.TubeRides, 5) },
            });
            missions.Add(new Mission
            {
                id = "second_pool", chapter = ch3, title = "A second zone", reward = 5000,
                story = "One pool isn't enough anymore. Open the water across the sandbar and double the surf!",
                hint = "Buy a new zone in the Pools tab.",
                marker = () => upgradeComputer,
                objectives =
                {
                    Count("Earn $40,000 in total", () => GameStats.TotalEarned, 40000),
                    Flag("Open a second zone", () => PoolRegistry.OpenPools >= 2),
                },
            });

            missions.Add(new Mission
            {
                id = "world_class", chapter = ch4, title = "World-class", reward = 50000,
                story = "Look at this place! Let's make it the best surf park in the world.",
                hint = "Serve lots of surfers, keep 5 stars, and open more zones.",
                objectives =
                {
                    Count("Give 300 full sessions", () => GameStats.SurfersServed, 300),
                    Flag("Reach a 5-star rating", () => ParkReputation.Instance != null && ParkReputation.Instance.Stars >= 5f),
                    Flag("Open 3 zones", () => PoolRegistry.OpenPools >= Mathf.Min(3, PoolRegistry.TotalPools) && PoolRegistry.TotalPools > 1), // main, Reef, Lagoon (the Cove and the Ranch come later)
                    Count("Earn $500,000 in total", () => GameStats.TotalEarned, 500000),
                },
            });
        }
    }
}
