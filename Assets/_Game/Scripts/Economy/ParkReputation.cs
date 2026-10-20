using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The park's rating out of 5. Every surfer who leaves nudges it toward how happy they were
    /// (lost customers count as 0). A better rating brings surfers in faster.
    /// </summary>
    public class ParkReputation : MonoBehaviour
    {
        public static ParkReputation Instance { get; private set; }

        [Tooltip("Starting reputation, 0 to 1.")]
        [Range(0f, 1f)] [SerializeField] float startingScore = 0.6f;
        [Tooltip("How much one customer moves the score (0 to 1). Higher = rating changes faster.")]
        [Range(0.01f, 0.5f)] [SerializeField] float weight = 0.12f;

        /// <summary>0 = terrible, 1 = perfect.</summary>
        public float Score { get; private set; }

        /// <summary>1 to 5, in half stars.</summary>
        public float Stars => Mathf.Round((1f + 4f * Score) * 2f) / 2f;

        void Awake()
        {
            Instance = this;
            Score = startingScore;
        }

        public void Restore(float score) => Score = Mathf.Clamp01(score);

        public void Record(float happiness)
        {
            Score = Mathf.Clamp01(Mathf.Lerp(Score, Mathf.Clamp01(happiness), Mathf.Clamp01(weight * DailyEvents.RatingSpeedMultiplier)));
        }
    }
}
