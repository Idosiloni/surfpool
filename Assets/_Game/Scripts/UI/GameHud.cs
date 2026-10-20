using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The in-game HUD (UI Toolkit, Assets/_Game/UI/Hud.uxml + Game.uss): money with +/- popups, park rating stars,
    /// wave machine panel, park stats, crosshair and [E] prompt, toasts, and speech bubbles over surfers.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameHud : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] FirstPersonController player;
        [SerializeField] PriceBoard pricing;
        [SerializeField] SurferSpawner spawner;
        [SerializeField] StaffPayroll payroll;
        [Tooltip("Surfers farther than this (meters) get no speech bubble.")]
        [SerializeField] float bubbleDistance = 45f;
        [Tooltip("Seconds before the controls hint fades away.")]
        [SerializeField] float hintSeconds = 90f;

        class Bubble
        {
            public VisualElement root;
            public Label badge;
            public Label text;
            public VisualElement patience;
            public VisualElement patienceFill;
        }

        VisualElement root, bubbleLayer, popupStack, toasts, crosshair, prompt, waveBarFill, rentalRow, wagesRow, billsRow;
        Label money, ratingLabel, waveType, waveStatus, waveAuto, ticketValue, rentalValue, servedValue, lostValue,
            wagesValue, billsValue, promptLabel, clickToPlay, hint, dayLabel;
        StarRating stars;
        VisualElement missionCard, missionObjectives, missionMarker;
        Label missionChapter, missionTitle, missionStory, missionHint, markerDistance, missionBanner;
        Mission shownMission;
        float nextMissionRefresh;
        MissionManager subscribedMissions;
        readonly Dictionary<Surfer, Bubble> bubbles = new();
        readonly List<Surfer> staleBubbles = new();
        Wallet wallet;
        UpgradeShop shop;
        VisualElement goalStrip, goalFill, xpFill;
        Label incomeRate, goalName, goalAmount, levelLabel, xpLabel, eventLabel, trashValue;
        VisualElement trashRow;
        ParkLevel subscribedLevel;
        float shownMoney;
        readonly Queue<(float time, int amount)> recentIncome = new();
        int incomeLastMinute;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            bubbleLayer = root.Q("bubbleLayer");
            popupStack = root.Q("popupStack");
            toasts = root.Q("toasts");
            crosshair = root.Q("crosshair");
            prompt = root.Q("prompt");
            waveBarFill = root.Q("waveBarFill");
            rentalRow = root.Q("rentalRow");
            wagesRow = root.Q("wagesRow");
            billsRow = root.Q("billsRow");
            billsValue = root.Q<Label>("billsValue");
            money = root.Q<Label>("moneyLabel");
            ratingLabel = root.Q<Label>("ratingLabel");
            waveType = root.Q<Label>("waveType");
            waveStatus = root.Q<Label>("waveStatus");
            waveAuto = root.Q<Label>("waveAuto");
            ticketValue = root.Q<Label>("ticketValue");
            rentalValue = root.Q<Label>("rentalValue");
            servedValue = root.Q<Label>("servedValue");
            lostValue = root.Q<Label>("lostValue");
            wagesValue = root.Q<Label>("wagesValue");
            promptLabel = root.Q<Label>("promptLabel");
            clickToPlay = root.Q<Label>("clickToPlay");
            hint = root.Q<Label>("hint");
            dayLabel = root.Q<Label>("dayLabel");
            incomeRate = root.Q<Label>("incomeRate");
            goalStrip = root.Q("goalStrip");
            goalFill = root.Q("goalFill");
            goalName = root.Q<Label>("goalName");
            goalAmount = root.Q<Label>("goalAmount");
            levelLabel = root.Q<Label>("levelLabel");
            eventLabel = root.Q<Label>("eventLabel");
            trashValue = root.Q<Label>("trashValue");
            trashRow = root.Q("trashRow");
            xpLabel = root.Q<Label>("xpLabel");
            xpFill = root.Q("xpFill");

            missionCard = root.Q("missionCard");
            missionObjectives = root.Q("missionObjectives");
            missionMarker = root.Q("missionMarker");
            missionChapter = root.Q<Label>("missionChapter");
            missionTitle = root.Q<Label>("missionTitle");
            missionStory = root.Q<Label>("missionStory");
            missionHint = root.Q<Label>("missionHint");
            markerDistance = root.Q<Label>("markerDistance");
            missionBanner = root.Q<Label>("missionBanner");

            stars = new StarRating();
            root.Q("ratingStarsSlot").Add(stars);

            Notifications.Posted += ShowToast;
        }

        void OnDisable()
        {
            Notifications.Posted -= ShowToast;
            if (wallet != null) wallet.Changed -= ShowPopup;
            wallet = null;
            if (subscribedMissions != null) subscribedMissions.Completed -= ShowMissionBanner;
            subscribedMissions = null;
            if (subscribedLevel != null) subscribedLevel.LeveledUp -= ShowLevelUp;
            subscribedLevel = null;
            bubbles.Clear();
        }

        void Update()
        {
            if (wallet == null && Wallet.Instance != null)
            {
                wallet = Wallet.Instance;
                wallet.Changed += ShowPopup;
                wallet.Changed += TrackIncome;
                shownMoney = wallet.Money;
            }

            UpdateMoneyAndRating();
            UpdateLevel();
            UpdateGoal();
            UpdateWavePanel();
            UpdateStats();
            UpdatePrompt();
            UpdateBubbles();
            UpdateMission();
            var surfing = PlayerSurfing.Instance;
            if (surfing != null && surfing.IsSurfing)
            {
                SetText(hint, surfing.Hint);
                hint.RemoveFromClassList("hint--hidden");
            }
            else
            {
                SetText(hint, "WASD move   ·   Shift sprint   ·   Space jump   ·   E use   ·   B build   ·   Tab menu   ·   Esc free mouse");
                hint.EnableInClassList("hint--hidden", Time.timeSinceLevelLoad > hintSeconds);
            }
        }

        void UpdateLevel()
        {
            var park = ParkLevel.Instance;
            if (park == null) return;
            if (subscribedLevel != park)
            {
                if (subscribedLevel != null) subscribedLevel.LeveledUp -= ShowLevelUp;
                park.LeveledUp += ShowLevelUp;
                subscribedLevel = park;
            }
            SetText(levelLabel, park.Level.ToString());
            SetText(xpLabel, $"{park.XpToNext:#,0} XP to level {park.Level + 1}");
            xpFill.style.width = Length.Percent(park.Progress * 100f);
        }

        void ShowLevelUp(int level)
        {
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            var unlocked = shop != null ? shop.UnlockedAt(level) : new List<string>();
            ShowBanner($"PARK LEVEL {level}!" + (unlocked.Count > 0 ? "\nNew: " + string.Join(", ", unlocked) : ""));
            Notifications.Post(unlocked.Count > 0 ? $"Park level {level}: unlocked {string.Join(", ", unlocked)}" : $"Park level {level}!");
            Sfx.Play(Sound.Mission);
            Sfx.Duck();
        }

        /// <summary>Earnings from the park itself (not one-off mission rewards) feed the "+$/min" readout.</summary>
        void TrackIncome(int amount, string reason)
        {
            if (amount <= 0 || reason == "mission reward") return;
            recentIncome.Enqueue((Time.time, amount));
            incomeLastMinute += amount;
        }

        /// <summary>
        /// The cheapest upgrade you can buy next, with a bar filling toward its price. When you can afford it, the
        /// strip turns teal: go to the computer.
        /// </summary>
        void UpdateGoal()
        {
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            if (shop == null || wallet == null) return;
            Upgrade next = null;
            foreach (var upgrade in shop.Upgrades)
                if (!upgrade.owned && shop.RequirementMet(upgrade) && shop.LevelMet(upgrade) && (next == null || upgrade.cost < next.cost))
                    next = upgrade;
            goalStrip.EnableInClassList("hidden", next == null || shop.IsOpen || BuildMode.Active || FishingSpot.Fishing);
            if (next == null) return;
            bool ready = wallet.Money >= next.cost;
            SetText(goalName, next.title);
            SetText(goalAmount, ready ? "Ready! Buy it at the computer" : $"{MoneyText.Format(Mathf.Max(0, wallet.Money))} / {MoneyText.Format(next.cost)}");
            goalFill.style.width = Length.Percent(Mathf.Clamp01(wallet.Money / (float)next.cost) * 100f);
            goalStrip.EnableInClassList("goal-strip--ready", ready);
        }

        void UpdateMoneyAndRating()
        {
            if (wallet != null)
            {
                // The number counts up (or down) to the real amount, fast for big jumps.
                float gap = Mathf.Abs(wallet.Money - shownMoney);
                shownMoney = Mathf.MoveTowards(shownMoney, wallet.Money, Mathf.Max(gap * 8f, 40f) * Time.unscaledDeltaTime);
                SetText(money, MoneyText.Format(Mathf.RoundToInt(shownMoney)));
                money.EnableInClassList("money-label--negative", wallet.Money < 0);

                while (recentIncome.Count > 0 && Time.time - recentIncome.Peek().time > 60f)
                    incomeLastMinute -= recentIncome.Dequeue().amount;
                SetText(incomeRate, (incomeLastMinute >= 0 ? "+" : "") + MoneyText.Format(incomeLastMinute) + " / min");
            }
            if (DayCycle.Instance != null) SetText(dayLabel, $"Day {DayCycle.Instance.Day}  ·  {DayCycle.Instance.Clock}");
            var dayEvent = DailyEvents.Instance != null ? DailyEvents.Instance.Today : DayEvent.None;
            eventLabel.EnableInClassList("hidden", dayEvent == DayEvent.None);
            SetText(eventLabel, "Today: " + DailyEvents.Title(dayEvent));
            if (ParkReputation.Instance != null)
            {
                stars.Stars = ParkReputation.Instance.Stars;
                SetText(ratingLabel, ParkReputation.Instance.Stars.ToString("0.0"));
            }
        }

        /// <summary>The wave machine of the open pool closest to the player.</summary>
        WaveMachine NearestMachine()
        {
            WaveMachine best = null;
            float bestDistance = float.MaxValue;
            Vector3 from = player != null ? player.transform.position : Vector3.zero;
            foreach (var pool in PoolRegistry.All)
            {
                if (!pool.IsOpen || pool.Machine == null) continue;
                float distance = (pool.Lineup.position - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = pool.Machine;
                    bestDistance = distance;
                }
            }
            return best;
        }

        void UpdateWavePanel()
        {
            var machine = NearestMachine();
            if (machine == null) return;
            var selected = machine.Selected;
            string direction = selected == WaveLevel.Beginner ? "Whitewater"
                : machine.SelectedDirection == WaveDirection.Peak ? "Peak" : machine.SelectedDirection.ToString();
            SetText(waveType, machine.Preset != null ? $"{machine.Preset.name}  ·  {selected}" : $"{selected}  ·  {direction}");
            waveType.EnableInClassList("wave-type--beginner", selected == WaveLevel.Beginner);
            waveType.EnableInClassList("wave-type--intermediate", selected == WaveLevel.Intermediate);
            waveType.EnableInClassList("wave-type--pro", selected == WaveLevel.Pro);

            waveBarFill.style.width = Length.Percent(machine.ReadyProgress * 100f);
            waveBarFill.EnableInClassList("bar-fill--charging", !machine.Ready);
            var service = machine.Service;
            waveStatus.EnableInClassList("small-text--alert", service != null && service.NeedsService);
            if (service != null && service.Broken)
                SetText(waveStatus, "BROKEN! Hold E at the machine house");
            else if (service != null && service.NeedsService)
                SetText(waveStatus, $"Needs a service (wear {Mathf.RoundToInt(service.Wear * 100f)}%)");
            else SetText(waveStatus, machine.Ready
                ? $"Ready - press the big button  ·  {MoneyText.Format(machine.PressCost)} power"
                : $"Recharging... {Mathf.CeilToInt(machine.SecondsUntilReady)}s");

            string auto = machine.AutoInterval <= 0f ? "Auto: off"
                : $"Auto: every {machine.AutoInterval:0}s" + (machine.SmartAuto ? " (smart)" : "");
            if (machine.WavesPerSet > 1) auto += $"  ·  sets of {machine.WavesPerSet}";
            SetText(waveAuto, auto);
        }

        void UpdateStats()
        {
            if (pricing != null)
            {
                SetText(ticketValue, SessionPrices());
                SetText(rentalValue, MoneyText.Format(pricing.RentalPrice));
            }
            if (spawner != null)
            {
                rentalRow.EnableInClassList("hidden", spawner.RenterChance <= 0f);
                SetText(servedValue, spawner.Served.ToString());
                SetText(lostValue, spawner.Lost.ToString());
                lostValue.EnableInClassList("stat-value--bad", spawner.Lost > 0);
            }
            if (payroll != null)
            {
                int wages = payroll.WagesPerMinute;
                wagesRow.EnableInClassList("hidden", wages <= 0);
                SetText(wagesValue, MoneyText.Format(wages) + "/min");
            }
            int trash = Trash.All.Count;
            trashRow.EnableInClassList("hidden", trash == 0);
            SetText(trashValue, trash.ToString());
            trashValue.EnableInClassList("stat-value--bad", trash > 5);
            var costs = OperatingCosts.Instance;
            if (costs != null && DayCycle.Instance != null)
                SetText(billsValue, MoneyText.Format(costs.EstimateToday(DayCycle.Instance.IncomeToday)));
        }

        /// <summary>"$20" at first; "$20 · $50 · $120" once better surfers come (beginner · intermediate · pro).</summary>
        string SessionPrices()
        {
            var best = spawner != null ? spawner.BestSkillComing : WaveLevel.Beginner;
            string text = MoneyText.Format(pricing.Price);
            for (var level = WaveLevel.Intermediate; level <= best; level++)
                text += "  ·  " + MoneyText.Format(pricing.PriceFor(level, null));
            return text;
        }

        void UpdatePrompt()
        {
            bool menuOpen = InputBlocker.Blocked;
            bool cursorFree = player != null && !player.CursorLocked && !menuOpen;
            var target = interactor != null && !menuOpen ? interactor.Target : null;
            string text = target?.Prompt;

            crosshair.EnableInClassList("hidden", menuOpen || cursorFree);
            crosshair.EnableInClassList("crosshair--active", !string.IsNullOrEmpty(text));
            prompt.EnableInClassList("hidden", string.IsNullOrEmpty(text) || cursorFree);
            if (!string.IsNullOrEmpty(text)) SetText(promptLabel, text);
            clickToPlay.EnableInClassList("hidden", !cursorFree);
        }

        void UpdateBubbles()
        {
            var cam = Camera.main;
            bool menuOpen = InputBlocker.Blocked;

            staleBubbles.Clear();
            foreach (var pair in bubbles)
                if (pair.Key == null || !Surfer.All.Contains(pair.Key)) staleBubbles.Add(pair.Key);
            foreach (var surfer in staleBubbles)
            {
                bubbles[surfer].root.RemoveFromHierarchy();
                bubbles.Remove(surfer);
            }

            foreach (var surfer in Surfer.All)
            {
                if (!bubbles.TryGetValue(surfer, out var bubble))
                {
                    bubble = CreateBubble(surfer.Skill);
                    bubbles.Add(surfer, bubble);
                }

                string status = surfer.Status;
                bool hasText = !string.IsNullOrEmpty(status);
                Vector3 world = surfer.transform.position + Vector3.up * 2.15f;
                Vector3 view = cam != null ? cam.WorldToViewportPoint(world) : Vector3.back;
                bool visible = !menuOpen && cam != null && view.z > 0f && view.z < bubbleDistance
                               && (hasText || surfer.ShowPatience);
                bubble.root.EnableInClassList("hidden", !visible);
                if (!visible) continue;

                Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, world, cam);
                bubble.root.style.left = panelPos.x;
                bubble.root.style.top = panelPos.y;

                bubble.text.EnableInClassList("hidden", !hasText);
                if (hasText) SetText(bubble.text, status);

                bubble.patience.EnableInClassList("hidden", !surfer.ShowPatience);
                float patience = Mathf.Clamp01(surfer.Patience);
                bubble.patienceFill.style.width = Length.Percent(patience * 100f);
                bubble.patienceFill.EnableInClassList("patience-fill--mid", patience < 0.6f && patience >= 0.3f);
                bubble.patienceFill.EnableInClassList("patience-fill--low", patience < 0.3f);
            }
        }

        void UpdateMission()
        {
            var missions = MissionManager.Instance;
            if (missions != null && subscribedMissions == null)
            {
                subscribedMissions = missions;
                missions.Completed += ShowMissionBanner;
            }
            var mission = missions != null ? missions.Current : null;
            bool menuOpen = InputBlocker.Blocked;
            missionCard.EnableInClassList("hidden", mission == null);

            // Marker over where the mission wants you to go (clamped to the screen edge when off screen).
            var target = mission?.marker?.Invoke();
            var cam = Camera.main;
            bool showMarker = target != null && cam != null && !menuOpen && target.gameObject.activeInHierarchy;
            missionMarker.EnableInClassList("hidden", !showMarker);
            if (showMarker)
            {
                Vector3 world = target.position + Vector3.up * (2.6f + Mathf.Sin(Time.time * 3f) * 0.15f);
                Vector3 view = cam.WorldToViewportPoint(world);
                if (view.z < 0f) view = new Vector3(1f - view.x, 0.02f, 0f); // behind you: show along the bottom edge
                view.x = Mathf.Clamp(view.x, 0.04f, 0.96f);
                view.y = Mathf.Clamp(view.y, 0.06f, 0.94f);
                Vector3 clamped = cam.ViewportToWorldPoint(new Vector3(view.x, view.y, Mathf.Max(view.z, 1f)));
                Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, clamped, cam);
                missionMarker.style.left = panelPos.x;
                missionMarker.style.top = panelPos.y;
                float distance = player != null ? Vector3.Distance(player.transform.position, target.position) : 0f;
                SetText(markerDistance, $"{distance:0} m");
            }

            if (mission == null || Time.unscaledTime < nextMissionRefresh && mission == shownMission) return;
            nextMissionRefresh = Time.unscaledTime + 0.25f;
            shownMission = mission;
            SetText(missionChapter, mission.chapter.ToUpperInvariant());
            SetText(missionTitle, mission.title);
            SetText(missionStory, mission.story);
            SetText(missionHint, "Tip: " + mission.hint + (mission.reward > 0 ? $"   Reward: ${mission.reward}" : ""));
            missionObjectives.Clear();
            foreach (var objective in mission.objectives)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("objective-row");
                var check = new VisualElement { pickingMode = PickingMode.Ignore };
                check.AddToClassList("objective-check");
                check.EnableInClassList("objective-check--done", objective.Done);
                var text = new Label(objective.text + objective.Progress) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("objective-text");
                text.EnableInClassList("objective-text--done", objective.Done);
                row.Add(check);
                row.Add(text);
                missionObjectives.Add(row);
            }
        }

        void ShowMissionBanner(Mission mission) =>
            ShowBanner("MISSION COMPLETE!\n" + mission.title + (mission.reward > 0 ? "   +" + MoneyText.Format(mission.reward) : ""));

        void ShowBanner(string text)
        {
            missionBanner.text = text;
            missionBanner.RemoveFromClassList("mission-banner--leaving");
            missionBanner.RemoveFromClassList("hidden");
            missionBanner.schedule.Execute(() => missionBanner.AddToClassList("mission-banner--leaving")).StartingIn(2500);
            missionBanner.schedule.Execute(() => missionBanner.AddToClassList("hidden")).StartingIn(3200);
        }

        Bubble CreateBubble(WaveLevel skill)
        {
            var bubble = new Bubble { root = new VisualElement { pickingMode = PickingMode.Ignore } };
            bubble.root.AddToClassList("bubble");

            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList("bubble-box");
            bubble.badge = new Label(skill.ToString().Substring(0, 1)) { pickingMode = PickingMode.Ignore };
            bubble.badge.AddToClassList("skill-badge");
            bubble.badge.AddToClassList("skill-badge--" + skill.ToString().ToLowerInvariant());
            bubble.text = new Label { pickingMode = PickingMode.Ignore };
            bubble.text.AddToClassList("bubble-text");
            box.Add(bubble.badge);
            box.Add(bubble.text);

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

        void ShowPopup(int amount, string reason)
        {
            var label = new Label((amount >= 0 ? "+" : "") + MoneyText.Short(amount) + (string.IsNullOrEmpty(reason) ? "" : "  " + reason))
            {
                pickingMode = PickingMode.Ignore,
            };
            label.AddToClassList("popup");
            label.style.top = popupStack.childCount * 30; // stack popups that arrive together
            label.EnableInClassList("popup--negative", amount < 0);
            popupStack.Add(label);
            label.schedule.Execute(() => label.AddToClassList("popup--leaving")).StartingIn(40);
            label.schedule.Execute(label.RemoveFromHierarchy).StartingIn(1500);
        }

        void ShowToast(string message, bool bad)
        {
            while (toasts.childCount >= 4) toasts.RemoveAt(0);
            var toast = new Label(message) { pickingMode = PickingMode.Ignore };
            toast.AddToClassList("toast");
            toast.EnableInClassList("toast--bad", bad);
            toasts.Add(toast);
            toast.schedule.Execute(() => toast.AddToClassList("toast--leaving")).StartingIn(3500);
            toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(4100);
        }

        static void SetText(Label label, string text)
        {
            if (label.text != text) label.text = text;
        }
    }
}
