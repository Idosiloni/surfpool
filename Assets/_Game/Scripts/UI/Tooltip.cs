using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Hover tooltips for a screen: any element with a <c>tooltip</c> text shows it in a small ink pill after a short
    /// hover (UI Toolkit only draws tooltips in the Editor, so the game draws its own). The pill sits above the
    /// cursor, never over the thing you're pointing at, and stays inside the screen.
    /// </summary>
    public class Tooltip
    {
        const long DelayMs = 350;
        const float Gap = 18f;

        readonly VisualElement root;
        readonly Label pill;
        VisualElement hovered;
        IVisualElementScheduledItem pending;
        Vector2 pointer;

        Tooltip(VisualElement root)
        {
            this.root = root;
            pill = new Label { pickingMode = PickingMode.Ignore };
            pill.AddToClassList("tooltip");
            pill.AddToClassList("hidden");
            root.Add(pill);
            root.RegisterCallback<PointerMoveEvent>(OnMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(_ => Hide(), TrickleDown.TrickleDown);
        }

        /// <summary>Turns on tooltips for everything under <paramref name="root"/>.</summary>
        public static void Attach(VisualElement root)
        {
            if (root != null) new Tooltip(root);
        }

        static VisualElement WithTooltip(IEventHandler target)
        {
            for (var element = target as VisualElement; element != null; element = element.parent)
                if (!string.IsNullOrEmpty(element.tooltip)) return element;
            return null;
        }

        void OnMove(PointerMoveEvent evt)
        {
            pointer = evt.position;
            var target = WithTooltip(evt.target);
            if (target == hovered)
            {
                if (!pill.ClassListContains("hidden")) Place();
                return;
            }
            Hide();
            hovered = target;
            if (target == null) return;
            pending = root.schedule.Execute(Show).StartingIn(DelayMs);
        }

        void Show()
        {
            if (hovered == null || hovered.panel == null) return;
            pill.text = hovered.tooltip;
            pill.BringToFront();
            pill.RemoveFromClassList("hidden");
            pill.style.left = -1000f; // measured next frame, then placed
            root.schedule.Execute(Place);
            Sfx.Play(Sound.UiOpen, null, 0.25f, 1.3f);
        }

        void Place()
        {
            var local = root.WorldToLocal(pointer);
            var size = new Vector2(pill.resolvedStyle.width, pill.resolvedStyle.height);
            if (float.IsNaN(size.x)) return;
            float x = Mathf.Clamp(local.x - size.x * 0.5f, 8f, root.layout.width - size.x - 8f);
            float y = local.y - size.y - Gap;
            if (y < 8f) y = local.y + Gap * 1.6f; // no room above: show it below the cursor
            pill.style.left = x;
            pill.style.top = y;
        }

        void Hide()
        {
            pending?.Pause();
            pending = null;
            hovered = null;
            pill.AddToClassList("hidden");
        }
    }
}
