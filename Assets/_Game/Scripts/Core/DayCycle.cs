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
    /// evening. At 8 PM the park closes: no new guests, the people still here finish up and go home while the sun
    /// sets, and once everyone is gone you flip the OPEN/CLOSED sign at the ticket desk (<see cref="ParkSign"/>) to
    /// end the day: bills and taxes are paid and the day report shows (<see cref="DayReportScreen"/>).
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
        [SerializeField] Color duskLight = new(0.55f, 0.45f, 0.8f);
        [Tooltip("The low afternoon sun on a Golden Hour day.")]
        [SerializeField] Color goldenLight = new(1f, 0.8f, 0.58f);
        [Tooltip("Real seconds the sun takes to set after closing time.")]
        [SerializeField] float duskLength = 50f;
        [Tooltip("Seconds after closing before surfers still in the water are told to come in right away.")]
        [SerializeField] float lastCall = 35f;

        float time;
        int servedAtStart, lostAtStart;
        DayReport today;
        Wallet wallet;

        public int Day { get; private set; } = 1;
        public float Progress => Mathf.Clamp01(time / dayLength);
        public bool Closed { get; private set; }

        /// <summary>Past closing time: no new guests; waiting for the last ones to leave and for you to end the day.</summary>
        public bool ParkClosed { get; private set; }

        /// <summary>Is the park letting guests in right now?</summary>
        public static bool Open => Instance == null || !Instance.ParkClosed && !Instance.Closed;

        /// <summary>Guests still in the park (surfers, shoppers, beach guests).</summary>
        public static int GuestsLeft => Surfer.All.Count + Shopper.All.Count + BeachGuest.All.Count;

        public bool CanEndDay => ParkClosed && !Closed && GuestsLeft == 0;

        /// <summary>Fires when the park closes for the night (8 PM).</summary>
        public event Action ParkClosing;

        float lastCallAt, nextNudge;
        bool emptyAnnounced;
        float ambientIntensity = -1f;
        Color ambientColor;

        /// <summary>Money earned so far today.</summary>
        public int IncomeToday => today != null ? today.TotalIncome : 0;

        /// <summary>Fires at closing time with the day's report.</summary>
        public event Action<DayReport> DayEnded;

        /// <summary>Clock text like "2:30 PM" (8 AM to 8 PM).</summary>
        public string Clock
        {
            get
            {
                // Keeps going after 8 PM while people leave, but stops at 11:50 PM (it never rolls into "2 PM").
                float hours = Mathf.Min(8f + time / dayLength * 12f, 23f + 50f / 60f);
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
            if (!ParkClosed)
            {
                UpdateSun();
                if (DailyEvents.Instance != null) DailyEvents.Instance.Tick(Progress);
                if (time >= dayLength) CloseGates();
                return;
            }

            UpdateDusk();
            if (Time.time >= lastCallAt && Time.time >= nextNudge)
            {
                // Last call, then every 30 s for anyone still dawdling.
                nextNudge = Time.time + 30f;
                SendHome(true);
            }
            if (!emptyAnnounced && GuestsLeft == 0)
            {
                emptyAnnounced = true;
                Sfx.Play(Sound.Mission);
                Notifications.Post("Everyone's gone home. Flip the sign at the ticket desk to end the day.", Notice.Good);
            }
        }

        /// <summary>8 PM: lock the gates, announce it, and send the people who haven't started surfing home.</summary>
        void CloseGates()
        {
            ParkClosed = true;
            emptyAnnounced = false;
            lastCallAt = Time.time + lastCall;
            nextNudge = 0f;
            Sfx.Play(Sound.PaChime, null, 0.9f);
            Lifeguard.Whistle();
            Notifications.Post("Closing time! No new guests. Let the last surfers finish their wave.", Notice.Info);
            SendHome(false);
            ParkClosing?.Invoke();
        }

        static void SendHome(bool now)
        {
            foreach (var surfer in Surfer.All.ToArray()) surfer.GoHome(now);
            foreach (var shopper in Shopper.All.ToArray()) shopper.GoHome(now);
            foreach (var guest in BeachGuest.All.ToArray()) guest.GoHome();
        }

        /// <summary>0 in daylight, rising to 1 as dusk falls after closing time (park lights come on with it).</summary>
        public float Darkness
        {
            get
            {
                float k = Mathf.Clamp01((time - dayLength * 0.92f) / (dayLength * 0.08f + duskLength));
                return k * k * (3f - 2f * k);
            }
        }

        /// <summary>The sun sinks below the horizon and the world goes dusky blue while the last guests leave.</summary>
        void UpdateDusk()
        {
            if (sun == null) return;
            float k = Mathf.Clamp01((time - dayLength) / duskLength);
            float s = k * k * (3f - 2f * k);
            sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(168f, 182f, s), -35f, 0f);
            sun.color = Color.Lerp(eveningLight, duskLight, s);
            sun.intensity = Mathf.Lerp(0.9f, 0.18f, s) * DailyEvents.SunMultiplier;
            if (ambientIntensity < 0f)
            {
                ambientIntensity = RenderSettings.ambientIntensity;
                ambientColor = RenderSettings.ambientLight;
            }
            RenderSettings.ambientIntensity = Mathf.Lerp(ambientIntensity, ambientIntensity * 0.5f, s);
            RenderSettings.ambientLight = Color.Lerp(ambientColor, ambientColor * new Color(0.55f, 0.55f, 0.8f), s);

            // A dusky sky: a copy of the skybox (never the asset) gets darker and purpler.
            if (daySky == null && RenderSettings.skybox != null)
            {
                daySky = RenderSettings.skybox;
                duskSky = new Material(daySky);
                RenderSettings.skybox = duskSky;
            }
            if (duskSky != null)
            {
                if (duskSky.HasProperty("_Exposure")) duskSky.SetFloat("_Exposure", Mathf.Lerp(daySky.GetFloat("_Exposure"), 0.35f, s));
                if (duskSky.HasProperty("_SkyTint")) duskSky.SetColor("_SkyTint", Color.Lerp(daySky.GetColor("_SkyTint"), new Color(0.35f, 0.3f, 0.7f), s));
                if (Time.time >= nextGiUpdate)
                {
                    nextGiUpdate = Time.time + 0.5f;
                    DynamicGI.UpdateEnvironment();
                }
            }
        }

        Material daySky, duskSky;
        float nextGiUpdate;

        /// <summary>From the sign at the desk: end the day once everyone has left. Returns false if people are still here.</summary>
        public bool EndDayNow()
        {
            if (!CanEndDay) return false;
            EndDay();
            return true;
        }

        void UpdateSun()
        {
            if (sun == null) return;
            float p = Progress;
            float elevation = Mathf.Lerp(12f, 168f, p);
            var color = p < 0.5f ? Color.Lerp(morningLight, noonLight, p * 2f) : Color.Lerp(noonLight, eveningLight, (p - 0.5f) * 2f);
            float intensity = Mathf.Lerp(0.9f, 1.4f, Mathf.Sin(p * Mathf.PI));
            if (DailyEvents.GoldenHour)
            {
                // Golden Hour day: after lunch the sun sinks to about 16 degrees and hangs there, warm, until closing,
                // so the waves are backlit and glow (StylizedWater's golden-hour lip glow) for hours.
                float g = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 0.68f, p));
                elevation = Mathf.Lerp(elevation, Mathf.Max(elevation, 164f), g);
                color = Color.Lerp(color, goldenLight, g);
                intensity = Mathf.Lerp(intensity, Mathf.Lerp(1.05f, 0.9f, Mathf.InverseLerp(0.85f, 1f, p)), g);
            }
            sun.transform.rotation = Quaternion.Euler(elevation, -35f, 0f);
            sun.color = color;
            sun.intensity = intensity * DailyEvents.SunMultiplier;
        }

        void BeginDay()
        {
            Closed = false;
            ParkClosed = false;
            if (ambientIntensity >= 0f)
            {
                RenderSettings.ambientIntensity = ambientIntensity;
                RenderSettings.ambientLight = ambientColor;
            }
            if (daySky != null)
            {
                RenderSettings.skybox = daySky;
                if (duskSky != null) Destroy(duskSky);
                daySky = duskSky = null;
                DynamicGI.UpdateEnvironment();
            }
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
            GameStats.BestDayIncome = Mathf.Max(GameStats.BestDayIncome, today.TotalIncome);
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
            if (DailyChallenges.Instance != null) DailyChallenges.Instance.NewDay();
        }

        /// <summary>Restores the day number from a save.</summary>
        public void Restore(int day)
        {
            Day = Mathf.Max(1, day);
            // The day may already have begun (Start order isn't fixed): today's report must carry the loaded day.
            if (today != null)
            {
                today.day = Day;
                if (ParkReputation.Instance != null) today.ratingStart = ParkReputation.Instance.Stars;
            }
        }
    }
}
