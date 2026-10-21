using System;
using UnityEngine;

namespace SurfPool
{
    /// <summary>Wave sizes the machine can send, and the matching surfer skill levels.</summary>
    public enum WaveLevel { Beginner, Intermediate, Pro }

    /// <summary>
    /// Which way a wave peels, from a surfer's point of view facing the beach.
    /// Right: the break moves to the surfer's right. Left: to their left. Peak: both ways from the middle.
    /// </summary>
    public enum WaveDirection { Left, Peak, Right }

    /// <summary>
    /// How the wave breaks (an operator setting, like real wave pools sell): Mellow = gentle and forgiving (spilling),
    /// Standard, Hollow = steep and throwing (barrels on green waves too; pros love it, others wipe out more).
    /// </summary>
    public enum WaveShape { Mellow, Standard, Hollow }

    /// <summary>
    /// Where along its peel a wave is (like the Surf Ranch): the takeoff, an open wall to carve, a hollow barrel
    /// section, then it closes out.
    /// </summary>
    public enum WaveSection { Takeoff, Wall, Barrel, Closeout }

    /// <summary>XL waves for the deepest pools (Lagoon, Surf Ranch): bigger Pro waves, for pros only.</summary>
    public enum WaveSize { Normal, Big, Giant }

    public static class WaveSizes
    {
        /// <summary>Height multiplier (Giant ~4.5 m faces in the Lagoon, about the 3.8 m pool world record and a bit).</summary>
        public static float Height(WaveSize size) => size switch { WaveSize.Big => 1.15f, WaveSize.Giant => 1.3f, _ => 1f };

        /// <summary>Power per wave multiplier.</summary>
        public static float Power(WaveSize size) => size switch { WaveSize.Big => 1.5f, WaveSize.Giant => 2f, _ => 1f };
    }

    /// <summary>The shape and breaking behavior of one kind of wave.</summary>
    [Serializable]
    public class WaveType
    {
        public WaveLevel level;
        [Tooltip("How tall the wave gets above the calm water (m).")]
        public float height = 2f;
        [Tooltip("How fast the wave travels down the pool (m/s).")]
        public float speed = 7f;
        [Tooltip("Width of the steep front face. Smaller = steeper.")]
        public float frontWidth = 2f;
        [Tooltip("Width of the gentle back of the wave.")]
        public float backWidth = 8f;

        [Header("Breaking")]
        [Tooltip("How far down the pool (0 to 1) the wave starts to break.")]
        [Range(0f, 1f)] public float breakAt = 0.25f;
        [Tooltip("The whole wave breaks at once into rolling whitewater (beginner waves).")]
        public bool closeout;
        [Tooltip("How fast the break point travels along the wave (m/s). Ignored for closeouts.")]
        public float peelSpeed = 2.6f;
        [Tooltip("Height of the broken whitewater compared to the clean wave (0 to 1).")]
        [Range(0f, 1f)] public float whitewashHeight = 0.45f;
        [Tooltip("The lip throws forward into a barrel near the break point.")]
        public bool barrel;
    }
}
