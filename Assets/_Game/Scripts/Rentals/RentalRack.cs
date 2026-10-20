using UnityEngine;

namespace SurfPool
{
    /// <summary>The rack of rental boards behind the rental counter. E takes a board, or puts the held one back.</summary>
    public class RentalRack : MonoBehaviour, IInteractable
    {
        [SerializeField] int capacity = 6;
        [SerializeField] int startingBoards = 6;
        [Tooltip("Board models in the rack, one per slot. The first Count are shown.")]
        [SerializeField] GameObject[] slots;

        public int Count { get; private set; }
        public int Capacity => capacity;

        void Awake()
        {
            Count = Mathf.Min(startingBoards, capacity);
            Refresh();
        }

        public string Prompt
        {
            get
            {
                var carry = PlayerCarry.Instance;
                if (carry != null && carry.HasBoard)
                    return Count < capacity ? $"Put board on rack ({Count}/{capacity})" : "Rack is full";
                return Count > 0
                    ? $"Take rental board ({Count}/{capacity})"
                    : "Rack is empty! Get returned boards from the bin";
            }
        }

        public void Interact()
        {
            var carry = PlayerCarry.Instance;
            if (carry == null) return;
            if (carry.HasBoard)
            {
                if (Count < capacity && carry.HandOverBoard()) TryPutBoard();
            }
            else if (Count > 0 && carry.PickUpBoard())
            {
                TryTakeBoard();
            }
        }

        public bool TryTakeBoard()
        {
            if (Count == 0) return false;
            Count--;
            Refresh();
            return true;
        }

        public bool TryPutBoard()
        {
            if (Count >= capacity) return false;
            Count++;
            Refresh();
            return true;
        }

        /// <summary>Upgrade: more slots, filled with new boards.</summary>
        public void AddBoards(int amount)
        {
            capacity += amount;
            Count += amount;
            Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].SetActive(i < Count);
        }
    }
}
