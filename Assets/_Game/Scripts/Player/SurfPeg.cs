using UnityEngine;

namespace SurfPool
{
    /// <summary>Your own board on a peg by a pool's wave console. E jumps you into that pool to surf.</summary>
    public class SurfPeg : MonoBehaviour, IInteractable
    {
        [SerializeField] PoolArea pool;

        public string Prompt => "Go surfing (your own board)";

        public void Interact()
        {
            if (PlayerSurfing.Instance != null) PlayerSurfing.Instance.Enter(pool);
        }
    }
}
