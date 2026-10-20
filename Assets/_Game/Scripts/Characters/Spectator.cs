using UnityEngine;

namespace SurfPool
{
    /// <summary>A grandstand spectator: sits, gets random colors, and now and then throws their arms up to cheer.</summary>
    [RequireComponent(typeof(CharacterRig))]
    public class Spectator : MonoBehaviour
    {
        CharacterRig rig;
        float nextCheer, cheerUntil;

        void Start()
        {
            rig = GetComponent<CharacterRig>();
            var skin = Color.Lerp(new Color(0.42f, 0.27f, 0.17f), new Color(1f, 0.84f, 0.7f), Random.value);
            var hair = Random.value < 0.25f ? new Color(0.95f, 0.82f, 0.45f) : Color.Lerp(new Color(0.08f, 0.06f, 0.05f), new Color(0.45f, 0.28f, 0.15f), Random.value);
            rig.Paint(skin, Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.6f, 1f), Random.ColorHSV(0f, 1f, 0.4f, 0.8f, 0.3f, 0.8f), hair, Random.Range(0, 2));
            rig.CurrentMode = CharacterRig.Mode.Sit;
            nextCheer = Time.time + Random.Range(5f, 25f);
        }

        void Update()
        {
            if (Time.time >= nextCheer)
            {
                cheerUntil = Time.time + Random.Range(1.5f, 3f);
                nextCheer = Time.time + Random.Range(10f, 35f);
            }
            rig.CheerArms = Time.time < cheerUntil;
        }
    }
}
