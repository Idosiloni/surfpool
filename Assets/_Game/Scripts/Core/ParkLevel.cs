using System;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Park level and XP. Happy surfers, finished missions, bought upgrades and your own rides earn XP; each level
    /// unlocks more upgrades on the computer (each upgrade has a minimum park level). XP needed grows each level:
    /// level L needs 60 x (L-1)^1.8 XP in total (60, 209, 433, 727, ...). See docs/BALANCE.md.
    /// </summary>
    public class ParkLevel : MonoBehaviour
    {
        public static ParkLevel Instance { get; private set; }

        public const int XpBeginner = 10, XpIntermediate = 20, XpPro = 35, XpMission = 50, XpUpgrade = 10, XpPlayerRide = 15;

        public int Xp { get; private set; }
        public int Level { get; private set; } = 1;

        /// <summary>Fires with the new level when the park levels up.</summary>
        public event Action<int> LeveledUp;

        public static int XpForLevel(int level) => level <= 1 ? 0 : Mathf.RoundToInt(60f * Mathf.Pow(level - 1, 1.8f));

        /// <summary>0..1 progress from this level to the next.</summary>
        public float Progress
        {
            get
            {
                int from = XpForLevel(Level), to = XpForLevel(Level + 1);
                return Mathf.Clamp01((Xp - from) / (float)(to - from));
            }
        }

        public int XpToNext => XpForLevel(Level + 1) - Xp;

        void Awake() => Instance = this;

        public static int XpForSurfer(WaveLevel skill) => skill switch
        {
            WaveLevel.Beginner => XpBeginner,
            WaveLevel.Intermediate => XpIntermediate,
            _ => XpPro,
        };

        public static void Add(int amount)
        {
            if (Instance != null) Instance.AddXp(amount);
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;
            while (Xp >= XpForLevel(Level + 1))
            {
                Level++;
                LeveledUp?.Invoke(Level);
            }
        }

        /// <summary>From a save: sets XP and the level it gives, without level-up fanfare.</summary>
        public void Restore(int xp)
        {
            Xp = Mathf.Max(0, xp);
            Level = 1;
            while (Xp >= XpForLevel(Level + 1)) Level++;
        }
    }
}
