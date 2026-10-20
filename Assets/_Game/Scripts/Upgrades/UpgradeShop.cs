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
        [Tooltip("Park level needed before it can be bought.")]
        public int level = 1;
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
        // Costs follow the curve in docs/BALANCE.md: each purchase takes a few minutes of the income you have when it
        // unlocks. Order matters: an upgrade's requirement must come before it in this list (saves restore in order).
        [SerializeField] List<Upgrade> upgrades = new()
        {
            // Pools: the main pool grows first, then whole new pools.
            new Upgrade { id = "pool_expand_1", category = UpgradeCategory.Pools, title = "Pool Expansion I", cost = 400, level = 3,
                description = "Dig the main pool out to 50 x 19 m. Room for 2 more surfers, and big enough for Intermediate waves." },
            new Upgrade { id = "pool_expand_2", category = UpgradeCategory.Pools, title = "Pool Expansion II", cost = 3000, level = 10, requires = "intermediate_waves",
                description = "The main pool's full size: 60 x 24 m. Room for 2 more surfers, and big enough for Pro waves." },
            new Upgrade { id = "pool_reef", category = UpgradeCategory.Pools, title = "Build the Reef Pool", cost = 18000, level = 15, requires = "pool_expand_2",
                description = "A second pool north of the main one. Longer peeling waves. Room for 4 more surfers, and every session there costs 2.5x." },
            new Upgrade { id = "pool_lagoon", category = UpgradeCategory.Pools, title = "Build the Big Wave Lagoon", cost = 80000, level = 20, requires = "pool_reef",
                description = "The biggest, deepest pool in the park. Huge waves. Room for 5 more surfers, and every session there costs 5x." },

            // Waves
            new Upgrade { id = "faster_machine", category = UpgradeCategory.Waves, title = "Turbo Pump", cost = 75, level = 1,
                description = "The wave machine recharges twice as fast (4 s -> 2 s)." },
            new Upgrade { id = "auto_waves", category = UpgradeCategory.Waves, title = "Auto Wave Timer", cost = 200, level = 2,
                description = "The machine sends the selected wave by itself every 12 seconds. A yellow light turns on." },
            new Upgrade { id = "intermediate_waves", category = UpgradeCategory.Waves, title = "Intermediate Wave Program", cost = 600, level = 3, requires = "pool_expand_1",
                description = "Unlocks bigger peeling Intermediate waves. Intermediate surfers start coming, and they pay 2.5x the base price." },
            new Upgrade { id = "wave_sets", category = UpgradeCategory.Waves, title = "Wave Sets", cost = 900, level = 5, requires = "faster_machine",
                description = "Each press sends a set of 2 waves, 2.5 s apart. More rides, less waiting." },
            new Upgrade { id = "smart_auto", category = UpgradeCategory.Waves, title = "Smart Wave Timer", cost = 2000, level = 8, requires = "auto_waves",
                description = "Auto waves pick the wave type most waiting surfers want." },
            new Upgrade { id = "wave_shaper", category = UpgradeCategory.Waves, title = "Reef Shaper", cost = 2500, level = 8, requires = "intermediate_waves",
                description = "Unlocks Lefts and Rights. They peel longer than a peak (longer rides, more tricks), and surfers pay $5 more for them. Regular-foot surfers love rights, goofy-foot surfers love lefts." },
            new Upgrade { id = "pro_waves", category = UpgradeCategory.Waves, title = "Pro Wave Program", cost = 4500, level = 11, requires = "pool_expand_2",
                description = "Unlocks steep, fast, barreling Pro waves. Pro surfers start coming: they pay 6x the base price and do tricks." },
            new Upgrade { id = "heavy_duty_pumps", category = UpgradeCategory.Waves, title = "Heavy-Duty Pumps", cost = 3500, level = 9,
                description = "Tougher pumps in every wave machine: they wear out half as fast, so they break down much less." },
            new Upgrade { id = "famous_waves", category = UpgradeCategory.Waves, title = "Famous Waves Pack", cost = 6000, level = 12, requires = "pro_waves",
                description = "Wave programs copied from real spots: Waikiki, Malibu, Snapper Rocks, Pipeline (and Teahupo'o in the Big Wave Lagoon). Surfers love them: happier rides and 50% bigger tips." },
            new Upgrade { id = "turbo_2", category = UpgradeCategory.Waves, title = "Turbo Pump II", cost = 4000, level = 12, requires = "faster_machine",
                description = "Recharge time drops to 1 s. Auto waves every 8 s." },

            // Park
            new Upgrade { id = "more_surfers", category = UpgradeCategory.Park, title = "Bigger Lineup", cost = 50, level = 1,
                description = "Two more surfers can be in the park at once. Adds a bench by the pool." },
            new Upgrade { id = "billboard", category = UpgradeCategory.Park, title = "Roadside Billboard", cost = 150, level = 2,
                description = "Surfers arrive 30% more often." },
            new Upgrade { id = "huge_lineup", category = UpgradeCategory.Park, title = "Huge Lineup", cost = 1200, level = 6, requires = "more_surfers",
                description = "Three more surfers can be in the park at once." },
            new Upgrade { id = "social_ads", category = UpgradeCategory.Park, title = "Social Media Ads", cost = 1500, level = 7, requires = "billboard",
                description = "Surfers arrive 40% more often." },
            new Upgrade { id = "tiki_garden", category = UpgradeCategory.Park, title = "Tiki Garden", cost = 2000, level = 8,
                description = "Palms, tiki torches and flower beds all around the pools. A prettier park: surfers will pay $10 more." },
            new Upgrade { id = "beach_passes", category = UpgradeCategory.Park, title = "Beach Passes", cost = 500, level = 3,
                description = "Open the beach to guests who don't surf: they pay for a pass to relax on your loungers and benches (prettier park = pricier passes). Place seats with build mode (B)!" },
            new Upgrade { id = "fishing_pier", category = UpgradeCategory.Park, title = "Fishing Pier", cost = 700, level = 4,
                description = "A wooden pier out into the ocean on the west beach. Go fishing yourself: hook, reel, sell the catch (more if the snack bar can cook it)." },
            new Upgrade { id = "pro_rod", category = UpgradeCategory.Park, title = "Pro Fishing Rod", cost = 2000, level = 8, requires = "fishing_pier",
                description = "A stronger line (tension builds 30% slower) and better bait: big fish bite twice as often." },
            new Upgrade { id = "trash_cans", category = UpgradeCategory.Park, title = "Trash Cans", cost = 300, level = 3,
                description = "Bins all around the decks. Surfers drop 60% less litter (trash on the ground makes them less happy)." },
            new Upgrade { id = "solar_panels", category = UpgradeCategory.Park, title = "Solar Panels", cost = 1500, level = 4,
                description = "Solar panels on every wave machine roof. Waves use half the electricity." },
            new Upgrade { id = "water_recycling", category = UpgradeCategory.Park, title = "Water Recycling", cost = 5000, level = 12, requires = "solar_panels",
                description = "Filter tanks next to every wave machine clean and reuse the pool water. The daily water bill is halved." },
            new Upgrade { id = "snack_bar", category = UpgradeCategory.Park, title = "Snack Bar", cost = 1200, level = 5,
                description = "A taco and smoothie kiosk by the exit. Happy surfers from the main pool stop for food ($10). Serve them at the grill (E)... and watch out for the wrappers." },
            new Upgrade { id = "smoothie_bar", category = UpgradeCategory.Park, title = "Smoothie Bar", cost = 2500, level = 8, requires = "snack_bar",
                description = "Fresh fruit smoothies and acai bowls on the menu. Every snack sells for $8 more." },
            new Upgrade { id = "surf_shop", category = UpgradeCategory.Park, title = "Surf Shop", cost = 3500, level = 10,
                description = "A merch kiosk by the entrance. Every happy surfer buys $15 of wax, stickers and tees on the way out." },
            new Upgrade { id = "vip_sessions", category = UpgradeCategory.Park, title = "VIP Sessions", cost = 8000, level = 14, requires = "intermediate_waves",
                description = "Fancy towels, smoothies, priority lineup. Surfers will pay $25 more (times their skill and pool)." },
            new Upgrade { id = "grandstand", category = UpgradeCategory.Park, title = "Grandstand", cost = 22000, level = 17, requires = "pro_waves",
                description = "Big bleachers by the main pool. Spectators pay $400 every minute to watch, and the buzz brings surfers 30% more often." },
            new Upgrade { id = "season_passes", category = UpgradeCategory.Park, title = "Season Passes", cost = 35000, level = 18, requires = "surf_shop",
                description = "Locals buy yearly passes: $1,000 every minute, even while you sleep." },

            // Rentals
            new Upgrade { id = "rental_stand", category = UpgradeCategory.Rentals, title = "Board Rental Stand", cost = 100, level = 1,
                description = "Opens a rental counter. Surfers without boards start coming. Hand them a board from the rack ($10), then carry returned boards from the bin back to the rack." },
            new Upgrade { id = "more_boards", category = UpgradeCategory.Rentals, title = "More Rental Boards", cost = 300, level = 2, requires = "rental_stand",
                description = "Four more boards and slots on the rental rack." },
            new Upgrade { id = "premium_boards", category = UpgradeCategory.Rentals, title = "Premium Boards", cost = 400, level = 4, requires = "rental_stand",
                description = "Nicer boards: rentals cost $10 more." },

            // Staff
            new Upgrade { id = "lifeguard", category = UpgradeCategory.Staff, title = "Hire Lifeguard", cost = 250, level = 2,
                description = "Wage $3/min. Surfers wait 50% longer for waves and wipeouts don't ruin their mood." },
            new Upgrade { id = "desk_clerk", category = UpgradeCategory.Staff, title = "Hire Desk Clerk", cost = 500, level = 3,
                description = "Wage $6/min. Sells sessions at the desk for you (at your prices)." },
            new Upgrade { id = "board_runner", category = UpgradeCategory.Staff, title = "Hire Board Runner", cost = 600, level = 4, requires = "rental_stand",
                description = "Wage $6/min. Carries returned boards from the bin back to the rack for you." },
            new Upgrade { id = "rental_clerk", category = UpgradeCategory.Staff, title = "Hire Rental Clerk", cost = 700, level = 5, requires = "rental_stand",
                description = "Wage $8/min. Hands rental boards to renters for you." },
            new Upgrade { id = "snack_cook", category = UpgradeCategory.Staff, title = "Hire Snack Cook", cost = 1500, level = 6, requires = "snack_bar",
                description = "Wage $6/min. Cooks and serves every snack order at the snack bar for you." },
            new Upgrade { id = "janitor", category = UpgradeCategory.Staff, title = "Hire Janitor", cost = 900, level = 5,
                description = "Wage $5/min. Walks the decks picking up every piece of trash, so the park stays spotless (and the inspector stays happy)." },
            new Upgrade { id = "mechanic", category = UpgradeCategory.Staff, title = "Hire Mechanic", cost = 1800, level = 6,
                description = "Wage $10/min. Services worn wave machines and fixes broken ones in every pool, so you don't have to." },
            new Upgrade { id = "surf_coach", category = UpgradeCategory.Staff, title = "Hire Surf Coach", cost = 2500, level = 9,
                description = "Wage $15/min. Gives beginners lessons: they wipe out far less, and each beginner session earns a $25 lesson fee." },
            new Upgrade { id = "wave_operator", category = UpgradeCategory.Staff, title = "Hire Wave Operator", cost = 6000, level = 13,
                description = "Wage $25/min. Watches every pool and sends exactly the wave the waiting surfers want. Full automation!" },
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
        [SerializeField] GameObject mechanic;
        [SerializeField] GameObject janitor;
        [SerializeField] GameObject snackBar;
        [SerializeField] GameObject snackCook;
        [SerializeField] GameObject fishingPier;
        [SerializeField] GameObject trashCans;

        [Header("Park decoration (hidden until bought)")]
        [Tooltip("Garden pieces: one on the main deck, one on each extra pool's deck (shown once that pool is built).")]
        [SerializeField] GameObject[] tikiGarden;
        [SerializeField] GameObject grandstand;

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
            if (!LevelMet(upgrade))
            {
                Message = $"Reach park level {upgrade.level} first.";
                Sfx.Play(Sound.Error);
                return false;
            }
            if (!RequirementMet(upgrade))
            {
                Message = $"Buy {TitleOf(upgrade.requires)} first.";
                Sfx.Play(Sound.Error);
                return false;
            }
            if (!Wallet.Instance.Spend(upgrade.cost, upgrade.title))
            {
                Message = $"Not enough money for {upgrade.title}.";
                Sfx.Play(Sound.Error);
                return false;
            }
            upgrade.owned = true;
            Apply(upgrade.id);
            Message = $"Bought {upgrade.title}!";
            ParkLevel.Add(ParkLevel.XpUpgrade);
            Sfx.Play(upgrade.category == UpgradeCategory.Pools ? Sound.Mission : Sound.Buy);
            if (upgrade.category == UpgradeCategory.Pools) Sfx.Duck();
            Notifications.Post($"Bought {upgrade.title}!");
            return true;
        }

        /// <summary>The worker an upgrade hires (null for other upgrades).</summary>
        public StaffMember StaffFor(string id)
        {
            var worker = id switch
            {
                "lifeguard" => lifeguard,
                "board_runner" => boardRunner,
                "rental_clerk" => rentalClerk,
                "desk_clerk" => deskClerk,
                "surf_coach" => surfCoach,
                "wave_operator" => waveOperator,
                "mechanic" => mechanic,
                "janitor" => janitor,
                "snack_cook" => snackCook,
                _ => null,
            };
            return worker != null ? worker.GetComponent<StaffMember>() : null;
        }

        public bool LevelMet(Upgrade upgrade) => ParkLevel.Instance == null || ParkLevel.Instance.Level >= upgrade.level;

        /// <summary>Upgrades that open up at exactly this park level (for the level-up banner).</summary>
        public List<string> UnlockedAt(int level) => upgrades.FindAll(u => u.level == level).ConvertAll(u => u.title);

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
                    break;
                case "wave_shaper":
                    pricing.FairPrice += 5;
                    break;
                case "pool_expand_1":
                    spawner.MainPool.SetStage(1);
                    spawner.MaxSurfers += 2;
                    break;
                case "pool_expand_2":
                    spawner.MainPool.SetStage(2);
                    spawner.MaxSurfers += 2;
                    break;
                case "snack_bar":
                    if (snackBar != null) snackBar.SetActive(true);
                    break;
                case "smoothie_bar":
                    ParkEffects.SnackPriceBonus += 8;
                    break;
                case "snack_cook":
                    if (snackCook != null) snackCook.SetActive(true);
                    break;
                case "janitor":
                    if (janitor != null) janitor.SetActive(true);
                    break;
                case "trash_cans":
                    if (trashCans != null) trashCans.SetActive(true);
                    ParkEffects.LitterMultiplier = 0.4f;
                    break;
                case "mechanic":
                    if (mechanic != null) mechanic.SetActive(true);
                    break;
                case "fishing_pier":
                    if (fishingPier != null) fishingPier.SetActive(true);
                    break;
                case "pro_rod":
                    ParkEffects.FishingTensionMultiplier = 0.7f;
                    ParkEffects.FishingRareMultiplier = 2f;
                    break;
                case "beach_passes":
                    ParkEffects.BeachPassesOpen = true;
                    break;
                case "famous_waves":
                    ParkEffects.FamousWavesUnlocked = true;
                    break;
                case "heavy_duty_pumps":
                    MachineService.WearMultiplier = 0.5f;
                    break;
                case "solar_panels":
                    ParkEffects.ElectricityCostMultiplier = 0.5f;
                    foreach (var area in PoolRegistry.All) if (area.SolarPanels != null) area.SolarPanels.SetActive(true);
                    break;
                case "water_recycling":
                    ParkEffects.WaterCostMultiplier = 0.5f;
                    foreach (var area in PoolRegistry.All) if (area.WaterTanks != null) area.WaterTanks.SetActive(true);
                    break;
                case "tiki_garden":
                    foreach (var garden in tikiGarden) if (garden != null) garden.SetActive(true);
                    pricing.FairPrice += 10;
                    break;
                case "grandstand":
                    if (grandstand != null) grandstand.SetActive(true);
                    ParkEffects.PassiveIncomePerMinute += 400;
                    spawner.SpawnDelayMultiplier *= 0.77f;
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
                    spawner.SpawnDelayMultiplier *= 0.77f;
                    break;
                case "social_ads":
                    spawner.SpawnDelayMultiplier *= 0.71f;
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
                    pricing.RentalPrice += 10;
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
                    ParkEffects.MerchPerHappySurfer = 15;
                    break;
                case "vip_sessions":
                    pricing.FairPrice += 25;
                    break;
                case "season_passes":
                    ParkEffects.PassiveIncomePerMinute += 1000;
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
            spawner.MaxSurfers += upgradeId == "pool_lagoon" ? 5 : 4;
        }
    }
}
