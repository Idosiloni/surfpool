using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfPool
{
    [Serializable]
    public class Upgrade
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public int cost;
        [Tooltip("Id of an upgrade that must be bought first. Empty = none.")]
        public string requires;
        [NonSerialized] public bool owned;
    }

    /// <summary>
    /// The computer on the ticket desk. Press E to open the upgrades menu, buy with the mouse, Esc or E to close.
    /// Each upgrade's effect is applied in <see cref="Apply"/> by its id.
    /// </summary>
    public class UpgradeShop : MonoBehaviour, IInteractable
    {
        [SerializeField] List<Upgrade> upgrades = new()
        {
            new Upgrade { id = "more_surfers", title = "Bigger Lineup", cost = 60,
                description = "Two more surfers can be in the park at once. Adds a bench by the pool." },
            new Upgrade { id = "rental_stand", title = "Board Rental Stand", cost = 80,
                description = "Opens a rental counter. Surfers without boards start coming. Hand them a board from the rack ($10), then carry returned boards from the bin back to the rack." },
            new Upgrade { id = "faster_machine", title = "Turbo Pump", cost = 100,
                description = "The wave machine recharges twice as fast (4s -> 2s)." },
            new Upgrade { id = "more_boards", title = "More Rental Boards", cost = 120, requires = "rental_stand",
                description = "Four more boards and slots on the rental rack." },
            new Upgrade { id = "auto_waves", title = "Auto Wave Timer", cost = 150,
                description = "The machine sends a wave by itself every 12 seconds. A yellow light turns on." },
            new Upgrade { id = "wave_sets", title = "Wave Sets", cost = 180,
                description = "Each press sends a set of 2 waves, 2.5 s apart. More rides, less waiting." },
            new Upgrade { id = "pro_waves", title = "Pro Wave Program", cost = 200,
                description = "Unlocks steep, fast Pro waves (select them on the wave console). Pro surfers pay the most and do tricks." },
            new Upgrade { id = "smart_auto", title = "Smart Wave Timer", cost = 220, requires = "auto_waves",
                description = "Auto waves pick the wave type most waiting surfers want." },
        };

        [Header("Things upgrades change")]
        [SerializeField] WaveMachine machine;
        [SerializeField] WaveWater water;
        [SerializeField] SurferSpawner spawner;
        [SerializeField] PriceBoard pricing;
        [SerializeField] GameObject autoWaveLight;
        [SerializeField] GameObject extraBench;
        [SerializeField] GameObject rentalStand;
        [SerializeField] RentalRack rentalRack;

        bool open;
        int openedFrame;
        string message;
        GUIStyle titleStyle, textStyle, buttonStyle;

        public string Prompt => "Use computer (upgrades)";

        public void Interact()
        {
            open = true;
            openedFrame = Time.frameCount;
            message = null;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
        }

        void Close()
        {
            open = false;
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
        }

        void Update()
        {
            if (!open || Time.frameCount == openedFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) Close();
        }

        public bool Buy(string id)
        {
            var upgrade = upgrades.Find(u => u.id == id);
            if (upgrade == null || upgrade.owned) return false;
            if (!RequirementMet(upgrade))
            {
                message = $"Buy {TitleOf(upgrade.requires)} first.";
                return false;
            }
            if (!Wallet.Instance.Spend(upgrade.cost, upgrade.title))
            {
                message = $"Not enough money for {upgrade.title}.";
                return false;
            }
            upgrade.owned = true;
            Apply(upgrade.id);
            message = $"Bought {upgrade.title}!";
            return true;
        }

        bool RequirementMet(Upgrade upgrade) =>
            string.IsNullOrEmpty(upgrade.requires) || upgrades.Exists(u => u.id == upgrade.requires && u.owned);

        string TitleOf(string id) => upgrades.Find(u => u.id == id)?.title ?? id;

        void Apply(string id)
        {
            switch (id)
            {
                case "more_surfers":
                    spawner.MaxSurfers += 2;
                    if (extraBench != null) extraBench.SetActive(true);
                    break;
                case "rental_stand":
                    rentalStand.SetActive(true);
                    spawner.RenterChance = 0.5f;
                    spawner.MaxSurfers += 1;
                    break;
                case "more_boards":
                    rentalRack.AddBoards(4);
                    break;
                case "faster_machine":
                    machine.Cooldown = 2f;
                    break;
                case "auto_waves":
                    machine.AutoInterval = 12f;
                    if (autoWaveLight != null) autoWaveLight.SetActive(true);
                    break;
                case "wave_sets":
                    machine.WavesPerSet = 2;
                    break;
                case "pro_waves":
                    machine.Unlock(WaveLevel.Pro);
                    pricing.FairPrice += 5;
                    break;
                case "smart_auto":
                    machine.SmartAuto = true;
                    break;
            }
        }

        void OnGUI()
        {
            if (!open) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
            }

            float w = Mathf.Min(640, Screen.width - 40), h = Mathf.Min(150 + upgrades.Count * 80, Screen.height - 40);
            var area = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
            GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(area.x + 20, area.y + 15, area.width - 40, area.height - 30));
            GUILayout.Label("Wave Pool Upgrades", titleStyle);
            GUILayout.Label($"Money: ${Wallet.Instance.Money}", textStyle);
            GUILayout.Space(8);

            foreach (var upgrade in upgrades)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label($"<b>{upgrade.title}</b>  —  ${upgrade.cost}", new GUIStyle(textStyle) { richText = true });
                GUILayout.Label(upgrade.description, textStyle);
                GUILayout.EndVertical();
                bool unlocked = RequirementMet(upgrade);
                GUI.enabled = !upgrade.owned && unlocked && Wallet.Instance.Money >= upgrade.cost;
                string label = upgrade.owned ? "Owned" : unlocked ? "Buy" : "Locked";
                if (GUILayout.Button(label, buttonStyle, GUILayout.Width(100), GUILayout.Height(40)))
                    Buy(upgrade.id);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(message)) GUILayout.Label(message, textStyle);
            if (GUILayout.Button("Close (Esc)", buttonStyle, GUILayout.Height(34))) Close();
            GUILayout.EndArea();
        }
    }
}
