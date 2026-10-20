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

        /// <summary>Multiplies beginners' wipeout chance. The surf coach lowers it.</summary>
        public static float BeginnerWipeoutMultiplier = 1f;

        /// <summary>Extra lesson fee paid when a beginner finishes a session (surf coach).</summary>
        public static int LessonFee;

        /// <summary>Merch each happy surfer buys at the surf shop.</summary>
        public static int MerchPerHappySurfer;

        /// <summary>Passive income per minute (season passes).</summary>
        public static int PassiveIncomePerMinute;

        /// <summary>Multiplies the power each wave uses. Solar Panels lower it.</summary>
        public static float ElectricityCostMultiplier = 1f;

        /// <summary>Multiplies the daily water bill. Water Recycling lowers it.</summary>
        public static float WaterCostMultiplier = 1f;

        /// <summary>Multiplies how often surfers drop litter. Trash Cans lower it.</summary>
        public static float LitterMultiplier = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatics()
        {
            WavePatienceMultiplier = 1f;
            WipeoutMood = 0.3f;
            BeginnerWipeoutMultiplier = 1f;
            LessonFee = 0;
            MerchPerHappySurfer = 0;
            PassiveIncomePerMinute = 0;
            ElectricityCostMultiplier = 1f;
            WaterCostMultiplier = 1f;
            LitterMultiplier = 1f;
        }
    }
}
