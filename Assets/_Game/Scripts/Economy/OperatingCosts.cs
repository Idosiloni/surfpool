using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The park's running costs, paid at closing time with the day report:
    /// - Electricity: every wave the machines send uses power (bigger waves and bigger pools use more). Metered
    ///   during the day, so the HUD can show today's bill growing.
    /// - Water: each open pool needs topping up and filtering every day (bigger pools cost more).
    /// - Business tax: a share of the day's income.
    /// Solar Panels and Water Recycling cut the bills (see <see cref="ParkEffects"/>).
    /// </summary>
    public class OperatingCosts : MonoBehaviour
    {
        public static OperatingCosts Instance { get; private set; }

        [Tooltip("Power for one wave in the main pool, by wave type (Beginner, Intermediate, Pro). Other pools multiply it by their price multiplier.")]
        [SerializeField] float[] powerPerWave = { 1f, 3f, 6f };
        [Tooltip("Share of the day's income paid as business tax (0 to 1).")]
        [Range(0f, 0.5f)] [SerializeField] float taxRate = 0.06f;

        float electricityToday;

        public float TaxRate => taxRate;
        public int ElectricityToday => Mathf.RoundToInt(electricityToday);

        public int WaterToday
        {
            get
            {
                float total = 0f;
                foreach (var pool in PoolRegistry.All)
                    if (pool.IsOpen) total += pool.DailyWaterCost;
                return Mathf.RoundToInt(total * ParkEffects.WaterCostMultiplier * DailyEvents.WaterMultiplier * ParkPerks.BillMultiplier);
            }
        }

        void Awake() => Instance = this;

        /// <summary>What one wave of this type costs to send in this pool right now.</summary>
        public int PowerCost(WaveLevel level, PoolArea pool)
        {
            float cost = powerPerWave[Mathf.Clamp((int)level, 0, powerPerWave.Length - 1)];
            if (pool != null) cost *= pool.PriceMultiplier;
            return Mathf.Max(1, Mathf.RoundToInt(cost * ParkEffects.ElectricityCostMultiplier * DailyEvents.ElectricityMultiplier * ParkPerks.BillMultiplier));
        }

        /// <summary>A machine just sent waves: add their power to today's meter.</summary>
        public void MeterWaves(WaveLevel level, PoolArea pool, int count, float scale = 1f)
        {
            electricityToday += PowerCost(level, pool) * count * scale;
        }

        /// <summary>Estimated bills so far today (power + water + tax on today's income).</summary>
        public int EstimateToday(int incomeToday) => ElectricityToday + WaterToday + Mathf.RoundToInt(incomeToday * taxRate);

        /// <summary>Closing time: pays power, water and tax. The day report lists them under expenses.</summary>
        public void PayDailyBills(int incomeToday)
        {
            var wallet = Wallet.Instance;
            if (wallet == null) return;
            wallet.Charge(ElectricityToday, "electricity");
            wallet.Charge(WaterToday, "water");
            wallet.Charge(Mathf.RoundToInt(incomeToday * taxRate), "business tax");
            electricityToday = 0f;
        }
    }
}
