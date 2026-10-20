using UnityEngine;

namespace SurfPool
{
    /// <summary>Park-wide modifiers that upgrades and staff change, read by surfers.</summary>
    public static class ParkEffects
    {
        /// <summary>Multiplies how long surfers will wait in the water for a wave. The lifeguard raises it.</summary>
        public static float WavePatienceMultiplier = 1f;

        /// <summary>Mood a surfer is left with after a wipeout (0 to 1). The lifeguard raises it.</summary>
        public static float WipeoutMood = 0.3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            WavePatienceMultiplier = 1f;
            WipeoutMood = 0.3f;
        }
    }
}
