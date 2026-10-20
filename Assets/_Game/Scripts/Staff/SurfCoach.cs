using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired surf coach: beginners wipe out far less and pay a lesson fee when they finish a session.</summary>
    public class SurfCoach : MonoBehaviour
    {
        [Range(0f, 1f)] [SerializeField] float wipeoutMultiplier = 0.35f;
        [SerializeField] int lessonFee = 25;

        void OnEnable()
        {
            ParkEffects.BeginnerWipeoutMultiplier = wipeoutMultiplier;
            ParkEffects.LessonFee = lessonFee;
        }

        void OnDisable()
        {
            ParkEffects.BeginnerWipeoutMultiplier = 1f;
            ParkEffects.LessonFee = 0;
        }
    }
}
