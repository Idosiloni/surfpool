using System.Collections;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// People talk Animal Crossing style: when a speech bubble pops up they babble a few short syllables at their own
    /// pitch (kids high, dads low; CharacterLook.voice), and they shout "yeeew", "woo" or "aah" at big moments.
    /// Only people near the camera talk, and only a few at once, so the park murmurs instead of shrieking.
    /// Clips come from tools/audio/make_sounds.py (voice_*).
    /// </summary>
    public class Voices : MonoBehaviour
    {
        const float Range = 26f;
        const int MaxTalkers = 4;

        static Voices runner;
        static int talking;

        static Voices Runner
        {
            get
            {
                if (runner == null)
                {
                    var go = new GameObject("Voices");
                    runner = go.AddComponent<Voices>();
                }
                return runner;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            runner = null;
            talking = 0;
        }

        static bool Near(Transform who)
        {
            var cam = Camera.main;
            return who != null && cam != null && (cam.transform.position - who.position).sqrMagnitude < Range * Range;
        }

        /// <summary>Babbles a line: about one syllable per 4 letters (2 to 9).</summary>
        public static void Talk(Transform who, float pitch, string text, float volume = 1f)
        {
            if (!Near(who) || talking >= MaxTalkers || string.IsNullOrEmpty(text)) return;
            int syllables = Mathf.Clamp(text.Length / 4, 2, 9);
            Runner.StartCoroutine(Babble(who, pitch, syllables, volume, text.TrimEnd().EndsWith("?")));
        }

        /// <summary>A quiet mumble to themselves (waiting in line, sunbathing): 2-4 low syllables and maybe a "hmm".</summary>
        public static void Mumble(Transform who, float pitch)
        {
            if (!Near(who) || talking >= MaxTalkers) return;
            Runner.StartCoroutine(Babble(who, pitch * 0.92f, Random.Range(2, 5), 0.55f, false, Random.value < 0.4f));
        }

        /// <summary>A shout: Yew (catching a wave), Woo (a trick), Aah (a wipeout), Ooh (surprise), Hmm.</summary>
        public static void Exclaim(Transform who, float pitch, Sound sound, float volume = 1f)
        {
            if (!Near(who)) return;
            Sfx.Play(sound, who.position + Vector3.up * 1.6f, volume, pitch * Random.Range(0.95f, 1.05f));
        }

        static IEnumerator Babble(Transform who, float pitch, int syllables, float volume, bool question, bool hmm = false)
        {
            talking++;
            try
            {
                for (int i = 0; i < syllables && who != null; i++)
                {
                    // A question rises at the end; statements drift down a little.
                    float contour = question && i == syllables - 1 ? 1.18f : 1f - 0.03f * i / syllables;
                    Sfx.Play(Sound.VoiceBabble, who.position + Vector3.up * 1.6f, volume, pitch * contour * Random.Range(0.93f, 1.07f));
                    yield return new WaitForSeconds(Random.Range(0.075f, 0.11f));
                    if (Random.value < 0.12f) yield return new WaitForSeconds(0.12f); // a little pause between words
                }
                if (hmm && who != null) Sfx.Play(Sound.VoiceHmm, who.position + Vector3.up * 1.6f, volume, pitch);
            }
            finally
            {
                talking--;
            }
        }
    }
}
