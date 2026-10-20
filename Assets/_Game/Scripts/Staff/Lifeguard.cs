using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired lifeguard: surfers feel safer, so they wait longer for waves and shrug off wipeouts.</summary>
    public class Lifeguard : MonoBehaviour
    {
        [SerializeField] float wavePatienceMultiplier = 1.5f;
        [Range(0f, 1f)] [SerializeField] float wipeoutMood = 0.6f;

        static Lifeguard onDuty;
        float nextWhistle;

        /// <summary>The lifeguard (if hired) blows the whistle from the tower: wipeouts, closing time.</summary>
        public static void Whistle()
        {
            if (onDuty == null || Time.time < onDuty.nextWhistle) return;
            onDuty.nextWhistle = Time.time + 6f;
            Sfx.Play(Sound.Whistle, onDuty.transform.position);
        }

        void OnEnable()
        {
            onDuty = this;
            ParkEffects.WavePatienceMultiplier = wavePatienceMultiplier;
            ParkEffects.WipeoutMood = wipeoutMood;
        }

        void OnDisable()
        {
            if (onDuty == this) onDuty = null;
            ParkEffects.WavePatienceMultiplier = 1f;
            ParkEffects.WipeoutMood = 0.3f;
        }
    }
}
