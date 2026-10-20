using UnityEngine;

namespace SurfPool
{
    /// <summary>Where renters drop their boards on the way out. The player carries them back to the rack.</summary>
    public class ReturnBin : MonoBehaviour, IInteractable
    {
        [Tooltip("Board models stacked in the bin. The first Count are shown.")]
        [SerializeField] GameObject[] pile;

        public int Count { get; private set; }

        void Awake() => Refresh();

        public string Prompt
        {
            get
            {
                var carry = PlayerCarry.Instance;
                if (Count == 0) return "Return bin (empty)";
                return carry != null && carry.HasBoard
                    ? $"Return bin ({Count}) - hands full"
                    : $"Pick up returned board ({Count})";
            }
        }

        public void Interact()
        {
            var carry = PlayerCarry.Instance;
            if (Count > 0 && carry != null && carry.PickUpBoard())
            {
                Count--;
                Refresh();
            }
        }

        /// <summary>A renter drops a board in.</summary>
        public void Add()
        {
            Count++;
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < pile.Length; i++)
                if (pile[i] != null) pile[i].SetActive(i < Count);
        }
    }
}
