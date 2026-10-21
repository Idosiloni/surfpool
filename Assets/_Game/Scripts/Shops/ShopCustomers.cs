using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Sends customers to every open counter shop. Each shop has its own arrival gap; ads, events and perks that
    /// bring more surfers bring more shoppers too (a little less strongly).
    /// </summary>
    public class ShopCustomers : MonoBehaviour
    {
        [SerializeField] Shopper shopperPrefab;
        [SerializeField] SurferSpawner spawner;

        readonly Dictionary<ShopCounter, float> nextArrival = new();

        void Update()
        {
            foreach (var shop in ShopCounter.All)
            {
                if (!nextArrival.TryGetValue(shop, out float due))
                {
                    nextArrival[shop] = Time.time + Random.Range(2f, 6f); // the first customer comes quickly
                    continue;
                }
                if (Time.time < due) continue;
                float busy = Mathf.Sqrt(spawner.SpawnDelayMultiplier / (DailyEvents.ArrivalMultiplier * ParkPerks.ArrivalMultiplier));
                nextArrival[shop] = Time.time + Random.Range(shop.ArrivalGap.x, shop.ArrivalGap.y) * busy;
                if (!shop.WantsCustomer) continue;
                var shopper = Instantiate(shopperPrefab);
                shopper.Begin(shop, spawner.Entrance);
            }
        }
    }
}
