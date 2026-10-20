using System;
using UnityEngine;

namespace SurfPool
{
    /// <summary>The player's money. One per scene. Money coming in plays a "ka-ching" (see <see cref="Sfx.Money"/>).</summary>
    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }

        [SerializeField] int startingMoney;

        public int Money { get; private set; }

        /// <summary>Fires with the change (positive = earned, negative = spent) and a short reason like "tip".</summary>
        public event Action<int, string> Changed;

        void Awake()
        {
            Instance = this;
            Money = startingMoney;
        }

        public void Add(int amount, string reason = null)
        {
            if (amount <= 0) return;
            Money += amount;
            GameStats.TotalEarned += amount;
            Sfx.Money(amount);
            Changed?.Invoke(amount, reason);
        }

        public bool Spend(int amount, string reason = null)
        {
            if (amount > Money) return false;
            Money -= amount;
            Changed?.Invoke(-amount, reason);
            return true;
        }

        public void Restore(int money) => Money = money;

        /// <summary>Takes money even if it goes below zero (bills like staff wages).</summary>
        public void Charge(int amount, string reason = null)
        {
            if (amount <= 0) return;
            Money -= amount;
            Changed?.Invoke(-amount, reason);
        }
    }
}
