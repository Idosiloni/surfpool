using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Custom Board shaper: the slow, premium one. A customer orders a board to their wish (shape, color, fins) and
    /// pays a deposit, then waits on the bench. You put a foam blank on the stand, pick the shape's template,
    /// hold E to plane it into a board (foam dust flies), spray it their color, fit their fins, and hand it over
    /// for the rest of the money. One board at a time. Big money per board; a quick job earns a big tip.
    /// </summary>
    public class BoardShaper : ShopCounter
    {
        public const string Blank = "blank", Stand = "stand";

        [Header("Shaper")]
        [SerializeField] int shortboardPrice = 900;
        [SerializeField] int funboardPrice = 750;
        [SerializeField] int quadExtra = 50;
        [Range(0f, 1f)] [SerializeField] float depositShare = 0.4f;
        [SerializeField] int fullTip = 120;
        [Tooltip("Finish within this many seconds of taking the order for the full tip.")]
        [SerializeField] float fastSeconds = 70f;
        [SerializeField] float shapeSeconds = 4f;
        [Tooltip("How long a customer waits on the bench for their board.")]
        [SerializeField] float benchPatience = 240f;
        [SerializeField] Transform bench;
        [Tooltip("Top of the shaping stand (boards lie along its X axis).")]
        [SerializeField] Transform standTop;
        [SerializeField] GameObject blankModel;
        [SerializeField] GameObject shortboardModel;
        [SerializeField] GameObject funboardModel;
        [SerializeField] ParticleSystem foamDust;
        [SerializeField] ParticleSystem paintMist;

        public static readonly (string id, string label, Color color)[] Colors =
        {
            ("paint_coral", "Coral", new Color(0.95f, 0.36f, 0.45f)),
            ("paint_teal", "Teal", new Color(0.16f, 0.72f, 0.68f)),
            ("paint_sun", "Sunny yellow", new Color(1f, 0.8f, 0.3f)),
            ("paint_purple", "Purple", new Color(0.55f, 0.35f, 0.8f)),
            ("paint_navy", "Navy", new Color(0.12f, 0.2f, 0.38f)),
        };

        Shopper client;          // whose board is being made
        bool hasJob;
        float jobStarted;
        int depositPaid;
        GameObject onStand;
        bool blankPlaced, shaped;
        string template;          // "short" or "fun" once picked
        float shapeProgress;
        float nextPlanerSound;

        public bool JobActive => client != null;
        public Shopper Client => client;
        float ShapeSeconds => shapeSeconds * ParkEffects.ShapeTimeMultiplier;
        protected override float WorkerSpeedFactor => 0.5f + 0.5f * ParkEffects.ShapeTimeMultiplier;
        public override float JobProgress => blankPlaced && template != null && !shaped && shapeProgress > 0f ? shapeProgress / ShapeSeconds : -1f;
        public override ShopOrder TicketOrder => client != null ? client.Order : Front?.Order;
        public override string Hint => client != null ? NextStep : Front != null ? "Take their order at the counter (E)" : null;
        /// <summary>The next thing to do on the current job (for the HUD).</summary>
        public string NextStep
        {
            get
            {
                if (client == null) return null;
                if (!blankPlaced) return "Put a foam blank on the stand";
                if (template == null) return "Pick the shape's template (back wall)";
                if (!shaped) return "Hold E on the blank to shape it";
                if (!Done("color")) return "Spray it their color";
                if (!Done("fins")) return "Fit their fins";
                return "Hand the board over at the counter";
            }
        }

        public static Color ColorOf(string paintId) => System.Array.Find(Colors, c => c.id == paintId).color;

        public override ShopOrder NewOrder()
        {
            bool shortboard = Random.value < 0.55f;
            var color = Colors[Random.Range(0, Colors.Length)];
            bool quad = Random.value < 0.4f;
            var order = new ShopOrder { title = "Custom board" };
            order.Add("shape", shortboard ? "Shortboard" : "Funboard", shortboard ? new Color(0.85f, 0.85f, 0.8f) : new Color(0.75f, 0.85f, 0.95f));
            order.Add("color", color.label, color.color);
            order.Add("fins", quad ? "Quad fins" : "Thruster fins", new Color(0.4f, 0.42f, 0.48f));
            order.price = (shortboard ? shortboardPrice : funboardPrice) + (quad ? quadExtra : 0);
            return order;
        }

        bool Done(string id) => client != null && client.Order.Find(id) is { done: true };
        bool Wants(string itemId, string label) => client != null && client.Order.Find(itemId)?.label == label;

        int Deposit(ShopOrder order) => Mathf.RoundToInt(order.price * depositShare / 10f) * 10;

        bool BoardReady => client != null && client.Order.AllDone;

        public override string Prompt
        {
            get
            {
                if (BoardReady) return $"Hand over {client.Order.items[0].label.ToLowerInvariant()} (+{MoneyText.Format(client.Order.price - depositPaid)})";
                var customer = Front;
                if (client != null) return customer != null ? "Finish the current board first" : $"Working on a {client.Order.items[0].label.ToLowerInvariant()}";
                if (customer == null) return $"{shopName} (no one waiting)";
                return $"Take custom order (deposit {MoneyText.Format(Deposit(customer.Order))})";
            }
        }

        public override void Interact()
        {
            if (BoardReady)
            {
                HandOver(SpeedTip());
                return;
            }
            var customer = Front;
            if (client == null && customer != null) TakeOrder(customer);
        }

        int SpeedTip()
        {
            float took = Time.time - jobStarted;
            return took <= fastSeconds ? fullTip : took <= fastSeconds * 2f ? fullTip / 2 : 0;
        }

        void TakeOrder(Shopper customer)
        {
            client = customer;
            hasJob = true;
            jobStarted = Time.time;
            depositPaid = Deposit(customer.Order);
            Wallet.Instance.Add(depositPaid, "custom boards");
            Sfx.Play(Sound.ShopBell, transform.position);
            customer.Say("Can't wait!", 2f);
            customer.WaitAt(bench, benchPatience);
        }

        void HandOver(int tip)
        {
            var customer = client;
            var board = onStand;
            onStand = null;
            ClearJob();
            GameStats.CustomBoards++;
            Sfx.Play(Sound.CashBig);
            Sell(customer, customer.Order.price - depositPaid, tip, board, Shopper.CarryStyle.UnderArm, "My dream board!!");
            depositPaid = 0;
        }

        void ClearJob()
        {
            client = null;
            hasJob = false;
            blankPlaced = shaped = false;
            template = null;
            shapeProgress = 0f;
            if (onStand != null) Destroy(onStand);
            onStand = null;
        }

        public override void Leave(Shopper shopper)
        {
            base.Leave(shopper);
            // The client got tired of waiting on the bench: the deposit stays, the half-made board is scrapped.
            if (shopper != null && shopper == client && !shopper.IsWaiting) ClearJob();
        }

        protected override void Update()
        {
            base.Update();
            if (hasJob && client == null) ClearJob(); // the client vanished
            if (foamDust != null && foamDust.isEmitting && Time.time > lastShapeHold + 0.15f) foamDust.Stop();
        }

        float lastShapeHold;

        // ---- stations ----

        public override string StationPrompt(string id)
        {
            if (client == null) return id == Stand || id == Blank ? "Take a customer's order first" : null;
            switch (id)
            {
                case Blank: return blankPlaced ? null : "Put a foam blank on the stand";
                case Stand:
                    if (!blankPlaced) return "Shaping stand (needs a blank)";
                    if (template == null) return "Pick a template first";
                    return shaped ? null : "Hold to shape the blank";
                case "template_short": return template == null && blankPlaced ? "Use the shortboard template" : null;
                case "template_fun": return template == null && blankPlaced ? "Use the funboard template" : null;
                case "fins_thruster": return shaped && !Done("fins") ? "Fit thruster fins (3)" : null;
                case "fins_quad": return shaped && !Done("fins") ? "Fit quad fins (4)" : null;
            }
            if (id.StartsWith("paint_")) return shaped && !Done("color") ? $"Spray it {System.Array.Find(Colors, c => c.id == id).label.ToLowerInvariant()}" : null;
            return null;
        }

        public override void UseStation(string id)
        {
            if (client == null) return;
            switch (id)
            {
                case Blank:
                    if (blankPlaced) return;
                    blankPlaced = true;
                    onStand = ShopArt.Spawn(blankModel, standTop, Vector3.zero, 1f, true, false);
                    Sfx.Play(Sound.Plop, standTop.position);
                    return;
                case "template_short":
                case "template_fun":
                    if (template != null || !blankPlaced) return;
                    string label = id == "template_short" ? "Shortboard" : "Funboard";
                    if (!Wants("shape", label))
                    {
                        Wrong($"They want a {client.Order.items[0].label.ToLowerInvariant()}!");
                        return;
                    }
                    template = id == "template_short" ? "short" : "fun";
                    Sfx.Play(Sound.Click, standTop.position);
                    return;
                case "fins_thruster":
                case "fins_quad":
                    if (!shaped || Done("fins")) return;
                    string fins = id == "fins_quad" ? "Quad fins" : "Thruster fins";
                    if (!Wants("fins", fins))
                    {
                        Wrong($"They asked for {client.Order.Find("fins").label.ToLowerInvariant()}!");
                        return;
                    }
                    client.Order.Find("fins").done = true;
                    Sfx.Play(Sound.Plop, standTop.position);
                    Bump();
                    return;
            }
            if (id.StartsWith("paint_") && shaped && !Done("color"))
            {
                var paint = System.Array.Find(Colors, c => c.id == id);
                if (!Wants("color", paint.label))
                {
                    Wrong($"Their wish card says {client.Order.Find("color").label.ToLowerInvariant()}!");
                    return;
                }
                client.Order.Find("color").done = true;
                ShopArt.Tint(onStand, "BoardDeck", paint.color);
                if (paintMist != null)
                {
                    var main = paintMist.main;
                    main.startColor = paint.color;
                    paintMist.Play();
                }
                Sfx.Play(Sound.SprayHiss, standTop.position);
                Bump();
            }
        }

        void Wrong(string line)
        {
            Sfx.Play(Sound.Error);
            Notifications.Post(line, Notice.Bad);
        }

        void Bump()
        {
            if (onStand != null && onStand.GetComponent<PopIn>() == null) onStand.AddComponent<PopIn>();
        }

        public override void HoldStation(string id, float deltaTime)
        {
            if (id != Stand || client == null || !blankPlaced || template == null || shaped) return;
            lastShapeHold = Time.time;
            shapeProgress += deltaTime;
            if (foamDust != null && !foamDust.isEmitting) foamDust.Play();
            if (Time.time >= nextPlanerSound)
            {
                nextPlanerSound = Time.time + 1f;
                Sfx.Play(Sound.Planer, standTop.position);
            }
            // The blank slims down as you plane it.
            float t = Mathf.Clamp01(shapeProgress / ShapeSeconds);
            if (onStand != null) onStand.transform.localScale = new Vector3(1f - 0.12f * t, 1f - 0.45f * t, 1f - 0.2f * t);
            if (t < 1f) return;
            shaped = true;
            if (onStand != null) Destroy(onStand);
            onStand = ShopArt.Spawn(template == "short" ? shortboardModel : funboardModel, standTop, new Vector3(0f, 0.04f, 0f), 1f, true, false);
            onStand.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            ShopArt.Tint(onStand, "BoardDeck", new Color(0.97f, 0.96f, 0.9f));
            client.Order.Find("shape").done = true;
            Sfx.Play(Sound.Plop, standTop.position);
            if (foamDust != null) foamDust.Stop();
        }

        // ---- the hired apprentice ----

        protected override Shopper WorkerTarget => client != null ? client : Front;

        protected override bool WorkerServe(Shopper customer)
        {
            if (customer == null) return false;
            if (client == null)
            {
                if (customer != Front) return false;
                TakeOrder(customer);
                return true;
            }
            // Finish the whole board in one go.
            var order = client.Order;
            if (onStand != null) Destroy(onStand);
            bool shortboard = order.Find("shape").label == "Shortboard";
            onStand = ShopArt.Spawn(shortboard ? shortboardModel : funboardModel, standTop, new Vector3(0f, 0.04f, 0f), 1f, true, false);
            onStand.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var paint = System.Array.Find(Colors, c => c.label == order.Find("color").label);
            ShopArt.Tint(onStand, "BoardDeck", paint.color);
            foreach (var item in order.items) item.done = true;
            HandOver(fullTip / 3);
            return true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearJob();
        }
    }
}
