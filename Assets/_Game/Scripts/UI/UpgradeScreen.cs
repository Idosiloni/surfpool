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
            if (cash != shownMoney || shop.OwnedCount != shownOwned)
            {
                shownMoney = cash;
                shownOwned = shop.OwnedCount;
                money.text = "$ " + cash;
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
                var cost = new Label("$" + upgrade.cost);
                cost.AddToClassList("cost-chip");
                top.Add(title);
                top.Add(cost);
                card.Add(top);

                var description = new Label(upgrade.description);
                description.AddToClassList("card-desc");
                card.Add(description);

                if (!unlocked)
                {
                    var requires = new Label("Needs " + shop.TitleOf(upgrade.requires));
                    requires.AddToClassList("card-requires");
                    card.Add(requires);
                }

                string id = upgrade.id;
                var buy = new Button(() => shop.Buy(id));
                buy.AddToClassList("buy-button");
                if (upgrade.owned)
                {
                    buy.text = "Owned";
                    buy.AddToClassList("buy-button--owned");
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
                    buy.text = $"Need ${upgrade.cost - cash} more";
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
