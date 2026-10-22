using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Small UI animations shared by the screens. USS transitions only play when a style changes on an element that is
    /// already showing, so "pop in" means: show it with an "enter" class for one frame, then remove the class and let
    /// Game.uss animate it back to normal.
    /// </summary>
    public static class UiMotion
    {
        const string OverlayEnter = "screen-overlay--enter", OverlayLeave = "screen-overlay--leave";

        /// <summary>Shows a full-screen window (.screen-overlay) with a fade and a little pop. Does nothing if it's already up.</summary>
        public static void Show(VisualElement overlay)
        {
            if (overlay == null) return;
            if (overlay.ClassListContains(OverlayLeave))
            {
                overlay.RemoveFromClassList(OverlayLeave); // opened again while closing: just come back
                overlay.pickingMode = PickingMode.Position;
                return;
            }
            overlay.pickingMode = PickingMode.Position;
            if (!overlay.ClassListContains("hidden")) return;
            Enter(overlay, OverlayEnter);
            Sfx.Play(Sound.UiOpen);
        }

        /// <summary>
        /// Closes a window with a quick fade and shrink (about 2/3 of the open time), then hides it. Does nothing if
        /// it's already hidden or closing.
        /// </summary>
        public static void Hide(VisualElement overlay)
        {
            if (overlay == null || overlay.ClassListContains("hidden") || overlay.ClassListContains(OverlayLeave)) return;
            overlay.AddToClassList(OverlayLeave);
            overlay.pickingMode = PickingMode.Ignore; // fading out: clicks go through to the game
            Sfx.Play(Sound.UiClose);
            overlay.schedule.Execute(() =>
            {
                if (!overlay.ClassListContains(OverlayLeave)) return; // reopened meanwhile
                overlay.RemoveFromClassList(OverlayLeave);
                overlay.AddToClassList("hidden");
            }).StartingIn(150);
        }

        /// <summary>On screen and not on its way out.</summary>
        public static bool IsShown(VisualElement overlay) =>
            overlay != null && !overlay.ClassListContains("hidden") && !overlay.ClassListContains(OverlayLeave);

        /// <summary>Removes .hidden, adds <paramref name="enterClass"/> for one frame, then lets it animate out of it.</summary>
        public static void Enter(VisualElement element, string enterClass)
        {
            element.AddToClassList(enterClass);
            element.RemoveFromClassList("hidden");
            element.schedule.Execute(() => element.RemoveFromClassList(enterClass)).StartingIn(30);
        }

        /// <summary>A quick scale punch (e.g. the money label when you earn).</summary>
        public static void Bump(VisualElement element, string bumpClass, long holdMs = 110)
        {
            element.AddToClassList(bumpClass);
            element.schedule.Execute(() => element.RemoveFromClassList(bumpClass)).StartingIn(holdMs);
        }
    }
}
