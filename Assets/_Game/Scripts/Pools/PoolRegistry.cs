using System.Collections.Generic;

namespace SurfPool
{
    /// <summary>All pools in the park (open or not yet built).</summary>
    public static class PoolRegistry
    {
        public static readonly List<PoolArea> All = new();

        /// <summary>Pools you can build, counted once each (the Cove's two sides are one pool).</summary>
        public static int TotalPools => Count(false);

        public static int OpenPools => Count(true);

        static int Count(bool openOnly)
        {
            var seen = new HashSet<string>();
            foreach (var pool in All)
                if (!openOnly || pool.IsOpen) seen.Add(pool.UpgradeId ?? "");
            return seen.Count;
        }
    }
}
