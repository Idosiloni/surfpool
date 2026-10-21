using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>Bigger pieces of the island outside the pools: the fishing pier, and the parking lot with its bridge.</summary>
    public static class ParkExtras
    {
        /// <summary>A wooden pier off the west beach into the ocean, with a rod stand at the end (hidden until bought).</summary>
        public static GameObject FishingPier(UnityEngine.UIElements.PanelSettings panelSettings)
        {
            var pier = new GameObject("Fishing Pier");
            var plank = GreyboxBuilder.Mat("PierWood", new Color(0.66f, 0.5f, 0.34f));
            var post = GreyboxBuilder.Mat("TikiPole", new Color(0.45f, 0.3f, 0.18f));
            const float z = 20f, startX = -79f, endX = -114f, width = 3.2f;
            float length = startX - endX;
            Box("Pier Deck", pier.transform, new Vector3((startX + endX) / 2f, -0.15f, z), new Vector3(length, 0.3f, width), plank, true);
            for (float x = startX - 2f; x >= endX; x -= 4f)
            {
                Box("Pier Post", pier.transform, new Vector3(x, -1.8f, z - width / 2f + 0.15f), new Vector3(0.3f, 3.4f, 0.3f), post, false);
                Box("Pier Post", pier.transform, new Vector3(x, -1.8f, z + width / 2f - 0.15f), new Vector3(0.3f, 3.4f, 0.3f), post, false);
                Box("Rail Post", pier.transform, new Vector3(x, 0.5f, z - width / 2f + 0.1f), new Vector3(0.12f, 1f, 0.12f), post, false);
                Box("Rail Post", pier.transform, new Vector3(x, 0.5f, z + width / 2f - 0.1f), new Vector3(0.12f, 1f, 0.12f), post, false);
            }
            Box("Rail", pier.transform, new Vector3((startX + endX) / 2f, 0.95f, z - width / 2f + 0.1f), new Vector3(length, 0.1f, 0.1f), plank, false);
            Box("Rail", pier.transform, new Vector3((startX + endX) / 2f, 0.95f, z + width / 2f - 0.1f), new Vector3(length, 0.1f, 0.1f), plank, false);
            Box("Rail End", pier.transform, new Vector3(endX + 0.1f, 0.95f, z), new Vector3(0.1f, 0.1f, width), plank, false);
            // Invisible walls so you can't walk off the sides or the end.
            Blocker(pier.transform, new Vector3((startX + endX) / 2f, 1f, z - width / 2f), new Vector3(length, 2f, 0.2f));
            Blocker(pier.transform, new Vector3((startX + endX) / 2f, 1f, z + width / 2f), new Vector3(length, 2f, 0.2f));
            Blocker(pier.transform, new Vector3(endX, 1f, z), new Vector3(0.2f, 2f, width));
            // Lamps and a sign.
            var lampMat = GreyboxBuilder.Mat("Beacon", new Color(1f, 0.85f, 0.1f), 0.5f, new Color(2f, 1.6f, 0.2f));
            for (float x = startX - 8f; x > endX; x -= 12f)
            {
                Box("Lamp Post", pier.transform, new Vector3(x, 1.4f, z + width / 2f - 0.1f), new Vector3(0.1f, 2.8f, 0.1f), post, false);
                Box("Lamp", pier.transform, new Vector3(x, 2.9f, z + width / 2f - 0.1f), new Vector3(0.3f, 0.3f, 0.3f), lampMat, false);
            }
            Box("Sign Post", pier.transform, new Vector3(startX + 1.5f, 1.2f, z - width / 2f - 0.6f), new Vector3(0.15f, 2.4f, 0.15f), post, false);
            Box("Fishing Sign", pier.transform, new Vector3(startX + 1.5f, 2.3f, z - width / 2f - 0.6f), new Vector3(0.1f, 0.7f, 1.8f),
                GreyboxBuilder.Mat("Sign", new Color(1f, 0.42f, 0.34f), 0.4f), false);
            Signs.Text("FISHING PIER", pier.transform, new Vector3(startX + 1.56f, 2.3f, z - width / 2f - 0.6f), -90f, 0.22f, Color.white, 1.7f);
            Signs.Text("FISHING PIER", pier.transform, new Vector3(startX + 1.44f, 2.3f, z - width / 2f - 0.6f), 90f, 0.22f, Color.white, 1.7f);

            // The rod stand at the end: look at it and press E to fish.
            var stand = new GameObject("Rod Stand").transform;
            stand.SetParent(pier.transform, false);
            stand.position = new Vector3(endX + 1.2f, 0f, z);
            Box("Bucket", stand, stand.position + new Vector3(0, 0.25f, 0.8f), new Vector3(0.4f, 0.5f, 0.4f), GreyboxBuilder.Mat("SeatTeal", new Color(0.1f, 0.68f, 0.62f), 0.35f), true);
            var rod = Box("Fishing Rod", stand, stand.position + new Vector3(-0.6f, 1.3f, 0f), new Vector3(0.05f, 0.05f, 2.4f), GreyboxBuilder.Mat("Console", new Color(0.22f, 0.22f, 0.25f)), true);
            rod.transform.rotation = Quaternion.Euler(-30f, -90f, 0f);
            Box("Rod Holder", stand, stand.position + new Vector3(0, 0.5f, 0), new Vector3(0.15f, 1f, 0.15f), post, true);
            var bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bobber.name = "Bobber";
            Object.DestroyImmediate(bobber.GetComponent<Collider>());
            bobber.transform.SetParent(pier.transform, false);
            bobber.transform.position = new Vector3(endX - 7f, -0.6f, z);
            bobber.transform.localScale = Vector3.one * 0.4f;
            bobber.GetComponent<MeshRenderer>().sharedMaterial = GreyboxBuilder.Mat("ButtonRed", new Color(0.9f, 0.2f, 0.15f), 0.6f);

            var fishingDoc = new GameObject("Fishing UI").AddComponent<UnityEngine.UIElements.UIDocument>();
            fishingDoc.transform.SetParent(pier.transform, false);
            fishingDoc.panelSettings = panelSettings;
            fishingDoc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Game/UI/Fishing.uxml");
            fishingDoc.sortingOrder = 7;
            var spot = stand.gameObject.AddComponent<FishingSpot>();
            var so = new SerializedObject(spot);
            so.FindProperty("bobber").objectReferenceValue = bobber.transform;
            so.FindProperty("ui").objectReferenceValue = fishingDoc;
            so.ApplyModifiedPropertiesWithoutUndo();
            pier.SetActive(false);
            return pier;
        }

        /// <summary>
        /// The parking lot south of the entrance (two rows of 8 spaces), a road to the island's edge and a long
        /// bridge over the ocean to the mainland, where the cars come from.
        /// </summary>
        public static void ParkingLot()
        {
            var lot = new GameObject("Parking Lot");
            var asphalt = GreyboxBuilder.Mat("Asphalt", new Color(0.28f, 0.29f, 0.31f), 0.15f);
            var paint = GreyboxBuilder.Mat("RoadPaint", new Color(0.97f, 0.97f, 0.92f), 0.2f);
            var yellow = GreyboxBuilder.Mat("RoadYellow", new Color(1f, 0.82f, 0.25f), 0.2f);
            var rail = GreyboxBuilder.Mat("Coping", new Color(0.97f, 0.96f, 0.92f), 0.3f);
            var pillar = GreyboxBuilder.Mat("Concrete", new Color(0.78f, 0.78f, 0.75f));
            const float cx = 65f, cz = -61f, top = -0.06f;

            Box("Lot", lot.transform, new Vector3(cx, top - 0.05f, cz), new Vector3(40f, 0.1f, 24f), asphalt, true);
            Box("Road", lot.transform, new Vector3(cx, top - 0.05f, -86f), new Vector3(8f, 0.1f, 28f), asphalt, true);
            Box("Bridge Deck", lot.transform, new Vector3(cx, top - 0.2f, -200f), new Vector3(8f, 0.4f, 200f), asphalt, true);
            for (float z = -110f; z > -300f; z += -16f)
            {
                Box("Bridge Pillar", lot.transform, new Vector3(cx - 3.2f, -2.5f, z), new Vector3(0.8f, 4.6f, 0.8f), pillar, false);
                Box("Bridge Pillar", lot.transform, new Vector3(cx + 3.2f, -2.5f, z), new Vector3(0.8f, 4.6f, 0.8f), pillar, false);
            }
            Box("Bridge Rail", lot.transform, new Vector3(cx - 4f, 0.45f, -200f), new Vector3(0.15f, 0.8f, 200f), rail, true);
            Box("Bridge Rail", lot.transform, new Vector3(cx + 4f, 0.45f, -200f), new Vector3(0.15f, 0.8f, 200f), rail, true);
            for (float z = -76f; z > -300f; z -= 6f)
                Box("Center Line", lot.transform, new Vector3(cx, top + 0.005f, z), new Vector3(0.15f, 0.01f, 3f), yellow, false);

            var route = new GameObject("Car Route").transform;
            route.SetParent(lot.transform, false);
            var spaces = new System.Collections.Generic.List<Object>();
            for (int row = 0; row < 2; row++)
            {
                float z = row == 0 ? cz + 6f : cz - 6f;
                for (int i = 0; i < 8; i++)
                {
                    float x = cx - 16f + i * 4.6f;
                    if (Mathf.Abs(x - cx) < 2.5f) continue; // keep the lane to the aisle clear
                    var space = new GameObject($"Space {spaces.Count + 1}").transform;
                    space.SetParent(route, false);
                    space.position = new Vector3(x, top, z);
                    space.rotation = Quaternion.LookRotation(row == 0 ? Vector3.forward : Vector3.back);
                    spaces.Add(space);
                    Box("Space Line", lot.transform, new Vector3(x - 2.3f, top + 0.005f, z), new Vector3(0.12f, 0.01f, 5f), paint, false);
                    Box("Space Line", lot.transform, new Vector3(x + 2.3f, top + 0.005f, z), new Vector3(0.12f, 0.01f, 5f), paint, false);
                }
            }
            Transform Point(string name, Vector3 at)
            {
                var p = new GameObject(name).transform;
                p.SetParent(route, false);
                p.position = at;
                return p;
            }
            var parking = lot.AddComponent<SurfPool.ParkingLot>();
            var so = new SerializedObject(parking);
            so.FindProperty("carPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Models/Car.fbx");
            var spacesProp = so.FindProperty("spaces");
            spacesProp.arraySize = spaces.Count;
            for (int i = 0; i < spaces.Count; i++) spacesProp.GetArrayElementAtIndex(i).objectReferenceValue = spaces[i];
            var roadIn = so.FindProperty("roadIn");
            roadIn.arraySize = 2;
            roadIn.GetArrayElementAtIndex(0).objectReferenceValue = Point("In Start", new Vector3(cx + 1.8f, top, -300f));
            roadIn.GetArrayElementAtIndex(1).objectReferenceValue = Point("In Lot", new Vector3(cx + 1.8f, top, cz));
            var roadOut = so.FindProperty("roadOut");
            roadOut.arraySize = 2;
            roadOut.GetArrayElementAtIndex(0).objectReferenceValue = Point("Out Lot", new Vector3(cx - 1.8f, top, cz));
            roadOut.GetArrayElementAtIndex(1).objectReferenceValue = Point("Out End", new Vector3(cx - 1.8f, top, -300f));
            so.FindProperty("aisleZ").floatValue = cz;
            so.ApplyModifiedPropertiesWithoutUndo();

            // A big welcome sign over the road where it reaches the island.
            var post = GreyboxBuilder.Mat("TikiPole", new Color(0.45f, 0.3f, 0.18f));
            Box("Arch Post", lot.transform, new Vector3(cx - 4.6f, 2.5f, -98f), new Vector3(0.5f, 5f, 0.5f), post, true);
            Box("Arch Post", lot.transform, new Vector3(cx + 4.6f, 2.5f, -98f), new Vector3(0.5f, 5f, 0.5f), post, true);
            Box("Welcome Sign", lot.transform, new Vector3(cx, 5.1f, -98f), new Vector3(10.4f, 1.4f, 0.3f), GreyboxBuilder.Mat("SeatTeal", new Color(0.1f, 0.68f, 0.62f), 0.35f), false);
            Box("Welcome Stripe", lot.transform, new Vector3(cx, 4.55f, -98.2f), new Vector3(10.4f, 0.2f, 0.05f), GreyboxBuilder.Mat("Sign", new Color(1f, 0.42f, 0.34f), 0.4f), false);
            Signs.Text("WELCOME TO SURF POOL", lot.transform, new Vector3(cx, 5.15f, -98.17f), 0f, 0.7f, Color.white, 10f)
                .gameObject.AddComponent<NameSign>().Setup("WELCOME TO {0}", true);
            Signs.Text("SEE YOU SOON!", lot.transform, new Vector3(cx, 5.15f, -97.83f), 180f, 0.7f, Color.white, 10f);
        }

        internal static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Blocker(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Pier Edge", typeof(BoxCollider));
            go.transform.SetParent(parent, true);
            go.transform.position = center;
            go.GetComponent<BoxCollider>().size = size;
        }
    }
}
