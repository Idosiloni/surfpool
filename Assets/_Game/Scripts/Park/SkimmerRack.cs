using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The skimmer net's rack on the main pool deck. E takes the net (or puts it back). With the net, aim at
    /// floating junk up to 9 m away and press E to scoop it (junk drifts to the walls, so it ends up in reach).
    /// </summary>
    public class SkimmerRack : MonoBehaviour, IInteractable
    {
        [SerializeField] GameObject netOnRack;

        public static bool Holding => holder != null;
        static SkimmerRack holder;

        public string Prompt
        {
            get
            {
                var carry = PlayerCarry.Instance;
                if (carry == null) return null;
                if (holder == this) return "Put the skimmer back";
                if (Holding) return null;
                return carry.HandsFull ? "Hands full" : "Take the skimmer net";
            }
        }

        public void Interact()
        {
            var carry = PlayerCarry.Instance;
            if (carry == null) return;
            if (holder == this)
            {
                carry.DropShopItem(this);
                holder = null;
                netOnRack.SetActive(true);
                Sfx.Play(Sound.Plop);
                return;
            }
            if (Holding || carry.HandsFull) return;
            if (!carry.HoldShopItem(netOnRack, this, new Vector3(0.38f, -0.52f, 0.3f), new Vector3(10f, 170f, 0f), 0.55f)) return;
            holder = this;
            netOnRack.SetActive(false);
            Sfx.Play(Sound.Plop);
        }

        void OnDisable()
        {
            if (holder == this) holder = null;
        }
    }
}
