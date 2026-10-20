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
        Label money, message;
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
            root.Q<Button>("closeButton").clicked += shop.Close;

            tabs.Clear();
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
            {
                var button = new Button(() => SelectTab(category)) { text = category.ToString() };
                button.AddToClassList("tab");
                tabs.Add(button);
            }
            SelectTab(UpgradeCategory.Waves);
        }

        void Update()
        {
            if (shop.IsOpen != shown)
            {
                shown = shop.IsOpen;
                screenRoot.EnableInClassList("hidden", !shown);
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
            foreach (var upgrade in shop.Upgrades)
            {
                if (upgrade.category != tab) continue;
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

                var top = new VisualElement();
                top.AddToClassList("card-top");
                var title = new Label(upgrade.title);
                title.AddToClassList("card-title");
                var cost = new Label(MoneyText.Format(upgrade.cost));
                cost.AddToClassList("cost-chip");
                top.Add(title);
                top.Add(cost);
                card.Add(top);

                var description = new Label(upgrade.description);
                description.AddToClassList("card-desc");
                card.Add(description);

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
                    var stars = new Label($"{StaffMember.StarText(worker.Stars)}   {MoneyText.Format(worker.WagePerMinute)}/min" +
                        (worker.Stars < StaffMember.MaxStars ? $"   ·   {worker.JobsToNextStar} jobs to next star" : "   ·   top rated!"));
                    stars.AddToClassList("card-stars");
                    card.Add(stars);
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
                row.Add(card);
            }
        }
    }
}
