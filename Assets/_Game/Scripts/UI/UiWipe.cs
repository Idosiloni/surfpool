using System;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// A wave washes across the whole screen (ink water with a foam edge, docs/research/game-ui-craft.md #8) and
    /// hides a big change: the title screen into the park, the day report into the next morning. The change runs
    /// while the screen is covered. With Reduce motion it's a quick fade instead. The element lives in the Settings
    /// document, the topmost one.
    /// </summary>
    public static class UiWipe
    {
        const long CoverMs = 380, HoldMs = 120, LeaveMs = 380;

        static VisualElement wipe;
        static bool busy;

        public static bool Busy => busy;

        public static void Register(VisualElement element)
        {
            wipe = element;
            busy = false;
        }

        /// <summary>Covers the screen, runs <paramref name="covered"/>, then washes off. Ignored while a wipe is running.</summary>
        public static void Play(Action covered)
        {
            if (busy) return;
            if (wipe == null || wipe.panel == null)
            {
                covered();
                return;
            }
            busy = true;
            bool fade = GameSettings.ReduceMotion;
            wipe.EnableInClassList("wipe--fade", fade);
            wipe.AddToClassList("wipe--start");
            wipe.RemoveFromClassList("wipe--end");
            wipe.RemoveFromClassList("hidden");
            wipe.BringToFront();
            Sfx.Play(Sound.SectionWall, null, 0.7f, 1.15f);
            long cover = fade ? 160 : CoverMs, leave = fade ? 160 : LeaveMs;
            wipe.schedule.Execute(() => wipe.RemoveFromClassList("wipe--start")).StartingIn(30);
            wipe.schedule.Execute(() =>
            {
                try { covered(); }
                finally
                {
                    wipe.schedule.Execute(() => wipe.AddToClassList("wipe--end")).StartingIn(HoldMs);
                    wipe.schedule.Execute(() =>
                    {
                        wipe.AddToClassList("hidden");
                        wipe.RemoveFromClassList("wipe--end");
                        busy = false;
                    }).StartingIn(HoldMs + leave + 40);
                }
            }).StartingIn(30 + cover);
        }
    }
}
