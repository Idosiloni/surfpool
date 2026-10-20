using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired snack cook: serves whoever is at the front of the snack line after a short cooking time.</summary>
    public class SnackCook : MonoBehaviour
    {
        [SerializeField] SnackBar snackBar;
        [Tooltip("Seconds to cook and serve one order.")]
        [SerializeField] float cookTime = 3f;

        Surfer serving;
        float doneAt;

        void Update()
        {
            var customer = snackBar.NextCustomer;
            if (customer != serving)
            {
                serving = customer;
                doneAt = Time.time + cookTime * StaffMember.TimeFactor(gameObject);
            }
            if (serving == null || Time.time < doneAt) return;
            if (snackBar.Serve()) StaffMember.RecordWork(gameObject);
            serving = null;
        }
    }
}
