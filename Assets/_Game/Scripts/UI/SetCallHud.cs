using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The set call at the top of the screen, for the pool you're at: "SET INCOMING" while the machine charges, one
    /// pip per wave that fills as each wave leaves, then "LULL" while the water settles (and when the timer sends the
    /// next set). Real lineups live by this rhythm: sets, then a quiet lull. Owned by <see cref="GameHud"/>.
    /// </summary>
    public class SetCallHud
    {
        const float LullShown = 6f, Glassy = 0.15f;

        readonly VisualElement call, pips;
        readonly Label title, note;
        bool shown;
        int pipCount = -1, pipsFired = -1;
        string lastTitle;
        float lullFrom = -1f;
        WaveMachine lastMachine;

        public SetCallHud(VisualElement root)
        {
            call = root.Q("setCall");
            pips = root.Q("setPips");
            title = root.Q<Label>("setTitle");
            note = root.Q<Label>("setNote");
        }

        public void Update(WaveMachine machine)
        {
            if (call == null) return;
            // The set runs from the press until a moment after its last wave left the machine; then the lull.
            // Timer (auto) sets only get the call while you're surfing: otherwise it would never leave the screen.
            var water = machine != null && machine.Pool != null ? machine.Pool.Water : null;
            bool surfing = PlayerSurfing.Instance != null && PlayerSurfing.Instance.IsSurfing;
            if (machine != null && machine.LastSetAuto && !surfing) machine = null;
            bool running = machine != null && (machine.SetRunning || Time.time - machine.LastWaveAt < 2.5f);
            if (machine != lastMachine)
            {
                lastMachine = machine;
                lullFrom = -1f;
            }
            if (running) lullFrom = -1f;
            else if (machine != null && lullFrom < 0f && Time.time - machine.LastWaveAt < 40f) lullFrom = Time.time;
            float chop = water != null ? water.Chop : 0f;
            // The lull shows for a few seconds, and on until the water has settled (glassy again).
            float lullFor = lullFrom > 0f ? Time.time - lullFrom : float.MaxValue;
            bool lull = machine != null && !running && (lullFor < LullShown || (chop >= Glassy && lullFor < 40f));
            // With the timer on, the last seconds before the next set count down.
            float nextAuto = machine != null ? machine.NextAutoIn : -1f;
            bool countdown = machine != null && !running && nextAuto > 0f && nextAuto <= 6f && machine.Ready;
            Show(running || lull || countdown);
            if (!running && !lull && !countdown) return;

            if (running)
            {
                int count = machine.SetCount, fired = machine.SetFired;
                SetTitle(fired == 0 ? (count > 1 ? "SET INCOMING" : "WAVE INCOMING")
                    : count > 1 ? $"WAVE {fired} OF {count}" : "WAVE!");
                title.RemoveFromClassList("set-title--lull");
                SetPips(count > 1 ? count : 0, fired);
                note.text = fired == 0 && count > 1 ? $"{count} waves on the way" : "";
            }
            else
            {
                SetTitle(countdown ? $"NEXT SET IN {Mathf.CeilToInt(nextAuto)}" : "LULL");
                title.AddToClassList("set-title--lull");
                SetPips(0, 0);
                note.text = chop < Glassy ? "Water's glassy" : "Water settling... (choppy water = worse rides)";
            }
            note.EnableInClassList("hidden", string.IsNullOrEmpty(note.text));
        }

        void Show(bool on)
        {
            if (on == shown) return;
            shown = on;
            if (on)
            {
                lastTitle = null;
                UiMotion.Enter(call, "set-call--out");
            }
            else
            {
                call.AddToClassList("set-call--out");
                call.schedule.Execute(() => { if (!shown) call.AddToClassList("hidden"); }).StartingIn(300);
            }
        }

        void SetTitle(string text)
        {
            if (text == lastTitle) return;
            lastTitle = text;
            title.text = text;
            UiMotion.Bump(title, "set-title--pulse", 140);
        }

        void SetPips(int count, int fired)
        {
            if (count != pipCount)
            {
                pipCount = count;
                pipsFired = -1;
                pips.Clear();
                for (int i = 0; i < count; i++)
                {
                    var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                    pip.AddToClassList("set-pip");
                    pips.Add(pip);
                }
            }
            if (fired == pipsFired) return;
            pipsFired = fired;
            for (int i = 0; i < pips.childCount; i++) pips[i].EnableInClassList("set-pip--fired", i < fired);
        }
    }
}
