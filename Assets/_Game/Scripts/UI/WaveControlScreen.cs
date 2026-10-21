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
            "Small rolling whitewater for beginners. Beginners also ride the whitewater of bigger waves from the inside.",
            "2 m peeling wave with a clean face. For intermediate surfers.",
            "2.8 m steep, fast, barreling wave. For pros.",
        };

        static readonly string[] Requirements =
        {
            "",
            "Buy the Intermediate Wave Program on the upgrade computer.",
            "Buy the Pro Wave Program on the upgrade computer.",
        };

        VisualElement root, cards, directionSection, directionRow, famousRow, shapeSection, shapeRow, setSection, setRow;
        Label directionHint, famousHint, shapeHint, setHint;
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
            shapeSection = doc.Q("shapeSection");
            shapeRow = doc.Q("shapeRow");
            shapeHint = doc.Q<Label>("shapeHint");
            setSection = doc.Q("setSection");
            setRow = doc.Q("setRow");
            setHint = doc.Q<Label>("setHint");
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
                if (shown) UiMotion.Show(root);
                else root.AddToClassList("hidden");
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
            BuildShapes(machine);
            BuildSets(machine);
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
            if (machine.HasFixedDirection)
            {
                // A Cove: the pier makes rights on one side and lefts on the other, every time, both at once.
                var fixedLabel = new Label($"{machine.SelectedDirection}s on this side, {(machine.SelectedDirection == WaveDirection.Right ? "lefts" : "rights")} on the far side");
                fixedLabel.AddToClassList("segment");
                fixedLabel.AddToClassList("segment--selected");
                directionRow.Add(fixedLabel);
                directionHint.text = "A Cove's pier can't make peaks: every set is a right on one side and a left on the other.";
                return;
            }
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
                button.EnableInClassList("segment--selected", !machine.Alternate && machine.SelectedDirection == direction);
                button.SetEnabled(machine.IsUnlocked(direction));
                directionRow.Add(button);
            }
            // Lefts and rights in turn: everyone gets their favorite every other wave.
            var alternate = new Button(() => { machine.SetAlternate(true); Refresh(); }) { text = "Alternate L / R" };
            alternate.AddToClassList("segment");
            alternate.EnableInClassList("segment--selected", machine.Alternate);
            alternate.SetEnabled(machine.IsUnlocked(WaveDirection.Left));
            directionRow.Add(alternate);
            directionHint.text = machine.IsUnlocked(WaveDirection.Left)
                ? "Regular-foot surfers love rights, goofy-foot surfers love lefts. A peak goes both ways but rides are shorter."
                : "Lefts and Rights are locked. Buy Reef Shaper on the upgrade computer.";
        }

        static readonly string[] ShapeText =
        {
            "Mellow: soft and forgiving, fewer wipeouts. Learners love it, pros find it boring.",
            "Standard: the classic wave.",
            "Hollow: steep and throwing, barrels even on green waves. Pros love it; everyone else wipes out more.",
        };

        void BuildShapes(WaveMachine machine)
        {
            bool whitewater = machine.Selected == WaveLevel.Beginner || machine.Preset != null;
            shapeSection.EnableInClassList("hidden", whitewater);
            if (whitewater) return;
            shapeRow.Clear();
            foreach (WaveShape shape in Enum.GetValues(typeof(WaveShape)))
            {
                var button = new Button(() => { machine.Select(shape); Refresh(); }) { text = shape.ToString() };
                button.AddToClassList("segment");
                button.EnableInClassList("segment--selected", machine.SelectedShape == shape);
                button.SetEnabled(machine.IsUnlocked(shape));
                shapeRow.Add(button);
            }
            shapeHint.text = machine.IsUnlocked(WaveShape.Hollow)
                ? ShapeText[(int)machine.SelectedShape]
                : ShapeText[(int)machine.SelectedShape] + " (Hollow needs Reef Shaper.)";
        }

        void BuildSets(WaveMachine machine)
        {
            int max = machine.WavesPerSet;
            setSection.EnableInClassList("hidden", max <= 1);
            if (max <= 1) return;
            setRow.Clear();
            for (int n = 1; n <= max; n++)
            {
                int size = n;
                var button = new Button(() => { machine.ChooseSetSize(size); Refresh(); }) { text = size == 1 ? "Single wave" : $"Set of {size}" };
                button.AddToClassList("segment");
                button.EnableInClassList("segment--selected", machine.SetSize == size);
                setRow.Add(button);
            }
            setHint.text = $"Each wave costs power. A set feeds more surfers at once (one press = {machine.SetSize} wave{(machine.SetSize == 1 ? "" : "s")}, {MoneyText.Format(machine.PressCost)}).";
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
