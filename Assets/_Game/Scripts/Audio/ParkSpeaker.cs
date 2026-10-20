using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A PA speaker pole. Once the park is festive enough for speakers to go up, <see cref="Sfx"/> plays the current
    /// song out of every speaker (in sync, so you hear it louder as you walk by) and the announcer talks through them.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ParkSpeaker : MonoBehaviour
    {
        public static readonly List<ParkSpeaker> Active = new();

        public AudioSource Source { get; private set; }

        void Awake()
        {
            Source = GetComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.loop = false;
            Source.spatialBlend = 1f;
            Source.rolloffMode = AudioRolloffMode.Linear;
            Source.minDistance = 4f;
            Source.maxDistance = 38f;
            Source.dopplerLevel = 0f;
        }

        void OnEnable() => Active.Add(this);

        void OnDisable()
        {
            Active.Remove(this);
            if (Source != null) Source.Stop();
        }

        /// <summary>The active speaker closest to a point (for announcements), or null.</summary>
        public static ParkSpeaker Nearest(Vector3 to)
        {
            ParkSpeaker best = null;
            float bestDistance = float.MaxValue;
            foreach (var speaker in Active)
            {
                float d = (speaker.transform.position - to).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = speaker; }
            }
            return best;
        }
    }
}
