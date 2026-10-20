using System;
using UnityEngine;

namespace SurfPool
{
    /// <summary>The player's money. One per scene. Plays a "ka-ching" when money comes in.</summary>
    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }

        [SerializeField] int startingMoney;
        [SerializeField] [Range(0f, 1f)] float chimeVolume = 0.25f;

        AudioSource audioSource;
        AudioClip chime;

        public int Money { get; private set; }

        /// <summary>Fires with the change (positive = earned, negative = spent) and a short reason like "tip".</summary>
        public event Action<int, string> Changed;

        void Awake()
        {
            Instance = this;
            Money = startingMoney;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            chime = BuildChime();
        }

        public void Add(int amount, string reason = null)
        {
            if (amount <= 0) return;
            Money += amount;
            audioSource.PlayOneShot(chime, chimeVolume);
            Changed?.Invoke(amount, reason);
        }

        public bool Spend(int amount, string reason = null)
        {
            if (amount > Money) return false;
            Money -= amount;
            Changed?.Invoke(-amount, reason);
            return true;
        }

        /// <summary>Takes money even if it goes below zero (bills like staff wages).</summary>
        public void Charge(int amount, string reason = null)
        {
            if (amount <= 0) return;
            Money -= amount;
            Changed?.Invoke(-amount, reason);
        }

        /// <summary>Two quick high notes, generated in code until we have real sound effects.</summary>
        static AudioClip BuildChime()
        {
            const int rate = 44100;
            const float noteLength = 0.09f;
            float[] notes = { 1318.5f, 1760f };
            int perNote = (int)(rate * noteLength);
            var data = new float[perNote * notes.Length + rate / 10];
            for (int n = 0; n < notes.Length; n++)
            for (int i = 0; i < perNote + rate / 10 && n * perNote + i < data.Length; i++)
            {
                float t = i / (float)rate;
                data[n * perNote + i] += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * Mathf.Exp(-t * 18f) * 0.5f;
            }
            var clip = AudioClip.Create("Chime", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
