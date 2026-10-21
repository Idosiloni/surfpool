using System.Collections.Generic;
using UnityEngine;
using UIElements = UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The toasts at the top of the screen (<see cref="Notifications"/>). At most 3 at once; more wait in a queue
    /// instead of pushing the oldest off unread. The same message again bumps a "x2" chip instead of a second pill.
    /// Bad news stays longer and cuts the line. Long messages stay up a little longer. Runs on unscaled time, so
    /// toasts still go away while the game is paused. Owned by <see cref="GameHud"/>.
    /// </summary>
    public class ToastStack
    {
        const int MaxShown = 3;
        const float LeaveSeconds = 0.5f;

        class Toast
        {
            public string message;
            public Notice kind;
            public int count = 1;
            public UIElements.VisualElement element;
            public UIElements.Label countChip;
            public float shownAt, leaveAt, goneAt = -1f;
        }

        readonly UIElements.VisualElement container;
        readonly List<Toast> shown = new();
        readonly List<Toast> waiting = new();

        public ToastStack(UIElements.VisualElement container) => this.container = container;

        static float Now => Time.unscaledTime;

        static float Seconds(string message, Notice kind)
        {
            float extra = Mathf.Max(0, message.Length - 40) * 0.04f;
            return Mathf.Min(6f, (kind == Notice.Bad ? 5.5f : 3.8f) + extra);
        }

        public void Post(string message, Notice kind)
        {
            // Already up (and not leaving): count it instead of a second pill.
            foreach (var toast in shown)
                if (toast.goneAt < 0f && toast.message == message)
                {
                    toast.count++;
                    toast.leaveAt = Now + Seconds(message, kind);
                    toast.countChip.text = "x" + toast.count;
                    toast.countChip.RemoveFromClassList("hidden");
                    UiMotion.Bump(toast.element, "toast--bump", 120);
                    return;
                }
            foreach (var toast in waiting)
                if (toast.message == message)
                {
                    toast.count++;
                    return;
                }

            var next = new Toast { message = message, kind = kind };
            if (kind == Notice.Bad) waiting.Insert(0, next); // bad news cuts the line
            else waiting.Add(next);
            // Bad news that would have to wait hurries the oldest calm toast off.
            if (kind == Notice.Bad && Live() >= MaxShown)
                foreach (var toast in shown)
                    if (toast.goneAt < 0f && toast.kind != Notice.Bad)
                    {
                        toast.leaveAt = Mathf.Min(toast.leaveAt, Mathf.Max(Now, toast.shownAt + 1.5f)); // still readable
                        break;
                    }
            Update();
        }

        int Live()
        {
            int live = 0;
            foreach (var toast in shown) if (toast.goneAt < 0f) live++;
            return live;
        }

        public void Update()
        {
            for (int i = shown.Count - 1; i >= 0; i--)
            {
                var toast = shown[i];
                if (toast.goneAt < 0f && Now >= toast.leaveAt)
                {
                    toast.goneAt = Now + LeaveSeconds;
                    toast.element.AddToClassList("toast--leaving");
                }
                else if (toast.goneAt > 0f && Now >= toast.goneAt)
                {
                    toast.element.RemoveFromHierarchy();
                    shown.RemoveAt(i);
                }
            }
            while (waiting.Count > 0 && Live() < MaxShown)
            {
                var toast = waiting[0];
                waiting.RemoveAt(0);
                Show(toast);
            }
        }

        void Show(Toast toast)
        {
            var element = new UIElements.VisualElement { pickingMode = UIElements.PickingMode.Ignore };
            element.AddToClassList("toast");
            element.AddToClassList("toast--" + toast.kind.ToString().ToLowerInvariant());
            var tile = new UIElements.VisualElement { pickingMode = UIElements.PickingMode.Ignore };
            tile.AddToClassList("toast-tile");
            var icon = new UIElements.VisualElement { pickingMode = UIElements.PickingMode.Ignore };
            icon.AddToClassList("icon");
            icon.AddToClassList("toast-icon");
            icon.AddToClassList(toast.kind switch
            {
                Notice.Good => "icon--coin",
                Notice.Reward => "icon--star",
                Notice.Bad => "icon--bolt",
                _ => "icon--wave",
            });
            tile.Add(icon);
            element.Add(tile);
            var text = new UIElements.Label(toast.message) { pickingMode = UIElements.PickingMode.Ignore };
            text.AddToClassList("toast-text");
            element.Add(text);
            var chip = new UIElements.Label("x" + toast.count) { pickingMode = UIElements.PickingMode.Ignore };
            chip.AddToClassList("toast-count");
            chip.EnableInClassList("hidden", toast.count < 2);
            element.Add(chip);

            toast.element = element;
            toast.countChip = chip;
            toast.shownAt = Now;
            toast.leaveAt = Now + Seconds(toast.message, toast.kind);
            shown.Add(toast);
            container.Add(element);
            UiMotion.Enter(element, "toast--entering");
        }
    }
}
