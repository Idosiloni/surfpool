using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// End-of-day report (UI Toolkit, Assets/_Game/UI/DayReport.uxml): income by type, expenses, surfers served and
    /// lost, rating change, and what guests said (ParkReviews). The game pauses while it's open; the button saves and starts the next day.
    /// It plays out like a closing ritual: rows slide in one by one and their money counts up with a tick, then the
    /// profit slams in big, then the reviews drop in. A click (or the button) skips straight to the end.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DayReportScreen : MonoBehaviour
    {
        [SerializeField] SaveSystem saveSystem;

        VisualElement root, incomeRows, expenseRows, reviewCards, factRows, totals;
        Label title, summary, totalIncome, totalSpent, totalProfit;
        Button next;
        bool open;

        /// <summary>One thing that appears during the reveal; money ones count up from 0 to `amount`.</summary>
        class Beat
        {
            public VisualElement element;
            public Label value;
            public string sign;
            public int amount;
            public float at;
            public bool started, done, slam;
        }

        readonly System.Collections.Generic.List<Beat> beats = new();
        bool revealing;
        float nextTick, skippedAt = -10f;
        const float RowGap = 0.09f, CountTime = 0.45f;

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
            totals = doc.Q("reportTotals");
            totalIncome = doc.Q<Label>("totalIncome");
            totalSpent = doc.Q<Label>("totalSpent");
            totalProfit = doc.Q<Label>("totalProfit");
            next = doc.Q<Button>("reportNext");
            // The same click that skipped the reveal (pointer down on the window) mustn't also close it.
            next.clicked += () => { if (revealing) Skip(); else if (Time.unscaledTime - skippedAt > 0.3f) Close(); };
            // Clicking anywhere on the report while it plays out skips to the end.
            root.RegisterCallback<PointerDownEvent>(_ => { if (revealing) Skip(); });
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
            beats.Clear();
            float t = Time.unscaledTime + 0.35f; // after the window has popped in
            t = Fill(incomeRows, report.income, "+", t);
            t = Fill(expenseRows, report.expenses, "-", t + 0.1f);
            int profit = report.TotalIncome - report.TotalExpenses;
            totalIncome.text = "Income " + MoneyText.Format(report.TotalIncome);
            totalSpent.text = "Spent " + MoneyText.Format(report.TotalExpenses);
            totalProfit.text = (profit >= 0 ? "+" : "") + MoneyText.Format(profit);
            totalProfit.EnableInClassList("report-profit--loss", profit < 0);
            beats.Add(new Beat { element = totals, at = t + 0.15f });
            beats.Add(new Beat { element = totalProfit, value = totalProfit, sign = profit >= 0 ? "+" : "-", amount = Mathf.Abs(profit), at = t + 0.3f, slam = true });
            FillReviews(t + 0.3f + CountTime + 0.25f);
            foreach (var beat in beats) beat.element.AddToClassList("report-beat--waiting");
            revealing = true;
            next.text = $"Save & start day {report.day + 1}";
            UiMotion.Show(root);
        }

        void Update()
        {
            if (open && UnityEngine.Cursor.lockState == CursorLockMode.Locked) FirstPersonController.LockCursor(false);
            if (!revealing) return;
            float now = Time.unscaledTime;
            bool counting = false, left = false;
            foreach (var beat in beats)
            {
                if (beat.done) continue;
                left = true;
                if (now < beat.at) continue;
                if (!beat.started)
                {
                    beat.started = true;
                    beat.element.RemoveFromClassList("report-beat--waiting");
                    if (beat.slam) Slam(beat);
                }
                if (beat.value == null || beat.slam)
                {
                    beat.done = true;
                    continue;
                }
                float k = Mathf.Clamp01((now - beat.at) / CountTime);
                k = 1f - (1f - k) * (1f - k); // ease out: fast, then settling
                beat.value.text = beat.sign + MoneyText.Format(Mathf.RoundToInt(beat.amount * k));
                counting |= k < 1f;
                if (k >= 1f) beat.done = true;
            }
            // A soft rising tick while money counts up (like a till).
            if (counting && now >= nextTick)
            {
                nextTick = now + 0.06f;
                Sfx.Play(Sound.UiTick, null, 1f, 0.9f + 0.4f * Mathf.PingPong(now * 2f, 1f));
            }
            if (!left) revealing = false;
        }

        void Slam(Beat beat)
        {
            beat.value.text = beat.sign == "-" ? "-" + MoneyText.Format(beat.amount) : "+" + MoneyText.Format(beat.amount);
            UiMotion.Enter(beat.element, "report-profit--slam");
            Sfx.Play(beat.sign == "-" ? Sound.Error : Sound.CashBig);
        }

        /// <summary>Everything appears at once, with the final numbers.</summary>
        void Skip()
        {
            skippedAt = Time.unscaledTime;
            foreach (var beat in beats)
            {
                if (beat.done) continue;
                beat.done = true;
                beat.element.RemoveFromClassList("report-beat--waiting");
                if (beat.slam && !beat.started) Slam(beat);
                else if (beat.value != null && !beat.slam) beat.value.text = beat.sign + MoneyText.Format(beat.amount);
            }
            revealing = false;
        }

        /// <summary>The best, the worst and a wild-card review of the day, then fun facts with names; they drop in from `at`.</summary>
        void FillReviews(float at)
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
                beats.Add(new Beat { element = card, at = at });
                at += 0.18f;
            }
            foreach (string fact in ParkReviews.Facts())
            {
                var line = new Label(fact);
                line.AddToClassList("report-fact");
                factRows.Add(line);
            }
            beats.Add(new Beat { element = factRows, at = at + 0.1f });
        }

        /// <summary>One row per line in the book; each slides in RowGap after the last and counts up. Returns when the last starts.</summary>
        float Fill(VisualElement container, System.Collections.Generic.SortedDictionary<string, int> book, string sign, float at)
        {
            container.Clear();
            if (book.Count == 0)
            {
                var none = new Label("Nothing today");
                none.AddToClassList("report-empty");
                container.Add(none);
                beats.Add(new Beat { element = none, at = at });
                return at;
            }
            foreach (var pair in book)
            {
                var row = new VisualElement();
                row.AddToClassList("stat-row");
                var name = new Label(char.ToUpperInvariant(pair.Key[0]) + pair.Key.Substring(1));
                name.AddToClassList("report-name");
                var value = new Label(sign + MoneyText.Format(0));
                value.AddToClassList("report-value");
                row.Add(name);
                row.Add(value);
                container.Add(row);
                beats.Add(new Beat { element = row, value = value, sign = sign, amount = pair.Value, at = at });
                at += RowGap;
            }
            return at;
        }

        void Close()
        {
            if (!open) return;
            open = false;
            revealing = false;
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
