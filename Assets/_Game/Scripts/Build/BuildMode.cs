using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Build mode (B): decorate the park. Pick an item with 1-0 or the mouse wheel, aim at the ground (a green
    /// ghost shows where it goes, red if it doesn't fit), R rotates, left click or E places it, X sells back the
    /// decoration you look at for half price, B leaves. You can build on decks and sand, but not in or right
    /// next to pools (even pools that aren't built yet) or wave machine houses.
    /// </summary>
    public class BuildMode : MonoBehaviour
    {
        [Serializable]
        public class Placed
        {
            public string id;
            public float x, y, z, yaw;
        }

        public static bool Active { get; private set; }
        public static BuildMode Instance { get; private set; }

        [SerializeField] DecorCatalog catalog;
        [SerializeField] Camera playerCamera;
        [SerializeField] Material ghostOk;
        [SerializeField] Material ghostBad;
        [SerializeField] UIDocument bar;
        [SerializeField] float range = 30f;

        int selected;
        float yaw;
        GameObject ghost;
        Vector3 ghostCenter, ghostSize;
        bool canPlace;
        Transform ghostHolder;
        VisualElement barRoot, itemRow;
        Label info;
        readonly List<VisualElement> cards = new();

        void Awake()
        {
            Instance = this;
            Active = false;
            ghostHolder = new GameObject("Ghost Holder").transform;
            ghostHolder.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            var root = bar.rootVisualElement;
            barRoot = root.Q("buildRoot");
            itemRow = root.Q("buildItems");
            info = root.Q<Label>("buildInfo");
            BuildCards();
        }

        void OnDestroy()
        {
            if (Instance == this) Active = false;
        }

        void BuildCards()
        {
            itemRow.Clear();
            cards.Clear();
            for (int i = 0; i < catalog.items.Count; i++)
            {
                var item = catalog.items[i];
                var card = new VisualElement();
                card.AddToClassList("build-card");
                var key = new Label(((i + 1) % 10).ToString());
                key.AddToClassList("build-key");
                var title = new Label(item.title);
                title.AddToClassList("build-title");
                var price = new Label(MoneyText.Format(item.cost));
                price.AddToClassList("build-price");
                var beauty = new Label($"+{item.beauty} beauty" + (item.seats > 0 ? $"  ·  {item.seats} seat{(item.seats > 1 ? "s" : "")}" : ""));
                beauty.AddToClassList("build-beauty");
                card.Add(key);
                card.Add(title);
                card.Add(price);
                card.Add(beauty);
                itemRow.Add(card);
                cards.Add(card);
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;

            bool surfing = PlayerSurfing.Instance != null && PlayerSurfing.Instance.IsSurfing;
            if (keyboard.bKey.wasPressedThisFrame && !surfing && (Active || !InputBlocker.Blocked)) SetActive(!Active);
            if (!Active) return;
            if (InputBlocker.Blocked) // a menu opened: leave build mode
            {
                SetActive(false);
                return;
            }

            for (int i = 0; i < Mathf.Min(10, catalog.items.Count); i++)
            {
                if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) Select(i); // Digit1..Digit9, then Digit0
            }
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f) Select((selected + catalog.items.Count - 1) % catalog.items.Count);
                else if (scroll < -0.1f) Select((selected + 1) % catalog.items.Count);
            }
            if (keyboard.rKey.wasPressedThisFrame) yaw = (yaw + (keyboard.shiftKey.isPressed ? -45f : 45f) + 360f) % 360f;

            UpdateGhost();
            bool place = keyboard.eKey.wasPressedThisFrame
                         || (mouse != null && UnityEngine.Cursor.lockState == CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame);
            if (place) TryPlace();
            if (keyboard.xKey.wasPressedThisFrame) TryRemove();
            RefreshInfo();
        }

        void SetActive(bool on)
        {
            Active = on;
            barRoot.EnableInClassList("hidden", !on);
            if (on) Select(selected);
            else if (ghost != null) Destroy(ghost);
        }

        void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, catalog.items.Count - 1);
            for (int i = 0; i < cards.Count; i++) cards[i].EnableInClassList("build-card--selected", i == selected);
            if (ghost != null) Destroy(ghost);
            var item = catalog.items[selected];
            ghost = Instantiate(item.prefab, ghostHolder);
            if (ghost.GetComponent<Decoration>() is { } decoration) Destroy(decoration);
            var box = ghost.GetComponent<BoxCollider>();
            ghostCenter = box != null ? box.center : Vector3.up * 0.5f;
            ghostSize = box != null ? box.size : Vector3.one;
            foreach (var collider in ghost.GetComponentsInChildren<Collider>()) Destroy(collider);
            ghost.transform.SetParent(null, false);
            SetGhostMaterial(false);
        }

        void SetGhostMaterial(bool ok)
        {
            foreach (var renderer in ghost.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++) materials[i] = ok ? ghostOk : ghostBad;
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void UpdateGhost()
        {
            if (ghost == null) return;
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            bool hit = Physics.Raycast(ray, out var ground, range, ~0, QueryTriggerInteraction.Ignore);
            ghost.SetActive(hit);
            if (!hit) return;
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            // Keep big things from landing on top of you: push the ghost out until its near edge is in front of you.
            Vector3 point = ground.point;
            Vector3 flat = playerCamera.transform.forward;
            flat.y = 0f;
            flat.Normalize();
            Vector3 fromPlayer = point - playerCamera.transform.position;
            fromPlayer.y = 0f;
            float needed = Mathf.Max(ghostSize.x, ghostSize.z) * 0.5f + 0.8f;
            if (fromPlayer.magnitude < needed) point += flat * (needed - fromPlayer.magnitude);
            ghost.transform.SetPositionAndRotation(point, rotation);
            bool ok = ground.normal.y > 0.8f && ground.point.y > -0.25f && ground.point.y < 0.2f
                      && OutsidePools(point) && !Blocked(point, rotation)
                      && Wallet.Instance != null && Wallet.Instance.Money >= catalog.items[selected].cost;
            if (ok != canPlace || ghost.GetComponentInChildren<Renderer>().sharedMaterial != (ok ? ghostOk : ghostBad)) SetGhostMaterial(ok);
            canPlace = ok;
        }

        bool OutsidePools(Vector3 point)
        {
            var spot = new Vector2(point.x, point.z);
            float reach = Mathf.Max(ghostSize.x, ghostSize.z) * 0.5f + 0.5f;
            foreach (var pool in PoolRegistry.All)
                if (pool.FullFootprint(reach).Contains(spot) || pool.MachineHouseFootprint(reach).Contains(spot)) return false;
            return true;
        }

        readonly Collider[] overlaps = new Collider[16];

        bool Blocked(Vector3 point, Quaternion rotation)
        {
            Vector3 center = point + rotation * ghostCenter + Vector3.up * 0.1f;
            Vector3 half = new(ghostSize.x * 0.45f, Mathf.Max(0.05f, ghostSize.y * 0.45f - 0.1f), ghostSize.z * 0.45f);
            int count = Physics.OverlapBoxNonAlloc(center, half, overlaps, rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (overlaps[i].GetComponentInParent<FirstPersonController>() == null) return true;
            return false;
        }

        void TryPlace()
        {
            if (ghost == null || !ghost.activeSelf) return;
            var item = catalog.items[selected];
            if (!canPlace)
            {
                Notifications.Post(Wallet.Instance.Money < item.cost ? $"Not enough money for {item.title}." : "Can't build there.", true);
                Sfx.Play(Sound.Error);
                return;
            }
            if (!Wallet.Instance.Spend(item.cost, "decorations")) return;
            Spawn(item, ghost.transform.position, yaw);
            Sfx.Play(Sound.Buy);
            ParkLevel.Add(2);
        }

        void TryRemove()
        {
            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, range, ~0, QueryTriggerInteraction.Ignore)) return;
            var decoration = hit.collider.GetComponentInParent<Decoration>();
            if (decoration == null) return;
            var item = catalog.Get(decoration.Id);
            int refund = item != null ? item.cost / 2 : 0;
            if (refund > 0) Wallet.Instance.Add(refund, "sold decorations");
            Notifications.Post($"Removed {item?.title ?? "decoration"} (+{MoneyText.Format(refund)}).");
            Sfx.Play(Sound.Click);
            Destroy(decoration.gameObject);
        }

        GameObject Spawn(DecorCatalog.Item item, Vector3 position, float rotation)
        {
            var placed = Instantiate(item.prefab, position, Quaternion.Euler(0f, rotation, 0f));
            placed.name = item.title;
            var decoration = placed.GetComponent<Decoration>();
            if (decoration == null) decoration = placed.AddComponent<Decoration>();
            decoration.Setup(item.id, item.beauty);
            return placed;
        }

        void RefreshInfo()
        {
            int beauty = Decoration.Beauty;
            info.text = $"Park beauty {beauty}  (+{MoneyText.Format(Decoration.FairBonus)} fair price)   ·   {Decoration.All.Count} decorations   ·   " +
                        "1-0 / wheel pick   R rotate   Click/E place   X sell back (half price)   B done";
            int money = Wallet.Instance != null ? Wallet.Instance.Money : 0;
            for (int i = 0; i < cards.Count; i++)
                cards[i].EnableInClassList("build-card--expensive", catalog.items[i].cost > money);
        }

        // ---------- save / load ----------

        public List<Placed> Export()
        {
            var list = new List<Placed>();
            foreach (var decoration in Decoration.All)
            {
                var t = decoration.transform;
                list.Add(new Placed { id = decoration.Id, x = t.position.x, y = t.position.y, z = t.position.z, yaw = t.eulerAngles.y });
            }
            return list;
        }

        public void Restore(List<Placed> list)
        {
            if (list == null) return;
            foreach (var placed in list)
            {
                var item = catalog.Get(placed.id);
                if (item == null) continue;
                // A pool may have grown since this was placed (pools got wider): refund it instead of floating it.
                bool inPool = false;
                foreach (var pool in PoolRegistry.All)
                    if (pool.FullFootprint(0.5f).Contains(new Vector2(placed.x, placed.z))) inPool = true;
                if (inPool)
                {
                    Wallet.Instance.Add(item.cost, "decoration refunds");
                    Notifications.Post($"The pool grew over your {item.title}: refunded {MoneyText.Format(item.cost)}.", Notice.Info);
                    continue;
                }
                Spawn(item, new Vector3(placed.x, placed.y, placed.z), placed.yaw);
            }
        }
    }
}
