using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The "Name your park" card (Hud.uxml "name*", Game.uss "Name your park"): type a name or click one of the
    /// silly ideas, see it painted on a sign preview, then "Paint the sign!". Shown after Uncle Kai's letter in a
    /// new game, once for older saves that never picked a name, and again when you interact with the gate sign
    /// (renaming costs a fee).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ParkNamer : MonoBehaviour
    {
        [Tooltip("What repainting the sign costs after the first (free) name.")]
        [SerializeField] int renameCost = 100;

        VisualElement overlay, ideas;
        TextField field;
        Label preview, intro, cost, heading;
        Button ok, cancel, reroll;
        bool open, firstTime, askedOldSave;
        int openedFrame;
        Action onDone;

        public static ParkNamer Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;
        public int RenameCost => renameCost;

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            overlay = root.Q("nameRoot");
            if (overlay == null) return;
            ideas = root.Q("nameIdeas");
            field = root.Q<TextField>("nameField");
            preview = root.Q<Label>("namePreview");
            intro = root.Q<Label>("nameIntro");
            cost = root.Q<Label>("nameCost");
            heading = root.Q<Label>("nameHeading");
            ok = root.Q<Button>("nameOk");
            cancel = root.Q<Button>("nameCancel");
            reroll = root.Q<Button>("nameReroll");
            field.maxLength = ParkName.MaxLength;
            field.RegisterValueChangedCallback(_ => Refresh());
            ok.clicked += Confirm;
            cancel.clicked += Cancel;
            reroll.clicked += () =>
            {
                Sfx.Play(Sound.Click);
                FillIdeas();
            };
        }

        void Update()
        {
            if (overlay == null) return;
            if (!open)
            {
                // Saves from before park names: ask once, when nothing else is going on.
                if (!askedOldSave && !ParkName.Named && MissionManager.Instance != null && MissionManager.Instance.Started
                    && !MainMenu.Showing && !InputBlocker.Blocked && Time.timeSinceLevelLoad > 4f)
                {
                    askedOldSave = true;
                    Open(true, null);
                }
                return;
            }
            if (UnityEngine.Cursor.lockState == CursorLockMode.Locked) FirstPersonController.LockCursor(false);
            var keyboard = Keyboard.current;
            if (keyboard == null || Time.frameCount == openedFrame) return;
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Confirm();
            else if (keyboard.escapeKey.wasPressedThisFrame && !firstTime) Cancel();
        }

        /// <summary>Opens the card. First time: free, and "keep" keeps Uncle Kai's name. Later: repainting costs a fee.</summary>
        public void Open(bool first, Action done = null)
        {
            if (open || overlay == null)
            {
                done?.Invoke();
                return;
            }
            open = true;
            firstTime = first;
            askedOldSave = true;
            onDone = done;
            openedFrame = Time.frameCount;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
            heading.text = first ? "Name your park" : "Repaint the sign";
            intro.text = first
                ? "Uncle Kai's sign is peeling anyway. What should the new one say? It goes on the gate, the welcome arch and everything after."
                : "Fresh paint, new name. The sign painter is ready.";
            cancel.text = first ? "Keep Kai's name" : "Never mind";
            cost.text = first ? "Free: the paint's on Uncle Kai." : $"Repainting costs {MoneyText.Format(renameCost)}.";
            field.SetValueWithoutNotify(first && !ParkName.Named ? ParkName.Suggest() : ParkName.Name);
            FillIdeas();
            Refresh();
            UiMotion.Show(overlay);
            field.schedule.Execute(() =>
            {
                field.Focus();
                field.SelectAll();
            });
            Sfx.Play(Sound.Click);
        }

        void FillIdeas()
        {
            ideas.Clear();
            for (int i = 0; i < 3; i++)
            {
                string idea = ParkName.Suggest();
                var chip = new Button(() =>
                {
                    field.value = idea;
                    Sfx.Play(Sound.Click);
                }) { text = idea };
                chip.AddToClassList("name-idea");
                ideas.Add(chip);
            }
        }

        void Refresh()
        {
            string clean = ParkName.Clean(field.value);
            preview.text = clean.Length > 0 ? clean.ToUpperInvariant() : "...";
            ok.SetEnabled(clean.Length > 0);
        }

        void Confirm()
        {
            if (!open) return;
            string name = ParkName.Clean(field.value);
            if (name.Length == 0) return;
            if (!firstTime && name != ParkName.Name)
            {
                if (Wallet.Instance == null || !Wallet.Instance.Spend(renameCost, "sign paint"))
                {
                    Sfx.Play(Sound.Error);
                    Notifications.Post($"Repainting costs {MoneyText.Format(renameCost)}.", Notice.Bad);
                    return;
                }
            }
            bool changed = name != ParkName.Name || !ParkName.Named;
            ParkName.Set(name);
            Close();
            if (changed)
            {
                Sfx.Play(Sound.Mission);
                Notifications.Post($"Welcome to {ParkName.Name}!", Notice.Reward);
            }
        }

        void Cancel()
        {
            if (!open) return;
            if (firstTime) ParkName.Set(ParkName.Default); // remembered, so we don't ask again
            Close();
        }

        void Close()
        {
            open = false;
            field.Blur();
            overlay.AddToClassList("hidden");
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
            var done = onDone;
            onDone = null;
            done?.Invoke();
        }
    }
}
