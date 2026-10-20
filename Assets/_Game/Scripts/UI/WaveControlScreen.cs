using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The Wave Control panel (UI Toolkit, Assets/_Game/UI/WaveControl.uxml + Game.uss), shown while
    /// <see cref="WaveControlPanel.OpenPanel"/> is set (any pool's console): a card per wave type with how many surfers want it, a Left / Peak / Right
    /// switch, and a SEND WAVE button.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WaveControlScreen : MonoBehaviour
    {
        [Tooltip("Seconds between refreshes of the cards while open (waiting counts change as surfers move).")]
        [SerializeField] float refreshInterval = 0.25f;

        static readonly string[] Descriptions =
        {
            "Small rolling whitewater. Easy to ride. For beginners.",
            "2 m peeling wave with a clean face. For intermediate surfers.",
            "2.8 m steep, fast, barreling wave. For pros.",
        };

        static readonly string[] Requirements =
        {
            "",
            "Buy the Intermediate Wave Program on the upgrade computer.",
            "Buy the Pro Wave Program on the upgrade computer.",
        };

        VisualElement root, cards, directionSection, directionRow, famousRow;
        Label directionHint, famousHint;
        Button send;
        bool shown;
        float nextRefresh;

        WaveControlPanel panel;

        WaveMachine Machine => panel.Machine;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("waveRoot");
            cards = doc.Q("waveCards");
            directionSection = doc.Q("directionSection");
            directionRow = doc.Q("directionRow");
            directionHint = doc.Q<Label>("directionHint");
            famousRow = doc.Q("famousRow");
            famousHint = doc.Q<Label>("famousHint");
            send = doc.Q<Button>("sendButton");
            send.clicked += () => panel?.Send();
            doc.Q<Button>("waveCloseButton").clicked += () => panel?.Close();
        }

        void Update()
        {
            bool open = WaveControlPanel.OpenPanel != null;
            if (open) panel = WaveControlPanel.OpenPanel;
            if (open != shown)
            {
                shown = open;
                root.EnableInClassList("hidden", !shown);
                nextRefresh = 0f;
            }
            if (!shown || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshInterval;
            Refresh();
        }

        void Refresh()
        {
            var machine = Machine;
            BuildWaveCards(machine);
            BuildDirections(machine);
            BuildFamous(machine);

            bool ready = machine.Ready;
            string waveName = machine.Preset != null ? machine.Preset.name.ToUpperInvariant() : machine.Selected.ToString().ToUpperInvariant() + " WAVE";
            send.text = ready ? $"SEND {waveName}"
                : $"Recharging... {Mathf.CeilToInt(machine.SecondsUntilReady)}s";
            send.EnableInClassList("send-button--charging", !ready);
        }

        void BuildWaveCards(WaveMachine machine)
        {
            cards.Clear();
            var levels = (WaveLevel[])Enum.GetValues(typeof(WaveLevel));
            for (int i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                bool unlocked = machine.IsUnlocked(level);

                var card = new VisualElement();
                card.AddToClassList("wave-card");
                card.EnableInClassList("wave-card--last", i == levels.Length - 1);
                card.EnableInClassList("wave-card--selected", machine.Selected == level);
                card.EnableInClassList("wave-card--locked", !unlocked);
                if (unlocked) card.AddManipulator(new Clickable(() => { machine.Select(level); Refresh(); }));

                var title = new Label(level.ToString());
                title.AddToClassList("wave-type");
                title.AddToClassList("wave-type--" + level.ToString().ToLowerInvariant());
                card.Add(title);

                var text = new Label(Descriptions[(int)level]);
                text.AddToClassList("wave-card-text");
                card.Add(text);

                if (unlocked)
                {
                    int waiting = WaitingFor(machine, level, null);
                    var count = new Label(waiting == 1 ? "1 surfer waiting" : $"{waiting} surfers waiting");
                    count.AddToClassList("wave-card-waiting");
                    card.Add(count);
                }
                else
                {
                    var locked = new Label("Locked. " + Requirements[(int)level]);
                    locked.AddToClassList("wave-card-locked");
                    card.Add(locked);
                }
                cards.Add(card);
            }
        }

        void BuildDirections(WaveMachine machine)
        {
            bool whitewater = machine.Selected == WaveLevel.Beginner;
            directionSection.EnableInClassList("hidden", whitewater);
            if (whitewater) return;

            directionRow.Clear();
            foreach (WaveDirection direction in Enum.GetValues(typeof(WaveDirection)))
            {
                string label = direction == WaveDirection.Peak ? "Peak (both ways)" : direction + "s";
                if (direction != WaveDirection.Peak)
                {
                    int fans = WaitingFor(machine, machine.Selected, direction);
                    if (fans > 0) label += $"  ({fans})";
                }
                var button = new Button(() => { machine.Select(direction); Refresh(); }) { text = label };
                button.AddToClassList("segment");
                button.EnableInClassList("segment--selected", machine.SelectedDirection == direction);
                button.SetEnabled(machine.IsUnlocked(direction));
                directionRow.Add(button);
            }
            directionHint.text = machine.IsUnlocked(WaveDirection.Left)
                ? "Regular-foot surfers love rights, goofy-foot surfers love lefts. A peak goes both ways but rides are shorter."
                : "Lefts and Rights are locked. Buy Reef Shaper on the upgrade computer.";
        }

        void BuildFamous(WaveMachine machine)
        {
            famousRow.Clear();
            if (!ParkEffects.FamousWavesUnlocked)
            {
                famousHint.text = "Locked. Buy the Famous Waves Pack on the upgrade computer (park level 12).";
                return;
            }
            foreach (var wave in FamousWaves.All)
            {
                var famous = wave;
                var button = new Button(() => { machine.SelectPreset(famous); Refresh(); }) { text = $"{wave.name}" };
                button.AddToClassList("segment");
                button.EnableInClassList("segment--selected", machine.Preset == wave);
                button.SetEnabled(FamousWaves.Available(wave, machine));
                famousRow.Add(button);
            }
            famousHint.text = machine.Preset != null
                ? $"{machine.Preset.name}, {machine.Preset.place} ({machine.Preset.level}): {machine.Preset.description}"
                : "Pick a famous wave. Surfers of the right level love them and tip 50% more.";
        }

        /// <summary>Surfers waiting in the water for this wave type (and, if given, preferring this direction).</summary>
        static int WaitingFor(WaveMachine machine, WaveLevel level, WaveDirection? direction)
        {
            int count = 0;
            foreach (var surfer in Surfer.All)
            {
                if (!surfer.WaitingForWave || surfer.Skill != level || surfer.Pool == null || surfer.Pool.Machine != machine) continue;
                if (direction.HasValue && surfer.PreferredDirection != direction.Value) continue;
                count++;
            }
            return count;
        }
    }
}
