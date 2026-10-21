using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Builds the three counter shops (Acai &amp; Shake Shack, Surf Gear shack, Custom Board shaper's shed) with their
    /// stations, workers, customer lines and the shopper prefab. Each shop is hidden until its upgrade is bought.
    /// Models come from tools/blender/build_shops.py. Inside a shop, local coordinates follow the Blender model:
    /// L(x, y, z) with Blender y = depth (-y is the customer side) and z = up.
    /// </summary>
    public static partial class GreyboxBuilder
    {
        public static readonly Vector3 AcaiAt = new(5f, 0f, -31f), GearAt = new(42f, 0f, -33f), ShaperAt = new(72f, 0f, -36f);
        const float AcaiYaw = 180f, GearYaw = 90f, ShaperYaw = 180f;

        static Vector3 L(float x, float y, float z) => new(x, z, y);

        static void BuildShops(UpgradeShop shop, SurferSpawner spawner, GameObject game, GameObject hudGo, Material skinMat)
        {
            var itemMat = Mat("ShopItem", Color.white, 0.25f);
            var acai = BuildAcaiShack(skinMat);
            var gear = BuildGearShack(skinMat, itemMat);
            var shaper = BuildShaperShed(skinMat);

            Set(shop, "acaiBar", acai.gameObject);
            Set(shop, "acaiBarista", acai.barista);
            Set(shop, "surfShop", gear.gameObject);
            Set(shop, "gearClerk", gear.clerk);
            Set(shop, "boardShaper", shaper.gameObject);
            Set(shop, "shaperApprentice", shaper.apprentice);

            var customers = game.AddComponent<ShopCustomers>();
            Set(customers, "shopperPrefab", BuildShopperPrefab());
            Set(customers, "spawner", spawner);
            hudGo.AddComponent<ShopHud>();
        }

        class BuiltShop
        {
            public GameObject gameObject;
            public GameObject barista, clerk, apprentice;
        }

        static Transform ShopRoot(string name, Vector3 position, float yaw)
        {
            var root = new GameObject(name).transform;
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return root;
        }

        /// <summary>A collider-only box in the shop's local space (models have no colliders).</summary>
        static void Solid(Transform root, string name, Vector3 localCenter, Vector3 localSize)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(root, false);
            go.transform.localPosition = localCenter;
            go.GetComponent<BoxCollider>().size = localSize;
        }

        /// <summary>A shop upgrade's props: a GlowItem under the shop that shows once the upgrade is owned.</summary>
        static Transform ShopProp(Transform root, string name, string upgrade, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = local;
            var item = go.AddComponent<GlowItem>();
            item.fromLevel = 1;
            item.requires = upgrade;
            go.SetActive(false);
            return go.transform;
        }

        /// <summary>The counter's collider: E here serves / rings up / takes orders.</summary>
        static void RegisterBox(Transform root, ShopCounter shop, Vector3 localCenter, Vector3 localSize)
        {
            var go = new GameObject("Counter (E)", typeof(BoxCollider));
            go.transform.SetParent(root, false);
            go.transform.localPosition = localCenter;
            go.GetComponent<BoxCollider>().size = localSize;
            Set(go.AddComponent<ShopRegister>(), "shop", shop);
        }

        /// <summary>A usable station: a model (local yaw relative to the shop) with a collider and a ShopStation.</summary>
        static GameObject Station(Transform root, ShopCounter shop, string id, string model, Vector3 localPosition, float localYaw,
            Vector3 colliderCenter, Vector3 colliderSize, float scale = 1f)
        {
            var go = new GameObject($"Station ({id})");
            go.transform.SetParent(root, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, localYaw, 0f);
            var art = model != null ? WorldArt.Model(model, go.transform, go.transform.position, root.eulerAngles.y + localYaw, scale) : null;
            var box = go.AddComponent<BoxCollider>();
            box.center = colliderCenter;
            box.size = colliderSize;
            var station = go.AddComponent<ShopStation>();
            SetString(station, "id", id);
            Set(station, "shop", shop);
            if (art != null) Set(station, "bumpTarget", art.transform);
            return go;
        }

        static void LineAndWorker(ShopCounter counter, Transform root, Vector3 lineLocal, Vector3 approachLocal, string shopName, string reason,
            float patience, Vector2 gap, float workerTime)
        {
            var line = Point("Line Front", root, root.TransformPoint(lineLocal));
            var approach = Point("Line Approach", root, root.TransformPoint(approachLocal));
            Set(counter, "lineSpot", line);
            Set(counter, "approach", approach);
            SetString(counter, "shopName", shopName);
            SetString(counter, "moneyReason", reason);
            SetFloat(counter, "patience", patience);
            SetVector2(counter, "arrivalGap", gap);
            QueueRopes(root, line.position, approach.position);
            SetFloat(counter, "workerTime", workerTime);
        }

        /// <summary>Rope stanchions along both sides of a shop's line (dressing only: customers queue between them).</summary>
        static void QueueRopes(Transform root, Vector3 front, Vector3 back)
        {
            var gold = Mat("Gold", new Color(1f, 0.78f, 0.3f), 0.8f);
            var rope = Mat("QueueRope", new Color(0.1f, 0.55f, 0.6f), 0.3f);
            Vector3 along = back - front;
            along.y = 0f;
            float length = Mathf.Min(along.magnitude, 4.4f);
            along.Normalize();
            var side = new Vector3(-along.z, 0f, along.x);
            var ropes = new GameObject("Queue Ropes").transform;
            ropes.SetParent(root, true);
            foreach (float s in new[] { -0.65f, 0.65f })
            {
                Vector3 start = front + along * 0.6f + side * s, end = front + along * length + side * s;
                for (int i = 0; i < 3; i++)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = "Stanchion";
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                    post.transform.SetParent(ropes, false);
                    post.transform.position = Vector3.Lerp(start, end, i / 2f) + Vector3.up * 0.45f;
                    post.transform.localScale = new Vector3(0.07f, 0.45f, 0.07f);
                    post.GetComponent<MeshRenderer>().sharedMaterial = gold;
                }
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Rope";
                Object.DestroyImmediate(line.GetComponent<Collider>());
                line.transform.SetParent(ropes, false);
                line.transform.position = (start + end) / 2f + Vector3.up * 0.8f;
                line.transform.rotation = Quaternion.LookRotation(along);
                line.transform.localScale = new Vector3(0.04f, 0.04f, (end - start).magnitude);
                line.GetComponent<MeshRenderer>().sharedMaterial = rope;
            }
        }

        static GameObject Worker(Transform root, ShopCounter counter, string name, Vector3 local, float localYaw, Color shirt, Material skinMat, int wage)
        {
            var person = Person(name, root.TransformPoint(local), root.eulerAngles.y + localYaw, Mat(name.Replace(" ", "") + "Shirt", shirt, 0.3f), skinMat);
            person.transform.SetParent(root, true);
            Staff(person, name.Replace("Hire ", ""), wage);
            Set(counter, "worker", person);
            person.SetActive(false);
            return person;
        }

        // ---------------------------------------------------------------- Acai & Shake Shack

        static BuiltShop BuildAcaiShack(Material skinMat)
        {
            var root = ShopRoot("Acai Shack", AcaiAt, AcaiYaw);
            WorldArt.Model("AcaiShack", root, root.position, AcaiYaw);
            Signs.Text("ACAI & SHAKES", root, root.TransformPoint(L(0f, -1.96f, 3.75f)), AcaiYaw, 0.32f, Color.white, 2.2f);
            var bar = root.gameObject.AddComponent<AcaiBar>();
            SetVector2(bar, "footprint", new Vector2(4.6f, 3.2f));

            // Walls and counters (the right end of the front counter is open: walk in there).
            RegisterBox(root, bar, L(-0.55f, -0.95f, 0.54f), new Vector3(3.0f, 1.08f, 0.6f));
            Solid(root, "Back Counter", L(0f, 1.1f, 0.5f), new Vector3(4.0f, 1.0f, 0.55f));
            Solid(root, "Fridge", L(1.55f, 1.05f, 0.85f), new Vector3(0.8f, 1.7f, 0.6f));
            Solid(root, "Back Wall", L(0f, 1.45f, 1.3f), new Vector3(4.4f, 2.6f, 0.1f));
            Solid(root, "Side Wall L", L(-2.16f, 0f, 0.55f), new Vector3(0.08f, 1.1f, 3.0f));
            Solid(root, "Side Wall R", L(2.16f, 0.85f, 0.55f), new Vector3(0.08f, 1.1f, 1.3f));

            // Topping tubs along the front counter, each with a pile of its topping.
            string[] ids = { "banana", "strawberry", "blueberry", "mango", "granola", "coconut", "honey" };
            string[] models = { "TopBanana", "TopStrawberry", "TopBlueberry", "TopMango", "TopGranola", "TopCoconut", "TopHoney" };
            var toppingAssets = new Object[models.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                var tub = Station(root, bar, ids[i], "ToppingTub", L(-1.88f + i * 0.33f, -0.88f, 1.085f), 0f,
                    new Vector3(0f, 0.08f, 0f), new Vector3(0.32f, 0.22f, 0.26f));
                var pile = WorldArt.Model(models[i], tub.transform, tub.transform.TransformPoint(new Vector3(0f, 0.1f, 0f)), AcaiYaw, 2.3f);
                if (pile != null) pile.transform.SetParent(tub.transform.GetChild(0), true);
                toppingAssets[i] = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Game/Models/{models[i]}.fbx");
                // Name label on the counter's front lip, readable from behind the counter (the server's side).
                Signs.Text(AcaiBar.Label(ids[i]).ToUpperInvariant(), tub.transform, root.TransformPoint(L(-1.88f + i * 0.33f, -0.62f, 1.1f)),
                    AcaiYaw + 180f, 0.045f, new Color(0.33f, 0.1f, 0.4f), 0.3f).transform.rotation = root.rotation * Quaternion.Euler(60f, 180f, 0f);
            }
            SetStringArray(bar, "toppingIds", ids);
            SetArray(bar, "toppingModels", toppingAssets);

            // Work spot at the right end of the front counter; stacks and the blender on the back counter.
            Set(bar, "workSpot", Point("Work Spot", root, root.TransformPoint(L(0.5f, -0.95f, 1.085f))));
            Station(root, bar, AcaiBar.Bowl, "BowlStack", L(-1.6f, 1.1f, 0.995f), 0f, new Vector3(0f, 0.12f, 0f), new Vector3(0.3f, 0.3f, 0.3f));
            Station(root, bar, AcaiBar.Cup, "CupStack", L(-1.1f, 1.1f, 0.995f), 0f, new Vector3(0f, 0.15f, 0f), new Vector3(0.24f, 0.35f, 0.24f));
            var blender = Station(root, bar, AcaiBar.Blend, "BlenderMachine", L(-0.4f, 1.1f, 0.995f), 0f, new Vector3(0f, 0.24f, 0f), new Vector3(0.3f, 0.5f, 0.3f));
            Set(bar, "blenderJar", blender.transform.GetChild(0));
            var bin = Station(root, bar, AcaiBar.Bin, null, L(-1.85f, 0.2f, 0f), 0f, new Vector3(0f, 0.35f, 0f), new Vector3(0.45f, 0.7f, 0.45f));
            var can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            can.name = "Bin";
            Object.DestroyImmediate(can.GetComponent<Collider>());
            can.transform.SetParent(bin.transform, false);
            can.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            can.transform.localScale = new Vector3(0.42f, 0.32f, 0.42f);
            can.GetComponent<MeshRenderer>().sharedMaterial = Mat("AcaiBin", new Color(0.36f, 0.12f, 0.42f), 0.4f);
            Set(bin.GetComponent<ShopStation>(), "bumpTarget", can.transform);

            Set(bar, "bowlModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/AcaiBowl.fbx"));
            Set(bar, "shakeModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/ShakeCup.fbx"));
            Set(bar, "cupModel", CupPrefab());

            LineAndWorker(bar, root, L(-0.4f, -1.9f, 0f), L(-0.4f, -7f, 0f), "Acai & Shake Shack", "acai & shakes", 50f, new Vector2(14f, 24f), 7f);
            // Turbo Blender upgrade: a big chrome blender with a TURBO sticker on the back counter.
            var turbo = ShopProp(root, "Turbo Blender", "acai_turbo_blender", L(0.35f, 1.1f, 0.995f));
            var big = WorldArt.Model("BlenderMachine", turbo, turbo.position, AcaiYaw, 1.45f);
            if (big != null) Recolor(big, "Navy", Mat("TurboRed", new Color(0.9f, 0.18f, 0.2f), 0.6f));
            Signs.Text("TURBO", turbo, turbo.position + root.rotation * L(0f, -0.16f, 0.13f), AcaiYaw, 0.06f, Color.white, 0.25f);

            var barista = Worker(root, bar, "Acai Barista", L(-0.6f, 0.15f, 0f), 180f, new Color(0.55f, 0.3f, 0.65f), skinMat, 7);
            WorldArt.Model("Bench", root, root.TransformPoint(L(3.6f, -2.2f, 0f)), AcaiYaw + 90f);
            root.gameObject.SetActive(false);
            return new BuiltShop { gameObject = root.gameObject, barista = barista };
        }

        /// <summary>An empty cup prefab made from the stack's single cup (the ShakeCup without its shake and lid).</summary>
        static GameObject CupPrefab()
        {
            const string path = "Assets/_Game/Prefabs/EmptyCup.prefab";
            var root = new GameObject("Empty Cup");
            var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(cup.GetComponent<Collider>());
            cup.transform.SetParent(root.transform, false);
            cup.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            cup.transform.localScale = new Vector3(0.11f, 0.09f, 0.11f);
            cup.GetComponent<MeshRenderer>().sharedMaterial = Mat("CupWhite", new Color(0.96f, 0.95f, 0.92f), 0.4f);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------------------------------------------------------- Surf Gear shack

        static BuiltShop BuildGearShack(Material skinMat, Material itemMat)
        {
            var root = ShopRoot("Surf Gear Shop", GearAt, GearYaw);
            WorldArt.Model("GearShack", root, root.position, GearYaw);
            Signs.Text("SURF GEAR", root, root.TransformPoint(L(0f, -2.82f, 3.95f)), GearYaw, 0.45f, Color.white, 3.1f);
            var gear = root.gameObject.AddComponent<GearShop>();
            SetVector2(gear, "footprint", new Vector2(7.2f, 4.6f));

            Solid(root, "Back Wall", L(0f, 2.14f, 1.5f), new Vector3(7f, 3f, 0.14f));
            Solid(root, "Side Wall L", L(-3.44f, 0f, 1.5f), new Vector3(0.14f, 3f, 4.4f));
            Solid(root, "Side Wall R", L(3.44f, 0f, 1.5f), new Vector3(0.14f, 3f, 4.4f));
            Solid(root, "Counter", L(2.3f, -1.1f, 0.53f), new Vector3(1.8f, 1.06f, 0.7f));

            // Register on the counter = the shop's own collider (E to ring up).
            WorldArt.Model("CashRegister", root, root.TransformPoint(L(2.3f, -1.0f, 1.06f)), GearYaw + 180f);
            RegisterBox(root, gear, L(2.3f, -1.05f, 1.25f), new Vector3(0.7f, 0.45f, 0.6f));

            // Racks, shelves and stands. Clothes racks are tinted per color.
            var colors = new (string id, Color color)[]
            {
                ("shorts_coral", new Color(0.95f, 0.36f, 0.45f)), ("shorts_teal", new Color(0.16f, 0.72f, 0.68f)),
                ("shorts_sun", new Color(1f, 0.8f, 0.3f)),
            };
            for (int i = 0; i < colors.Length; i++)
            {
                var rack = Station(root, gear, colors[i].id, "ShortsRack", L(-2.55f + i * 1.65f, 1.45f, 0f), 0f,
                    new Vector3(0f, 1.0f, 0f), new Vector3(1.45f, 1.0f, 0.5f));
                Recolor(rack, "Garment", Mat("Garment " + colors[i].id, colors[i].color, 0.25f));
            }
            var rash = Station(root, gear, "rash", "RashRack", L(2.4f, 1.45f, 0f), 0f, new Vector3(0f, 1.0f, 0f), new Vector3(1.45f, 1.0f, 0.5f));
            Recolor(rash, "Garment", Mat("Garment rash", new Color(0.2f, 0.32f, 0.62f), 0.25f));
            foreach (var item in GearShop.Catalog) GearTag(root, item);
            Station(root, gear, "fins", "FinWall", L(-3.05f, 0.2f, 0f), -90f, new Vector3(0f, 1.15f, 0f), new Vector3(1.2f, 1.0f, 0.3f));
            Station(root, gear, "leash", "LeashHooks", L(-3.05f, -1.15f, 0f), -90f, new Vector3(0f, 1.2f, 0f), new Vector3(0.9f, 0.8f, 0.3f));
            Station(root, gear, "wax", "WaxCrate", L(-0.9f, -0.2f, 0f), 15f, new Vector3(0f, 0.25f, 0f), new Vector3(0.65f, 0.5f, 0.5f));
            Station(root, gear, "sunscreen", "SunscreenStand", L(0.7f, -0.1f, 0f), 0f, new Vector3(0f, 0.75f, 0f), new Vector3(0.75f, 1.4f, 0.4f));

            Set(gear, "basketModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/ShopBasket.fbx"));
            Set(gear, "bagModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/ShopBag.fbx"));
            Set(gear, "itemMaterial", itemMat);

            // Card Reader upgrade: a tap-to-pay terminal and a sign on the counter.
            var reader = ShopProp(root, "Card Reader", "gear_card_reader", L(1.75f, -1.3f, 1.06f));
            var terminal = Box("Card Terminal", reader, reader.position + Vector3.up * 0.08f, new Vector3(0.12f, 0.16f, 0.2f), Mat("MachineNavy", new Color(0.06f, 0.16f, 0.22f), 0.3f));
            terminal.transform.rotation = root.rotation * Quaternion.Euler(-20f, 0f, 0f);
            Object.DestroyImmediate(terminal.GetComponent<Collider>());
            var screen = Box("Card Screen", reader, reader.position + Vector3.up * 0.13f + root.rotation * new Vector3(0f, 0f, -0.035f), new Vector3(0.09f, 0.06f, 0.01f),
                Mat("LampReady", new Color(0.3f, 1f, 0.5f), 0.6f, new Color(0.4f, 2.2f, 0.8f)));
            screen.transform.rotation = terminal.transform.rotation;
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            var plate = Box("Tap Sign", reader, reader.position + root.rotation * L(0.85f, 0.25f, 0.2f), new Vector3(0.5f, 0.3f, 0.03f), Mat("PropSun", new Color(1f, 0.8f, 0.3f), 0.35f));
            plate.transform.rotation = root.rotation;
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            Signs.Text("TAP TO PAY", reader, plate.transform.position + root.rotation * new Vector3(0f, 0f, -0.02f), GearYaw, 0.08f, new Color(0.06f, 0.16f, 0.22f), 0.45f);

            LineAndWorker(gear, root, L(2.3f, -2.05f, 0f), L(2.3f, -7f, 0f), "Surf Gear Shop", "surf gear", 70f, new Vector2(18f, 30f), 9f);
            var clerk = Worker(root, gear, "Shop Clerk", L(2.3f, -0.4f, 0f), 180f, new Color(1f, 0.78f, 0.25f), skinMat, 10);
            root.gameObject.SetActive(false);
            return new BuiltShop { gameObject = root.gameObject, clerk = clerk };
        }

        /// <summary>A little price tag on a gear display ("WAX $6"), facing into the shop.</summary>
        static void GearTag(Transform root, GearShop.Item item)
        {
            var spots = new System.Collections.Generic.Dictionary<string, (Vector3 at, float yaw)>
            {
                { "shorts_coral", (L(-2.55f, 1.2f, 1.62f), 0f) }, { "shorts_teal", (L(-0.9f, 1.2f, 1.62f), 0f) },
                { "shorts_sun", (L(0.75f, 1.2f, 1.62f), 0f) }, { "rash", (L(2.4f, 1.2f, 1.62f), 0f) },
                { "fins", (L(-2.95f, 0.2f, 0.62f), -90f) }, { "leash", (L(-2.95f, -1.15f, 0.72f), -90f) },
                { "wax", (L(-0.9f, -0.48f, 0.42f), 15f) }, { "sunscreen", (L(0.7f, -0.33f, 1.48f), 0f) },
            };
            if (!spots.TryGetValue(item.id, out var spot)) return;
            var rotation = root.rotation * Quaternion.Euler(0f, spot.yaw, 0f);
            var tag = Box($"Price Tag ({item.id})", root, root.TransformPoint(spot.at), new Vector3(0.62f, 0.2f, 0.02f), Mat("PropSun", new Color(1f, 0.8f, 0.3f), 0.35f));
            tag.transform.rotation = rotation;
            Object.DestroyImmediate(tag.GetComponent<Collider>());
            string label = item.id.StartsWith("shorts") ? "SHORTS" : item.label.ToUpperInvariant();
            var text = Signs.Text($"{label} ${item.price}", root, tag.transform.position + rotation * new Vector3(0f, 0f, -0.015f), 0f, 0.07f,
                new Color(0.06f, 0.16f, 0.22f), 0.58f);
            text.transform.rotation = rotation;
        }

        static void Recolor(GameObject root, string materialName, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && materials[i].name.StartsWith(materialName))
                    {
                        materials[i] = material;
                        changed = true;
                    }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        // ---------------------------------------------------------------- Custom Board shaper

        static BuiltShop BuildShaperShed(Material skinMat)
        {
            var root = ShopRoot("Board Shaper", ShaperAt, ShaperYaw);
            WorldArt.Model("ShaperShed", root, root.position, ShaperYaw);
            Signs.Text("CUSTOM BOARDS", root, root.TransformPoint(L(0f, -2.84f, 3.95f)), ShaperYaw, 0.42f, new Color(1f, 0.84f, 0.4f), 3.9f);
            var shaper = root.gameObject.AddComponent<BoardShaper>();
            SetVector2(shaper, "footprint", new Vector2(8.2f, 5.2f));

            Solid(root, "Back Wall", L(0f, 2.43f, 1.6f), new Vector3(8f, 3.2f, 0.14f));
            Solid(root, "Side Wall L", L(-3.93f, 0f, 1.6f), new Vector3(0.14f, 3.2f, 5f));
            Solid(root, "Side Wall R", L(3.93f, 0f, 1.6f), new Vector3(0.14f, 3.2f, 5f));
            Solid(root, "Workbench", L(3.45f, 0.8f, 0.48f), new Vector3(0.9f, 0.96f, 2.5f));

            // The counter out front, right side (E: take orders / hand boards over).
            WorldArt.Model("Counter", root, root.TransformPoint(L(2.3f, -2.0f, 0f)), ShaperYaw);
            RegisterBox(root, shaper, L(2.3f, -2.0f, 0.56f), new Vector3(3.0f, 1.12f, 0.9f));
            WorldArt.Model("CashRegister", root, root.TransformPoint(L(3.1f, -2.0f, 1.15f)), ShaperYaw + 180f);
            WorldArt.Model("WishBoard", root, root.TransformPoint(L(-4.6f, -3.3f, 0f)), ShaperYaw);

            // Shaping stand in the middle; a pile of foam blanks against the back wall; templates on the back wall.
            var stand = Station(root, shaper, BoardShaper.Stand, "ShapingStand", L(-0.3f, 0.3f, 0f), 0f,
                new Vector3(0f, 0.75f, 0f), new Vector3(2.3f, 0.5f, 0.7f));
            Set(shaper, "standTop", Point("Stand Top", stand.transform, stand.transform.TransformPoint(new Vector3(0f, 0.94f, 0f))));
            var pile = Station(root, shaper, BoardShaper.Blank, null, L(-1.0f, 1.75f, 0f), 0f, new Vector3(0f, 0.3f, 0f), new Vector3(2.3f, 0.6f, 0.7f));
            for (int i = 0; i < 3; i++)
                WorldArt.Model("FoamBlank", pile.transform, pile.transform.TransformPoint(new Vector3(0f, 0.13f * i, 0f)), ShaperYaw + (i - 1) * 2f);
            Station(root, shaper, "template_short", null, L(-2.6f, 2.25f, 0f), 0f, new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 1.9f, 0.2f));
            Station(root, shaper, "template_fun", null, L(-1.6f, 2.25f, 0f), 0f, new Vector3(0f, 1.6f, 0f), new Vector3(0.6f, 2.2f, 0.2f));
            Signs.Text("SHORT", root, root.TransformPoint(L(-2.6f, 2.15f, 2.7f)), ShaperYaw, 0.14f, new Color(0.15f, 0.15f, 0.2f), 0.6f);
            Signs.Text("FUN", root, root.TransformPoint(L(-1.6f, 2.15f, 2.85f)), ShaperYaw, 0.14f, new Color(0.15f, 0.15f, 0.2f), 0.6f);

            // Spray cans and fin boxes on the workbench (right wall), facing into the room.
            for (int i = 0; i < BoardShaper.Colors.Length; i++)
            {
                var color = BoardShaper.Colors[i];
                var can = Station(root, shaper, color.id, "SprayCan", L(3.3f, -0.25f + i * 0.28f, 0.96f), 0f,
                    new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.3f, 0.18f));
                Recolor(can, "Paint", Mat("Paint " + color.id, color.color, 0.4f));
            }
            var thruster = Station(root, shaper, "fins_thruster", "FinBox", L(3.35f, 1.35f, 0.96f), 90f, new Vector3(0f, 0.15f, 0f), new Vector3(0.45f, 0.35f, 0.35f));
            Recolor(thruster, "Paint", Mat("FinThruster", new Color(0.2f, 0.22f, 0.28f), 0.4f));
            var quad = Station(root, shaper, "fins_quad", "FinBox", L(3.35f, 1.85f, 0.96f), 90f, new Vector3(0f, 0.15f, 0f), new Vector3(0.45f, 0.35f, 0.35f));
            Recolor(quad, "Paint", Mat("FinQuad", new Color(0.95f, 0.36f, 0.45f), 0.4f));
            Signs.Text("3 FINS", root, root.TransformPoint(L(3.0f, 1.35f, 1.42f)), ShaperYaw + 90f, 0.1f, new Color(0.15f, 0.15f, 0.2f), 0.5f);
            Signs.Text("4 FINS", root, root.TransformPoint(L(3.0f, 1.85f, 1.42f)), ShaperYaw + 90f, 0.1f, new Color(0.15f, 0.15f, 0.2f), 0.5f);

            // Spray booth outside the west wall (dressing) and the waiting bench out front.
            WorldArt.Model("SprayBooth", root, root.TransformPoint(L(-5.5f, 0.6f, 0f)), ShaperYaw - 90f);
            WorldArt.Model("Bench", root, root.TransformPoint(L(-2.4f, -4.0f, 0f)), ShaperYaw);
            var seat = Point("Bench Seat", root, root.TransformPoint(L(-2.4f, -4.0f, 0.5f)));
            seat.rotation = Quaternion.Euler(0f, ShaperYaw, 0f);
            Set(shaper, "bench", seat);

            Set(shaper, "blankModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/FoamBlank.fbx"));
            Set(shaper, "shortboardModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/BoardShort.fbx"));
            Set(shaper, "funboardModel", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/BoardFun.fbx"));
            var dust = ShopParticles("Foam Dust", new Color(0.98f, 0.97f, 0.9f), 0.03f, 0.07f, 25f);
            dust.transform.SetParent(stand.transform, false);
            dust.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            Set(shaper, "foamDust", dust);
            var mist = ShopParticles("Paint Mist", Color.white, 0.05f, 0.14f, 0f);
            mist.transform.SetParent(stand.transform, false);
            mist.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var burst = mist.emission;
            burst.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            var mistMain = mist.main;
            mistMain.loop = false;
            mistMain.duration = 0.6f;
            Set(shaper, "paintMist", mist);

            // Power Planer upgrade: a heavy planer hanging from a ceiling rail over the stand, and a dust extractor.
            var planer = ShopProp(root, "Power Planer", "shaper_power_planer", L(-0.3f, 0.3f, 0f));
            var steelMat = Mat("MachineSteel", new Color(0.62f, 0.66f, 0.7f), 0.5f);
            var rail = Box("Planer Rail", planer, planer.position + Vector3.up * 2.9f, new Vector3(2.6f, 0.08f, 0.08f), steelMat);
            rail.transform.rotation = root.rotation;
            Object.DestroyImmediate(rail.GetComponent<Collider>());
            var cable = Box("Planer Cable", planer, planer.position + Vector3.up * 2.35f, new Vector3(0.03f, 1.1f, 0.03f), Mat("MachineNavy", new Color(0.06f, 0.16f, 0.22f), 0.3f));
            Object.DestroyImmediate(cable.GetComponent<Collider>());
            var tool = Box("Power Planer Body", planer, planer.position + Vector3.up * 1.72f, new Vector3(0.5f, 0.22f, 0.24f), Mat("PropSun", new Color(1f, 0.8f, 0.3f), 0.35f));
            tool.transform.rotation = root.rotation;
            Object.DestroyImmediate(tool.GetComponent<Collider>());
            var extractor = Box("Dust Extractor", planer, root.TransformPoint(L(-2.9f, 1.6f, 0.45f)), new Vector3(0.7f, 0.9f, 0.7f), Mat("PropCoral", new Color(0.95f, 0.38f, 0.42f), 0.35f));
            extractor.transform.rotation = root.rotation;
            var hose = Cylinder("Extractor Hose", planer, Vector3.Lerp(root.TransformPoint(L(-2.9f, 1.6f, 0.95f)), planer.position + Vector3.up * 1.8f, 0.5f),
                new Vector3(0.12f, Vector3.Distance(root.TransformPoint(L(-2.9f, 1.6f, 0.95f)), planer.position + Vector3.up * 1.8f) / 2f, 0.12f),
                Quaternion.FromToRotation(Vector3.up, planer.position + Vector3.up * 1.8f - root.TransformPoint(L(-2.9f, 1.6f, 0.95f))), Mat("Duct", new Color(0.3f, 0.62f, 0.86f), 0.45f));
            Object.DestroyImmediate(hose.GetComponent<Collider>());

            LineAndWorker(shaper, root, L(2.3f, -2.9f, 0f), L(2.3f, -8f, 0f), "Custom Boards", "custom boards", 60f, new Vector2(70f, 110f), 40f);
            var apprentice = Worker(root, shaper, "Apprentice Shaper", L(1.0f, 0.3f, 0f), -90f, new Color(0.35f, 0.6f, 0.95f), skinMat, 30);
            root.gameObject.SetActive(false);
            return new BuiltShop { gameObject = root.gameObject, apprentice = apprentice };
        }

        static ParticleSystem ShopParticles(string name, Color color, float minSize, float maxSize, float rate)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = color;
            main.gravityModifier = 0.25f;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.6f, 0.1f, 0.4f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/ShopParticle.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, $"{MaterialFolder}/ShopParticle.mat");
            }
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        static void SetVector2(Object target, string field, Vector2 value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetStringArray(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>You, for the instant replay: a person with a shortboard child named "Board".</summary>
        static CharacterRig BuildReplayAvatarPrefab()
        {
            const string path = "Assets/_Game/Prefabs/ReplayAvatar.prefab";
            var root = new GameObject("Replay Avatar");
            var rig = BuildCharacter(root.transform);
            var board = WorldArt.Model("BoardShort", root.transform, root.transform.position, 0f);
            if (board != null)
            {
                board.name = "Board";
                board.transform.localRotation = Quaternion.identity;
                board.transform.localPosition = CharacterRig.BoardPivot;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<CharacterRig>();
        }

        static Shopper BuildShopperPrefab()
        {
            const string path = "Assets/_Game/Prefabs/Shopper.prefab";
            var root = new GameObject("Shopper");
            BuildCharacter(root.transform);
            root.AddComponent<Shopper>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<Shopper>();
        }
    }
}
