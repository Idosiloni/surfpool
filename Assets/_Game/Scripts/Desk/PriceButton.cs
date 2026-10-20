using UnityEngine;

namespace SurfPool
{
    /// <summary>A + or − button next to the desk that changes the ticket price.</summary>
    public class PriceButton : MonoBehaviour, IInteractable
    {
        [SerializeField] PriceBoard pricing;
        [Tooltip("+1 raises the price, -1 lowers it.")]
        [SerializeField] int direction = 1;

        public string Prompt => (direction > 0 ? "Raise" : "Lower") + $" ticket price (now ${pricing.Price})";

        public void Interact() => pricing.Change(direction);
    }
}
