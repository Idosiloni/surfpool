namespace SurfPool
{
    /// <summary>Something you use by holding E (or the left mouse button) for a while, like repairing a machine.</summary>
    public interface IHoldInteractable
    {
        /// <summary>Called every frame the button is held while looking at it.</summary>
        void Hold(float deltaTime);
    }
}
