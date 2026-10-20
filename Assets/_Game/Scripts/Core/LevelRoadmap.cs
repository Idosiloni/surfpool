using System.Collections.Generic;

namespace SurfPool
{
    /// <summary>
    /// What the next park levels bring (upgrades that unlock, perk picks), so the HUD, the goal strip and the
    /// upgrade computer all preview the same thing. Skips levels that bring nothing, so the line is never empty.
    /// </summary>
    public static class LevelRoadmap
    {
        /// <summary>
        /// The next level above `level` that brings something, and what (or null if nothing within 15). With
        /// maxNames > 0, long lists are cut to that many names plus "+N more" (for the small HUD line).
        /// </summary>
        public static string Next(UpgradeShop shop, int level, out int at, int maxNames = 0)
        {
            for (int l = level + 1; l <= level + 15; l++)
            {
                var parts = shop != null ? shop.UnlockedAt(l) : new List<string>();
                bool pick = ParkPerks.PickAt(l);
                if (parts.Count == 0 && !pick) continue;
                at = l;
                if (maxNames > 0 && parts.Count > maxNames)
                {
                    int extra = parts.Count - maxNames;
                    parts.RemoveRange(maxNames, extra);
                    parts.Add($"{extra} more");
                }
                if (pick) parts.Add("a perk pick");
                return Join(parts);
            }
            at = -1;
            return null;
        }

        /// <summary>"A, B and C".</summary>
        public static string Join(List<string> parts)
        {
            if (parts.Count <= 1) return parts.Count == 1 ? parts[0] : "";
            return string.Join(", ", parts.GetRange(0, parts.Count - 1)) + " and " + parts[parts.Count - 1];
        }
    }
}
