using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A counter shop (Acai &amp; Shake Shack, Surf Gear, Custom Boards). Customers (<see cref="Shopper"/>) line up,
    /// the one at the front shows their order, and you make it with the shop's stations (<see cref="ShopStation"/>)
    /// and hand it over at the counter (E). Each shop plays differently; this base class does the line, the hired
    /// worker, paying and tips. A hired worker serves the front customer by themself after a short time.
    /// The counter itself is used through a <see cref="ShopRegister"/> on its collider.
    /// </summary>
    public abstract class ShopCounter : MonoBehaviour
    {
        public static readonly List<ShopCounter> All = new();

        /// <summary>A sale went through: where (over the counter), the price and the tip. The HUD floats "+$" there.</summary>
        public static event System.Action<Vector3, int, int> Sold;

        [SerializeField] protected string shopName = "Shop";
        [Tooltip("Reason shown on money pop-ups and in the day report.")]
        [SerializeField] protected string moneyReason = "shop";
        [Tooltip("Front of the line, at the counter.")]
        [SerializeField] Transform lineSpot;
        [Tooltip("Where customers walk to before joining the back of the line (sets which way the line grows).")]
        [SerializeField] Transform approach;
        [SerializeField] float queueSpacing = 1.1f;
        [SerializeField] int maxLine = 4;
        [Tooltip("Seconds a customer waits (in line and at the counter) before giving up.")]
        [SerializeField] float patience = 60f;
        [Tooltip("Seconds between customers arriving (random in this range).")]
        [SerializeField] Vector2 arrivalGap = new(15f, 25f);
        [Tooltip("The hired worker (inactive until hired).")]
        [SerializeField] protected GameObject worker;
        [Tooltip("Seconds the hired worker needs per order at 1 star.")]
        [SerializeField] protected float workerTime = 6f;
        [Tooltip("Size of the building (local x = width, y = depth) for walking routes to go around.")]
        [SerializeField] Vector2 footprint = new(4f, 3f);

        protected readonly List<Shopper> line = new();
        Shopper workerServing;
        float workerDoneAt;

        public string ShopName => shopName;
        public Vector2 ArrivalGap => arrivalGap;
        public float Patience => patience * ParkPerks.PatienceMultiplier;
        public bool Open => isActiveAndEnabled;
        public virtual bool WantsCustomer => Open && line.Count < maxLine;
        public Vector3 ApproachPoint => approach.position;
        public Shopper Front => line.Count > 0 && line[0].AtCounter ? line[0] : null;
        public bool StaffOnDuty => worker != null && worker.activeInHierarchy;
        /// <summary>0..1 progress of the worker's current order (for the HUD), or -1 when not working.</summary>
        public float WorkerProgress => StaffOnDuty && workerServing != null && workerServing == Front
            ? Mathf.Clamp01(1f - (workerDoneAt - Time.time) / WorkerSeconds) : -1f;

        protected virtual void OnEnable() => All.Add(this);

        protected virtual void OnDisable()
        {
            All.Remove(this);
            foreach (var shopper in line.ToArray())
                if (shopper != null) shopper.GiveUp("Closed?!");
            line.Clear();
        }

        public void Join(Shopper shopper)
        {
            if (!line.Contains(shopper)) line.Add(shopper);
        }

        public virtual void Leave(Shopper shopper) => line.Remove(shopper);

        public bool InLine(Shopper shopper) => line.Contains(shopper);

        /// <summary>The building's ground rectangle (x, z), for <see cref="WalkRouter"/>. Shops are turned in 90 degree steps.</summary>
        public Rect Footprint(float margin)
        {
            bool turned = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, 270f)) < 1f;
            Vector2 size = turned ? new Vector2(footprint.y, footprint.x) : footprint;
            Vector3 c = transform.position;
            return Rect.MinMaxRect(c.x - size.x / 2f - margin, c.z - size.y / 2f - margin, c.x + size.x / 2f + margin, c.z + size.y / 2f + margin);
        }

        /// <summary>Where this customer stands in line, whether that's the front, and which way to face.</summary>
        public (Vector3 slot, bool front, Vector3 facing) LineSlot(Shopper shopper)
        {
            int index = Mathf.Max(0, line.IndexOf(shopper));
            Vector3 back = approach.position - lineSpot.position;
            back.y = 0f;
            back.Normalize();
            return (lineSpot.position + back * (queueSpacing * index), index == 0, -back);
        }

        /// <summary>The order the HUD ticket shows (usually the customer at the counter's).</summary>
        public virtual ShopOrder TicketOrder => Front?.Order;

        /// <summary>One line telling the player what to do next (HUD ticket).</summary>
        public virtual string Hint => null;

        /// <summary>0..1 for a hold-to-do job in progress (blending, shaping), or -1.</summary>
        public virtual float JobProgress => -1f;

        /// <summary>The order a new customer of this shop wants.</summary>
        public abstract ShopOrder NewOrder();

        /// <summary>A customer just stepped up to the counter: ring the little bell (you hear it nearby).</summary>
        public virtual void ReachedCounter(Shopper shopper)
        {
            if (!StaffOnDuty) Sfx.Play(Sound.ShopBell, lineSpot.position, 0.6f);
        }

        public abstract string Prompt { get; }
        public abstract void Interact();

        // ---- stations (topping tubs, shelves, the shaping stand...) forward here ----
        public virtual string StationPrompt(string id) => null;
        public virtual void UseStation(string id) { }
        public virtual void HoldStation(string id, float deltaTime) { }

        /// <summary>The worker finishes the front customer's whole order. Return false if there was nothing to do.</summary>
        protected abstract bool WorkerServe(Shopper shopper);

        /// <summary>Upgrades that speed the job up speed the worker up too (1 = normal).</summary>
        protected virtual float WorkerSpeedFactor => 1f;

        float WorkerSeconds => workerTime * StaffMember.TimeFactor(worker) * WorkerSpeedFactor;

        /// <summary>Who the hired worker is busy with (the HUD shows their progress), if anyone.</summary>
        protected virtual Shopper WorkerTarget => Front;

        protected virtual void Update()
        {
            if (!StaffOnDuty) return;
            var target = WorkerTarget;
            if (target != workerServing)
            {
                workerServing = target;
                workerDoneAt = Time.time + WorkerSeconds;
            }
            if (workerServing == null || Time.time < workerDoneAt) return;
            if (WorkerServe(workerServing)) StaffMember.RecordWork(worker);
            workerServing = null;
        }

        /// <summary>Takes the money for a finished order (price plus a tip) and sends the customer off happy.</summary>
        protected void Sell(Shopper shopper, int price, int tip, GameObject carry = null, Shopper.CarryStyle style = Shopper.CarryStyle.Hand,
            string thanks = null)
        {
            tip = Mathf.RoundToInt(tip * ParkPerks.TipMultiplier * (shopper.Order != null ? shopper.Order.tipScale : 1f));
            Wallet.Instance.Add(price + tip, moneyReason);
            Sold?.Invoke(lineSpot.position + Vector3.up * 2.2f, price, tip);
            GameStats.ShopSales++;
            ParkLevel.Add(2);
            Leave(shopper);
            shopper.Served(carry, thanks ?? (tip > 0 ? $"Thanks! +{MoneyText.Format(tip)} tip" : "Thanks!"), style);
        }

        /// <summary>Speed tip: the full tip within `fast` seconds of reaching the counter, half within twice that, then none.</summary>
        public static int SpeedTip(ShopOrder order, int fullTip, float fast)
        {
            if (order == null || order.placedAt < 0f) return 0;
            float waited = Time.time - order.placedAt;
            return waited <= fast ? fullTip : waited <= fast * 2f ? fullTip / 2 : 0;
        }
    }
}
