namespace SurfPool
{
    /// <summary>The counter / register collider of a shop: E here serves, rings up or takes orders (see <see cref="ShopCounter"/>).</summary>
    public class ShopRegister : UnityEngine.MonoBehaviour, IInteractable
    {
        [UnityEngine.SerializeField] ShopCounter shop;

        public string Prompt => shop != null && shop.Open ? shop.Prompt : null;

        public void Interact()
        {
            if (shop != null && shop.Open) shop.Interact();
        }
    }
}
