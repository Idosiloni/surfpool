using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The end of the fishing pier (an upgrade). E casts the line; when the bobber dips, press E fast to hook the
    /// fish; then HOLD E to reel in, letting go when the line tension gets near the red so it doesn't snap.
    /// Catches sell for cash, worth 50% more when the snack bar is open (fresh fish tacos). Q stops fishing.
    /// </summary>
    public class FishingSpot : MonoBehaviour, IInteractable
    {
        class Fish
        {
            public string name;
            public int value, weight;
            public float pull;
        }

        static readonly Fish[] Fishes =
        {
            new() { name = "sardine", value = 8, weight = 45, pull = 0.35f },
            new() { name = "red snapper", value = 20, weight = 30, pull = 0.5f },
            new() { name = "mahi-mahi", value = 45, weight = 15, pull = 0.7f },
            new() { name = "yellowfin tuna", value = 100, weight = 8, pull = 0.95f },
            new() { name = "GOLDEN MARLIN", value = 400, weight = 2, pull = 1.25f },
        };

        enum Phase { Off, Waiting, Bite, Reeling }

        [SerializeField] Transform bobber;
        [SerializeField] UIDocument ui;
        [Tooltip("Seconds to react when a fish bites.")]
        [SerializeField] float biteWindow = 0.9f;

        Phase phase;
        Fish hooked;
        float timer, tension, progress, lastHeld, runUntil;
        Vector3 bobberHome;
        VisualElement root, tensionFill, reelFill, bars;
        Label status, hint;

        public static bool Fishing { get; private set; }
        public string Prompt => "Go fishing";

        void Awake()
        {
            bobberHome = bobber.position;
            bobber.gameObject.SetActive(false);
        }

        /// <summary>Finds the UI elements (done on first use: the UI document builds itself when the pier appears).</summary>
        void EnsureUi()
        {
            if (root != null) return;
            var doc = ui.rootVisualElement;
            root = doc.Q("fishRoot");
            tensionFill = doc.Q("tensionFill");
            reelFill = doc.Q("reelFill");
            bars = doc.Q("fishBars");
            status = doc.Q<Label>("fishStatus");
            hint = doc.Q<Label>("fishHint");
        }

        void OnDisable()
        {
            if (Fishing) Stop();
            root = null;
        }

        public void Interact()
        {
            if (Fishing) return;
            EnsureUi();
            Fishing = true;
            InputBlocker.Block();
            root.RemoveFromClassList("hidden");
            Cast();
        }

        void Stop()
        {
            Fishing = false;
            phase = Phase.Off;
            InputBlocker.Release();
            root.AddToClassList("hidden");
            bobber.gameObject.SetActive(false);
        }

        void Cast()
        {
            phase = Phase.Waiting;
            timer = Time.time + Random.Range(2f, 7f);
            bobber.gameObject.SetActive(true);
            bars.AddToClassList("hidden");
            Show("Waiting for a bite...", "Watch the bobber. Press E the moment it dips!   Q stop fishing");
            Sfx.Play(Sound.Splash, bobberHome, 0.4f);
        }

        void Update()
        {
            if (!Fishing) return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            bool pressed = keyboard.eKey.wasPressedThisFrame;
            bool held = keyboard.eKey.isPressed;
            if (keyboard.qKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
            {
                Stop();
                return;
            }

            float bob = Mathf.Sin(Time.time * 3f) * 0.04f;
            switch (phase)
            {
                case Phase.Waiting:
                    bobber.position = bobberHome + Vector3.up * bob;
                    if (pressed)
                    {
                        Show("Too early! The fish swam off.", "Press E to cast again.   Q stop fishing");
                        phase = Phase.Off;
                    }
                    else if (Time.time >= timer)
                    {
                        phase = Phase.Bite;
                        timer = Time.time + biteWindow;
                        hooked = Roll();
                        Show("!!! BITE !!!", "Press E NOW!");
                        Sfx.Play(Sound.Splash, bobberHome, 0.8f);
                    }
                    break;
                case Phase.Bite:
                    bobber.position = bobberHome + Vector3.down * 0.25f + Vector3.up * bob;
                    if (pressed)
                    {
                        phase = Phase.Reeling;
                        tension = 0.2f;
                        progress = 0f;
                        lastHeld = Time.time;
                        bars.RemoveFromClassList("hidden");
                        Show("Hooked something! Reel it in!", "HOLD E to reel. Let go when the line gets tight (red), or it snaps!");
                    }
                    else if (Time.time >= timer)
                    {
                        Show("Missed it! Too slow.", "Press E to cast again.   Q stop fishing");
                        phase = Phase.Off;
                    }
                    break;
                case Phase.Reeling:
                    Reel(held);
                    break;
                case Phase.Off:
                    bobber.position = bobberHome + Vector3.up * bob;
                    if (pressed) Cast();
                    break;
            }
        }

        void Reel(bool held)
        {
            // Now and then the fish makes a run: the line gets tight fast even if you let go a little.
            if (Time.time > runUntil && Random.value < 0.6f * Time.deltaTime) runUntil = Time.time + Random.Range(0.6f, 1.4f);
            bool running = Time.time < runUntil;
            float pull = hooked.pull * ParkEffects.FishingTensionMultiplier;
            if (held)
            {
                lastHeld = Time.time;
                progress += 0.22f * Time.deltaTime;
                tension += pull * (running ? 1.6f : 0.75f) * Time.deltaTime;
            }
            else
            {
                progress = Mathf.Max(0f, progress - 0.05f * Time.deltaTime);
                tension -= (running ? 0.15f : 0.7f) * Time.deltaTime;
            }
            tension = Mathf.Clamp01(tension);
            bobber.position = Vector3.Lerp(bobberHome, transform.position + Vector3.down * 0.6f, progress) + Vector3.up * Mathf.Sin(Time.time * 12f) * 0.05f;
            tensionFill.style.width = Length.Percent(tension * 100f);
            tensionFill.EnableInClassList("fish-tension--danger", tension > 0.75f);
            reelFill.style.width = Length.Percent(progress * 100f);
            if (running) status.text = "It's making a run! Careful!";

            if (tension >= 1f)
            {
                Show($"SNAP! The line broke and the {hooked.name} got away.", "Press E to cast again.   Q stop fishing");
                Sfx.Play(Sound.Error);
                phase = Phase.Off;
                bars.AddToClassList("hidden");
            }
            else if (Time.time - lastHeld > 3f)
            {
                Show($"The {hooked.name} slipped off the hook.", "Press E to cast again.   Q stop fishing");
                phase = Phase.Off;
                bars.AddToClassList("hidden");
            }
            else if (progress >= 1f) Land();
        }

        void Land()
        {
            bool tacos = SnackBar.Instance != null;
            int value = Mathf.RoundToInt(hooked.value * (tacos ? 1.5f : 1f));
            Wallet.Instance.Add(value, "fishing");
            GameStats.FishCaught++;
            ParkLevel.Add(hooked.value >= 100 ? 15 : 5);
            Sfx.Play(hooked.value >= 100 ? Sound.Mission : Sound.Buy);
            Show($"Caught a {hooked.name}! +{MoneyText.Format(value)}" + (tacos ? " (fresh fish tacos!)" : ""), "Press E to cast again.   Q stop fishing");
            phase = Phase.Off;
            bars.AddToClassList("hidden");
        }

        static Fish Roll()
        {
            float rareBoost = ParkEffects.FishingRareMultiplier;
            float total = 0f;
            foreach (var fish in Fishes) total += fish.weight * (fish.value >= 100 ? rareBoost : 1f);
            float roll = Random.value * total;
            foreach (var fish in Fishes)
            {
                roll -= fish.weight * (fish.value >= 100 ? rareBoost : 1f);
                if (roll <= 0f) return fish;
            }
            return Fishes[0];
        }

        void Show(string text, string help)
        {
            status.text = text;
            hint.text = help;
        }
    }
}
