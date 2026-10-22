namespace SurfPool
{
    /// <summary>Anything the player can look at and press E on.</summary>
    public interface IInteractable
    {
        /// <summary>Text shown under the crosshair, e.g. "Start wave". Null hides the prompt.</summary>
        string Prompt { get; }

        /// <summary>False = pressing E does nothing yet (charging, waiting for guests): the prompt shows as greyed out
        /// and shakes when you try.</summary>
        bool CanUse => true;

        void Interact();
    }
}
