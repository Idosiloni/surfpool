using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Surf Gear shop: the walk-around one. A customer asks for 1-3 things (boardshorts in a color, a rash guard,
    /// wax, fins, a leash, sunscreen). Walk to the racks and shelves and grab them into your basket (E again puts
    /// one back), then ring them up at the register: each item scans with a beep. Bring something they didn't ask
    /// for and they might buy it anyway (an upsell), or hand it back. Fast service earns a tip.
    /// The shop also keeps selling wax and stickers to happy surfers on their way out (the old merch bonus).
    /// </summary>
    public class GearShop : ShopCounter
    {
        [Header("Gear")]
        [SerializeField] int fullTip = 6;
        [SerializeField] float fastSeconds = 25f;
        [Tooltip("Chance a customer takes an extra item you bring (once per customer).")]
        [Range(0f, 1f)] [SerializeField] float upsellChance = 0.5f;
        [SerializeField] int basketSize = 4;
        [SerializeField] GameObject basketModel;
        [SerializeField] GameObject bagModel;
        [Tooltip("Plain material for the little folded items in the basket (tinted per item).")]
        [SerializeField] Material itemMaterial;

        public class Item
        {
            public string id, label;
            public Color color;
            public int price;
        }

        public static readonly Item[] Catalog =
        {
            new() { id = "shorts_coral", label = "Coral boardshorts", color = new Color(0.95f, 0.36f, 0.45f), price = 40 },
            new() { id = "shorts_teal", label = "Teal boardshorts", color = new Color(0.16f, 0.72f, 0.68f), price = 40 },
            new() { id = "shorts_sun", label = "Sunny boardshorts", color = new Color(1f, 0.8f, 0.3f), price = 40 },
            new() { id = "rash", label = "Rash guard", color = new Color(0.2f, 0.32f, 0.62f), price = 35 },
            new() { id = "wax", label = "Surf wax", color = new Color(0.98f, 0.94f, 0.75f), price = 6 },
            new() { id = "fins", label = "Fins", color = new Color(0.35f, 0.35f, 0.4f), price = 55 },
            new() { id = "leash", label = "Leash", color = new Color(0.12f, 0.12f, 0.14f), price = 25 },
            new() { id = "sunscreen", label = "Sunscreen", color = new Color(1f, 0.6f, 0.2f), price = 12 },
        };

        public const string Register = "register";

        readonly List<string> basket = new();
        readonly List<GameObject> basketBits = new();
        readonly HashSet<Shopper> upsold = new();
        readonly Dictionary<Shopper, int> extras = new();
        bool scanning;

        public override string Hint
        {
            get
            {
                var order = Front?.Order;
                if (order == null) return basket.Count > 0 ? $"Basket: {basket.Count} item{(basket.Count == 1 ? "" : "s")}" : null;
                var missing = order.items.Find(i => !i.done && !basket.Contains(i.id));
                if (missing != null) return $"Grab {missing.label.ToLowerInvariant()} from the shelves";
                return "Ring it up at the register (E)";
            }
        }

        public static Item Find(string id) => System.Array.Find(Catalog, i => i.id == id);
        public IReadOnlyList<string> Basket => basket;

        public override ShopOrder NewOrder()
        {
            var order = new ShopOrder { title = "Surf gear" };
            float roll = Random.value;
            int count = roll < 0.45f ? 1 : roll < 0.85f ? 2 : 3;
            var picked = new List<int>();
            bool hasShorts = false;
            while (picked.Count < count)
            {
                int i = Random.Range(0, Catalog.Length);
                bool shorts = Catalog[i].id.StartsWith("shorts");
                if (picked.Contains(i) || (shorts && hasShorts)) continue;
                hasShorts |= shorts;
                picked.Add(i);
            }
            foreach (int i in picked)
            {
                order.Add(Catalog[i].id, Catalog[i].label, Catalog[i].color);
                order.price += Catalog[i].price;
            }
            return order;
        }

        public override string Prompt
        {
            get
            {
                if (scanning) return null;
                var customer = Front;
                if (customer == null) return basket.Count > 0 ? "Put the basket back (no one waiting)" : $"{shopName} register (no one waiting)";
                if (basket.Count == 0) return "Grab what they want from the racks";
                return $"Ring up {basket.Count} item{(basket.Count == 1 ? "" : "s")}";
            }
        }

        public override void Interact()
        {
            var customer = Front;
            if (basket.Count == 0 || scanning) return;
            if (customer == null)
            {
                foreach (var id in basket.ToArray()) RemoveFromBasket(id); // everything back on the shelves
                Sfx.Play(Sound.Click);
                return;
            }
            StartCoroutine(RingUp(customer));
        }

        IEnumerator RingUp(Shopper customer)
        {
            scanning = true;
            var leftovers = new List<string>();
            foreach (var id in basket.ToArray())
            {
                if (customer == null || !InLine(customer)) break;
                var wanted = customer.Order.Find(id);
                if (wanted != null && !wanted.done)
                {
                    wanted.done = true;
                    RemoveFromBasket(id);
                    Sfx.Play(Sound.ScanBeep, transform.position);
                    yield return new WaitForSeconds(0.28f * ParkEffects.ScanTimeMultiplier);
                }
                else leftovers.Add(id);
            }
            // Upsell: one thing they didn't ask for, maybe they'll take it.
            if (customer != null && InLine(customer) && leftovers.Count > 0 && !upsold.Contains(customer))
            {
                upsold.Add(customer);
                var extra = Find(leftovers[0]);
                if (Random.value < upsellChance)
                {
                    RemoveFromBasket(extra.id);
                    extras[customer] = (extras.TryGetValue(customer, out int e) ? e : 0) + extra.price;
                    customer.Say($"Ooh, {extra.label.ToLowerInvariant()}? Sure, I'll take it!", 2.5f);
                    Sfx.Play(Sound.ScanBeep, transform.position);
                    yield return new WaitForSeconds(0.28f);
                }
                else customer.Say($"No {extra.label.ToLowerInvariant()} for me, thanks.", 2f);
            }
            scanning = false;
            if (customer == null || !InLine(customer)) yield break;
            if (customer.Order.AllDone) Finish(customer, SpeedTip(customer.Order, fullTip, fastSeconds) + ParkEffects.CardTip);
            else
            {
                var missing = customer.Order.items.Find(i => !i.done);
                customer.Say($"Still need {missing.label.ToLowerInvariant()}!", 2f);
            }
        }

        void Finish(Shopper customer, int tip)
        {
            int extra = extras.TryGetValue(customer, out int e) ? e : 0;
            extras.Remove(customer);
            upsold.Remove(customer);
            var bag = bagModel != null ? Instantiate(bagModel) : null;
            Sfx.Play(Sound.CashBig);
            Sell(customer, customer.Order.price + extra, tip, bag, Shopper.CarryStyle.Hand,
                extra > 0 ? $"Stoked! (+{MoneyText.Format(extra)} extra)" : null);
        }

        public override void Leave(Shopper shopper)
        {
            base.Leave(shopper);
            extras.Remove(shopper);
            upsold.Remove(shopper);
        }

        // ---- racks and shelves ----

        public override string StationPrompt(string id)
        {
            var item = Find(id);
            if (item == null) return null;
            if (basket.Contains(id)) return $"Put back {item.label.ToLowerInvariant()}";
            var carry = PlayerCarry.Instance;
            if (carry != null && carry.HasBoard) return $"{item.label} (hands full)";
            if (basket.Count >= basketSize) return $"{item.label} (basket full)";
            return $"Grab {item.label.ToLowerInvariant()} ({MoneyText.Format(item.price)})";
        }

        public override void UseStation(string id)
        {
            var item = Find(id);
            if (item == null || scanning) return;
            if (basket.Contains(id))
            {
                RemoveFromBasket(id);
                Sfx.Play(Sound.Click);
                return;
            }
            var carry = PlayerCarry.Instance;
            if (carry == null || basket.Count >= basketSize) return;
            if (basket.Count == 0 && !carry.HoldShopItem(basketModel, this, new Vector3(0.32f, -0.38f, 0.62f), new Vector3(-15f, -25f, 0f), 0.9f))
            {
                Sfx.Play(Sound.Error);
                return;
            }
            basket.Add(id);
            AddBit(item);
            Sfx.Play(Sound.Plop);
        }

        void AddBit(Item item)
        {
            var holder = PlayerCarry.Instance != null ? PlayerCarry.Instance.ShopItem(this) : null;
            if (holder == null || itemMaterial == null) return;
            var bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(bit.GetComponent<Collider>());
            bit.transform.SetParent(holder.transform, false);
            int n = basketBits.Count;
            bit.transform.localPosition = new Vector3(-0.1f + (n % 2) * 0.2f, 0.05f + (n / 2) * 0.06f, -0.04f + (n % 2) * 0.06f);
            bit.transform.localEulerAngles = new Vector3(0f, Random.Range(-20f, 20f), 0f);
            bit.transform.localScale = new Vector3(0.16f, 0.05f, 0.12f);
            var renderer = bit.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = itemMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", item.color);
            renderer.SetPropertyBlock(block);
            bit.AddComponent<PopIn>();
            basketBits.Add(bit);
        }

        void RemoveFromBasket(string id)
        {
            int index = basket.IndexOf(id);
            if (index < 0) return;
            basket.RemoveAt(index);
            if (index < basketBits.Count)
            {
                if (basketBits[index] != null) Destroy(basketBits[index]);
                basketBits.RemoveAt(index);
            }
            if (basket.Count == 0) PlayerCarry.Instance?.DropShopItem(this);
        }

        protected override float WorkerSpeedFactor => 0.5f + 0.5f * ParkEffects.ScanTimeMultiplier;

        protected override bool WorkerServe(Shopper customer)
        {
            if (customer == null || scanning) return false;
            foreach (var item in customer.Order.items) item.done = true;
            Sfx.Play(Sound.ScanBeep, transform.position);
            Finish(customer, fullTip / 2 + ParkEffects.CardTip);
            return true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            basket.Clear();
            basketBits.Clear();
            scanning = false;
            if (PlayerCarry.Instance != null) PlayerCarry.Instance.DropShopItem(this);
        }
    }
}
