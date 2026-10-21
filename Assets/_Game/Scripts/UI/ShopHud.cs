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
            public Label title, say, mood;
            public ShopOrder shownOrder;
            public int shownDone = -1;
        }

        VisualElement root, bubbleLayer, ticket, ticketItems, ticketBar, ticketFill, cashLayer;
        Label ticketShop, ticketTitle, ticketHint, ticketPrice, ticketMood;
        HoldRing holdRing;
        PlayerInteractor interactor;
        readonly List<(Label label, Vector3 world, float born)> cashPops = new();
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
            ticketMood = root.Q<Label>("shopTicketMood");
            holdRing = root.Q<HoldRing>("holdRing");
            cashLayer = root.Q("cashPopLayer");
            bubbles.Clear();
            ShopCounter.Sold += OnSold;
        }

        void OnDisable() => ShopCounter.Sold -= OnSold;

        /// <summary>Floats "+$price" (and the tip under it) up from the counter.</summary>
        void OnSold(Vector3 at, int price, int tip)
        {
            if (cashLayer == null) return;
            AddCashPop("+" + MoneyText.Format(price), at, false);
            if (tip > 0) AddCashPop("+" + MoneyText.Format(tip) + " tip", at + Vector3.down * 0.35f, true);
        }

        void AddCashPop(string text, Vector3 world, bool tip)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("cash-pop");
            label.EnableInClassList("cash-pop--tip", tip);
            cashLayer.Add(label);
            cashPops.Add((label, world, Time.unscaledTime));
        }

        void UpdateCashPops()
        {
            var cam = Camera.main;
            for (int i = cashPops.Count - 1; i >= 0; i--)
            {
                var (label, world, born) = cashPops[i];
                float age = Time.unscaledTime - born;
                if (age > 1.6f || cam == null)
                {
                    label.RemoveFromHierarchy();
                    cashPops.RemoveAt(i);
                    continue;
                }
                Vector3 at = world + Vector3.up * (age * 0.8f);
                bool visible = cam.WorldToViewportPoint(at).z > 0f && !InputBlocker.Blocked;
                label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible) continue;
                Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, at, cam);
                label.style.left = panelPos.x;
                label.style.top = panelPos.y;
                label.style.opacity = age < 1f ? 1f : 1f - (age - 1f) / 0.6f;
                float pop = age < 0.15f ? Mathf.Lerp(0.6f, 1.15f, age / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((age - 0.15f) / 0.2f));
                label.style.scale = new Scale(new Vector3(pop, pop, 1f));
            }
        }

        /// <summary>The crosshair ring for hold-E jobs: blending, shaping, repairing a wave machine.</summary>
        void UpdateHoldRing()
        {
            if (holdRing == null) return;
            if (interactor == null) interactor = FindAnyObjectByType<PlayerInteractor>();
            float progress = -1f;
            var target = interactor != null ? interactor.Target : null;
            if (target is ShopStation station && station.Shop != null && (station.Id == AcaiBar.Blend || station.Id == BoardShaper.Stand))
                progress = station.Shop.JobProgress;
            else if (target is MachineService service) progress = service.HoldFraction;
            holdRing.EnableInClassList("hidden", progress < 0f);
            if (progress >= 0f) holdRing.Progress = progress;
        }

        void LateUpdate()
        {
            if (bubbleLayer == null) return;
            UpdateBubbles();
            UpdateTicket();
            UpdateCashPops();
            UpdateHoldRing();
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
                bool hasMood = showOrder && !string.IsNullOrEmpty(shopper.Order.mood);
                bubble.mood.EnableInClassList("hidden", !hasMood);
                if (hasMood)
                {
                    bubble.mood.text = shopper.Order.mood;
                    bubble.mood.EnableInClassList("order-mood--good", shopper.Order.moodGood);
                }
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
            bubble.mood = new Label { pickingMode = PickingMode.Ignore };
            bubble.mood.AddToClassList("order-mood");
            box.Add(bubble.say);
            box.Add(bubble.title);
            box.Add(bubble.mood);
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
            bool mood = order != null && !string.IsNullOrEmpty(order.mood);
            ticketMood.EnableInClassList("hidden", !mood);
            if (mood)
            {
                ticketMood.text = order.mood + (order.tipScale > 1f ? $"  (tips x{order.tipScale:0.#})" : "");
                ticketMood.EnableInClassList("order-mood--good", order.moodGood);
            }
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
