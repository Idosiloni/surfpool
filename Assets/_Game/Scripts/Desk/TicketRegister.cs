using UnityEngine;

namespace SurfPool
{
    /// <summary>The cash register on the ticket desk. Press E to sell the surfer at the front of the line a session.</summary>
    public class TicketRegister : MonoBehaviour, IInteractable
    {
        [SerializeField] SurferSpawner spawner;
        [SerializeField] PriceBoard pricing;

        public string Prompt
        {
            get
            {
                var customer = spawner.NextCustomer;
                if (customer == null) return "Register (no customer waiting)";
                return $"Sell {customer.Skill} session at {customer.Pool.DisplayName} ({MoneyText.Format(pricing.PriceFor(customer.Skill, customer.Pool))})";
            }
        }

        public void Interact()
        {
            var customer = spawner.NextCustomer;
            if (customer == null) return;
            int price = pricing.PriceFor(customer.Skill, customer.Pool);
            if (price > customer.MaxPrice)
            {
                customer.RefusePrice();
                return;
            }
            Wallet.Instance.Add(price, "tickets");
            GameStats.TicketsSold++;
            customer.Pay(price);
        }
    }
}
