namespace SurfPool
{
    /// <summary>The sign at the park gate (any stage of it): press E to repaint it with a new park name.</summary>
    public class GateSign : UnityEngine.MonoBehaviour, IInteractable
    {
        public string Prompt => ParkNamer.Instance == null ? null
            : $"Repaint the sign ({MoneyText.Format(ParkNamer.Instance.RenameCost)})";

        public void Interact()
        {
            if (ParkNamer.Instance != null) ParkNamer.Instance.Open(false);
        }
    }
}
