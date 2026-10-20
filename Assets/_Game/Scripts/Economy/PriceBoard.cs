using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The ticket price the player sets, and the "fair" price surfers expect.
    /// Each surfer will pay up to somewhere between 80% and 150% of the fair price.
    /// </summary>
    public class PriceBoard : MonoBehaviour
    {
        [SerializeField] int price = 20;
        [Tooltip("What an average surfer thinks a session is worth. Upgrades raise it.")]
        [SerializeField] int fairPrice = 25;
        [SerializeField] int step = 5;
        [SerializeField] int minPrice = 5;
        [SerializeField] int maxPrice = 150;

        [Header("Board rental")]
        [SerializeField] int rentalPrice = 10;

        public int Price => price;
        public int RentalPrice
        {
            get => rentalPrice;
            set => rentalPrice = Mathf.Max(0, value);
        }

        public int FairPrice
        {
            get => fairPrice;
            set => fairPrice = Mathf.Max(1, value);
        }

        public void Restore(int ticket, int rental, int fair)
        {
            if (ticket > 0) price = ticket;
            if (rental > 0) rentalPrice = rental;
            if (fair > 0) fairPrice = fair;
        }

        public void Change(int direction)
        {
            price = Mathf.Clamp(price + step * direction, minPrice, maxPrice);
        }
    }
}
