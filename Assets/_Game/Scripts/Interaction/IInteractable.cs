namespace SurfPool
{
    /// <summary>Anything the player can look at and press E on.</summary>
    public interface IInteractable
    {
        /// <summary>Text shown under the crosshair, e.g. "Start wave". Null hides the prompt.</summary>
        string Prompt { get; }
        void Interact();
    }
}
