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

        /// <summary>Multiplies how fast fishing line tension builds (Pro Fishing Rod lowers it).</summary>
        public static float FishingTensionMultiplier = 1f;

        /// <summary>Multiplies the chance of big fish (Pro Fishing Rod raises it).</summary>
        public static float FishingRareMultiplier = 1f;

        /// <summary>Beach guests come to relax on placed loungers and benches (Beach Passes).</summary>
        public static bool BeachPassesOpen;

        /// <summary>Famous wave presets on the wave consoles (Famous Waves Pack).</summary>
        public static bool FamousWavesUnlocked;

        /// <summary>Added to the snack price (Bigger Snack Menu, id smoothie_bar); acai shakes get half.</summary>
        public static int SnackPriceBonus;

        /// <summary>Shop speed-ups: blending (Turbo Blender), ringing up gear (Card Reader), planing boards (Power Planer).</summary>
        public static float BlendTimeMultiplier = 1f, ScanTimeMultiplier = 1f, ShapeTimeMultiplier = 1f;

        /// <summary>Added to every gear shop tip (Card Reader: the card screen asks for a tip).</summary>
        public static int CardTip;

        /// <summary>Multiplies how often surfers drop litter. Trash Cans lower it.</summary>
        public static float LitterMultiplier = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatics()
        {
            ParkPerks.Reset();
            WavePatienceMultiplier = 1f;
            WipeoutMood = 0.3f;
            BeginnerWipeoutMultiplier = 1f;
            LessonFee = 0;
            MerchPerHappySurfer = 0;
            PassiveIncomePerMinute = 0;
            ElectricityCostMultiplier = 1f;
            WaterCostMultiplier = 1f;
            LitterMultiplier = 1f;
            SnackPriceBonus = 0;
            BlendTimeMultiplier = ScanTimeMultiplier = ShapeTimeMultiplier = 1f;
            CardTip = 0;
            FamousWavesUnlocked = false;
            BeachPassesOpen = false;
            FishingTensionMultiplier = 1f;
            FishingRareMultiplier = 1f;
        }
    }
}
