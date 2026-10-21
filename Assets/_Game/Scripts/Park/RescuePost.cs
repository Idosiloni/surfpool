using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A red post with a ring buoy on the pool deck (<see cref="KookRescue"/>). E grabs the ring (or hangs it back);
    /// with the ring in hand, look at a surfer in trouble and press E to throw it.
    /// </summary>
    public class RescuePost : MonoBehaviour, IInteractable
    {
        [Tooltip("The ring hanging on the post (hidden while it's taken).")]
        [SerializeField] GameObject ringOnPost;

        /// <summary>The post whose ring the player is holding (null if none).</summary>
        public static RescuePost Holder { get; private set; }

        bool taken;

        public GameObject RingModel => ringOnPost;

        public string Prompt
        {
            get
            {
                var carry = PlayerCarry.Instance;
                if (carry == null) return null;
                if (Holder == this) return "Hang the ring back";
                if (taken || Holder != null) return null;
                return carry.HandsFull ? "Hands full" : KookRescue.Current != null ? "Grab the rescue ring!" : "Grab the rescue ring";
            }
        }

        public void Interact()
        {
            var carry = PlayerCarry.Instance;
            if (carry == null) return;
            if (Holder == this)
            {
                carry.DropShopItem(this);
                Holder = null;
                RingBack();
                return;
            }
            if (taken || Holder != null || carry.HandsFull) return;
            if (!carry.HoldShopItem(ringOnPost, this, new Vector3(0.42f, -0.36f, 0.8f), new Vector3(15f, -35f, 0f), 0.42f)) return;
            Holder = this;
            taken = true;
            ringOnPost.SetActive(false);
            Sfx.Play(Sound.Plop);
        }

        /// <summary>Throws the held ring at a surfer in trouble (from the player's view).</summary>
        public static void ThrowAt(Surfer surfer, Vector3 from)
        {
            if (Holder == null || PlayerCarry.Instance == null) return;
            var post = Holder;
            PlayerCarry.Instance.DropShopItem(post);
            Holder = null;
            KookRescue.Throw(surfer, from, post.ringOnPost, post);
        }

        /// <summary>The ring is back on the hook (after a rescue, or hung back by hand).</summary>
        public void RingBack()
        {
            taken = false;
            if (ringOnPost != null) ringOnPost.SetActive(true);
        }

        void OnDisable()
        {
            if (Holder == this) Holder = null;
        }
    }
}
