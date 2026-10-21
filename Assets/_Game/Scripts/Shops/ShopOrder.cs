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
        /// <summary>A twist on this customer ("In a hurry!", "Big tipper"), shown on the bubble and ticket. Null = none.</summary>
        public string mood;
        public bool moodGood;
        public float patienceScale = 1f, tipScale = 1f;

        /// <summary>Rolls an occasional twist: in a hurry (less patience, double tip) or a big tipper.</summary>
        public void RollMood()
        {
            float roll = Random.value;
            if (roll < 0.15f)
            {
                mood = "In a hurry!";
                patienceScale = 0.6f;
                tipScale = 2f;
            }
            else if (roll < 0.27f)
            {
                mood = "Big tipper";
                moodGood = true;
                tipScale = 2.5f;
            }
        }

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
