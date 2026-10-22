using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The in-game HUD (UI Toolkit, Assets/_Game/UI/Hud.uxml + Game.uss). Quiet by default
    /// (docs/research/hud-less-is-more.md): only money, the park level, the clock and a one-line mission are always on.
    /// Everything else (<see cref="Reveal"/>) pops in for a few seconds when something rare happens (a level-up, a new
    /// half star, a lost customer, the morning's event) and is all there while you hold Alt (peek) or open the journal
    /// (J). Also: the wallet tide and +/- popups, the wave machine panel near a pool, crosshair and [E] prompt, toasts,
    /// and speech bubbles over surfers. H hides the panels (GameSettings.CleanHud).
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
            public bool famous;
            public VisualElement root;
            public Label badge;
            public Label text;
            public VisualElement patience;
            public VisualElement patienceFill;
            public VisualElement face; // colorblind marks: smile / meh / frown next to the patience bar
        }

        VisualElement hudRootElement;
        VisualElement root, bubbleLayer, popupStack, toasts, crosshair, prompt, waveBarFill, rentalRow, wagesRow, billsRow;
        Label money, ratingLabel, waveType, waveStatus, waveAuto, waveWater, ticketValue, rentalValue, servedValue, lostValue,
            wagesValue, billsValue, promptLabel, clickToPlay, hint, dayLabel;
        StarRating stars;
        SetCallHud setCall;
        ToastStack toastStack;
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
        Label incomeRate, goalName, goalAmount, goalKicker, levelLabel, xpLabel, levelNext, eventLabel, trashValue;
        VisualElement trashRow, challengeCard, challengeList, wavePanel, lostRow;
        bool journalOpen;
        float missionShownAt;
        Label challengeReward;
        float nextChallengeRefresh;
        ParkLevel subscribedLevel;
        VisualElement celebration;
        Label celebrationTitle, celebrationSubtitle;
        // Big banners (missions, level-ups, new pools) take turns instead of covering each other.
        readonly Queue<System.Action> banners = new();
        float bannerFreeAt;
        float shownMoney;
        readonly Queue<(float time, int amount)> recentIncome = new();
        int incomeLastMinute;

        /// <summary>Park earnings over the last minute (the "+$/min" readout).</summary>
        public static int IncomePerMinute { get; private set; }

        VisualElement promptBox, tide, tideCrest, servedRow, missionLine, parkPanel;
        Label promptKey, missionLineText;
        bool promptUp;
        string promptKeyName;
        float tideLevel, tideSlosh, tidePhase;
        bool tideFull;
        int tideDay = -1;

        /// <summary>Test hook: acts as if Alt (peek) is held (simulated keys don't reach the game).</summary>
        public static bool PeekOverride;
        bool peek;
        bool lastExpanded;
        Reveal incomeReveal, levelReveal, ratingReveal, eventReveal, challengeReveal, goalReveal, parkReveal, waveReveal;
        Reveal ticketReveal, servedReveal, lostReveal, trashReveal, billsReveal;
        int lastXpStep = -1, lastHalfStars = -1, eventDay = -1, lostSeen = -1, trashStep;
        string challengeSignature, goalId;

        /// <summary>The full HUD: journal open (J) or Alt held (peek).</summary>
        bool Expanded => journalOpen || peek;

        /// <summary>
        /// A HUD piece that stays hidden until something worth seeing happens: <see cref="Ping"/> shows it for a few
        /// seconds (it pops in), and it's always up while the HUD is expanded or while <c>keep</c> is true.
        /// </summary>
        class Reveal
        {
            readonly VisualElement element;
            float until = -1f, pingedAt = -10f;
            bool shown;

            public Reveal(VisualElement element)
            {
                this.element = element;
                shown = element != null && !element.ClassListContains("hidden");
            }

            public bool Shown => shown;

            /// <summary>Pinged in the last second (for a highlight).</summary>
            public bool Fresh => Time.unscaledTime - pingedAt < 1f;

            public void Ping(float seconds)
            {
                until = Mathf.Max(until, Time.unscaledTime + seconds);
                pingedAt = Time.unscaledTime;
            }

            public void Update(bool allowed, bool expanded, bool keep = false)
            {
                if (element == null) return;
                bool on = allowed && (expanded || keep || Time.unscaledTime < until);
                if (on == shown) return;
                shown = on;
                if (on) UiMotion.Enter(element, "hud-reveal--enter");
                else element.AddToClassList("hidden");
            }
        }

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
            waveWater = root.Q<Label>("waveWater");
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
            wavePanel = root.Q("wavePanel");
            lostRow = root.Q("lostRow");
            challengeCard = root.Q("challengeCard");
            challengeList = root.Q("challengeList");
            challengeReward = root.Q<Label>("challengeReward");
            xpLabel = root.Q<Label>("xpLabel");
            levelNext = root.Q<Label>("levelNext");
            goalKicker = root.Q<Label>("goalKicker");
            xpFill = root.Q("xpFill");
            promptBox = root.Q("promptBox");
            promptKey = root.Q<Label>("promptKey");
            tide = root.Q("tide");
            tideCrest = root.Q("tideCrest");
            servedRow = root.Q("servedRow");
            missionLine = root.Q("missionLine");
            missionLineText = root.Q<Label>("missionLineText");
            incomeReveal = new Reveal(incomeRate);
            levelReveal = new Reveal(root.Q("levelRow"));
            ratingReveal = new Reveal(root.Q("ratingRow"));
            eventReveal = new Reveal(eventLabel);
            challengeReveal = new Reveal(challengeCard);
            goalReveal = new Reveal(goalStrip);
            parkReveal = new Reveal(root.Q("parkPanel"));
            waveReveal = new Reveal(wavePanel);
            ticketReveal = new Reveal(root.Q("ticketRow"));
            servedReveal = new Reveal(servedRow);
            lostReveal = new Reveal(lostRow);
            trashReveal = new Reveal(trashRow);
            billsReveal = new Reveal(billsRow);

            missionCard = root.Q("missionCard");
            missionObjectives = root.Q("missionObjectives");
            missionMarker = root.Q("missionMarker");
            missionChapter = root.Q<Label>("missionChapter");
            missionTitle = root.Q<Label>("missionTitle");
            missionStory = root.Q<Label>("missionStory");
            missionHint = root.Q<Label>("missionHint");
            markerDistance = root.Q<Label>("markerDistance");
            missionBanner = root.Q<Label>("missionBanner");
            celebration = root.Q("celebrationBanner");
            celebrationTitle = root.Q<Label>("celebrationTitle");
            celebrationSubtitle = root.Q<Label>("celebrationSubtitle");

            stars = new StarRating { OnDark = true };
            root.Q("ratingStarsSlot").Add(stars);
            setCall = new SetCallHud(root);
            toastStack = new ToastStack(toasts);

            Notifications.Posted += ShowToast;
            Celebration.Shown += ShowCelebration;
            PlayerInteractor.Refused += ShakePrompt;
            GameSettings.Changed += ApplyClean;
            ApplyClean();
        }

        void OnDisable()
        {
            Notifications.Posted -= ShowToast;
            Celebration.Shown -= ShowCelebration;
            PlayerInteractor.Refused -= ShakePrompt;
            GameSettings.Changed -= ApplyClean;
            banners.Clear();
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
            if (banners.Count > 0 && Time.unscaledTime >= bannerFreeAt)
            {
                bannerFreeAt = Time.unscaledTime + 3.4f;
                banners.Dequeue()();
            }
            if (wallet == null && Wallet.Instance != null)
            {
                wallet = Wallet.Instance;
                wallet.Changed += ShowPopup;
                wallet.Changed += TrackIncome;
                shownMoney = wallet.Money;
            }

            UpdateMoneyAndRating();
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame && !InputBlocker.Blocked) journalOpen = !journalOpen;
            // Peek: hold Alt to see the whole HUD for a moment.
            peek = PeekOverride || (keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed) && !InputBlocker.Blocked);
            if (Expanded != lastExpanded)
            {
                lastExpanded = Expanded;
                nextChallengeRefresh = 0f;
                nextMissionRefresh = 0f;
            }
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame && !InputBlocker.Blocked)
            {
                GameSettings.SetCleanHud(!GameSettings.CleanHud);
                Notifications.Post(GameSettings.CleanHud ? "HUD hidden. Press H to bring it back." : "HUD back.", Notice.Info, false);
            }
            UpdateTide();
            UpdateLevel();
            UpdateGoal();
            UpdateChallenges();
            UpdateWavePanel();
            setCall?.Update(ShownMachine);
            toastStack?.Update();
            UpdateStats();
            UpdatePrompt();
            UpdateBubbles();
            UpdateMission();
            var surfing = PlayerSurfing.Instance;
            hudRootElement ??= root.Q("hudRoot");
            hudRootElement?.EnableInClassList("hud-root--surfing", surfing != null && surfing.IsSurfing);
            if (surfing != null && surfing.IsSurfing)
            {
                SetText(hint, surfing.Hint);
                hint.RemoveFromClassList("hint--hidden");
            }
            else
            {
                SetText(hint, "WASD move   ·   Shift sprint   ·   E use   ·   hold Alt: full HUD   ·   J journal   ·   Tab menu");
                // Only on the very first day (the keys are in the pause menu and Settings after that).
                bool firstDay = DayCycle.Instance == null || DayCycle.Instance.Day <= 1;
                hint.EnableInClassList("hint--hidden", !firstDay || Time.timeSinceLevelLoad > hintSeconds);
            }
        }

        void UpdateChallenges()
        {
            // Today's challenges: in the journal, and for a few seconds whenever one moves forward.
            var challenges = DailyChallenges.Instance;
            challengeReveal.Update(challenges != null && challenges.Today.Count > 0, Expanded);
            challengeCard.EnableInClassList("hud-compact", !Expanded);
            if (challenges == null || Time.unscaledTime < nextChallengeRefresh) return;
            nextChallengeRefresh = Time.unscaledTime + 0.5f;
            int done = 0;
            var signature = new System.Text.StringBuilder();
            foreach (var challenge in challenges.Today)
            {
                if (challenge.done) done++;
                signature.Append(challenge.done).Append(challenge.Progress).Append(',');
            }
            string now = signature.ToString();
            if (challengeSignature != null && now != challengeSignature) challengeReveal.Ping(4f);
            challengeSignature = now;
            SetText(challengeReward, Expanded
                ? $"{MoneyText.Format(challenges.Reward)} + 30 XP each"
                : $"{done}/{challenges.Today.Count} done  ·  J for details");
            challengeList.EnableInClassList("hidden", !Expanded);
            challengeList.Clear();
            foreach (var challenge in challenges.Today)
            {
                var row = new VisualElement();
                row.AddToClassList("objective-row");
                var box = new VisualElement();
                box.AddToClassList("objective-check");
                box.EnableInClassList("objective-check--done", challenge.done);
                var label = new Label(challenge.text + (challenge.target > 1 && !challenge.done ? $" ({challenge.Progress}/{challenge.target})" : ""));
                label.AddToClassList("objective-text");
                label.EnableInClassList("objective-text--done", challenge.done);
                row.Add(box);
                row.Add(label);
                challengeList.Add(row);
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
            // The XP bar shows up when you level up or pass another quarter of the way to the next level.
            int step = park.Level * 4 + Mathf.FloorToInt(park.Progress * 4f);
            if (lastXpStep >= 0 && step > lastXpStep) levelReveal.Ping(4.5f);
            lastXpStep = step;
            levelReveal.Update(true, Expanded);
            SetText(xpLabel, $"Level {park.Level}  ·  {park.XpToNext:#,0} XP to level {park.Level + 1}");
            xpFill.style.width = Length.Percent(park.Progress * 100f);
            // What's coming: the next level that brings something (an upgrade or a perk pick).
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            string next = LevelRoadmap.Next(shop, park.Level, out int at, 2);
            SetText(levelNext, next == null ? "" : $"Level {at}: {next}");
        }

        void ShowLevelUp(int level)
        {
            levelReveal.Ping(6f); // with the "next level brings..." line, it's a carrot right after a level-up
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            var unlocked = shop != null ? shop.UnlockedAt(level) : new List<string>();
            // The banner, fanfare and fireworks come from Celebration; the toast keeps a record of what unlocked.
            Notifications.Post(unlocked.Count > 0 ? $"Park level {level}: unlocked {string.Join(", ", unlocked)}" : $"Park level {level}!", Notice.Reward);
        }

        /// <summary>The upgrade the current mission wants next (following "needs X first" to X), or null.</summary>
        Upgrade PlanUpgrade()
        {
            var mission = MissionManager.Instance != null ? MissionManager.Instance.Current : null;
            if (mission == null) return null;
            foreach (var objective in mission.objectives)
            {
                if (objective.Done || string.IsNullOrEmpty(objective.upgradeId)) continue;
                var upgrade = FindUpgrade(objective.upgradeId);
                for (int guard = 0; upgrade != null && !upgrade.owned && !shop.RequirementMet(upgrade) && guard < 8; guard++)
                    upgrade = FindUpgrade(upgrade.requires);
                if (upgrade != null && !upgrade.owned) return upgrade;
            }
            return null;
        }

        Upgrade FindUpgrade(string id)
        {
            foreach (var upgrade in shop.Upgrades)
                if (upgrade.id == id) return upgrade;
            return null;
        }

        float DistanceToPool(PoolArea pool)
        {
            var rect = pool.Footprint(0f);
            var p = new Vector2(player.transform.position.x, player.transform.position.z);
            float dx = Mathf.Max(rect.xMin - p.x, 0f, p.x - rect.xMax);
            float dy = Mathf.Max(rect.yMin - p.y, 0f, p.y - rect.yMax);
            return Mathf.Sqrt(dx * dx + dy * dy);
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
        /// <summary>
        /// The goal strip follows the plan: the upgrade the current mission asks for (or what it needs first, or the
        /// park level it needs), so the mission card and the strip never point at different things. Only when the
        /// mission isn't about an upgrade does it fall back to the cheapest useful upgrade.
        /// </summary>
        void UpdateGoal()
        {
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            if (shop == null || wallet == null) return;
            Upgrade next = PlanUpgrade();
            bool plan = next != null;
            if (next == null)
                foreach (var upgrade in shop.Upgrades)
                    if (!upgrade.owned && shop.RequirementMet(upgrade) && shop.LevelMet(upgrade) && (next == null || upgrade.cost < next.cost))
                        next = upgrade;
            // The mission line already shows Kai's plan (with its price), so the strip is for the side goal: up for 6 s
            // when it changes, and while you can afford it ("Ready!"). Peek / journal shows it either way.
            bool blocked = next == null || shop.IsOpen || BuildMode.Active || FishingSpot.Fishing;
            var park = ParkLevel.Instance;
            bool levelGated = next != null && park != null && !shop.LevelMet(next);
            bool ready = next != null && !levelGated && wallet.Money >= next.cost;
            string id = next?.id;
            if (id != goalId)
            {
                if (goalId != null && !plan) goalReveal.Ping(6f);
                goalId = id;
            }
            goalReveal.Update(!blocked, Expanded, !plan && ready);
            goalStrip.EnableInClassList("hud-compact", !Expanded);
            if (next == null) return;
            SetText(goalKicker, plan ? "KAI'S PLAN" : "NEXT GOAL");
            goalStrip.EnableInClassList("goal-strip--plan", plan);
            if (levelGated)
            {
                // The plan needs a higher park level first: show the XP still to earn.
                int from = ParkLevel.XpForLevel(park.Level), to = ParkLevel.XpForLevel(next.level);
                SetText(goalName, $"{next.title}  ·  park level {next.level}");
                SetText(goalAmount, $"{Mathf.Max(0, to - park.Xp):#,0} XP to go");
                goalFill.style.width = Length.Percent(Mathf.Clamp01((park.Xp - from) / (float)Mathf.Max(1, to - from)) * 100f);
                goalStrip.EnableInClassList("goal-strip--ready", false);
                return;
            }
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
                IncomePerMinute = incomeLastMinute;
                SetText(incomeRate, (incomeLastMinute >= 0 ? "+" : "") + MoneyText.Format(incomeLastMinute) + " / min");
            }
            incomeReveal.Update(true, Expanded); // peek / journal only: it changes every few seconds in a busy park
            if (DayCycle.Instance != null)
                SetText(dayLabel, $"Day {DayCycle.Instance.Day}  ·  {DayCycle.Instance.Clock}" + (DayCycle.Instance.ParkClosed
                    ? DayCycle.GuestsLeft > 0 ? $"  ·  CLOSED ({DayCycle.GuestsLeft} left)" : "  ·  CLOSED: end the day at the desk sign" : ""));
            var dayEvent = DailyEvents.Instance != null ? DailyEvents.Instance.Today : DayEvent.None;
            var wind = DailyEvents.TodayWind;
            // Today's event and wind: shown for the first 8 s of each day, then only on peek / journal.
            int day = DayCycle.Instance != null ? DayCycle.Instance.Day : 0;
            if (day != eventDay)
            {
                eventDay = day;
                eventReveal.Ping(8f);
            }
            eventReveal.Update(dayEvent != DayEvent.None || wind != Wind.Calm, Expanded);
            string today = dayEvent == DayEvent.None ? DailyEvents.WindTitle(wind)
                : wind == Wind.Calm ? DailyEvents.Title(dayEvent) : DailyEvents.Title(dayEvent) + "  ·  " + DailyEvents.WindTitle(wind);
            SetText(eventLabel, "Today: " + today);
            if (ParkReputation.Instance != null)
            {
                stars.Stars = ParkReputation.Instance.Stars;
                SetText(ratingLabel, ParkReputation.Instance.Stars.ToString("0.0"));
                // The stars show up when the rating reaches a new half star (up or down).
                int halves = Mathf.RoundToInt(ParkReputation.Instance.Stars * 2f);
                if (lastHalfStars >= 0 && halves != lastHalfStars) ratingReveal.Ping(4f);
                lastHalfStars = halves;
            }
            ratingReveal.Update(ParkReputation.Instance != null, Expanded);
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
                    best = pool.Machine.Leader; // a Cove's two sides are run from one console
                    bestDistance = distance;
                }
            }
            return best;
        }

        /// <summary>Within a few steps of one of this machine's console buttons.</summary>
        bool AtConsole(WaveMachine machine)
        {
            foreach (var button in WaveButton.All)
                if (button.Machine != null && button.Machine.Leader == machine.Leader
                    && (button.transform.position - player.transform.position).sqrMagnitude < 7f * 7f)
                    return true;
            return false;
        }

        /// <summary>The machine at the pool you're at (the SET INCOMING call follows it), or null away from pools.</summary>
        public WaveMachine ShownMachine { get; private set; }

        void UpdateWavePanel()
        {
            var machine = NearestMachine();
            // Only shown near a pool (or while surfing): the rest of the time the HUD stays clear.
            bool surfing = PlayerSurfing.Instance != null && PlayerSurfing.Instance.IsSurfing;
            bool near = machine != null && player != null && machine.Pool != null && (DistanceToPool(machine.Pool) < 18f || surfing);
            ShownMachine = near ? machine : null; // the SET INCOMING call follows the pool you're at
            // The panel itself only when you stand at the console, surf, need to service the machine, or peek / journal.
            bool needsService = near && machine.Service != null && machine.Service.NeedsService;
            waveReveal.Update(near, Expanded, surfing || needsService || AtConsole(machine));
            wavePanel.EnableInClassList("hud-compact", !Expanded);
            if (!near) return;
            var selected = machine.Selected;
            string direction = selected == WaveLevel.Beginner ? "Whitewater"
                : machine.HasTwin ? "Rights + lefts" // a Cove: both sides of the pier at once
                : machine.SelectedDirection == WaveDirection.Peak ? "Peak" : machine.SelectedDirection.ToString();
            SetText(waveType, machine.Preset != null ? $"{machine.Preset.name}  ·  {selected}" : $"{selected}  ·  {direction}");
            waveType.EnableInClassList("wave-type--beginner", selected == WaveLevel.Beginner);
            waveType.EnableInClassList("wave-type--intermediate", selected == WaveLevel.Intermediate);
            waveType.EnableInClassList("wave-type--pro", selected == WaveLevel.Pro);

            waveBarFill.style.width = Length.Percent(machine.ReadyProgress * 100f);
            waveBarFill.EnableInClassList("bar-fill--charging", !machine.Ready);
            var service = machine.Service;
            bool alert = service != null && service.NeedsService;
            // Just the wave and its charge bar; the lines below come up when they need you (service, choppy water)
            // or while you peek (Alt) / open the journal (J).
            waveStatus.EnableInClassList("hidden", !(alert || Expanded));
            waveAuto.EnableInClassList("hidden", !Expanded);
            waveStatus.EnableInClassList("small-text--alert", alert);
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

            // Leftover chop: waves bouncing off the beach. Give it a lull and the water goes glassy again.
            float chop = machine.Pool.Water != null ? machine.Pool.Water.Chop : 0f;
            SetText(waveWater, chop < 0.15f ? "Water: glassy" : chop < 0.45f ? "Water: settling..." : "Water: choppy - give it a lull");
            waveWater.EnableInClassList("hidden", !(chop >= 0.45f || Expanded));
            waveWater.EnableInClassList("water-line--glassy", chop < 0.15f);
            waveWater.EnableInClassList("water-line--choppy", chop >= 0.45f);
        }

        void UpdateStats()
        {
            if (pricing != null)
            {
                SetText(ticketValue, SessionPrices());
                SetText(rentalValue, MoneyText.Format(pricing.RentalPrice));
            }
            // The Park panel is empty most of the time: rows come in on peek / journal, lost customers and piles of
            // trash pop in when they happen, and today's bills from 6 PM. The panel itself only shows with a row in it.
            ticketReveal.Update(true, Expanded);
            if (spawner != null)
            {
                if (lostSeen >= 0 && spawner.Lost > lostSeen) lostReveal.Ping(4f);
                lostSeen = spawner.Lost;
                servedReveal.Update(spawner.Served > 0, Expanded);
                lostReveal.Update(spawner.Lost > 0, Expanded);
                lostRow.EnableInClassList("stat-row--flash", lostReveal.Fresh);
                SetText(servedValue, spawner.Served.ToString());
                SetText(lostValue, spawner.Lost.ToString());
                lostValue.EnableInClassList("stat-value--bad", spawner.Lost > 0);
            }
            if (payroll != null)
            {
                int wages = payroll.WagesPerMinute;

                SetText(wagesValue, MoneyText.Format(wages) + "/min");
            }
            int trash = Trash.All.Count;
            if (trash / 5 > trashStep && trash >= 5) trashReveal.Ping(4f); // every 5 more pieces of trash
            trashStep = trash / 5;
            trashReveal.Update(trash > 0, Expanded);
            trashRow.EnableInClassList("stat-row--flash", trashReveal.Fresh);
            bool evening = DayCycle.Instance != null && (DayCycle.Instance.Progress >= 10f / 12f || DayCycle.Instance.ParkClosed);
            billsReveal.Update(true, Expanded, evening);
            parkReveal.Update(true, false, ticketReveal.Shown || servedReveal.Shown || lostReveal.Shown || trashReveal.Shown || billsReveal.Shown);
            parkPanel ??= root.Q("parkPanel");
            parkPanel.EnableInClassList("hud-compact", !Expanded);
            SetText(trashValue, trash.ToString());
            trashValue.EnableInClassList("stat-value--bad", trash > 5);
            var costs = OperatingCosts.Instance;
            if (costs != null && DayCycle.Instance != null)
                SetText(billsValue, MoneyText.Format(costs.EstimateToday(DayCycle.Instance.IncomeToday)));
        }

        void ApplyClean() => root?.Q("hudRoot")?.EnableInClassList("hud-root--clean", GameSettings.CleanHud);

        /// <summary>
        /// The wallet tide: water behind the money rises with today's takings toward your best day so far, sloshes
        /// when money comes in, and foams over (once a day) when you beat it.
        /// </summary>
        void UpdateTide()
        {
            var day = DayCycle.Instance;
            if (tide == null || day == null) return;
            if (day.Day != tideDay)
            {
                tideDay = day.Day;
                tideFull = false;
            }
            int goal = Mathf.Max(GameStats.BestDayIncome, 200);
            float dt = Time.unscaledDeltaTime;
            tideLevel = Mathf.MoveTowards(tideLevel, Mathf.Clamp01(day.IncomeToday / (float)goal), dt * 0.25f);
            tideSlosh = Mathf.MoveTowards(tideSlosh, 0f, dt * 1.2f);
            tidePhase = (tidePhase + dt / 4f * (1f + 2.5f * tideSlosh)) % 1f;
            float t = Time.unscaledTime;
            float bob = Mathf.Sin(t * 1.7f) * 2f + Mathf.Sin(t * 8f) * 7f * tideSlosh;
            tide.style.height = Length.Percent(Mathf.Clamp(tideLevel * 100f + bob, 0f, 100f));
            tideCrest.style.translate = new Translate(Length.Percent(-50f * tidePhase), 0f);
            bool full = GameStats.BestDayIncome > 0 && day.IncomeToday >= goal;
            tide.EnableInClassList("tide--full", full);
            if (full && !tideFull)
            {
                tideFull = true;
                tideSlosh = 1f;
                Notifications.Post($"Best day ever! You just beat your record of {MoneyText.Format(goal)} (the tide in your wallet).", Notice.Reward);
                Sfx.Play(Sound.CashBig);
            }
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

        /// <summary>
        /// The [E] prompt: pops in when you look at something, cross-fades when its action changes, shows HOLD on the
        /// key cap for hold actions, and goes sandy (and shakes if you press anyway) when it can't be used yet.
        /// </summary>
        void UpdatePrompt()
        {
            bool menuOpen = InputBlocker.Blocked;
            bool cursorFree = player != null && !player.CursorLocked && !menuOpen;
            var target = interactor != null && !menuOpen ? interactor.Target : null;
            string text = target?.Prompt;
            bool show = !string.IsNullOrEmpty(text) && !cursorFree;

            crosshair.EnableInClassList("hidden", menuOpen || cursorFree);
            crosshair.EnableInClassList("crosshair--active", !string.IsNullOrEmpty(text));
            if (show)
            {
                bool hold = text.StartsWith("Hold E");
                string label = hold ? HoldLabel(text) : text;
                promptKeyName ??= UnityEngine.InputSystem.Keyboard.current != null ? UnityEngine.InputSystem.Keyboard.current.eKey.displayName : "E";
                SetText(promptKey, hold ? "HOLD " + promptKeyName : promptKeyName);
                promptBox.EnableInClassList("prompt-box--no", !target.CanUse);
                if (!promptUp) UiMotion.Enter(prompt, "prompt--enter");
                else if (WithoutDigits(promptLabel.text) != WithoutDigits(label)) UiMotion.Bump(promptLabel, "prompt-label--swap", 70);
                SetText(promptLabel, label);
            }
            else prompt.AddToClassList("hidden");
            promptUp = show;
            clickToPlay.EnableInClassList("hidden", !cursorFree);
        }

        /// <summary>"Hold E to repair the machine" -> "Repair the machine" (the key cap already says HOLD E).</summary>
        static string HoldLabel(string text)
        {
            string rest = text.Substring(6).TrimStart();
            if (rest.StartsWith("to ")) rest = rest.Substring(3);
            return rest.Length > 0 ? char.ToUpperInvariant(rest[0]) + rest.Substring(1) : text;
        }

        /// <summary>Countdowns ("charging... 3") change every second; only a new action should cross-fade.</summary>
        static string WithoutDigits(string text)
        {
            var chars = new System.Text.StringBuilder(text.Length);
            foreach (char c in text) if (!char.IsDigit(c)) chars.Append(c);
            return chars.ToString();
        }

        /// <summary>Pressed E on something that can't be used yet: the prompt shakes its head.</summary>
        void ShakePrompt()
        {
            if (promptBox == null || GameSettings.ReduceMotion) return;
            Sfx.Play(Sound.UiClose, null, 0.9f, 0.85f);
            string[] steps = { "prompt-box--shake-left", "prompt-box--shake-right", "prompt-box--shake-left", null };
            for (int i = 0; i < steps.Length; i++)
            {
                string step = steps[i];
                promptBox.schedule.Execute(() =>
                {
                    promptBox.RemoveFromClassList("prompt-box--shake-left");
                    promptBox.RemoveFromClassList("prompt-box--shake-right");
                    if (step != null) promptBox.AddToClassList(step);
                }).StartingIn(i * 40);
            }
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

                string status = surfer.Status ?? surfer.NameTag;
                bool hasText = !string.IsNullOrEmpty(status);
                if (surfer.Famous != null && !bubble.famous)
                {
                    bubble.famous = true;
                    bubble.root.AddToClassList("bubble--famous");
                    bubble.badge.text = "★";
                }
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
                bubble.face.EnableInClassList("hidden", !surfer.ShowPatience);
                bubble.face.EnableInClassList("icon--smile", patience >= 0.6f);
                bubble.face.EnableInClassList("icon--meh", patience < 0.6f && patience >= 0.3f);
                bubble.face.EnableInClassList("icon--frown", patience < 0.3f);
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
            if (mission != shownMission) missionShownAt = Time.unscaledTime;
            shownMission = mission;
            // One line (what to do right now) unless the mission just started (8 s), or you peek (Alt) / open the journal (J).
            bool details = Expanded || Time.unscaledTime - missionShownAt < 8f;
            missionCard.EnableInClassList("mission-card--compact", !details);
            SetText(missionLineText, MissionLine(mission));
            SetText(missionChapter, mission.chapter.ToUpperInvariant());
            SetText(missionTitle, mission.title);
            SetText(missionStory, mission.story);
            SetText(missionHint, "Tip: " + mission.hint + (mission.reward > 0 ? "   Reward: " + MoneyText.Format(mission.reward) : ""));
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

        /// <summary>"Buy the Turbo Pump  ·  $40 / $75   1/3": the next step, its money if it's an upgrade, and how far along.</summary>
        string MissionLine(Mission mission)
        {
            Objective next = null;
            int done = 0;
            foreach (var objective in mission.objectives)
            {
                if (objective.Done) done++;
                else next ??= objective;
            }
            if (next == null) return mission.title;
            string line = next.text + next.Progress;
            var upgrade = shop != null ? PlanUpgrade() : null;
            if (upgrade != null && wallet != null)
            {
                if (!shop.LevelMet(upgrade)) line += $"  ·  park level {upgrade.level}";
                else if (upgrade.id != next.upgradeId) line += $"  ·  first: {upgrade.title}";
                else line += wallet.Money >= upgrade.cost ? "  ·  ready at the computer!"
                    : $"  ·  {MoneyText.Format(Mathf.Max(0, wallet.Money))} / {MoneyText.Format(upgrade.cost)}";
            }
            if (mission.objectives.Count > 1) line += $"   {done}/{mission.objectives.Count}";
            return line;
        }

        void ShowMissionBanner(Mission mission) =>
            ShowBanner("MISSION COMPLETE!\n" + mission.title + (mission.reward > 0 ? "   +" + MoneyText.Format(mission.reward) : ""));

        void ShowBanner(string text) => banners.Enqueue(() => PlayBanner(text));

        void PlayBanner(string text)
        {
            missionBanner.text = text;
            missionBanner.RemoveFromClassList("mission-banner--leaving");
            missionBanner.RemoveFromClassList("hidden");
            missionBanner.schedule.Execute(() => missionBanner.AddToClassList("mission-banner--leaving")).StartingIn(2500);
            missionBanner.schedule.Execute(() => missionBanner.AddToClassList("hidden")).StartingIn(3200);
        }

        void ShowCelebration(string title, string subtitle, bool big) => banners.Enqueue(() => PlayCelebration(title, subtitle, big));

        /// <summary>The big party banner: a bouncing title over a ribbon (teal for new pools, coral for level-ups).</summary>
        void PlayCelebration(string title, string subtitle, bool big)
        {
            if (celebration == null) return;
            celebrationTitle.text = title;
            celebrationSubtitle.text = subtitle;
            celebration.EnableInClassList("celebration--big", big);
            celebration.RemoveFromClassList("celebration--leaving");
            UiMotion.Enter(celebration, "celebration--enter");
            celebration.schedule.Execute(() => celebration.AddToClassList("celebration--leaving")).StartingIn(2900);
            celebration.schedule.Execute(() => celebration.AddToClassList("hidden")).StartingIn(3400);
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
            bubble.face = new VisualElement { pickingMode = PickingMode.Ignore };
            bubble.face.AddToClassList("icon");
            bubble.face.AddToClassList("patience-face");

            bubble.root.Add(box);
            bubble.root.Add(tail);
            bubble.root.Add(bubble.patience);
            bubble.root.Add(bubble.face);
            bubbleLayer.Add(bubble.root);
            return bubble;
        }

        void ShowPopup(int amount, string reason)
        {
            if (amount > 0)
            {
                UiMotion.Bump(money, "money-label--bump");
                tideSlosh = Mathf.Max(tideSlosh, 0.6f);
            }
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

        void ShowToast(string message, Notice kind) => toastStack?.Post(message, kind);

        static void SetText(Label label, string text)
        {
            if (label.text != text) label.text = text;
        }
    }
}
