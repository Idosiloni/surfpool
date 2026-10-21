using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Something to SEE for every upgrade that used to only change a number: a roadside billboard, a selfie wall
    /// (Social Media Ads), extra benches (Huge Lineup), a beach-pass booth, a VIP lounge, season-pass turnstiles,
    /// a pro rod rack on the pier, a premium board stand and the bigger snack menu sign. Each is a
    /// <see cref="GlowItem"/> that needs its upgrade, so <see cref="ParkGlowUp"/> pops it in when bought.
    /// </summary>
    public static partial class GreyboxBuilder
    {
        static void BuildUpgradeProps()
        {
            var root = new GameObject("Upgrade Props").transform;
            var navy = Mat("MachineNavy", new Color(0.06f, 0.16f, 0.22f), 0.3f);
            var coral = Mat("PropCoral", new Color(0.95f, 0.38f, 0.42f), 0.35f);
            var teal = Mat("TealTrim", new Color(0.08f, 0.72f, 0.65f), 0.4f);
            var sun = Mat("PropSun", new Color(1f, 0.8f, 0.3f), 0.35f);
            var white = Mat("PropWhite", new Color(0.96f, 0.95f, 0.92f), 0.3f);
            var wood = Mat("TikiPole", new Color(0.45f, 0.3f, 0.18f));
            var steel = Mat("MachineSteel", new Color(0.62f, 0.66f, 0.7f), 0.5f);
            var gold = Mat("Gold", new Color(1f, 0.78f, 0.3f), 0.8f);
            var red = Mat("RedCarpet", new Color(0.75f, 0.1f, 0.15f), 0.2f);

            // Roadside Billboard: by the road in, facing the cars coming up from the bridge.
            var billboard = Prop(root, "Roadside Billboard", "billboard", new Vector3(76f, 0f, -88f));
            foreach (float dx in new[] { -3.4f, 3.4f })
                Box("Billboard Post", billboard, billboard.position + new Vector3(dx, 3.2f, 0.2f), new Vector3(0.35f, 6.4f, 0.35f), wood);
            Box("Billboard Panel", billboard, billboard.position + new Vector3(0f, 6.2f, 0f), new Vector3(9.2f, 4.2f, 0.25f), navy);
            Box("Billboard Stripe", billboard, billboard.position + new Vector3(0f, 4.55f, -0.14f), new Vector3(9.2f, 0.5f, 0.05f), coral);
            Box("Billboard Sun", billboard, billboard.position + new Vector3(3.75f, 7.65f, -0.14f), new Vector3(0.9f, 0.9f, 0.05f), sun);
            Signs.Text("KAI'S SURF PARK", billboard, billboard.position + new Vector3(-0.5f, 6.9f, -0.2f), 0f, 0.85f, new Color(1f, 0.85f, 0.45f), 7f);
            Signs.Text("REAL WAVES  EVERY DAY", billboard, billboard.position + new Vector3(-0.5f, 5.8f, -0.2f), 0f, 0.45f, Color.white, 7f);
            Signs.Text("NEXT LEFT", billboard, billboard.position + new Vector3(0f, 4.55f, -0.2f), 0f, 0.3f, Color.white, 4f);

            // Social Media Ads: a selfie wall with a ring light and a phone on a tripod, by the path in.
            var selfie = Prop(root, "Selfie Wall", "social_ads", new Vector3(21f, 0f, -37.5f));
            Box("Selfie Panel", selfie, selfie.position + new Vector3(0f, 1.6f, 0f), new Vector3(0.2f, 2.6f, 3.6f), teal);
            Box("Selfie Frame", selfie, selfie.position + new Vector3(0.12f, 1.6f, 0f), new Vector3(0.05f, 2.2f, 3.2f), white);
            Signs.Text("#KAISURFPARK", selfie, selfie.position + new Vector3(0.16f, 2.25f, 0f), -90f, 0.32f, new Color(0.95f, 0.38f, 0.42f), 3f);
            Signs.Text("tag us & ride free!", selfie, selfie.position + new Vector3(0.16f, 1.75f, 0f), -90f, 0.18f, new Color(0.06f, 0.16f, 0.22f), 3f);
            foreach (float dz in new[] { -1.6f, 1.6f })
                Box("Selfie Leg", selfie, selfie.position + new Vector3(0f, 0.15f, dz), new Vector3(0.6f, 0.3f, 0.2f), navy);
            var ring = Cylinder("Ring Light", selfie, selfie.position + new Vector3(2.4f, 1.7f, 0.9f), new Vector3(0.7f, 0.03f, 0.7f),
                Quaternion.Euler(0f, 0f, 90f), Mat("RingLight", Color.white, 0.6f, new Color(2f, 2f, 1.8f)));
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            Cylinder("Ring Stand", selfie, selfie.position + new Vector3(2.4f, 0.75f, 0.9f), new Vector3(0.05f, 0.75f, 0.05f), Quaternion.identity, steel);
            Box("Phone", selfie, selfie.position + new Vector3(2.3f, 1.35f, -0.4f), new Vector3(0.03f, 0.16f, 0.09f), navy);
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                var leg = Box("Tripod Leg", selfie, selfie.position + new Vector3(2.3f + Mathf.Cos(a) * 0.2f, 0.62f, -0.4f + Mathf.Sin(a) * 0.2f),
                    new Vector3(0.03f, 1.3f, 0.03f), steel);
                leg.transform.rotation = Quaternion.Euler(Mathf.Sin(a) * 12f, 0f, -Mathf.Cos(a) * 12f);
            }

            // Huge Lineup: two more benches along the main pool.
            var benches = Prop(root, "Huge Lineup Benches", "huge_lineup", new Vector3(30f, 0f, -15f));
            WorldArt.Model("Bench", benches, new Vector3(18f, 0f, -15f), 180f);
            WorldArt.Model("Bench", benches, new Vector3(42f, 0f, -15f), 180f);

            // Beach Passes: a little booth by the gate with stacks of rolled towels.
            var booth = Prop(root, "Beach Pass Booth", "beach_passes", new Vector3(11f, 0f, -39.5f));
            WorldArt.Model("KioskTeal", booth, booth.position, 180f);
            WorldArt.Model("Counter", booth, booth.position, 180f);
            Signs.Kiosk("BEACH PASSES", booth, booth.position, 180f);
            for (int i = 0; i < 6; i++)
            {
                var towel = Cylinder("Towel", booth, booth.position + new Vector3(-1.1f + i * 0.42f, 1.22f, 0.15f), new Vector3(0.16f, 0.2f, 0.16f),
                    Quaternion.Euler(0f, 0f, 90f), i % 3 == 0 ? coral : i % 3 == 1 ? teal : sun);
                Object.DestroyImmediate(towel.GetComponent<Collider>());
            }

            // VIP Sessions: a roped-off lounge on a red carpet west of the desk.
            var vip = Prop(root, "VIP Lounge", "vip_sessions", new Vector3(-7f, 0f, -26f));
            var carpet = Box("Red Carpet", vip, vip.position + new Vector3(0f, 0.015f, 0f), new Vector3(7f, 0.03f, 4.6f), red);
            Object.DestroyImmediate(carpet.GetComponent<Collider>());
            WorldArt.Model("LoungeChair", vip, vip.position + new Vector3(-1.4f, 0.03f, 0.4f), 180f);
            WorldArt.Model("LoungeChair", vip, vip.position + new Vector3(1.4f, 0.03f, 0.4f), 180f);
            WorldArt.Model("BeachUmbrellaCoral", vip, vip.position + new Vector3(0f, 0.03f, 1.2f), 20f);
            for (int i = 0; i < 6; i++)
            {
                float x = -3.2f + i * 1.28f;
                Cylinder("Stanchion", vip, vip.position + new Vector3(x, 0.5f, -2.1f), new Vector3(0.08f, 0.5f, 0.08f), Quaternion.identity, gold);
                if (i < 5)
                {
                    var rope = Box("Velvet Rope", vip, vip.position + new Vector3(x + 0.64f, 0.85f, -2.1f), new Vector3(1.2f, 0.05f, 0.05f), red);
                    Object.DestroyImmediate(rope.GetComponent<Collider>());
                }
            }
            Box("VIP Sign Post", vip, vip.position + new Vector3(3.6f, 1.1f, -2.1f), new Vector3(0.12f, 2.2f, 0.12f), gold);
            Box("VIP Sign", vip, vip.position + new Vector3(3.6f, 2.1f, -2.15f), new Vector3(1.3f, 0.6f, 0.08f), navy);
            Signs.Text("VIP", vip, vip.position + new Vector3(3.6f, 2.12f, -2.2f), 0f, 0.4f, new Color(1f, 0.82f, 0.4f), 1.2f);

            // Season Passes: members' turnstiles and a sign at the gate.
            var members = Prop(root, "Season Pass Gate", "season_passes", new Vector3(30f, 0f, -40.5f));
            foreach (float dx in new[] { -4.5f, 4.5f })
            {
                var post = members.position + new Vector3(dx, 0f, 0f);
                Box("Turnstile", members, post + new Vector3(0f, 0.5f, 0f), new Vector3(0.35f, 1f, 0.6f), steel);
                for (int i = 0; i < 3; i++)
                {
                    var arm = Box("Turnstile Arm", members, post + new Vector3(dx > 0 ? -0.4f : 0.4f, 0.95f, 0f), new Vector3(0.7f, 0.05f, 0.05f), steel);
                    arm.transform.rotation = Quaternion.Euler(i * 120f, 0f, 0f);
                    Object.DestroyImmediate(arm.GetComponent<Collider>());
                }
                Box("Pass Reader", members, post + new Vector3(0f, 1.1f, -0.2f), new Vector3(0.2f, 0.15f, 0.15f), Mat("LampReady", new Color(0.3f, 1f, 0.5f), 0.6f, new Color(0.4f, 2.2f, 0.8f)));
            }
            Box("Members Sign", members, members.position + new Vector3(7.4f, 2.0f, 0f), new Vector3(2.8f, 1.0f, 0.1f), sun);
            Box("Members Sign Post", members, members.position + new Vector3(7.4f, 0.75f, 0.05f), new Vector3(0.14f, 1.5f, 0.14f), wood);
            Signs.Text("SEASON PASS", members, members.position + new Vector3(7.4f, 2.18f, -0.06f), 0f, 0.26f, new Color(0.06f, 0.16f, 0.22f), 2.6f);
            Signs.Text("MEMBERS THIS WAY", members, members.position + new Vector3(7.4f, 1.8f, -0.06f), 0f, 0.15f, new Color(0.06f, 0.16f, 0.22f), 2.6f);

            // Pro Fishing Rod: a rod rack and a tackle box at the end of the pier.
            var rods = Prop(root, "Pro Rod Rack", "pro_rod", new Vector3(-110.5f, 0f, 21.1f));
            Box("Rod Rack", rods, rods.position + new Vector3(0f, 0.6f, 0f), new Vector3(1.2f, 1.2f, 0.15f), wood);
            for (int i = 0; i < 3; i++)
            {
                var rod = Cylinder("Pro Rod", rods, rods.position + new Vector3(-0.4f + i * 0.4f, 1.6f, -0.1f), new Vector3(0.04f, 1.4f, 0.04f),
                    Quaternion.Euler(-8f, 0f, 0f), i == 1 ? coral : navy);
                Object.DestroyImmediate(rod.GetComponent<Collider>());
            }
            Box("Tackle Box", rods, rods.position + new Vector3(1.0f, 0.2f, -0.3f), new Vector3(0.6f, 0.4f, 0.35f), sun);

            // Premium Boards: a glossy display stand next to the rental stand.
            var premium = Prop(root, "Premium Board Stand", "premium_boards", new Vector3(35.2f, 0f, -19.8f));
            Box("Stand Base", premium, premium.position + new Vector3(0f, 0.1f, 0f), new Vector3(0.8f, 0.2f, 2.2f), navy);
            var gloss = Mat("PremiumBoard", new Color(0.98f, 0.97f, 0.95f), 0.9f);
            for (int i = 0; i < 3; i++)
                Board("Premium Board", premium, premium.position + new Vector3(0f, 1.05f, -0.7f + i * 0.7f), Quaternion.Euler(-82f, 90f, 0f), 0.95f,
                    i == 1 ? Mat("PremiumBoardGold", new Color(1f, 0.78f, 0.3f), 0.9f) : gloss);
            Box("Premium Sign", premium, premium.position + new Vector3(0f, 2.3f, 0f), new Vector3(0.08f, 0.45f, 1.8f), sun);
            Signs.Text("PREMIUM", premium, premium.position + new Vector3(-0.05f, 2.3f, 0f), 90f, 0.26f, new Color(0.06f, 0.16f, 0.22f), 1.6f);
            Signs.Text("PREMIUM", premium, premium.position + new Vector3(0.05f, 2.3f, 0f), -90f, 0.26f, new Color(0.06f, 0.16f, 0.22f), 1.6f);

            // Bigger Snack Menu: an A-frame "new!" sign in front of the snack bar.
            var menu = Prop(root, "New Menu Sign", "smoothie_bar", new Vector3(54.6f, 0f, -20.6f));
            var left = Box("A-Frame", menu, menu.position + new Vector3(0f, 0.6f, -0.18f), new Vector3(0.9f, 1.25f, 0.05f), coral);
            left.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            var back = Box("A-Frame", menu, menu.position + new Vector3(0f, 0.6f, 0.18f), new Vector3(0.9f, 1.25f, 0.05f), coral);
            back.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            Signs.Text("NEW!", menu, menu.position + new Vector3(0f, 0.95f, -0.29f), 0f, 0.2f, Color.white, 0.8f).transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            Signs.Text("BURRITOS", menu, menu.position + new Vector3(0f, 0.68f, -0.24f), 0f, 0.13f, new Color(1f, 0.9f, 0.6f), 0.8f).transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
            Signs.Text("+ FRIES", menu, menu.position + new Vector3(0f, 0.48f, -0.2f), 0f, 0.13f, new Color(1f, 0.9f, 0.6f), 0.8f).transform.rotation = Quaternion.Euler(-12f, 0f, 0f);
        }

        /// <summary>The OPEN / CLOSED sign by the ticket desk (flip it to end the day once everyone has gone home).</summary>
        static void BuildParkSign()
        {
            var at = new Vector3(16.6f, 0f, -24.8f);
            var root = new GameObject("Park Sign").transform;
            root.position = at;
            var wood = Mat("TikiPole", new Color(0.45f, 0.3f, 0.18f));
            Box("Sign Post", root, at + new Vector3(0f, 0.9f, 0.05f), new Vector3(0.1f, 1.8f, 0.1f), wood);
            Box("Sign Foot", root, at + new Vector3(0f, 0.04f, 0.05f), new Vector3(0.6f, 0.08f, 0.4f), wood);
            var board = new GameObject("Board").transform;
            board.SetParent(root, false);
            board.position = at + new Vector3(0f, 1.55f, 0f);
            Box("Open Face", board, board.position + new Vector3(0f, 0f, -0.03f), new Vector3(1.1f, 0.5f, 0.04f), Mat("SignOpen", new Color(0.12f, 0.7f, 0.45f), 0.35f));
            Box("Closed Face", board, board.position + new Vector3(0f, 0f, 0.03f), new Vector3(1.1f, 0.5f, 0.04f), Mat("SignClosed", new Color(0.9f, 0.25f, 0.25f), 0.35f));
            Signs.Text("OPEN", board, board.position + new Vector3(0f, 0f, -0.06f), 0f, 0.3f, Color.white, 1f);
            Signs.Text("CLOSED", board, board.position + new Vector3(0f, 0f, 0.06f), 180f, 0.3f, Color.white, 1f);
            Set(root.gameObject.AddComponent<ParkSign>(), "board", board);
        }

        /// <summary>A root for one upgrade's props: a GlowItem that shows once the upgrade is owned (hidden at start).</summary>
        static Transform Prop(Transform root, string name, string upgrade, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = position;
            var item = go.AddComponent<GlowItem>();
            item.fromLevel = 1;
            item.requires = upgrade;
            go.SetActive(false);
            return go.transform;
        }
    }
}
