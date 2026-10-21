using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Acai &amp; Shake Shack: the fast one. Customers want an acai bowl with 2-3 toppings, or a shake with 1-2
    /// fruits. Grab a bowl or a cup (E on the stacks), click the topping tubs in any order (a wrong one gets a
    /// "no thanks"), hold E on the blender for shakes, then hand it over at the counter. Fast service earns tips.
    /// What you made goes home in the customer's hand.
    /// </summary>
    public class AcaiBar : ShopCounter
    {
        public const string Bowl = "bowl", Cup = "cup", Blend = "blender", Bin = "bin";

        [Header("Acai")]
        [SerializeField] int bowlPrice = 12;
        [SerializeField] int shakePrice = 9;
        [Tooltip("Each extra topping / fruit adds this much.")]
        [SerializeField] int extraPrice = 2;
        [SerializeField] int fullTip = 4;
        [Tooltip("Serve within this many seconds of the customer reaching the counter for the full tip.")]
        [SerializeField] float fastSeconds = 12f;
        [SerializeField] float blendSeconds = 1.2f;
        [Tooltip("Where the bowl or cup you're making sits on the counter.")]
        [SerializeField] Transform workSpot;
        [SerializeField] Transform blenderJar;
        [SerializeField] GameObject bowlModel;
        [SerializeField] GameObject cupModel;
        [SerializeField] GameObject shakeModel;
        [Tooltip("Ids (banana, strawberry, ...) matching the topping models below.")]
        [SerializeField] string[] toppingIds;
        [SerializeField] GameObject[] toppingModels;

        static readonly (string id, string label, Color color)[] Toppings =
        {
            ("banana", "Banana", new Color(1f, 0.86f, 0.35f)),
            ("strawberry", "Strawberry", new Color(0.92f, 0.22f, 0.3f)),
            ("blueberry", "Blueberry", new Color(0.35f, 0.38f, 0.85f)),
            ("granola", "Granola", new Color(0.8f, 0.56f, 0.3f)),
            ("coconut", "Coconut", new Color(0.92f, 0.9f, 0.84f)),
            ("honey", "Honey", new Color(0.98f, 0.66f, 0.12f)),
        };
        static readonly string[] ShakeFruits = { "banana", "strawberry", "blueberry", "mango" };

        enum Making { Nothing, Bowl, Shake }

        Making making;
        GameObject work;
        readonly List<string> added = new();
        bool blended;
        float blendProgress;

        public static string Label(string id) => id == "mango" ? "Mango" : System.Array.Find(Toppings, t => t.id == id).label ?? id;

        public static Color ColorOf(string id) => id == "mango" ? new Color(1f, 0.6f, 0.15f)
            : id == Blend ? new Color(0.62f, 0.4f, 0.75f) : System.Array.Find(Toppings, t => t.id == id).color;

        float BlendSeconds => blendSeconds * ParkEffects.BlendTimeMultiplier;
        protected override float WorkerSpeedFactor => 0.5f + 0.5f * ParkEffects.BlendTimeMultiplier;
        public override float JobProgress => making == Making.Shake && !blended && blendProgress > 0f ? blendProgress / BlendSeconds : -1f;

        public override string Hint
        {
            get
            {
                var order = Front?.Order;
                if (order == null) return null;
                bool shake = IsShake(order);
                if (making == Making.Nothing) return shake ? "Take a cup from the stack" : "Take a bowl from the stack";
                if (shake != (making == Making.Shake)) return "Wrong container: bin it (E on the bin)";
                if (Complete(order)) return "Serve it at the counter (E)";
                var next = order.items.Find(i => !i.done && i.id != Blend);
                if (next != null) return $"Add {next.label.ToLowerInvariant()} from its tub";
                return "Hold E on the blender";
            }
        }

        public override ShopOrder NewOrder()
        {
            var order = new ShopOrder();
            if (Random.value < 0.6f)
            {
                order.title = "Acai bowl";
                int count = Random.value < 0.5f ? 2 : 3;
                foreach (int i in Pick(Toppings.Length, count)) order.Add(Toppings[i].id, Toppings[i].label, Toppings[i].color);
                order.price = bowlPrice + extraPrice * (count - 2);
            }
            else
            {
                order.title = "Shake";
                int count = Random.value < 0.6f ? 1 : 2;
                foreach (int i in Pick(ShakeFruits.Length, count)) order.Add(ShakeFruits[i], Label(ShakeFruits[i]), ColorOf(ShakeFruits[i]));
                order.Add(Blend, "Blend", ColorOf(Blend));
                order.price = shakePrice + extraPrice * (count - 1);
            }
            order.price += ParkEffects.SnackPriceBonus / 2;
            return order;
        }

        static IEnumerable<int> Pick(int range, int count)
        {
            var pool = new List<int>();
            for (int i = 0; i < range; i++) pool.Add(i);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int k = Random.Range(0, pool.Count);
                yield return pool[k];
                pool.RemoveAt(k);
            }
        }

        bool IsShake(ShopOrder order) => order != null && order.title == "Shake";

        public override string Prompt
        {
            get
            {
                var customer = Front;
                if (customer == null) return making == Making.Nothing ? $"{shopName} (no one waiting)" : null;
                if (making == Making.Nothing) return IsShake(customer.Order) ? "Grab a cup for the shake" : "Grab a bowl for the acai";
                return Complete(customer.Order) ? $"Serve {customer.Order.title.ToLowerInvariant()} ({MoneyText.Format(customer.Order.price)})" : "Not done yet";
            }
        }

        public override void Interact()
        {
            var customer = Front;
            if (customer == null || making == Making.Nothing) return;
            if (!Complete(customer.Order))
            {
                Sfx.Play(Sound.Error);
                customer.Say(MissingLine(customer.Order), 2f);
                return;
            }
            var carry = work;
            work = null;
            ResetWork();
            Sfx.Play(Sound.ShopBell, transform.position);
            Sell(customer, customer.Order.price, SpeedTip(customer.Order, fullTip, fastSeconds), carry, Shopper.CarryStyle.Front);
        }

        string MissingLine(ShopOrder order)
        {
            var missing = order.items.Find(i => !i.done);
            if (missing == null) return "Hmm?";
            return missing.id == Blend ? "Blend it first!" : $"I asked for {missing.label.ToLowerInvariant()}!";
        }

        bool Complete(ShopOrder order)
        {
            if (order == null) return false;
            if (IsShake(order) != (making == Making.Shake)) return false;
            return order.AllDone;
        }

        public override string StationPrompt(string id)
        {
            switch (id)
            {
                case Bowl: return making == Making.Nothing ? "Take a bowl" : null;
                case Cup: return making == Making.Nothing ? "Take a cup" : null;
                case Blend:
                    if (making != Making.Shake) return "Blender (for shakes)";
                    return blended ? "Blended!" : "Hold to blend";
                case Bin: return making != Making.Nothing ? "Bin it and start over" : null;
                default:
                    return making == Making.Nothing ? $"{Label(id)} (take a bowl or cup first)" : $"Add {Label(id).ToLowerInvariant()}";
            }
        }

        public override void UseStation(string id)
        {
            var customer = Front;
            switch (id)
            {
                case Bowl:
                case Cup:
                    if (making != Making.Nothing) return;
                    making = id == Bowl ? Making.Bowl : Making.Shake;
                    work = ShopArt.Spawn(id == Bowl ? bowlModel : cupModel, workSpot, Vector3.zero, 1f);
                    if (customer != null && IsShake(customer.Order) != (making == Making.Shake))
                        customer.Say(IsShake(customer.Order) ? "I wanted a shake!" : "I wanted a bowl!", 2f);
                    Sfx.Play(Sound.Click, workSpot.position);
                    break;
                case Bin:
                    if (making == Making.Nothing) return;
                    if (work != null) Destroy(work);
                    ResetWork();
                    Sfx.Play(Sound.Splash, workSpot.position);
                    break;
                case Blend:
                    break; // hold to blend
                default:
                    AddTopping(id, customer);
                    break;
            }
        }

        void AddTopping(string id, Shopper customer)
        {
            if (making == Making.Nothing || work == null) return;
            if (making == Making.Shake && (blended || System.Array.IndexOf(ShakeFruits, id) < 0))
            {
                Sfx.Play(Sound.Error);
                if (customer != null) customer.Say(blended ? "It's already blended!" : $"No {Label(id).ToLowerInvariant()} in a shake!", 2f);
                return;
            }
            if (added.Contains(id)) return;
            var wanted = customer?.Order?.Find(id);
            if (customer != null && (wanted == null || wanted.done))
            {
                Sfx.Play(Sound.Error);
                customer.Say($"No {Label(id).ToLowerInvariant()} for me!", 2f);
                return;
            }
            added.Add(id);
            if (wanted != null) wanted.done = true;
            int slot = added.Count - 1;
            float angle = slot * 2.1f;
            var offset = making == Making.Bowl
                ? new Vector3(Mathf.Cos(angle) * 0.045f, 0.072f, Mathf.Sin(angle) * 0.045f)
                : new Vector3(Mathf.Cos(angle) * 0.015f, 0.17f + slot * 0.012f, Mathf.Sin(angle) * 0.015f);
            int index = System.Array.IndexOf(toppingIds, id);
            if (index >= 0) ShopArt.Spawn(toppingModels[index], work.transform, offset, making == Making.Bowl ? 1f : 0.55f, true);
            Sfx.Play(Sound.Plop, workSpot.position);
        }

        public override void HoldStation(string id, float deltaTime)
        {
            if (id != Blend || making != Making.Shake || blended || work == null) return;
            if (added.Count == 0)
            {
                if (blendProgress == 0f && Front != null) Front.Say("Fruit first!", 1.5f);
                return;
            }
            if (blendProgress == 0f) Sfx.Play(Sound.Blender, blenderJar != null ? blenderJar.position : workSpot.position);
            blendProgress += deltaTime;
            if (blenderJar != null) blenderJar.localRotation = Quaternion.Euler(0f, Random.Range(-6f, 6f), Random.Range(-3f, 3f));
            if (blendProgress < BlendSeconds) return;
            FinishShake(Front);
        }

        void FinishShake(Shopper customer)
        {
            blended = true;
            if (blenderJar != null) blenderJar.localRotation = Quaternion.identity;
            var color = Color.black;
            foreach (var fruit in added) color += ColorOf(fruit);
            color = Color.Lerp(color / Mathf.Max(1, added.Count), new Color(0.55f, 0.25f, 0.6f), 0.35f);
            if (work != null) Destroy(work);
            work = ShopArt.Spawn(shakeModel, workSpot, Vector3.zero, 1f, true);
            ShopArt.Tint(work, "AcaiLight", color);
            var item = customer?.Order?.Find(Blend);
            if (item != null) item.done = true;
            Sfx.Play(Sound.Plop, workSpot.position);
        }

        void ResetWork()
        {
            making = Making.Nothing;
            added.Clear();
            blended = false;
            blendProgress = 0f;
            if (blenderJar != null) blenderJar.localRotation = Quaternion.identity;
        }

        /// <summary>The order on the counter no longer fits if the customer left: throw it out.</summary>
        public override void Leave(Shopper shopper)
        {
            bool wasFront = line.Count > 0 && line[0] == shopper;
            base.Leave(shopper);
            if (wasFront && work != null && shopper != null && shopper.Order != null && !shopper.Order.AllDone)
            {
                // Keep a half-made bowl only if it's empty; anything with toppings was for them.
                if (added.Count > 0)
                {
                    Destroy(work);
                    ResetWork();
                }
            }
        }

        protected override bool WorkerServe(Shopper customer)
        {
            if (customer == null || making != Making.Nothing) return false;
            bool shake = IsShake(customer.Order);
            var made = ShopArt.Spawn(shake ? shakeModel : bowlModel, workSpot, Vector3.zero, 1f);
            if (!shake)
            {
                int slot = 0;
                foreach (var item in customer.Order.items)
                {
                    int index = System.Array.IndexOf(toppingIds, item.id);
                    float angle = slot++ * 2.1f;
                    if (index >= 0) ShopArt.Spawn(toppingModels[index], made.transform, new Vector3(Mathf.Cos(angle) * 0.045f, 0.072f, Mathf.Sin(angle) * 0.045f), 1f);
                }
            }
            foreach (var item in customer.Order.items) item.done = true;
            Sfx.Play(Sound.ShopBell, transform.position);
            Sell(customer, customer.Order.price, fullTip / 2, made, Shopper.CarryStyle.Front);
            return true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (work != null) Destroy(work);
            ResetWork();
        }

    }
}
