using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The surfing part of the HUD (Hud.uxml "surf*" elements, styled in Game.uss "Surf HUD"): while you ride, the
    /// score, a flow bar and the combo with its countdown; big call-outs (PERFECT TAKEOFF, CARVE x2, COMBO LOST);
    /// water drops on the lens when spray hits you; and a score card when the ride ends. Listens to PlayerSurfing.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SurfHud : MonoBehaviour
    {
        const float CallSeconds = 1.4f, CardSeconds = 3.2f, DropSeconds = 1.6f;

        static readonly string[] MoodClasses = { "surf-call--info", "surf-call--good", "surf-call--reward", "surf-call--bad" };

        VisualElement panel, flowFill, comboTimer, drops, card;
        Label score, combo, call, cardRating, cardScore, cardDetails, cardJudges, cardPraise;
        PlayerSurfing surfing;
        float callUntil = -1f, callStart, cardUntil = -1f;
        readonly List<(VisualElement drop, float born)> liveDrops = new();

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            panel = root.Q("surfPanel");
            flowFill = root.Q("surfFlowFill");
            comboTimer = root.Q("surfComboTimer");
            drops = root.Q("surfDrops");
            card = root.Q("surfCard");
            score = root.Q<Label>("surfScore");
            combo = root.Q<Label>("surfCombo");
            call = root.Q<Label>("surfCall");
            cardRating = root.Q<Label>("surfCardRating");
            cardScore = root.Q<Label>("surfCardScore");
            cardDetails = root.Q<Label>("surfCardDetails");
            cardJudges = root.Q<Label>("surfCardJudges");
            cardPraise = root.Q<Label>("surfCardPraise");

            surfing = PlayerSurfing.Instance;
            if (surfing == null) return;
            surfing.Called += OnCall;
            surfing.Splashed += OnSplash;
            surfing.RideFinished += OnRideFinished;
            surfing.Judged += OnJudged;
        }

        void OnDestroy()
        {
            if (surfing == null) return;
            surfing.Called -= OnCall;
            surfing.Splashed -= OnSplash;
            surfing.RideFinished -= OnRideFinished;
            surfing.Judged -= OnJudged;
        }

        void Update()
        {
            if (panel == null) return;
            float now = Time.unscaledTime;
            bool riding = surfing != null && surfing.IsRiding;
            panel.EnableInClassList("hidden", !riding);
            if (riding)
            {
                score.text = surfing.Score.ToString("N0");
                flowFill.style.width = Length.Percent(surfing.Flow * 100f);
                flowFill.EnableInClassList("surf-flow-fill--hot", surfing.Flow > 0.75f);
                bool chaining = surfing.ComboCount > 1;
                combo.EnableInClassList("hidden", surfing.ComboCount == 0);
                comboTimer.EnableInClassList("hidden", surfing.ComboCount == 0);
                combo.text = chaining
                    ? $"x{surfing.ComboMultiplier:0.#} COMBO  {surfing.ComboPoints}"
                    : $"COMBO  {surfing.ComboPoints}";
                comboTimer.style.width = Length.Percent(surfing.ComboTimeLeft * 100f);
            }

            call.EnableInClassList("surf-call--shown", now < callUntil);
            call.EnableInClassList("surf-call--pop", now - callStart < 0.08f);
            card.EnableInClassList("surf-card--shown", now < cardUntil);

            // Lens drops fade away and are removed.
            for (int i = liveDrops.Count - 1; i >= 0; i--)
            {
                var (drop, born) = liveDrops[i];
                float age = (now - born) / DropSeconds;
                if (age >= 1f)
                {
                    drop.RemoveFromHierarchy();
                    liveDrops.RemoveAt(i);
                    continue;
                }
                drop.style.opacity = 1f - age * age;
            }
        }

        void OnCall(string text, Notice mood)
        {
            call.text = text;
            foreach (var c in MoodClasses) call.RemoveFromClassList(c);
            call.AddToClassList(MoodClasses[Mathf.Clamp((int)mood, 0, MoodClasses.Length - 1)]);
            callStart = Time.unscaledTime;
            callUntil = callStart + CallSeconds;
        }

        void OnSplash(int count)
        {
            for (int i = 0; i < count && liveDrops.Count < 24; i++)
            {
                var drop = new VisualElement { pickingMode = PickingMode.Ignore };
                drop.AddToClassList("surf-drop");
                float size = Random.Range(16f, 48f);
                drop.style.width = size;
                drop.style.height = size * Random.Range(1f, 1.25f);
                drop.style.left = Length.Percent(Random.Range(4f, 92f));
                drop.style.top = Length.Percent(Random.Range(6f, 80f));
                var shine = new VisualElement { pickingMode = PickingMode.Ignore };
                shine.AddToClassList("surf-drop-shine");
                drop.Add(shine);
                drops.Add(drop);
                liveDrops.Add((drop, Time.unscaledTime));
            }
        }

        void OnJudged(float judges, string praise)
        {
            if (cardJudges == null) return;
            cardJudges.text = $"JUDGES  {judges:0.00}";
            cardJudges.EnableInClassList("surf-card-judges--great", judges >= 7f);
            cardPraise.text = praise;
        }

        void OnRideFinished(int total, string rating, float bestCombo, float topSpeedKmh, int airs)
        {
            cardRating.text = rating;
            cardScore.text = total.ToString("N0");
            cardDetails.text = $"{surfing.LastRideSeconds:0.0} s ride   ·   {surfing.LastRideLine:0} m down the line   ·   Best combo x{bestCombo:0.#}   ·   Top speed {topSpeedKmh:0} km/h";
            cardUntil = Time.unscaledTime + CardSeconds;
        }
    }
}
