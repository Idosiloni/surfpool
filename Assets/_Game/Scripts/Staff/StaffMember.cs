using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A hired worker. Active staff are paid their wage every in-game minute by <see cref="StaffPayroll"/>.
    /// Staff get better with practice: every job done is experience, and they earn up to 5 stars. Each star makes
    /// them 10% quicker, and their wage goes up 15% per star (experienced staff cost more).
    /// </summary>
    public class StaffMember : MonoBehaviour
    {
        public static readonly List<StaffMember> Active = new();

        /// <summary>Jobs done to reach 2, 3, 4 and 5 stars.</summary>
        public static readonly int[] StarThresholds = { 15, 50, 120, 250 };
        public const int MaxStars = 5;

        [SerializeField] string jobTitle = "Staff";
        [Tooltip("Dollars paid every minute at 1 star.")]
        [SerializeField] int wagePerMinute = 3;

        public string JobTitle => jobTitle;
        public int Experience { get; private set; }

        public int Stars
        {
            get
            {
                int stars = 1;
                foreach (int threshold in StarThresholds)
                    if (Experience >= threshold) stars++;
                return stars;
            }
        }

        public int WagePerMinute => Mathf.RoundToInt(wagePerMinute * (1f + 0.15f * (Stars - 1)));

        /// <summary>Time a job takes compared to a 1-star worker (1 star = 1, 5 stars = 0.6).</summary>
        public float TimeMultiplier => 1f - 0.1f * (Stars - 1);

        /// <summary>Jobs left until the next star (0 at max).</summary>
        public int JobsToNextStar => Stars >= MaxStars ? 0 : StarThresholds[Stars - 1] - Experience;

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public void AddExperience(int jobs = 1)
        {
            int before = Stars;
            Experience += jobs;
            if (Stars > before)
                Notifications.Post($"{jobTitle} reached {Stars} stars! Works {Mathf.RoundToInt((1f - TimeMultiplier) * 100f)}% faster.");
        }

        public void RestoreExperience(int jobs) => Experience = Mathf.Max(0, jobs);

        /// <summary>Speed factor for the worker on this object (1 if it isn't staff).</summary>
        public static float TimeFactor(GameObject worker)
        {
            var member = worker.GetComponentInParent<StaffMember>();
            return member != null ? member.TimeMultiplier : 1f;
        }

        /// <summary>Counts one finished job for the worker on this object.</summary>
        public static void RecordWork(GameObject worker)
        {
            var member = worker.GetComponentInParent<StaffMember>();
            if (member != null) member.AddExperience();
        }

        public static string StarText(int stars) => $"{stars}/{MaxStars} stars";
    }
}
