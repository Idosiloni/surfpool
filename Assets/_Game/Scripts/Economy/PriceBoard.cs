using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The ticket price the player sets, and the "fair" price surfers expect.
    /// Each surfer will pay up to somewhere between 80% and 150% of the fair price.
    /// </summary>
    public class PriceBoard : MonoBehaviour
    {
        [SerializeField] int price = 25;
        [Tooltip("What an average surfer thinks a session is worth. Upgrades raise it.")]
        [SerializeField] int fairPrice = 25;
        [SerializeField] int step = 5;
        [SerializeField] int minPrice = 5;
        [SerializeField] int maxPrice = 150;

        public int Price => price;

        public int FairPrice
        {
            get => fairPrice;
            set => fairPrice = Mathf.Max(1, value);
        }

        public void Change(int direction)
        {
            price = Mathf.Clamp(price + step * direction, minPrice, maxPrice);
        }
    }
}
