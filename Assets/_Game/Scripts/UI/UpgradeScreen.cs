using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The upgrade computer's screen (UI Toolkit, Assets/_Game/UI/UpgradeScreen.uxml + Game.uss).
    /// Shows while <see cref="UpgradeShop.IsOpen"/>: category tabs and a card per upgrade with a Buy button.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UpgradeScreen : MonoBehaviour
    {
        [SerializeField] UpgradeShop shop;

        VisualElement screenRoot, tabs;
        ScrollView cards;
        Label money, message, subtitle;
        UpgradeCategory tab;
        bool shown;
        int shownMoney = int.MinValue;
        int shownOwned = -1;
        int shownLevel = -1;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            screenRoot = root.Q("screenRoot");
            tabs = root.Q("tabs");
            cards = root.Q<ScrollView>("cards");
            money = root.Q<Label>("screenMoney");
            message = root.Q<Label>("screenMessage");
            subtitle = root.Q<Label>("screenSubtitle");
            root.Q<Button>("closeButton").clicked += shop.Close;

            tabs.Clear();
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
            {
                var button = new Button(() => SelectTab(category));
                button.AddToClassList("tab");
                button.AddToClassList("tab--" + category.ToString().ToLowerInvariant());
                var icon = new VisualElement();
                icon.AddToClassList("icon");
                icon.AddToClassList("tab-icon");
                icon.AddToClassList("icon--" + IconFor(category));
                button.Add(icon);
                var label = new Label(category.ToString());
                label.AddToClassList("tab-label");
                button.Add(label);
                var badge = new Label { name = "tabBadge" };
                badge.AddToClassList("tab-badge");
                badge.AddToClassList("hidden");
                button.Add(badge);
                tabs.Add(button);
            }
            SelectTab(UpgradeCategory.Waves);
        }

        void Update()
        {
            if (shop.IsOpen != shown)
            {
                shown = shop.IsOpen;
                if (shown) UiMotion.Show(screenRoot);
                else screenRoot.AddToClassList("hidden");
                shownMoney = int.MinValue;
            }
            if (!shown) return;

            int cash = Wallet.Instance != null ? Wallet.Instance.Money : 0;
            int level = ParkLevel.Instance != null ? ParkLevel.Instance.Level : 1;
            if (cash != shownMoney || shop.OwnedCount != shownOwned || level != shownLevel)
            {
                shownMoney = cash;
                shownOwned = shop.OwnedCount;
                shownLevel = level;
                money.text = MoneyText.Format(cash);
                string next = LevelRoadmap.Next(shop, level, out int at);
                subtitle.text = next == null ? $"Park level {level}" : $"Park level {level}  ·  Level {at} brings {next}";
                BuildCards();
            }
            message.text = shop.Message ?? "";
        }

        void SelectTab(UpgradeCategory category)
        {
            tab = category;
            int i = 0;
            foreach (var child in tabs.Children())
                child.EnableInClassList("tab--active", i++ == (int)category);
            BuildCards();
        }

        void BuildCards()
        {
            cards.Clear();
            int cash = Wallet.Instance != null ? Wallet.Instance.Money : 0;
            VisualElement row = null;
            // Buyable first, then too expensive, then locked, owned last (list order within each group).
            var list = new System.Collections.Generic.List<Upgrade>();
            foreach (var upgrade in shop.Upgrades)
                if (upgrade.category == tab) list.Add(upgrade);
            int Rank(Upgrade u) => u.owned ? 3 : !shop.LevelMet(u) || !shop.RequirementMet(u) ? 2 : cash < u.cost ? 1 : 0;
            var order = new System.Collections.Generic.List<int>();
            for (int i = 0; i < list.Count; i++) order.Add(i);
            order.Sort((a, b) => Rank(list[a]) != Rank(list[b]) ? Rank(list[a]).CompareTo(Rank(list[b])) : a.CompareTo(b));
            foreach (int index in order)
            {
                var upgrade = list[index];
                if (row == null || row.childCount == 2)
                {
                    row = new VisualElement();
                    row.AddToClassList("card-row");
                    cards.Add(row);
                }
                bool unlocked = shop.RequirementMet(upgrade);

                var card = new VisualElement();
                card.AddToClassList("upgrade-card");
                card.AddToClassList("upgrade-card--" + upgrade.category.ToString().ToLowerInvariant());
                card.EnableInClassList("upgrade-card--owned", upgrade.owned);

                var top = new VisualElement();
                top.AddToClassList("card-top");
                var tile = new VisualElement();
                tile.AddToClassList("icon-tile");
                var icon = new VisualElement();
                icon.AddToClassList("icon");
                icon.AddToClassList("card-icon");
                icon.AddToClassList("icon--" + IconFor(upgrade.category));
                tile.Add(icon);
                top.Add(tile);
                var title = new Label(upgrade.title);
                title.AddToClassList("card-title");
                top.Add(title);
                card.Add(top);

                var description = new Label(upgrade.description);
                description.AddToClassList("card-desc");
                card.Add(description);

                // Stickers: what Kai's plan wants next, and what the last level just unlocked.
                var mission = MissionManager.Instance != null ? MissionManager.Instance.Current : null;
                bool planned = !upgrade.owned && mission != null && mission.objectives.Exists(o => !o.Done && o.upgradeId == upgrade.id);
                bool fresh = !upgrade.owned && upgrade.level > 1 && upgrade.level == shownLevel;
                if (planned || fresh)
                {
                    var sticker = new Label(planned ? "PLAN" : "NEW");
                    sticker.AddToClassList("card-sticker");
                    sticker.AddToClassList(planned ? "card-sticker--plan" : "card-sticker--new");
                    card.Add(sticker);
                }

                bool levelOk = shop.LevelMet(upgrade);
                if (!levelOk && !upgrade.owned)
                {
                    var requires = new Label($"Unlocks at park level {upgrade.level}");
                    requires.AddToClassList("card-requires");
                    card.Add(requires);
                }
                else if (!unlocked)
                {
                    var requires = new Label("Needs " + shop.TitleOf(upgrade.requires));
                    requires.AddToClassList("card-requires");
                    card.Add(requires);
                }

                string id = upgrade.id;
                var buy = new Button(() => shop.Buy(id));
                buy.AddToClassList("buy-button");
                var worker = upgrade.owned ? shop.StaffFor(upgrade.id) : null;
                if (worker != null)
                {
                    var starRow = new VisualElement();
                    starRow.AddToClassList("card-stars");
                    for (int s = 1; s <= StaffMember.MaxStars; s++)
                    {
                        var star = new VisualElement();
                        star.AddToClassList("icon");
                        star.AddToClassList("icon--star");
                        star.AddToClassList(s <= worker.Stars ? "staff-star" : "staff-star--empty");
                        starRow.Add(star);
                    }
                    var stars = new Label($"{MoneyText.Format(worker.WagePerMinute)}/min" +
                        (worker.Stars < StaffMember.MaxStars ? $"   ·   {worker.JobsToNextStar} jobs to next star" : "   ·   top rated!"));
                    stars.AddToClassList("card-stars-text");
                    starRow.Add(stars);
                    card.Add(starRow);
                }
                if (upgrade.owned)
                {
                    buy.text = worker != null ? "Hired" : "Owned";
                    buy.AddToClassList("buy-button--owned");
                    buy.SetEnabled(false);
                }
                else if (!levelOk)
                {
                    buy.text = $"Park level {upgrade.level}";
                    buy.AddToClassList("buy-button--blocked");
                    buy.SetEnabled(false);
                }
                else if (!unlocked)
                {
                    buy.text = "Locked";
                    buy.AddToClassList("buy-button--blocked");
                    buy.SetEnabled(false);
                }
                else if (cash < upgrade.cost)
                {
                    buy.text = $"Need {MoneyText.Format(upgrade.cost - cash)} more";
                    buy.AddToClassList("buy-button--blocked");
                    buy.SetEnabled(false);
                }
                else
                {
                    buy.text = "Buy";
                }
                card.Add(buy);

                // Corner sticker: the price (coral when you can't afford it yet), or a stamp once it's yours.
                var corner = new Label(upgrade.owned ? (worker != null ? "HIRED" : "OWNED") : MoneyText.Format(upgrade.cost));
                corner.AddToClassList(upgrade.owned ? "price-stamp" : "cost-chip");
                corner.EnableInClassList("cost-chip--short", !upgrade.owned && cash < upgrade.cost);
                card.Add(corner);
                row.Add(card);
            }
            UpdateTabBadges(cash);
        }

        /// <summary>The little coral number on each tab: upgrades you can buy right now.</summary>
        void UpdateTabBadges(int cash)
        {
            int i = 0;
            foreach (var button in tabs.Children())
            {
                var category = (UpgradeCategory)i++;
                int count = 0;
                foreach (var upgrade in shop.Upgrades)
                    if (upgrade.category == category && !upgrade.owned && shop.LevelMet(upgrade) && shop.RequirementMet(upgrade) && cash >= upgrade.cost)
                        count++;
                var badge = button.Q<Label>("tabBadge");
                badge.text = count.ToString();
                badge.EnableInClassList("hidden", count == 0);
            }
        }

        static string IconFor(UpgradeCategory category) => category switch
        {
            UpgradeCategory.Waves => "wave",
            UpgradeCategory.Pools => "pool",
            UpgradeCategory.Park => "palm",
            UpgradeCategory.Rentals => "board",
            _ => "person",
        };
    }
}
