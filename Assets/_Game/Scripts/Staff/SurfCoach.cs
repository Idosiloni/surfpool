using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired surf coach: beginners wipe out far less and pay a lesson fee when they finish a session.</summary>
    public class SurfCoach : MonoBehaviour
    {
        [Range(0f, 1f)] [SerializeField] float wipeoutMultiplier = 0.35f;
        [SerializeField] int lessonFee = 25;

        static SurfCoach onDuty;

        /// <summary>Lesson fee for one beginner session: grows 10% per coach star. Counts as a lesson taught.</summary>
        public static int TeachLesson()
        {
            if (onDuty == null || ParkEffects.LessonFee <= 0) return 0;
            var staff = onDuty.GetComponent<StaffMember>();
            int stars = staff != null ? staff.Stars : 1;
            if (staff != null) staff.AddExperience();
            return Mathf.RoundToInt(ParkEffects.LessonFee * (1f + 0.1f * (stars - 1)));
        }

        void OnEnable()
        {
            onDuty = this;
            ParkEffects.BeginnerWipeoutMultiplier = wipeoutMultiplier;
            ParkEffects.LessonFee = lessonFee;
        }

        void OnDisable()
        {
            if (onDuty == this) onDuty = null;
            ParkEffects.BeginnerWipeoutMultiplier = 1f;
            ParkEffects.LessonFee = 0;
        }
    }
}
