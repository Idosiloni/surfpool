using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    public enum Sound
    {
        Click, Error, Buy, Coin, CashBig, Mission, DayEnd,
        WaveMachine, CrashSmall, CrashMedium, CrashBig,
        Splash, SplashBig, Cheer, Whistle, Seagull,
        PaChime, PaVoice, FanfareLevel, FanfarePool, FireworkLaunch, FireworkBang,
    }

    /// <summary>
    /// Every sound clip in the game and how it plays (volume, 3D distance). Filled in by the scene builder from the
    /// WAVs that tools/audio/make_sounds.py writes to Assets/_Game/Audio/.
    /// </summary>
    public class SoundBank : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public Sound id;
            [Tooltip("Variants: a different one plays each time.")]
            public AudioClip[] clips;
            [Range(0f, 1f)] public float volume = 1f;
            [Tooltip("3D sounds get quieter with distance; 2D sounds (UI, money) don't.")]
            public bool spatial;
            public float minDistance = 5f;
            public float maxDistance = 60f;
            [Tooltip("Random pitch change, so repeats don't sound identical.")]
            public float pitchJitter = 0.06f;
            [Tooltip("Seconds before this sound can play again.")]
            public float minGap = 0.08f;
        }

        public List<Entry> entries = new();
        public AudioClip surfLoop;
        public AudioClip crowdLoop;
        [Tooltip("A packed park: fades in on top of the crowd bed as the park level grows.")]
        public AudioClip crowdBigLoop;
        public AudioClip windLoop;
        public AudioClip[] music;
        [Tooltip("Upbeat songs that join the rotation once the park is busy (and the speakers are up).")]
        public AudioClip[] partyMusic;
        public AudioClip jetSkiLoop;
        public AudioClip heliLoop;

        public Entry Get(Sound id) => entries.Find(e => e.id == id);
    }
}
