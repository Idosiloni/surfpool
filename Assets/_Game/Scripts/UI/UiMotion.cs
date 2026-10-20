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
        const string OverlayEnter = "screen-overlay--enter";

        /// <summary>Shows a full-screen window (.screen-overlay) with a fade and a little pop. Does nothing if it's already up.</summary>
        public static void Show(VisualElement overlay)
        {
            if (overlay == null || !overlay.ClassListContains("hidden")) return;
            Enter(overlay, OverlayEnter);
        }

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
