using UnityEngine;

namespace SurfPool
{
    /// <summary>Pays all hired staff once per in-game minute. Money can go below zero if you can't afford them.</summary>
    public class StaffPayroll : MonoBehaviour
    {
        [SerializeField] float payInterval = 60f;

        float nextPay;

        public int WagesPerMinute
        {
            get
            {
                int total = 0;
                foreach (var staff in StaffMember.Active) total += staff.WagePerMinute;
                return total;
            }
        }

        public float SecondsUntilPayday => Mathf.Max(0f, nextPay - Time.time);

        void Start() => nextPay = Time.time + payInterval;

        void Update()
        {
            if (Time.time < nextPay) return;
            nextPay = Time.time + payInterval;
            float minutes = payInterval / 60f;
            int wages = Mathf.RoundToInt(WagesPerMinute * minutes);
            if (wages > 0) Wallet.Instance.Charge(wages, "staff wages");
        }
    }
}
