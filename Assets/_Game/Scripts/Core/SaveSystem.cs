using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Saves and loads the park as JSON in the player's data folder: money, day, upgrades, prices, rating,
    /// mission progress and lifetime stats. Saves at the end of every day and when the game quits; loads on start.
    /// Runs early so the story letter knows whether this is a new game.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class SaveSystem : MonoBehaviour
    {
        [Serializable]
        class SaveData
        {
            public int version = 2;
            public int money;
            public int day;
            public List<string> upgrades = new();
            public int ticketPrice;
            public float rating;
            public int missionIndex;
            public bool missionStarted;
            public int served, lost;
            public int ticketsSold, wavesSent, surfersServed, boardsRented, tubeRides, tricksLanded, wipeouts, playerRides;
            public long totalEarned;
        }

        [SerializeField] UpgradeShop shop;
        [SerializeField] PriceBoard pricing;
        [SerializeField] SurferSpawner spawner;

        public static string SavePath => Path.Combine(Application.persistentDataPath, "surfpool_save.json");
        public static bool HasSave => File.Exists(SavePath);

        void Start()
        {
            if (HasSave) Load();
        }

        void OnApplicationQuit()
        {
            if (enabled) Save();
        }

        public void Save()
        {
            var data = new SaveData
            {
                money = Wallet.Instance != null ? Wallet.Instance.Money : 0,
                day = DayCycle.Instance != null ? DayCycle.Instance.Day : 1,
                upgrades = shop.OwnedIds(),
                ticketPrice = pricing.Price,
                rating = ParkReputation.Instance != null ? ParkReputation.Instance.Score : 0.6f,
                missionIndex = MissionManager.Instance != null ? MissionManager.Instance.Index : 0,
                missionStarted = MissionManager.Instance != null && MissionManager.Instance.Started,
                served = spawner.Served,
                lost = spawner.Lost,
                ticketsSold = GameStats.TicketsSold,
                wavesSent = GameStats.WavesSent,
                surfersServed = GameStats.SurfersServed,
                boardsRented = GameStats.BoardsRented,
                tubeRides = GameStats.TubeRides,
                tricksLanded = GameStats.TricksLanded,
                wipeouts = GameStats.Wipeouts,
                playerRides = GameStats.PlayerRides,
                totalEarned = GameStats.TotalEarned,
            };
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }

        void Load()
        {
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Couldn't read the save ({e.Message}). Starting a new game.");
                return;
            }
            if (data == null) return;

            // Upgrades rebuild the fair and rental prices (and pool sizes); only the player's base price is saved.
            shop.RestoreOwned(data.upgrades);
            pricing.RestorePrice(data.ticketPrice);
            Wallet.Instance.Restore(data.money);
            if (ParkReputation.Instance != null) ParkReputation.Instance.Restore(data.rating);
            if (DayCycle.Instance != null) DayCycle.Instance.Restore(data.day);
            if (MissionManager.Instance != null) MissionManager.Instance.Restore(data.missionIndex, data.missionStarted);
            spawner.RestoreCounts(data.served, data.lost);
            GameStats.TicketsSold = data.ticketsSold;
            GameStats.WavesSent = data.wavesSent;
            GameStats.SurfersServed = data.surfersServed;
            GameStats.BoardsRented = data.boardsRented;
            GameStats.TubeRides = data.tubeRides;
            GameStats.TricksLanded = data.tricksLanded;
            GameStats.Wipeouts = data.wipeouts;
            GameStats.PlayerRides = data.playerRides;
            GameStats.TotalEarned = data.totalEarned;
            Notifications.Post($"Welcome back! Day {data.day}, {MoneyText.Format(data.money)} in the bank.");
        }

        /// <summary>Deletes the save so the next start is a brand-new game.</summary>
        public static void DeleteSave()
        {
            if (HasSave) File.Delete(SavePath);
        }
    }
}
