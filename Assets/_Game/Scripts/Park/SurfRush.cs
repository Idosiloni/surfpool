using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Rush hour: some days (from day 3) a surf-camp bus pulls up and 6-9 campers in matching rash guards flood the
    /// desk at once. A pill on the HUD counts the campers served and the time left; when the rush is over the camp
    /// leader tips per camper who got in, double if they were all served in time. "Suffering from success."
    /// </summary>
    public class SurfRush : MonoBehaviour
    {
        [SerializeField] SurferSpawner spawner;
        [SerializeField] Surfer surferPrefab;
        [Tooltip("Chance that a day (from day 3) has a rush.")]
        [Range(0f, 1f)] [SerializeField] float chancePerDay = 0.45f;
        [SerializeField] Vector2Int campers = new(6, 9);
        [Tooltip("Seconds to get every camper paid in for the double tip.")]
        [SerializeField] float timeLimit = 150f;
        [Tooltip("Camp leader's tip per camper who got a session.")]
        [SerializeField] int tipPerCamper = 12;

        static readonly Color[] CampColors =
        {
            new(1f, 0.6f, 0.15f), new(0.95f, 0.25f, 0.45f), new(0.25f, 0.75f, 0.3f), new(0.55f, 0.35f, 0.9f),
        };
        static readonly string[] CampNames = { "Camp Kahuna", "Little Rippers", "Wave Academy", "Sunny Shores Camp" };

        readonly List<Surfer> group = new();
        readonly HashSet<Surfer> paid = new(); // remembered: campers who surfed are gone (destroyed) later
        int plannedDay = -1, total, campIndex;
        float rushAt = -1f, startedAt, nextSpawn;
        int toSpawn;
        bool running;
        Label pill;

        public static bool Active { get; private set; }

        void Start()
        {
            var hud = FindAnyObjectByType<GameHud>();
            if (hud != null) pill = hud.GetComponent<UIDocument>().rootVisualElement.Q<Label>("rushPill");
        }

        void OnDisable() => Active = false;

        void Update()
        {
            var day = DayCycle.Instance;
            if (day == null) return;
            if (plannedDay != day.Day)
            {
                plannedDay = day.Day;
                rushAt = day.Day >= 3 && Random.value < chancePerDay ? Random.Range(0.25f, 0.6f) : -1f;
            }
            if (!running && rushAt > 0f && DayCycle.Open && day.Progress >= rushAt)
            {
                rushAt = -1f;
                Begin();
            }
            if (running) Tick();
        }

        /// <summary>Starts a rush right now (also for testing).</summary>
        public void Begin()
        {
            if (running) return;
            running = true;
            Active = true;
            group.Clear();
            paid.Clear();
            campIndex = Random.Range(0, CampNames.Length);
            total = toSpawn = Random.Range(campers.x, campers.y + 1);
            startedAt = Time.time;
            nextSpawn = Time.time + 1.5f;
            Sfx.Play(Sound.PaChime, null, 0.9f);
            Notifications.Post($"RUSH! The {CampNames[campIndex]} bus just pulled up: {total} campers incoming!", Notice.Reward);
        }

        void Tick()
        {
            if (toSpawn > 0 && Time.time >= nextSpawn)
            {
                toSpawn--;
                nextSpawn = Time.time + Random.Range(0.6f, 1.1f);
                var surfer = Instantiate(surferPrefab, spawner.Entrance, Quaternion.identity);
                surfer.Init(spawner, false);
                surfer.MakeCamper(CampColors[campIndex % CampColors.Length]);
                group.Add(surfer);
            }

            int gone = 0;
            foreach (var camper in group)
            {
                if (paid.Contains(camper)) continue;
                if (camper != null && camper.HasPaid) paid.Add(camper);
                else if (camper == null || camper.LeftAngry) gone++;
            }
            int served = paid.Count;
            float left = timeLimit - (Time.time - startedAt);
            if (pill != null)
            {
                pill.EnableInClassList("hidden", false);
                pill.EnableInClassList("rush-pill--late", left <= 0f);
                pill.text = left > 0f
                    ? $"RUSH  {served}/{total} served  ·  {Mathf.CeilToInt(left) / 60}:{Mathf.CeilToInt(left) % 60:00}"
                    : $"RUSH  {served}/{total} served  ·  overtime";
            }
            if (toSpawn == 0 && served + gone >= total) Finish(served, left > 0f && gone == 0);
        }

        void Finish(int served, bool perfect)
        {
            running = false;
            Active = false;
            if (pill != null) pill.EnableInClassList("hidden", true);
            int tip = served * tipPerCamper * (perfect ? 2 : 1);
            if (tip > 0) Wallet.Instance.Add(tip, "surf camp");
            if (perfect)
            {
                Sfx.Play(Sound.Mission);
                Notifications.Post($"You survived the rush! All {total} campers served. The camp leader tips {MoneyText.Format(tip)}.", Notice.Reward);
            }
            else
            {
                Notifications.Post(served > 0
                    ? $"Rush over: {served}/{total} campers surfed. Camp leader's tip: {MoneyText.Format(tip)}."
                    : "Rush over... no camper got in. The bus left grumpy.", served > 0 ? Notice.Good : Notice.Bad);
            }
            ParkReviews.AddFact(perfect ? $"Survived the rush: {served}/{total} campers" : $"Surf camp rush: {served}/{total} campers");
        }
    }
}
