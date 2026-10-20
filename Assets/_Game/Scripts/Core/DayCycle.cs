using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>What happened during one day, for the end-of-day report.</summary>
    public class DayReport
    {
        public int day;
        public readonly SortedDictionary<string, int> income = new();
        public readonly SortedDictionary<string, int> expenses = new();
        public int served, lost;
        public float ratingStart, ratingEnd;

        public int TotalIncome { get { int t = 0; foreach (var v in income.Values) t += v; return t; } }
        public int TotalExpenses { get { int t = 0; foreach (var v in expenses.Values) t += v; return t; } }

        public void Record(int amount, string reason)
        {
            string key = string.IsNullOrEmpty(reason) ? "other" : reason;
            var book = amount >= 0 ? income : expenses;
            book.TryGetValue(key, out int current);
            book[key] = current + Mathf.Abs(amount);
        }
    }

    /// <summary>
    /// The park day: 8 AM to 8 PM over a few real minutes. The sun moves across the sky and turns golden in the
    /// evening. At closing time the game pauses and the day report is shown (see <see cref="DayReportScreen"/>).
    /// </summary>
    public class DayCycle : MonoBehaviour
    {
        public static DayCycle Instance { get; private set; }

        [SerializeField] Light sun;
        [Tooltip("Real seconds in one park day.")]
        [SerializeField] float dayLength = 360f;
        [SerializeField] SurferSpawner spawner;
        [SerializeField] Color morningLight = new(1f, 0.82f, 0.68f);
        [SerializeField] Color noonLight = new(1f, 0.95f, 0.85f);
        [SerializeField] Color eveningLight = new(1f, 0.62f, 0.4f);

        float time;
        int servedAtStart, lostAtStart;
        DayReport today;
        Wallet wallet;

        public int Day { get; private set; } = 1;
        public float Progress => Mathf.Clamp01(time / dayLength);
        public bool Closed { get; private set; }

        /// <summary>Money earned so far today.</summary>
        public int IncomeToday => today != null ? today.TotalIncome : 0;

        /// <summary>Fires at closing time with the day's report.</summary>
        public event Action<DayReport> DayEnded;

        /// <summary>Clock text like "2:30 PM" (8 AM to 8 PM).</summary>
        public string Clock
        {
            get
            {
                float hours = 8f + Progress * 12f;
                int h = Mathf.FloorToInt(hours);
                int m = Mathf.FloorToInt((hours - h) * 60f / 10f) * 10;
                int h12 = h % 12 == 0 ? 12 : h % 12;
                return $"{h12}:{m:00} {(h < 12 ? "AM" : "PM")}";
            }
        }

        void Awake() => Instance = this;

        void Start() => BeginDay();

        void Update()
        {
            if (wallet == null && Wallet.Instance != null)
            {
                wallet = Wallet.Instance;
                wallet.Changed += (amount, reason) => today?.Record(amount, reason);
            }
            if (Closed) return;

            time += Time.deltaTime;
            UpdateSun();
            if (DailyEvents.Instance != null) DailyEvents.Instance.Tick(Progress);
            if (time >= dayLength) EndDay();
        }

        void UpdateSun()
        {
            if (sun == null) return;
            float p = Progress;
            float elevation = Mathf.Lerp(12f, 168f, p);
            sun.transform.rotation = Quaternion.Euler(elevation, -35f, 0f);
            sun.color = p < 0.5f ? Color.Lerp(morningLight, noonLight, p * 2f) : Color.Lerp(noonLight, eveningLight, (p - 0.5f) * 2f);
            sun.intensity = Mathf.Lerp(0.9f, 1.4f, Mathf.Sin(p * Mathf.PI)) * DailyEvents.SunMultiplier;
        }

        void BeginDay()
        {
            Closed = false;
            time = 0f;
            today = new DayReport { day = Day, ratingStart = ParkReputation.Instance != null ? ParkReputation.Instance.Stars : 0f };
            if (spawner != null)
            {
                servedAtStart = spawner.Served;
                lostAtStart = spawner.Lost;
            }
            UpdateSun();
        }

        void EndDay()
        {
            Closed = true;
            today.ratingEnd = ParkReputation.Instance != null ? ParkReputation.Instance.Stars : 0f;
            if (spawner != null)
            {
                today.served = spawner.Served - servedAtStart;
                today.lost = spawner.Lost - lostAtStart;
            }
            if (OperatingCosts.Instance != null) OperatingCosts.Instance.PayDailyBills(today.TotalIncome);
            Sfx.Play(Sound.DayEnd);
            Lifeguard.Whistle();
            DayEnded?.Invoke(today);
        }

        /// <summary>Called from the report screen: start the next morning.</summary>
        public void NextDay()
        {
            Day++;
            BeginDay();
            if (DailyEvents.Instance != null) DailyEvents.Instance.StartDay(Day);
        }

        /// <summary>Restores the day number from a save.</summary>
        public void Restore(int day) => Day = Mathf.Max(1, day);
    }
}
