using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfPool
{
    public enum UpgradeCategory { Waves, Pools, Park, Rentals, Staff }

    [Serializable]
    public class Upgrade
    {
        public string id;
        public UpgradeCategory category;
        public string title;
        [TextArea] public string description;
        public int cost;
        [Tooltip("Id of an upgrade that must be bought first. Empty = none.")]
        public string requires;
        [NonSerialized] public bool owned;
    }

    /// <summary>
    /// The computer on the ticket desk. Press E to open the upgrades menu (drawn by <see cref="UpgradeScreen"/>),
    /// buy with the mouse, Esc or E to close. Each upgrade's effect is applied in <see cref="Apply"/> by its id.
    /// </summary>
    public class UpgradeShop : MonoBehaviour, IInteractable
    {
        [SerializeField] List<Upgrade> upgrades = new()
        {
            // Waves
            new Upgrade { id = "faster_machine", category = UpgradeCategory.Waves, title = "Turbo Pump", cost = 100,
                description = "The wave machine recharges twice as fast (4 s -> 2 s)." },
            new Upgrade { id = "auto_waves", category = UpgradeCategory.Waves, title = "Auto Wave Timer", cost = 150,
                description = "The machine sends the selected wave by itself every 12 seconds. A yellow light turns on." },
            new Upgrade { id = "intermediate_waves", category = UpgradeCategory.Waves, title = "Intermediate Wave Program", cost = 120,
                description = "Unlocks bigger peeling Intermediate waves on the console. Intermediate surfers start coming to your park, and they pay more." },
            new Upgrade { id = "wave_shaper", category = UpgradeCategory.Waves, title = "Reef Shaper", cost = 160, requires = "intermediate_waves",
                description = "Unlocks Lefts and Rights on the wave console. They peel longer than a peak (longer rides, more tricks). Regular-foot surfers love rights, goofy-foot surfers love lefts." },
            new Upgrade { id = "wave_sets", category = UpgradeCategory.Waves, title = "Wave Sets", cost = 180, requires = "faster_machine",
                description = "Each press sends a set of 2 waves, 2.5 s apart. More rides, less waiting." },
            new Upgrade { id = "pro_waves", category = UpgradeCategory.Waves, title = "Pro Wave Program", cost = 200, requires = "intermediate_waves",
                description = "Unlocks steep, fast, barreling Pro waves. Pro surfers start coming: they pay the most and do tricks." },
            new Upgrade { id = "smart_auto", category = UpgradeCategory.Waves, title = "Smart Wave Timer", cost = 220, requires = "auto_waves",
                description = "Auto waves pick the wave type most waiting surfers want." },
            new Upgrade { id = "turbo_2", category = UpgradeCategory.Waves, title = "Turbo Pump II", cost = 350, requires = "faster_machine",
                description = "Recharge time drops to 1 s. Auto waves every 8 s." },

            // Pools
            new Upgrade { id = "pool_reef", category = UpgradeCategory.Pools, title = "Build the Reef Pool", cost = 3000,
                description = "A second pool north of the main one. Longer peeling waves, a little bigger. Room for 3 more surfers, and surfers will pay $10 more." },
            new Upgrade { id = "pool_lagoon", category = UpgradeCategory.Pools, title = "Build the Big Wave Lagoon", cost = 12000, requires = "pool_reef",
                description = "The biggest, deepest pool in the park. Huge waves for the best surfers. Room for 4 more surfers, and surfers will pay $15 more." },

            // Park
            new Upgrade { id = "more_surfers", category = UpgradeCategory.Park, title = "Bigger Lineup", cost = 60,
                description = "Two more surfers can be in the park at once. Adds a bench by the pool." },
            new Upgrade { id = "billboard", category = UpgradeCategory.Park, title = "Roadside Billboard", cost = 120,
                description = "Surfers arrive 30% more often." },
            new Upgrade { id = "huge_lineup", category = UpgradeCategory.Park, title = "Huge Lineup", cost = 250, requires = "more_surfers",
                description = "Three more surfers can be in the park at once." },
            new Upgrade { id = "social_ads", category = UpgradeCategory.Park, title = "Social Media Ads", cost = 300, requires = "billboard",
                description = "Surfers arrive 30% more often again." },
            new Upgrade { id = "surf_shop", category = UpgradeCategory.Park, title = "Surf Shop", cost = 600,
                description = "A merch kiosk by the entrance. Every happy surfer buys $4 of wax, stickers and tees on the way out." },
            new Upgrade { id = "vip_sessions", category = UpgradeCategory.Park, title = "VIP Sessions", cost = 800, requires = "intermediate_waves",
                description = "Fancy towels, smoothies, priority lineup. Surfers will pay about $15 more per session." },
            new Upgrade { id = "season_passes", category = UpgradeCategory.Park, title = "Season Passes", cost = 2500, requires = "surf_shop",
                description = "Locals buy yearly passes: $40 every minute, even while you sleep." },

            // Rentals
            new Upgrade { id = "rental_stand", category = UpgradeCategory.Rentals, title = "Board Rental Stand", cost = 80,
                description = "Opens a rental counter. Surfers without boards start coming. Hand them a board from the rack ($10), then carry returned boards from the bin back to the rack." },
            new Upgrade { id = "more_boards", category = UpgradeCategory.Rentals, title = "More Rental Boards", cost = 120, requires = "rental_stand",
                description = "Four more boards and slots on the rental rack." },
            new Upgrade { id = "premium_boards", category = UpgradeCategory.Rentals, title = "Premium Boards", cost = 200, requires = "rental_stand",
                description = "Nicer boards: rentals cost $5 more." },

            // Staff
            new Upgrade { id = "lifeguard", category = UpgradeCategory.Staff, title = "Hire Lifeguard", cost = 150,
                description = "Wage $3/min. Surfers wait 50% longer for waves and wipeouts don't ruin their mood." },
            new Upgrade { id = "board_runner", category = UpgradeCategory.Staff, title = "Hire Board Runner", cost = 200, requires = "rental_stand",
                description = "Wage $4/min. Carries returned boards from the bin back to the rack for you." },
            new Upgrade { id = "rental_clerk", category = UpgradeCategory.Staff, title = "Hire Rental Clerk", cost = 250, requires = "rental_stand",
                description = "Wage $5/min. Hands rental boards to renters for you." },
            new Upgrade { id = "desk_clerk", category = UpgradeCategory.Staff, title = "Hire Desk Clerk", cost = 300,
                description = "Wage $6/min. Sells tickets at the desk for you (at your price)." },
            new Upgrade { id = "surf_coach", category = UpgradeCategory.Staff, title = "Hire Surf Coach", cost = 350,
                description = "Wage $6/min. Gives beginners lessons: they wipe out far less, and each beginner session earns a $10 lesson fee." },
            new Upgrade { id = "wave_operator", category = UpgradeCategory.Staff, title = "Hire Wave Operator", cost = 400,
                description = "Wage $8/min. Watches every pool and sends exactly the wave the waiting surfers want. Full automation!" },
        };

        [Header("Things upgrades change")]
        [SerializeField] SurferSpawner spawner;
        [SerializeField] PriceBoard pricing;
        [SerializeField] GameObject extraBench;
        [SerializeField] GameObject rentalStand;
        [SerializeField] RentalRack rentalRack;

        [Header("Staff (hidden until hired)")]
        [SerializeField] GameObject lifeguard;
        [SerializeField] GameObject boardRunner;
        [SerializeField] GameObject rentalClerk;
        [SerializeField] GameObject deskClerk;
        [SerializeField] GameObject surfCoach;
        [SerializeField] GameObject waveOperator;
        [SerializeField] GameObject surfShop;

        bool open;
        int openedFrame;

        public string Prompt => "Use computer (upgrades)";
        public bool IsOpen => open;
        public IReadOnlyList<Upgrade> Upgrades => upgrades;
        public int OwnedCount => upgrades.FindAll(u => u.owned).Count;

        /// <summary>Feedback from the last purchase attempt, shown at the bottom of the screen.</summary>
        public string Message { get; private set; }

        public void Interact()
        {
            open = true;
            openedFrame = Time.frameCount;
            Message = null;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) Close();
        }

        public bool IsOwned(string id) => upgrades.Exists(u => u.id == id && u.owned);

        public List<string> OwnedIds() => upgrades.FindAll(u => u.owned).ConvertAll(u => u.id);

        /// <summary>Re-applies saved upgrades (in list order, so requirements come first) without charging.</summary>
        public void RestoreOwned(List<string> ids)
        {
            if (ids == null) return;
            foreach (var upgrade in upgrades)
            {
                if (upgrade.owned || !ids.Contains(upgrade.id)) continue;
                upgrade.owned = true;
                Apply(upgrade.id);
            }
        }

        public bool Buy(string id)
        {
            var upgrade = upgrades.Find(u => u.id == id);
            if (upgrade == null || upgrade.owned) return false;
            if (!RequirementMet(upgrade))
            {
                Message = $"Buy {TitleOf(upgrade.requires)} first.";
                return false;
            }
            if (!Wallet.Instance.Spend(upgrade.cost, upgrade.title))
            {
                Message = $"Not enough money for {upgrade.title}.";
                return false;
            }
            upgrade.owned = true;
            Apply(upgrade.id);
            Message = $"Bought {upgrade.title}!";
            Notifications.Post($"Bought {upgrade.title}!");
            return true;
        }

        public bool RequirementMet(Upgrade upgrade) => string.IsNullOrEmpty(upgrade.requires) || IsOwned(upgrade.requires);

        public string TitleOf(string id) => upgrades.Find(u => u.id == id)?.title ?? id;

        static readonly string[] WaveUpgrades =
            { "faster_machine", "auto_waves", "wave_sets", "intermediate_waves", "pro_waves", "wave_shaper", "smart_auto", "turbo_2" };

        void Apply(string id)
        {
            if (Array.IndexOf(WaveUpgrades, id) >= 0)
                foreach (var pool in PoolRegistry.All) ApplyWaveUpgrade(id, pool);

            switch (id)
            {
                case "intermediate_waves":
                    foreach (var pool in PoolRegistry.All) pool.Machine.Select(WaveLevel.Intermediate);
                    spawner.UnlockSkill(WaveLevel.Intermediate);
                    break;
                case "pro_waves":
                    spawner.UnlockSkill(WaveLevel.Pro);
                    pricing.FairPrice += 5;
                    break;
                case "pool_reef":
                case "pool_lagoon":
                    OpenPool(id);
                    break;
                case "more_surfers":
                    spawner.MaxSurfers += 2;
                    if (extraBench != null) extraBench.SetActive(true);
                    break;
                case "huge_lineup":
                    spawner.MaxSurfers += 3;
                    break;
                case "billboard":
                case "social_ads":
                    spawner.SpawnDelayMultiplier *= 0.7f;
                    break;
                case "rental_stand":
                    rentalStand.SetActive(true);
                    spawner.RenterChance = 0.5f;
                    spawner.MaxSurfers += 1;
                    break;
                case "more_boards":
                    rentalRack.AddBoards(4);
                    break;
                case "premium_boards":
                    pricing.RentalPrice += 5;
                    break;
                case "lifeguard":
                    lifeguard.SetActive(true);
                    break;
                case "board_runner":
                    boardRunner.SetActive(true);
                    break;
                case "rental_clerk":
                    rentalClerk.SetActive(true);
                    break;
                case "desk_clerk":
                    deskClerk.SetActive(true);
                    break;
                case "surf_coach":
                    surfCoach.SetActive(true);
                    break;
                case "wave_operator":
                    waveOperator.SetActive(true);
                    break;
                case "surf_shop":
                    surfShop.SetActive(true);
                    ParkEffects.MerchPerHappySurfer = 4;
                    break;
                case "vip_sessions":
                    pricing.FairPrice += 15;
                    break;
                case "season_passes":
                    ParkEffects.PassiveIncomePerMinute += 40;
                    break;
            }
        }

        /// <summary>Wave machine upgrades apply to every pool, including pools built later.</summary>
        void ApplyWaveUpgrade(string id, PoolArea pool)
        {
            var machine = pool.Machine;
            if (machine == null) return;
            bool turbo2 = IsOwned("turbo_2");
            switch (id)
            {
                case "faster_machine":
                    machine.Cooldown = turbo2 ? 1f : 2f;
                    break;
                case "auto_waves":
                    machine.AutoInterval = turbo2 ? 8f : 12f;
                    if (pool.AutoWaveLight != null) pool.AutoWaveLight.SetActive(true);
                    break;
                case "wave_sets":
                    machine.WavesPerSet = 2;
                    break;
                case "intermediate_waves":
                    machine.Unlock(WaveLevel.Intermediate);
                    break;
                case "pro_waves":
                    machine.Unlock(WaveLevel.Pro);
                    break;
                case "wave_shaper":
                    machine.UnlockDirections();
                    break;
                case "smart_auto":
                    machine.SmartAuto = true;
                    break;
                case "turbo_2":
                    machine.Cooldown = 1f;
                    if (IsOwned("auto_waves")) machine.AutoInterval = 8f;
                    break;
            }
        }

        void OpenPool(string upgradeId)
        {
            foreach (var pool in PoolRegistry.All)
            {
                if (pool.UpgradeId != upgradeId) continue;
                pool.Open();
                foreach (var wave in WaveUpgrades)
                    if (IsOwned(wave)) ApplyWaveUpgrade(wave, pool);
                if (IsOwned("intermediate_waves")) pool.Machine.Select(WaveLevel.Intermediate);
            }
            spawner.MaxSurfers += upgradeId == "pool_lagoon" ? 4 : 3;
            pricing.FairPrice += upgradeId == "pool_lagoon" ? 15 : 10;
        }
    }
}
