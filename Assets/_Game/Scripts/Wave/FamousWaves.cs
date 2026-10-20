namespace SurfPool
{
    /// <summary>A wave modeled on a real famous surf spot: a wave type and direction with its own size and peel speed.</summary>
    public class FamousWave
    {
        public string name, place, description;
        public WaveLevel level;
        public WaveDirection direction;
        public float size = 1f, peel = 1f;
        public bool lagoonOnly;
    }

    /// <summary>
    /// The Famous Waves Pack (an upgrade): presets on the wave console inspired by real spots. Surfers riding one
    /// get excited ("I'm surfing Pipeline!"), enjoy it more and tip 50% more. Teahupo'o is too big for anything
    /// but the Big Wave Lagoon. See docs/research/wave-types-real.md.
    /// </summary>
    public static class FamousWaves
    {
        public static readonly FamousWave[] All =
        {
            new() { name = "Waikiki", place = "Hawaii", level = WaveLevel.Beginner, direction = WaveDirection.Peak, size = 0.85f,
                description = "Soft, friendly rollers. The birthplace of modern surfing." },
            new() { name = "Malibu", place = "California", level = WaveLevel.Intermediate, direction = WaveDirection.Right, size = 0.9f, peel = 0.6f,
                description = "A long, mellow right point. Long rides with time for lots of turns." },
            new() { name = "Snapper Rocks", place = "Australia", level = WaveLevel.Pro, direction = WaveDirection.Right, size = 1.0f, peel = 0.75f,
                description = "A long, fast, sand-bottom right with barrel sections." },
            new() { name = "Pipeline", place = "Hawaii", level = WaveLevel.Pro, direction = WaveDirection.Left, size = 1.15f, peel = 1.25f,
                description = "The most famous barrel on Earth: a heavy, hollow left. Short and scary." },
            new() { name = "Teahupo'o", place = "Tahiti", level = WaveLevel.Pro, direction = WaveDirection.Left, size = 1.35f, peel = 1.1f, lagoonOnly = true,
                description = "The heaviest wave in the world. Only the Big Wave Lagoon is deep enough." },
        };

        /// <summary>Can this machine make this wave right now?</summary>
        public static bool Available(FamousWave wave, WaveMachine machine) =>
            ParkEffects.FamousWavesUnlocked && machine.IsUnlocked(wave.level) && machine.IsUnlocked(wave.direction) &&
            (!wave.lagoonOnly || (machine.Pool != null && machine.Pool.UpgradeId == "pool_lagoon"));
    }
}
