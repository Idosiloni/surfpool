using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A spectator (in the grandstand, or standing at the pool rail): gets random colors, and now and then throws their
    /// arms up to cheer. <see cref="CheerAll"/> makes the whole crowd cheer at once (level-ups, new pools).
    /// </summary>
    [RequireComponent(typeof(CharacterRig))]
    public class Spectator : MonoBehaviour
    {
        [Tooltip("Stands (at a rail) instead of sitting.")]
        [SerializeField] bool standing;

        static float crowdCheerUntil;

        CharacterRig rig;
        float nextCheer, cheerUntil;

        /// <summary>Every spectator cheers for a few seconds.</summary>
        public static void CheerAll(float seconds) => crowdCheerUntil = Time.time + seconds;

        void Start()
        {
            rig = GetComponent<CharacterRig>();
            var skin = Color.Lerp(new Color(0.42f, 0.27f, 0.17f), new Color(1f, 0.84f, 0.7f), Random.value);
            var hair = Random.value < 0.25f ? new Color(0.95f, 0.82f, 0.45f) : Color.Lerp(new Color(0.08f, 0.06f, 0.05f), new Color(0.45f, 0.28f, 0.15f), Random.value);
            rig.Paint(skin, Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.6f, 1f), Random.ColorHSV(0f, 1f, 0.4f, 0.8f, 0.3f, 0.8f), hair, Random.Range(0, 2));
            rig.CurrentMode = standing ? CharacterRig.Mode.Locomotion : CharacterRig.Mode.Sit;
            nextCheer = Time.time + Random.Range(5f, 25f);
        }

        void Update()
        {
            if (Time.time >= nextCheer)
            {
                cheerUntil = Time.time + Random.Range(1.5f, 3f);
                nextCheer = Time.time + Random.Range(10f, 35f);
            }
            rig.CheerArms = Time.time < cheerUntil || Time.time < crowdCheerUntil;
        }
    }
}
