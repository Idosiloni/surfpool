using UnityEngine;

namespace SurfPool
{
    /// <summary>The cash register on the ticket desk. Press E to sell the surfer at the front of the line a session.</summary>
    public class TicketRegister : MonoBehaviour, IInteractable
    {
        [SerializeField] SurferSpawner spawner;
        [SerializeField] PriceBoard pricing;

        public string Prompt => spawner.NextCustomer != null
            ? $"Sell surf session (${pricing.Price})"
            : "Register (no customer waiting)";

        public void Interact()
        {
            var customer = spawner.NextCustomer;
            if (customer == null) return;
            if (pricing.Price > customer.MaxPrice)
            {
                customer.RefusePrice();
                return;
            }
            Wallet.Instance.Add(pricing.Price, "ticket");
            GameStats.TicketsSold++;
            customer.Pay(pricing.Price);
        }
    }
}
