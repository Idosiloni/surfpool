using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Three small goals each day ("Catch 3 fish", "Serve 8 surfers"...), picked from what the park has unlocked.
    /// Each pays a bonus that grows with the park (100 + 15% of yesterday's income) and 30 XP. New ones every
    /// morning (and after loading a save). Shown on the HUD under the mission card.
    /// </summary>
    public class DailyChallenges : MonoBehaviour
    {
        public class Challenge
        {
            public string text;
            public Func<long> stat;
            public long start, target;
            public bool done;
            public long Progress => Math.Min(stat() - start, target);
        }

        public static DailyChallenges Instance { get; private set; }

        [SerializeField] UpgradeShop shop;

        readonly List<Challenge> today = new();
        int reward = 100, lastIncome;

        public IReadOnlyList<Challenge> Today => today;
        public int Reward => reward;

        void Awake() => Instance = this;

        void Start()
        {
            if (DayCycle.Instance != null) DayCycle.Instance.DayEnded += report => lastIncome = report.TotalIncome;
            Invoke(nameof(NewDay), 0.5f); // after the save has loaded
        }

        /// <summary>Picks today's three challenges (called each morning).</summary>
        public void NewDay()
        {
            today.Clear();
            int day = DayCycle.Instance != null ? DayCycle.Instance.Day : 1;
            reward = 100 + Mathf.RoundToInt(lastIncome * 0.15f);
            var options = new List<Func<Challenge>>
            {
                () => Make($"Give {5 + day} surfers a full session", () => GameStats.SurfersServed, 5 + day),
                () => Make($"Send {15 + day * 3} waves", () => GameStats.WavesSent, 15 + day * 3),
                () => Make($"Pick up {4 + day / 2} pieces of trash", () => GameStats.TrashCollected, 4 + day / 2),
                () => Make("Ride 2 waves yourself", () => GameStats.PlayerRides, 2),
            };
            if (shop.IsOwned("fishing_pier")) options.Add(() => Make($"Catch {3 + day / 3} fish", () => GameStats.FishCaught, 3 + day / 3));
            if (shop.IsOwned("snack_bar")) options.Add(() => Make($"Sell {4 + day / 2} snacks", () => GameStats.SnacksSold, 4 + day / 2));
            if (shop.IsOwned("rental_stand")) options.Add(() => Make($"Rent out {3 + day / 2} boards", () => GameStats.BoardsRented, 3 + day / 2));
            if (shop.IsOwned("beach_passes")) options.Add(() => Make($"Host {4 + day / 2} beach guests", () => GameStats.BeachGuests, 4 + day / 2));
            if (shop.IsOwned("acai_bar") || shop.IsOwned("surf_shop"))
                options.Add(() => Make($"Make {5 + day / 2} shop sales", () => GameStats.ShopSales, 5 + day / 2));
            if (shop.IsOwned("board_shaper")) options.Add(() => Make("Shape 2 custom boards", () => GameStats.CustomBoards, 2));
            if (shop.IsOwned("intermediate_waves")) options.Add(() => Make($"See {6 + day} tricks", () => GameStats.TricksLanded, 6 + day));
            if (shop.IsOwned("pro_waves")) options.Add(() => Make("See 3 tube rides", () => GameStats.TubeRides, 3));
            for (int i = 0; i < 3 && options.Count > 0; i++)
            {
                int pick = UnityEngine.Random.Range(0, options.Count);
                today.Add(options[pick]());
                options.RemoveAt(pick);
            }
        }

        static Challenge Make(string text, Func<long> stat, long target) => new() { text = text, stat = stat, start = stat(), target = target };

        void Update()
        {
            foreach (var challenge in today)
            {
                if (challenge.done || challenge.Progress < challenge.target) continue;
                challenge.done = true;
                Wallet.Instance.Add(reward, "daily challenges");
                ParkLevel.Add(30);
                Sfx.Play(Sound.Buy);
                Notifications.Post($"Daily challenge done: {challenge.text}! +{MoneyText.Format(reward)}", Notice.Reward);
            }
        }
    }
}
