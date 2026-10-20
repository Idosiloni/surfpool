using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired desk clerk: sells a ticket to whoever is at the front of the desk line, after a short moment.</summary>
    public class DeskClerk : MonoBehaviour
    {
        [SerializeField] SurferSpawner spawner;
        [SerializeField] TicketRegister register;
        [Tooltip("Seconds the clerk takes to serve a customer.")]
        [SerializeField] float serveTime = 3f;

        Surfer serving;
        float serveDoneAt;

        void Update()
        {
            var customer = spawner.NextCustomer;
            if (customer != serving)
            {
                serving = customer;
                serveDoneAt = Time.time + serveTime;
            }
            if (serving == null || Time.time < serveDoneAt) return;
            register.Interact();
            serving = null;
        }
    }
}
