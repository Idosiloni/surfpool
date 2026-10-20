using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// End-of-day report (UI Toolkit, Assets/_Game/UI/DayReport.uxml): income by type, expenses, surfers served and
    /// lost, rating change. The game pauses while it's open; the button saves and starts the next day.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DayReportScreen : MonoBehaviour
    {
        [SerializeField] SaveSystem saveSystem;

        VisualElement root, incomeRows, expenseRows;
        Label title, summary, totals;
        Button next;
        bool open;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("reportRoot");
            incomeRows = doc.Q("incomeRows");
            expenseRows = doc.Q("expenseRows");
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
            summary.text = $"{report.served} happy surfers  ·  {report.lost} lost  ·  rating {report.ratingStart:0.0} → {report.ratingEnd:0.0} stars";
            Fill(incomeRows, report.income, "+");
            Fill(expenseRows, report.expenses, "-");
            int profit = report.TotalIncome - report.TotalExpenses;
            totals.text = $"Income {MoneyText.Format(report.TotalIncome)}   ·   Spent {MoneyText.Format(report.TotalExpenses)}   ·   Profit {(profit >= 0 ? "+" : "")}{MoneyText.Format(profit)}";
            next.text = $"Save & start day {report.day + 1}";
            root.RemoveFromClassList("hidden");
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
            root.AddToClassList("hidden");
            Time.timeScale = 1f;
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
            DayCycle.Instance.NextDay();
            if (saveSystem != null) saveSystem.Save();
            Notifications.Post($"Good morning! Day {DayCycle.Instance.Day} begins. Game saved.");
        }
    }
}
