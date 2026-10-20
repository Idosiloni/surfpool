using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>A hired worker. Active staff are paid their wage every in-game minute by <see cref="StaffPayroll"/>.</summary>
    public class StaffMember : MonoBehaviour
    {
        public static readonly List<StaffMember> Active = new();

        [SerializeField] string jobTitle = "Staff";
        [Tooltip("Dollars paid every minute this worker is hired.")]
        [SerializeField] int wagePerMinute = 3;

        public string JobTitle => jobTitle;
        public int WagePerMinute => wagePerMinute;

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);
    }
}
