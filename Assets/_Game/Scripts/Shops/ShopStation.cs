using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A usable spot in a shop: a topping tub, the blender, a clothes rack, the shaping stand, a spray can...
    /// It only forwards to its <see cref="ShopCounter"/> with its id; the shop decides what it does.
    /// When used it gives a little squash "bump" so you feel the click.
    /// </summary>
    public class ShopStation : MonoBehaviour, IInteractable, IHoldInteractable
    {
        [SerializeField] ShopCounter shop;
        [SerializeField] string id;
        [Tooltip("Squashed when used (defaults to this object).")]
        [SerializeField] Transform bumpTarget;

        Vector3 baseScale;
        float bumpAt = -10f;

        public string Id => id;
        public ShopCounter Shop => shop;

        void Awake()
        {
            if (bumpTarget == null) bumpTarget = transform;
            baseScale = bumpTarget.localScale;
        }

        public string Prompt => shop != null && shop.Open ? shop.StationPrompt(id) : null;

        public void Interact()
        {
            if (shop == null) return;
            shop.UseStation(id);
            Bump();
        }

        public void Hold(float deltaTime)
        {
            if (shop != null) shop.HoldStation(id, deltaTime);
        }

        public void Bump() => bumpAt = Time.time;

        void Update()
        {
            float t = (Time.time - bumpAt) / 0.25f;
            if (t > 1.2f) return;
            float squash = t < 1f ? Mathf.Sin(t * Mathf.PI) * 0.12f : 0f;
            bumpTarget.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z * (1f + squash));
        }
    }
}
