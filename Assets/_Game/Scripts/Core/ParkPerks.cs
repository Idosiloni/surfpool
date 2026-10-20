using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Permanent park perks: every second park level earns a pick of 1 of 3 (PerkPicker shows the card). Picks are
    /// earned from the level (Level / 2) minus perks already chosen, so a pick waiting when you quit is never lost;
    /// the save only stores the chosen ids. Each perk keeps its own multiplier here, multiplied in where the value
    /// is used, so upgrades (which assign their own values) and perks never overwrite each other.
    /// Numbers are small on purpose (see docs/BALANCE.md).
    /// </summary>
    public static class ParkPerks
    {
        public class Perk
        {
            public string id, title, text, icon;
        }

        public static readonly Perk[] All =
        {
            new() { id = "locals_favorite", title = "Locals' Favorite", text = "Surfers tip 15% more.", icon = "coin" },
            new() { id = "crowd_pleaser", title = "Crowd Pleaser", text = "Surfers arrive 10% more often.", icon = "person" },
            new() { id = "green_park", title = "Green Park", text = "Power and water bills 12% lower.", icon = "bolt" },
            new() { id = "chill_vibes", title = "Chill Vibes", text = "Surfers wait 15% longer before they give up.", icon = "clock" },
            new() { id = "soft_landings", title = "Soft Landings", text = "Wipeouts upset surfers less.", icon = "wave" },
            new() { id = "well_oiled", title = "Well-Oiled Machines", text = "Wave machines wear 15% slower.", icon = "pool" },
            new() { id = "tidy_guests", title = "Tidy Guests", text = "20% less litter on the deck.", icon = "trash" },
            new() { id = "sponsor_darling", title = "Sponsor Darling", text = "Your own rides pay 25% more sponsor money.", icon = "board" },
        };

        static readonly List<string> chosen = new();

        public static IReadOnlyList<string> Chosen => chosen;

        public static float TipMultiplier = 1f, ArrivalMultiplier = 1f, BillMultiplier = 1f, PatienceMultiplier = 1f,
            WearMultiplier = 1f, LitterMultiplier = 1f, SponsorMultiplier = 1f, WipeoutMoodBonus;

        /// <summary>Levels per perk pick.</summary>
        public const int LevelsPerPick = 2;

        /// <summary>Picks earned by the current park level and not yet spent.</summary>
        public static int PicksAvailable
        {
            get
            {
                int level = ParkLevel.Instance != null ? ParkLevel.Instance.Level : 1;
                return Mathf.Max(0, Mathf.Min(level / LevelsPerPick, All.Length) - chosen.Count);
            }
        }

        /// <summary>Is a pick earned at this exact level? (for "Level N brings" previews)</summary>
        public static bool PickAt(int level) => level % LevelsPerPick == 0 && level / LevelsPerPick <= All.Length;

        public static Perk Find(string id) => System.Array.Find(All, p => p.id == id);

        public static bool Has(string id) => chosen.Contains(id);

        /// <summary>Up to count random perks not chosen yet.</summary>
        public static List<Perk> Offer(int count)
        {
            var pool = new List<Perk>();
            foreach (var perk in All)
                if (!chosen.Contains(perk.id)) pool.Add(perk);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            if (pool.Count > count) pool.RemoveRange(count, pool.Count - count);
            return pool;
        }

        public static void Choose(string id)
        {
            if (Find(id) == null || chosen.Contains(id)) return;
            chosen.Add(id);
            Apply(id);
        }

        /// <summary>From a save: the chosen ids (unknown or repeated ids are skipped).</summary>
        public static void Restore(List<string> ids)
        {
            Reset();
            if (ids == null) return;
            foreach (var id in ids) Choose(id);
        }

        /// <summary>No perks (new game; called from ParkEffects.ResetStatics).</summary>
        public static void Reset()
        {
            chosen.Clear();
            TipMultiplier = ArrivalMultiplier = BillMultiplier = PatienceMultiplier = WearMultiplier = LitterMultiplier = SponsorMultiplier = 1f;
            WipeoutMoodBonus = 0f;
        }

        static void Apply(string id)
        {
            switch (id)
            {
                case "locals_favorite": TipMultiplier *= 1.15f; break;
                case "crowd_pleaser": ArrivalMultiplier *= 1.1f; break;
                case "green_park": BillMultiplier *= 0.88f; break;
                case "chill_vibes": PatienceMultiplier *= 1.15f; break;
                case "soft_landings": WipeoutMoodBonus += 0.2f; break;
                case "well_oiled": WearMultiplier *= 0.85f; break;
                case "tidy_guests": LitterMultiplier *= 0.8f; break;
                case "sponsor_darling": SponsorMultiplier *= 1.25f; break;
            }
        }
    }
}
