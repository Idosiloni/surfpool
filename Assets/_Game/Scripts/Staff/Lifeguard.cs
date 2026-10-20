using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired lifeguard: surfers feel safer, so they wait longer for waves and shrug off wipeouts.</summary>
    public class Lifeguard : MonoBehaviour
    {
        [SerializeField] float wavePatienceMultiplier = 1.5f;
        [Range(0f, 1f)] [SerializeField] float wipeoutMood = 0.6f;

        void OnEnable()
        {
            ParkEffects.WavePatienceMultiplier = wavePatienceMultiplier;
            ParkEffects.WipeoutMood = wipeoutMood;
        }

        void OnDisable()
        {
            ParkEffects.WavePatienceMultiplier = 1f;
            ParkEffects.WipeoutMood = 0.3f;
        }
    }
}
