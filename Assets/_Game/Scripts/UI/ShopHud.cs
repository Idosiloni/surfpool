using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Shop customers on the HUD: a bubble over each shopper (their order as colored chips, ticked when done, a
    /// patience bar, and what they say), and the order ticket on the right when you're near a shop with someone
    /// at the counter (shop name, order checklist, what to do next, a progress bar for blending/shaping).
    /// Shares the HUD document with <see cref="GameHud"/>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ShopHud : MonoBehaviour
    {
        [Tooltip("Shoppers farther than this (meters) get no bubble.")]
        [SerializeField] float bubbleDistance = 30f;
        [Tooltip("The order ticket shows within this distance of a shop's counter.")]
        [SerializeField] float ticketDistance = 11f;

        class Bubble
        {
            public VisualElement root, chips, patience, patienceFill;
            public Label title, say;
            public ShopOrder shownOrder;
            public int shownDone = -1;
        }

        VisualElement root, bubbleLayer, ticket, ticketItems, ticketBar, ticketFill;
        Label ticketShop, ticketTitle, ticketHint, ticketPrice;
        readonly Dictionary<Shopper, Bubble> bubbles = new();
        readonly List<Shopper> stale = new();
        ShopOrder ticketOrder;
        int ticketDone = -1;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            bubbleLayer = root.Q("bubbleLayer");
            ticket = root.Q("shopTicket");
            ticketItems = root.Q("shopTicketItems");
            ticketBar = root.Q("shopTicketBar");
            ticketFill = root.Q("shopTicketFill");
            ticketShop = root.Q<Label>("shopTicketShop");
            ticketTitle = root.Q<Label>("shopTicketTitle");
            ticketHint = root.Q<Label>("shopTicketHint");
            ticketPrice = root.Q<Label>("shopTicketPrice");
            bubbles.Clear();
        }

        void LateUpdate()
        {
            if (bubbleLayer == null) return;
            UpdateBubbles();
            UpdateTicket();
        }

        void UpdateBubbles()
        {
            var cam = Camera.main;
            bool menuOpen = InputBlocker.Blocked;
            stale.Clear();
            foreach (var pair in bubbles)
                if (pair.Key == null || !Shopper.All.Contains(pair.Key)) stale.Add(pair.Key);
            foreach (var shopper in stale)
            {
                bubbles[shopper].root.RemoveFromHierarchy();
                bubbles.Remove(shopper);
            }

            foreach (var shopper in Shopper.All)
            {
                if (!bubbles.TryGetValue(shopper, out var bubble))
                {
                    bubble = CreateBubble();
                    bubbles.Add(shopper, bubble);
                }
                string say = shopper.Status;
                bool showOrder = shopper.ShowOrder && shopper.Order != null;
                Vector3 world = shopper.transform.position + Vector3.up * 2.15f;
                Vector3 view = cam != null ? cam.WorldToViewportPoint(world) : Vector3.back;
                bool visible = !menuOpen && cam != null && view.z > 0f && view.z < bubbleDistance
                               && (showOrder || !string.IsNullOrEmpty(say) || shopper.ShowPatience);
                bubble.root.EnableInClassList("hidden", !visible);
                if (!visible) continue;

                Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, world, cam);
                bubble.root.style.left = panelPos.x;
                bubble.root.style.top = panelPos.y;

                bool hasSay = !string.IsNullOrEmpty(say);
                bubble.say.EnableInClassList("hidden", !hasSay);
                if (hasSay && bubble.say.text != say) bubble.say.text = say;
                bubble.title.EnableInClassList("hidden", !showOrder || hasSay);
                bubble.chips.EnableInClassList("hidden", !showOrder);
                if (showOrder)
                {
                    if (bubble.title.text != shopper.Order.title) bubble.title.text = shopper.Order.title;
                    int done = shopper.Order.DoneCount;
                    if (bubble.shownOrder != shopper.Order || bubble.shownDone != done)
                    {
                        bubble.shownOrder = shopper.Order;
                        bubble.shownDone = done;
                        FillChips(bubble.chips, shopper.Order, "bubble-chip");
                    }
                }

                bubble.patience.EnableInClassList("hidden", !shopper.ShowPatience);
                float patience = shopper.Patience01;
                bubble.patienceFill.style.width = Length.Percent(patience * 100f);
                bubble.patienceFill.EnableInClassList("patience-fill--mid", patience < 0.6f && patience >= 0.3f);
                bubble.patienceFill.EnableInClassList("patience-fill--low", patience < 0.3f);
            }
        }

        Bubble CreateBubble()
        {
            var bubble = new Bubble { root = new VisualElement { pickingMode = PickingMode.Ignore } };
            bubble.root.AddToClassList("bubble");
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList("bubble-box");
            box.AddToClassList("shop-bubble-box");
            bubble.say = new Label { pickingMode = PickingMode.Ignore };
            bubble.say.AddToClassList("bubble-text");
            bubble.say.AddToClassList("shop-bubble-say");
            bubble.title = new Label { pickingMode = PickingMode.Ignore };
            bubble.title.AddToClassList("shop-bubble-title");
            bubble.chips = new VisualElement { pickingMode = PickingMode.Ignore };
            bubble.chips.AddToClassList("bubble-chips");
            box.Add(bubble.say);
            box.Add(bubble.title);
            box.Add(bubble.chips);
            var tail = new VisualElement { pickingMode = PickingMode.Ignore };
            tail.AddToClassList("bubble-tail");
            bubble.patience = new VisualElement { pickingMode = PickingMode.Ignore };
            bubble.patience.AddToClassList("patience");
            bubble.patienceFill = new VisualElement { pickingMode = PickingMode.Ignore };
            bubble.patienceFill.AddToClassList("patience-fill");
            bubble.patience.Add(bubble.patienceFill);
            bubble.root.Add(box);
            bubble.root.Add(tail);
            bubble.root.Add(bubble.patience);
            bubbleLayer.Add(bubble.root);
            return bubble;
        }

        static void FillChips(VisualElement holder, ShopOrder order, string chipClass)
        {
            holder.Clear();
            foreach (var item in order.items)
            {
                var chip = new VisualElement { pickingMode = PickingMode.Ignore };
                chip.AddToClassList(chipClass);
                chip.EnableInClassList(chipClass + "--done", item.done);
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("order-dot");
                dot.style.backgroundColor = item.color; // per-item color, set from code like bar widths
                var label = new Label(item.done ? $"<s>{item.label}</s>" : item.label) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("order-chip-label");
                chip.Add(dot);
                chip.Add(label);
                holder.Add(chip);
            }
        }

        void UpdateTicket()
        {
            if (ticket == null) return;
            var cam = Camera.main;
            ShopCounter nearest = null;
            float best = ticketDistance;
            if (cam != null && !InputBlocker.Blocked)
                foreach (var shop in ShopCounter.All)
                {
                    float distance = Vector3.Distance(cam.transform.position, shop.transform.position);
                    if (distance < best && (shop.TicketOrder != null || !string.IsNullOrEmpty(shop.Hint)))
                    {
                        best = distance;
                        nearest = shop;
                    }
                }
            bool show = nearest != null;
            ticket.EnableInClassList("hidden", !show);
            if (!show)
            {
                ticketOrder = null;
                return;
            }
            var order = nearest.TicketOrder;
            ticketShop.text = nearest.ShopName.ToUpperInvariant();
            ticketTitle.EnableInClassList("hidden", order == null);
            ticketItems.EnableInClassList("hidden", order == null);
            ticketPrice.EnableInClassList("hidden", order == null);
            if (order != null)
            {
                ticketTitle.text = order.title;
                ticketPrice.text = MoneyText.Format(order.price);
                if (order != ticketOrder || order.DoneCount != ticketDone)
                {
                    ticketOrder = order;
                    ticketDone = order.DoneCount;
                    FillChips(ticketItems, order, "ticket-chip");
                }
            }
            string hint = nearest.StaffOnDuty ? $"Staff is on it... {nearest.Hint}" : nearest.Hint;
            ticketHint.EnableInClassList("hidden", string.IsNullOrEmpty(hint));
            if (!string.IsNullOrEmpty(hint)) ticketHint.text = hint;
            float progress = nearest.JobProgress;
            if (progress < 0f) progress = nearest.WorkerProgress;
            ticketBar.EnableInClassList("hidden", progress < 0f);
            if (progress >= 0f) ticketFill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
        }
    }
}
