using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// What the player is holding: one rental board, or one shop thing (the surf gear basket). Shown in view while
    /// held. You can't hold both: hands are full.
    /// </summary>
    public class PlayerCarry : MonoBehaviour
    {
        public static PlayerCarry Instance { get; private set; }

        [Tooltip("The board model attached to the camera, shown while carrying.")]
        [SerializeField] GameObject heldBoard;

        GameObject shopItem;
        Object shopItemOwner;

        public bool HasBoard { get; private set; }
        public bool HasShopItem => shopItem != null;
        public bool HandsFull => HasBoard || HasShopItem;

        void Awake()
        {
            Instance = this;
            Refresh();
        }

        /// <summary>Picks up a board. Returns false if hands are already full.</summary>
        public bool PickUpBoard()
        {
            if (HandsFull) return false;
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

        /// <summary>Shows a shop thing in the lower right of the view (a copy of the model). False if hands are full.</summary>
        public bool HoldShopItem(GameObject model, Object owner, Vector3 localPosition, Vector3 localEuler, float scale)
        {
            if (HandsFull) return shopItemOwner == owner && shopItem != null;
            var view = heldBoard != null ? heldBoard.transform.parent : transform;
            shopItem = Instantiate(model, view);
            shopItem.transform.localPosition = localPosition;
            shopItem.transform.localEulerAngles = localEuler;
            shopItem.transform.localScale = Vector3.one * scale;
            foreach (var renderer in shopItem.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shopItem.AddComponent<PopIn>();
            shopItemOwner = owner;
            return true;
        }

        /// <summary>The held shop thing (to add little models into it), if this owner is holding one.</summary>
        public GameObject ShopItem(Object owner) => shopItemOwner == owner ? shopItem : null;

        public void DropShopItem(Object owner)
        {
            if (shopItem == null || shopItemOwner != owner) return;
            Destroy(shopItem);
            shopItem = null;
            shopItemOwner = null;
        }

        void Refresh()
        {
            if (heldBoard != null) heldBoard.SetActive(HasBoard);
        }
    }
}
