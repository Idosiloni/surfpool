using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The in-game HUD (UI Toolkit, Assets/_Game/UI/Hud.uxml + Game.uss): money with +/- popups, park rating stars,
    /// wave machine panel, park stats, crosshair and [E] prompt, toasts, and speech bubbles over surfers.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameHud : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] FirstPersonController player;
        [SerializeField] PriceBoard pricing;
        [SerializeField] SurferSpawner spawner;
        [SerializeField] WaveMachine machine;
        [SerializeField] StaffPayroll payroll;
        [Tooltip("Surfers farther than this (meters) get no speech bubble.")]
        [SerializeField] float bubbleDistance = 45f;
        [Tooltip("Seconds before the controls hint fades away.")]
        [SerializeField] float hintSeconds = 90f;

        class Bubble
        {
            public VisualElement root;
            public Label badge;
            public Label text;
            public VisualElement patience;
            public VisualElement patienceFill;
        }

        VisualElement root, bubbleLayer, popupStack, toasts, crosshair, prompt, waveBarFill, rentalRow, wagesRow;
        Label money, ratingLabel, waveType, waveStatus, waveAuto, ticketValue, rentalValue, servedValue, lostValue,
            wagesValue, promptLabel, clickToPlay, hint;
        StarRating stars;
        readonly Dictionary<Surfer, Bubble> bubbles = new();
        readonly List<Surfer> staleBubbles = new();
        Wallet wallet;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            bubbleLayer = root.Q("bubbleLayer");
            popupStack = root.Q("popupStack");
            toasts = root.Q("toasts");
            crosshair = root.Q("crosshair");
            prompt = root.Q("prompt");
            waveBarFill = root.Q("waveBarFill");
            rentalRow = root.Q("rentalRow");
            wagesRow = root.Q("wagesRow");
            money = root.Q<Label>("moneyLabel");
            ratingLabel = root.Q<Label>("ratingLabel");
            waveType = root.Q<Label>("waveType");
            waveStatus = root.Q<Label>("waveStatus");
            waveAuto = root.Q<Label>("waveAuto");
            ticketValue = root.Q<Label>("ticketValue");
            rentalValue = root.Q<Label>("rentalValue");
            servedValue = root.Q<Label>("servedValue");
            lostValue = root.Q<Label>("lostValue");
            wagesValue = root.Q<Label>("wagesValue");
            promptLabel = root.Q<Label>("promptLabel");
            clickToPlay = root.Q<Label>("clickToPlay");
            hint = root.Q<Label>("hint");

            stars = new StarRating();
            root.Q("ratingStarsSlot").Add(stars);

            Notifications.Posted += ShowToast;
        }

        void OnDisable()
        {
            Notifications.Posted -= ShowToast;
            if (wallet != null) wallet.Changed -= ShowPopup;
            wallet = null;
            bubbles.Clear();
        }

        void Update()
        {
            if (wallet == null && Wallet.Instance != null)
            {
                wallet = Wallet.Instance;
                wallet.Changed += ShowPopup;
            }

            UpdateMoneyAndRating();
            UpdateWavePanel();
            UpdateStats();
            UpdatePrompt();
            UpdateBubbles();
            hint.EnableInClassList("hint--hidden", Time.timeSinceLevelLoad > hintSeconds);
        }

        void UpdateMoneyAndRating()
        {
            if (wallet != null)
            {
                SetText(money, "$ " + wallet.Money);
                money.EnableInClassList("money-label--negative", wallet.Money < 0);
            }
            if (ParkReputation.Instance != null)
            {
                stars.Stars = ParkReputation.Instance.Stars;
                SetText(ratingLabel, ParkReputation.Instance.Stars.ToString("0.0"));
            }
        }

        void UpdateWavePanel()
        {
            if (machine == null) return;
            var selected = machine.Selected;
            string direction = selected == WaveLevel.Beginner ? "Whitewater"
                : machine.SelectedDirection == WaveDirection.Peak ? "Peak" : machine.SelectedDirection.ToString();
            SetText(waveType, $"{selected}  ·  {direction}");
            waveType.EnableInClassList("wave-type--beginner", selected == WaveLevel.Beginner);
            waveType.EnableInClassList("wave-type--intermediate", selected == WaveLevel.Intermediate);
            waveType.EnableInClassList("wave-type--pro", selected == WaveLevel.Pro);

            waveBarFill.style.width = Length.Percent(machine.ReadyProgress * 100f);
            waveBarFill.EnableInClassList("bar-fill--charging", !machine.Ready);
            SetText(waveStatus, machine.Ready
                ? "Ready - press the big button"
                : $"Recharging... {Mathf.CeilToInt(machine.SecondsUntilReady)}s");

            string auto = machine.AutoInterval <= 0f ? "Auto: off"
                : $"Auto: every {machine.AutoInterval:0}s" + (machine.SmartAuto ? " (smart)" : "");
            if (machine.WavesPerSet > 1) auto += $"  ·  sets of {machine.WavesPerSet}";
            SetText(waveAuto, auto);
        }

        void UpdateStats()
        {
            if (pricing != null)
            {
                SetText(ticketValue, "$" + pricing.Price);
                SetText(rentalValue, "$" + pricing.RentalPrice);
            }
            if (spawner != null)
            {
                rentalRow.EnableInClassList("hidden", spawner.RenterChance <= 0f);
                SetText(servedValue, spawner.Served.ToString());
                SetText(lostValue, spawner.Lost.ToString());
                lostValue.EnableInClassList("stat-value--bad", spawner.Lost > 0);
            }
            if (payroll != null)
            {
                int wages = payroll.WagesPerMinute;
                wagesRow.EnableInClassList("hidden", wages <= 0);
                SetText(wagesValue, $"${wages}/min");
            }
        }

        void UpdatePrompt()
        {
            bool menuOpen = InputBlocker.Blocked;
            bool cursorFree = player != null && !player.CursorLocked && !menuOpen;
            var target = interactor != null && !menuOpen ? interactor.Target : null;
            string text = target?.Prompt;

            crosshair.EnableInClassList("hidden", menuOpen || cursorFree);
            crosshair.EnableInClassList("crosshair--active", !string.IsNullOrEmpty(text));
            prompt.EnableInClassList("hidden", string.IsNullOrEmpty(text) || cursorFree);
            if (!string.IsNullOrEmpty(text)) SetText(promptLabel, text);
            clickToPlay.EnableInClassList("hidden", !cursorFree);
        }

        void UpdateBubbles()
        {
            var cam = Camera.main;
            bool menuOpen = InputBlocker.Blocked;

            staleBubbles.Clear();
            foreach (var pair in bubbles)
                if (pair.Key == null || !Surfer.All.Contains(pair.Key)) staleBubbles.Add(pair.Key);
            foreach (var surfer in staleBubbles)
            {
                bubbles[surfer].root.RemoveFromHierarchy();
                bubbles.Remove(surfer);
            }

            foreach (var surfer in Surfer.All)
            {
                if (!bubbles.TryGetValue(surfer, out var bubble))
                {
                    bubble = CreateBubble(surfer.Skill);
                    bubbles.Add(surfer, bubble);
                }

                string status = surfer.Status;
                bool hasText = !string.IsNullOrEmpty(status);
                Vector3 world = surfer.transform.position + Vector3.up * 2.15f;
                Vector3 view = cam != null ? cam.WorldToViewportPoint(world) : Vector3.back;
                bool visible = !menuOpen && cam != null && view.z > 0f && view.z < bubbleDistance
                               && (hasText || surfer.ShowPatience);
                bubble.root.EnableInClassList("hidden", !visible);
                if (!visible) continue;

                Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, world, cam);
                bubble.root.style.left = panelPos.x;
                bubble.root.style.top = panelPos.y;

                bubble.text.EnableInClassList("hidden", !hasText);
                if (hasText) SetText(bubble.text, status);

                bubble.patience.EnableInClassList("hidden", !surfer.ShowPatience);
                float patience = Mathf.Clamp01(surfer.Patience);
                bubble.patienceFill.style.width = Length.Percent(patience * 100f);
                bubble.patienceFill.EnableInClassList("patience-fill--mid", patience < 0.6f && patience >= 0.3f);
                bubble.patienceFill.EnableInClassList("patience-fill--low", patience < 0.3f);
            }
        }

        Bubble CreateBubble(WaveLevel skill)
        {
            var bubble = new Bubble { root = new VisualElement { pickingMode = PickingMode.Ignore } };
            bubble.root.AddToClassList("bubble");

            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList("bubble-box");
            bubble.badge = new Label(skill.ToString().Substring(0, 1)) { pickingMode = PickingMode.Ignore };
            bubble.badge.AddToClassList("skill-badge");
            bubble.badge.AddToClassList("skill-badge--" + skill.ToString().ToLowerInvariant());
            bubble.text = new Label { pickingMode = PickingMode.Ignore };
            bubble.text.AddToClassList("bubble-text");
            box.Add(bubble.badge);
            box.Add(bubble.text);

            var tail = new VisualElement { pickingMode = PickingMode.Ignore };
            tail.AddToClassList("bubble-tail");

            bubble.patience = new VisualElement { pickingMode = PickingMode.Ignore };
            bubble.patience.AddToClassList("patience");
            bubble.patienceFill = new VisualElement { pickingMode = PickingMode.Ignore };
            bubble.patienceFill.AddToClassList("patience-fill");
            bubble.patience.Add(bubble.patienceFill);

            bubble.root.Add(box);
            bubble.root.Add(tail);
            bubble.root.Add(bubble.patience);
            bubbleLayer.Add(bubble.root);
            return bubble;
        }

        void ShowPopup(int amount, string reason)
        {
            var label = new Label((amount >= 0 ? "+$" : "-$") + Mathf.Abs(amount) + (string.IsNullOrEmpty(reason) ? "" : "  " + reason))
            {
                pickingMode = PickingMode.Ignore,
            };
            label.AddToClassList("popup");
            label.style.top = popupStack.childCount * 30; // stack popups that arrive together
            label.EnableInClassList("popup--negative", amount < 0);
            popupStack.Add(label);
            label.schedule.Execute(() => label.AddToClassList("popup--leaving")).StartingIn(40);
            label.schedule.Execute(label.RemoveFromHierarchy).StartingIn(1500);
        }

        void ShowToast(string message, bool bad)
        {
            while (toasts.childCount >= 4) toasts.RemoveAt(0);
            var toast = new Label(message) { pickingMode = PickingMode.Ignore };
            toast.AddToClassList("toast");
            toast.EnableInClassList("toast--bad", bad);
            toasts.Add(toast);
            toast.schedule.Execute(() => toast.AddToClassList("toast--leaving")).StartingIn(3500);
            toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(4100);
        }

        static void SetText(Label label, string text)
        {
            if (label.text != text) label.text = text;
        }
    }
}
