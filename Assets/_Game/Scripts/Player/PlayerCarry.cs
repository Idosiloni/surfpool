using UnityEngine;

namespace SurfPool
{
    /// <summary>What the player is holding. For now: one rental board at a time, shown in view while held.</summary>
    public class PlayerCarry : MonoBehaviour
    {
        public static PlayerCarry Instance { get; private set; }

        [Tooltip("The board model attached to the camera, shown while carrying.")]
        [SerializeField] GameObject heldBoard;

        public bool HasBoard { get; private set; }

        void Awake()
        {
            Instance = this;
            Refresh();
        }

        /// <summary>Picks up a board. Returns false if hands are already full.</summary>
        public bool PickUpBoard()
        {
            if (HasBoard) return false;
            HasBoard = true;
            Refresh();
            return true;
        }

        /// <summary>Lets go of the board. Returns false if not holding one.</summary>
        public bool HandOverBoard()
        {
            if (!HasBoard) return false;
            HasBoard = false;
            Refresh();
            return true;
        }

        void Refresh()
        {
            if (heldBoard != null) heldBoard.SetActive(HasBoard);
        }
    }
}
