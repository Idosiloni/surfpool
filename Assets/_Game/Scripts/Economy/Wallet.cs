using System;
using UnityEngine;

namespace SurfPool
{
    /// <summary>The player's money. One per scene.</summary>
    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }

        [SerializeField] int startingMoney;

        public int Money { get; private set; }

        /// <summary>Fires with the amount earned, for "+$20" popups.</summary>
        public event Action<int> Earned;

        void Awake()
        {
            Instance = this;
            Money = startingMoney;
        }

        public void Add(int amount)
        {
            Money += amount;
            Earned?.Invoke(amount);
        }
    }
}
