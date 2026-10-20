using System.Collections.Generic;

namespace SurfPool
{
    /// <summary>All pools in the park (open or not yet built).</summary>
    public static class PoolRegistry
    {
        public static readonly List<PoolArea> All = new();

        public static int TotalPools => All.Count;

        public static int OpenPools
        {
            get
            {
                int open = 0;
                foreach (var pool in All)
                    if (pool.IsOpen) open++;
                return open;
            }
        }
    }
}
