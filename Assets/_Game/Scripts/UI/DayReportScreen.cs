using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// End-of-day report (UI Toolkit, Assets/_Game/UI/DayReport.uxml): income by type, expenses, surfers served and
    /// lost, rating change, and what guests said (ParkReviews). The game pauses while it's open; the button saves and starts the next day.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DayReportScreen : MonoBehaviour
    {
        [SerializeField] SaveSystem saveSystem;

        VisualElement root, incomeRows, expenseRows, reviewCards, factRows;
        Label title, summary, totals;
        Button next;
        bool open;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("reportRoot");
            incomeRows = doc.Q("incomeRows");
            expenseRows = doc.Q("expenseRows");
            reviewCards = doc.Q("reviewCards");
            factRows = doc.Q("factRows");
            title = doc.Q<Label>("reportTitle");
            summary = doc.Q<Label>("reportSummary");
            totals = doc.Q<Label>("reportTotals");
            next = doc.Q<Button>("reportNext");
            next.clicked += Close;
        }

        void Start()
        {
            if (DayCycle.Instance != null) DayCycle.Instance.DayEnded += Show;
        }

        void OnDestroy()
        {
            if (DayCycle.Instance != null) DayCycle.Instance.DayEnded -= Show;
        }

        void Show(DayReport report)
        {
            open = true;
            Time.timeScale = 0f;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);

            var dayEvent = DailyEvents.Instance != null ? DailyEvents.Instance.Today : DayEvent.None;
            title.text = $"Day {report.day} is over!" + (dayEvent != DayEvent.None ? $"  ({DailyEvents.Title(dayEvent)})" : "");
            summary.text = $"{ParkName.Name}  ·  {report.served} happy surfers  ·  {report.lost} lost  ·  rating {report.ratingStart:0.0} to {report.ratingEnd:0.0} stars";
            Fill(incomeRows, report.income, "+");
            Fill(expenseRows, report.expenses, "-");
            FillReviews();
            int profit = report.TotalIncome - report.TotalExpenses;
            totals.text = $"Income {MoneyText.Format(report.TotalIncome)}   ·   Spent {MoneyText.Format(report.TotalExpenses)}   ·   Profit {(profit >= 0 ? "+" : "")}{MoneyText.Format(profit)}";
            next.text = $"Save & start day {report.day + 1}";
            UiMotion.Show(root);
        }

        /// <summary>The best, the worst and a wild-card review of the day, then fun facts with names.</summary>
        void FillReviews()
        {
            reviewCards.Clear();
            factRows.Clear();
            var picks = ParkReviews.Highlights();
            if (picks.Count == 0)
            {
                var none = new Label("No reviews yet. Serve some surfers!");
                none.AddToClassList("report-empty");
                reviewCards.Add(none);
            }
            foreach (var review in picks)
            {
                var card = new VisualElement();
                card.AddToClassList("review-card");
                if (review.stars <= 2) card.AddToClassList("review-card--bad");
                var stars = new Label(new string('★', review.stars) + new string('☆', 5 - review.stars));
                stars.AddToClassList("review-stars");
                var text = new Label($"\"{review.text}\"");
                text.AddToClassList("review-text");
                var who = new Label("- " + review.who);
                who.AddToClassList("review-who");
                card.Add(stars);
                card.Add(text);
                card.Add(who);
                reviewCards.Add(card);
            }
            foreach (string fact in ParkReviews.Facts())
            {
                var line = new Label(fact);
                line.AddToClassList("report-fact");
                factRows.Add(line);
            }
        }

        static void Fill(VisualElement container, System.Collections.Generic.SortedDictionary<string, int> book, string sign)
        {
            container.Clear();
            if (book.Count == 0)
            {
                var none = new Label("Nothing today");
                none.AddToClassList("report-empty");
                container.Add(none);
                return;
            }
            foreach (var pair in book)
            {
                var row = new VisualElement();
                row.AddToClassList("stat-row");
                var name = new Label(char.ToUpperInvariant(pair.Key[0]) + pair.Key.Substring(1));
                name.AddToClassList("report-name");
                var value = new Label(sign + MoneyText.Format(pair.Value));
                value.AddToClassList("report-value");
                row.Add(name);
                row.Add(value);
                container.Add(row);
            }
        }

        void Update()
        {
            if (open && UnityEngine.Cursor.lockState == CursorLockMode.Locked) FirstPersonController.LockCursor(false);
        }

        void Close()
        {
            if (!open) return;
            open = false;
            UiMotion.Hide(root);
            Time.timeScale = 1f;
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
            DayCycle.Instance.NextDay();
            ParkReviews.NewDay();
            if (saveSystem != null) saveSystem.Save();
            Notifications.Post($"Good morning! Day {DayCycle.Instance.Day} begins. Game saved.");
        }
    }
}
