using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>Everything you can place in build mode. Filled in by the scene builder (prefabs in Assets/_Game/Prefabs/Decor).</summary>
    public class DecorCatalog : ScriptableObject
    {
        [Serializable]
        public class Item
        {
            public string id;
            public string title;
            public int cost;
            [Tooltip("How much prettier the park gets (raises what surfers pay and how happy they are).")]
            public int beauty;
            [Tooltip("Places a beach guest can sit or lie down.")]
            public int seats;
            public GameObject prefab;
        }

        public List<Item> items = new();

        public Item Get(string id) => items.Find(i => i.id == id);
    }
}
