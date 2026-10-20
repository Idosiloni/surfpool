using System;
using UnityEngine;

namespace SurfPool
{
    /// <summary>Wave sizes the machine can send, and the matching surfer skill levels.</summary>
    public enum WaveLevel { Beginner, Intermediate, Pro }

    /// <summary>The shape of one kind of wave.</summary>
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
    }
}
