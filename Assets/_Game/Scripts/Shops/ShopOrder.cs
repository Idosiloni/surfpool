using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>One line on a customer's order: a topping, a piece of gear, a board color...</summary>
    public class OrderItem
    {
        public string id;
        public string label;
        public Color color;
        public bool done;
    }

    /// <summary>
    /// What a shop customer wants. Each shop fills in its own items (and reads them back); the HUD draws the title,
    /// the items as colored chips (ticked when done) and the price.
    /// </summary>
    public class ShopOrder
    {
        public string title;
        public readonly List<OrderItem> items = new();
        public int price;
        /// <summary>When the customer reached the counter (speed tips count from here).</summary>
        public float placedAt = -1f;

        public bool AllDone => items.TrueForAll(i => i.done);
        public int DoneCount => items.FindAll(i => i.done).Count;

        public OrderItem Find(string id) => items.Find(i => i.id == id && !i.done) ?? items.Find(i => i.id == id);

        public OrderItem Add(string id, string label, Color color)
        {
            var item = new OrderItem { id = id, label = label, color = color };
            items.Add(item);
            return item;
        }
    }
}
