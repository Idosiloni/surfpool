using UnityEngine;

namespace SurfPool
{
    /// <summary>The cash register on the ticket desk. Press E to sell the waiting surfer a session.</summary>
    public class TicketRegister : MonoBehaviour, IInteractable
    {
        [SerializeField] SurferSpawner spawner;
        [SerializeField] int sessionPrice = 20;

        public string Prompt => spawner.NextCustomer != null
            ? $"Sell surf session (${sessionPrice})"
            : "Register (no customer waiting)";

        public void Interact()
        {
            var customer = spawner.NextCustomer;
            if (customer == null) return;
            Wallet.Instance.Add(sessionPrice);
            customer.Pay();
        }
    }
}
