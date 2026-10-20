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
            public int version = 3;
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
            public int parkXp;
            public int snacksSold;
            public List<BuildMode.Placed> decorations = new();
            public List<int> treasuresFound = new();
            public int fishCaught, beachGuests;
            public List<NamedValue> staffExperience = new();
            public List<MachineState> machines = new();
            public List<string> perks = new();
        }

        [Serializable]
        class NamedValue
        {
            public string id;
            public int value;
        }

        [Serializable]
        class MachineState
        {
            public string pool;
            public float wear;
            public bool broken;
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
            if (enabled && !MainMenu.Showing) Save(); // quitting from the title screen changes nothing
        }

        public void Save()
        {
            var data = new SaveData
            {
                money = Wallet.Instance != null ? Wallet.Instance.Money : 0,
                day = DayCycle.Instance != null ? DayCycle.Instance.Day : 1,
                upgrades = shop.OwnedIds(),
                perks = new List<string>(ParkPerks.Chosen),
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
                parkXp = ParkLevel.Instance != null ? ParkLevel.Instance.Xp : 0,
                snacksSold = GameStats.SnacksSold,
                decorations = BuildMode.Instance != null ? BuildMode.Instance.Export() : new List<BuildMode.Placed>(),
                treasuresFound = Treasure.FoundIndices(Treasure.Total),
                fishCaught = GameStats.FishCaught,
                beachGuests = GameStats.BeachGuests,
            };
            foreach (var id in data.upgrades)
            {
                var worker = shop.StaffFor(id);
                if (worker != null) data.staffExperience.Add(new NamedValue { id = id, value = worker.Experience });
            }
            foreach (var pool in PoolRegistry.All)
                if (pool.Machine != null && pool.Machine.Service != null)
                    data.machines.Add(new MachineState { pool = pool.DisplayName, wear = pool.Machine.Service.Wear, broken = pool.Machine.Service.Broken });
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

            // Saves from before park levels (v2) get XP for the surfers they already served.
            if (ParkLevel.Instance != null) ParkLevel.Instance.Restore(data.version < 3 ? data.surfersServed * 15 : data.parkXp);
            // Upgrades rebuild the fair and rental prices (and pool sizes); only the player's base price is saved.
            shop.RestoreOwned(data.upgrades);
            ParkPerks.Restore(data.perks); // older saves have none: picks are earned from the level, so they show up
            pricing.RestorePrice(data.ticketPrice);
            Wallet.Instance.Restore(data.money);
            if (ParkReputation.Instance != null) ParkReputation.Instance.Restore(data.rating);
            if (DayCycle.Instance != null) DayCycle.Instance.Restore(data.day);
            if (MissionManager.Instance != null) MissionManager.Instance.Restore(data.missionIndex, data.missionStarted);
            spawner.RestoreCounts(data.served, data.lost);
            if (data.staffExperience != null)
                foreach (var entry in data.staffExperience)
                {
                    var worker = shop.StaffFor(entry.id);
                    if (worker != null) worker.RestoreExperience(entry.value);
                }
            if (data.machines != null)
                foreach (var state in data.machines)
                    foreach (var pool in PoolRegistry.All)
                        if (pool.DisplayName == state.pool && pool.Machine != null && pool.Machine.Service != null)
                            pool.Machine.Service.Restore(state.wear, state.broken);
            GameStats.TicketsSold = data.ticketsSold;
            GameStats.WavesSent = data.wavesSent;
            GameStats.SurfersServed = data.surfersServed;
            GameStats.BoardsRented = data.boardsRented;
            GameStats.TubeRides = data.tubeRides;
            GameStats.TricksLanded = data.tricksLanded;
            GameStats.Wipeouts = data.wipeouts;
            GameStats.PlayerRides = data.playerRides;
            GameStats.TotalEarned = data.totalEarned;
            GameStats.SnacksSold = data.snacksSold;
            if (BuildMode.Instance != null) BuildMode.Instance.Restore(data.decorations);
            Treasure.Restore(data.treasuresFound);
            GameStats.FishCaught = data.fishCaught;
            GameStats.BeachGuests = data.beachGuests;
            Notifications.Post($"Welcome back! Day {data.day}, {MoneyText.Format(data.money)} in the bank.");
        }

        /// <summary>Deletes the save so the next start is a brand-new game.</summary>
        public static void DeleteSave()
        {
            if (HasSave) File.Delete(SavePath);
        }
    }
}
