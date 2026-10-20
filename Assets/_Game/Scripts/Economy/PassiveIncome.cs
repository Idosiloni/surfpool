using UnityEngine;

namespace SurfPool
{
    /// <summary>Pays passive income (season passes, grandstand spectators) once a minute.</summary>
    public class PassiveIncome : MonoBehaviour
    {
        float nextPay;

        void Start() => nextPay = Time.time + 60f;

        void Update()
        {
            if (Time.time < nextPay) return;
            nextPay = Time.time + 60f;
            if (ParkEffects.PassiveIncomePerMinute > 0) Wallet.Instance.Add(ParkEffects.PassiveIncomePerMinute, "passes & spectators");
        }
    }
}
