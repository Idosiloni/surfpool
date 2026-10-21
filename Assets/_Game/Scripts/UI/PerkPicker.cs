using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The level-up perk card (Hud.uxml "perk*", styled in Game.uss "Perk picker"): when a pick is earned
    /// (ParkPerks.PicksAvailable) and you're free (no menu, not surfing, fishing or building, a moment after the
    /// level-up banner), it offers 3 perks; click one or press 1 / 2 / 3. Several waiting picks come one at a time.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PerkPicker : MonoBehaviour
    {
        [Tooltip("Seconds after a level-up (or loading) before the card may appear, so the banner plays first.")]
        [SerializeField] float delayAfterLevelUp = 4.5f;

        VisualElement overlay, cardRow;
        Label title, more;
        readonly List<ParkPerks.Perk> offer = new();
        bool open;
        float readyAt, nextCheck;
        ParkLevel subscribed;

        public static bool Open { get; private set; }

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            overlay = root.Q("perkPicker");
            cardRow = root.Q("perkCards");
            title = root.Q<Label>("perkTitle");
            more = root.Q<Label>("perkMore");
            readyAt = Time.unscaledTime + delayAfterLevelUp;
        }

        void OnDestroy()
        {
            if (subscribed != null) subscribed.LeveledUp -= OnLeveledUp;
            if (open) Close();
        }

        void OnLeveledUp(int level) => readyAt = Mathf.Max(readyAt, Time.unscaledTime + delayAfterLevelUp);

        void Update()
        {
            if (overlay == null) return;
            if (subscribed == null && ParkLevel.Instance != null)
            {
                subscribed = ParkLevel.Instance;
                subscribed.LeveledUp += OnLeveledUp;
            }

            if (open)
            {
                var keyboard = Keyboard.current;
                if (keyboard == null) return;
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) Pick(0);
                else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) Pick(1);
                else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) Pick(2);
                return;
            }

            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 0.5f;
            if (Time.unscaledTime >= readyAt && ParkPerks.PicksAvailable > 0 && Free()) Show();
        }

        /// <summary>Nothing else on screen and nothing in your hands.</summary>
        static bool Free() =>
            !InputBlocker.Blocked && Time.timeScale > 0.99f && !MainMenu.Showing && !BuildMode.Active && !FishingSpot.Fishing
            && (PlayerSurfing.Instance == null || !PlayerSurfing.Instance.IsSurfing);

        void Show()
        {
            offer.Clear();
            offer.AddRange(ParkPerks.Offer(3));
            if (offer.Count == 0) return;

            int level = ParkLevel.Instance != null ? ParkLevel.Instance.Level : 1;
            int earnedAt = (ParkPerks.Chosen.Count + 1) * ParkPerks.LevelsPerPick;
            title.text = earnedAt < level ? $"Park level {earnedAt} perk" : $"Park level {level}!";
            int waiting = ParkPerks.PicksAvailable - 1;
            more.text = waiting > 0 ? $"{waiting} more pick{(waiting > 1 ? "s" : "")} waiting after this one" : "Perks are small, permanent, and they stack.";

            cardRow.Clear();
            for (int i = 0; i < offer.Count; i++)
            {
                int index = i;
                var perk = offer[i];
                var card = new Button(() => Pick(index));
                card.AddToClassList("perk-card");
                card.AddToClassList($"perk-card--{i}");
                var tile = new VisualElement { pickingMode = PickingMode.Ignore };
                tile.AddToClassList("perk-tile");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("icon");
                icon.AddToClassList("perk-icon");
                icon.AddToClassList("icon--" + perk.icon);
                tile.Add(icon);
                card.Add(tile);
                card.Add(Text(perk.title, "perk-name"));
                card.Add(Text(perk.text, "perk-text"));
                card.Add(Text((i + 1).ToString(), "perk-key"));
                cardRow.Add(card);
            }

            open = Open = true;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
            UiMotion.Show(overlay);
            Sfx.Play(Sound.Mission);
        }

        static Label Text(string text, string cls)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(cls);
            return label;
        }

        void Pick(int index)
        {
            if (!open || index < 0 || index >= offer.Count) return;
            var perk = offer[index];
            ParkPerks.Choose(perk.id);
            Sfx.Play(Sound.Buy);
            Notifications.Post($"New perk: {perk.title}. {perk.text}", Notice.Reward);
            Close();
            readyAt = Time.unscaledTime + 1.2f; // the next waiting pick, after a beat
        }

        void Close()
        {
            open = Open = false;
            UiMotion.Hide(overlay);
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
        }
    }
}
