using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// The visible wave machine at each pool's deep end (the "Air Wall"): a row of air chambers along the pool wall
    /// (one per ~3 m of the pool's full width), a duct manifold, blowers under a roof behind them, a pressure dial
    /// and status lamp by the south deck, and the parts each wave upgrade adds (blowers, air tanks, timer clock,
    /// control cabin, smart screen, famous-waves board, reef switch). Animated by <see cref="WaveMachineRig"/>.
    /// Models come from tools/blender/build_shops.py. Look/upgrade stages: docs/research/shops-and-machines.md.
    /// </summary>
    public static partial class GreyboxBuilder
    {
        /// <summary>World offset of a Blender point inside a model placed with WorldArt.Model at this yaw.</summary>
        static Vector3 B(float yaw, float x, float y, float z) => Quaternion.Euler(0f, yaw, 0f) * new Vector3(x, z, y);

        static WaveMachineRig BuildAirWall(Transform built, float zc, float southZ, float width, string display, WaveMachine machine,
            PoolArea area, GameObject autoLight, GameObject brokenLight, Transform smoke)
        {
            var root = new GameObject("Air Wall Machine").transform;
            root.SetParent(built, false);
            var rig = root.gameObject.AddComponent<WaveMachineRig>();
            Set(rig, "machine", machine);
            Set(rig, "pool", area);
            float northZ = southZ + width;
            var duct = Mat("Duct", new Color(0.3f, 0.62f, 0.86f), 0.45f);
            var steel = Mat("MachineSteel", new Color(0.62f, 0.66f, 0.7f), 0.5f);
            var navy = Mat("MachineNavy", new Color(0.06f, 0.16f, 0.22f), 0.3f);

            // ---- chambers along the pool wall ----
            int count = Mathf.Max(2, Mathf.RoundToInt(width / 3f));
            float module = width / count;
            var chambers = new WaveMachineRig.Chamber[count];
            for (int i = 0; i < count; i++)
            {
                var chamber = new GameObject($"Chamber {i + 1}").transform;
                chamber.SetParent(root, false);
                chamber.position = new Vector3(-3f, 0f, southZ + (i + 0.5f) * module);
                var model = WorldArt.Model("MachineChamber", chamber, chamber.position, -90f);
                if (model != null) model.transform.localScale = new Vector3(module / 3f, 1f, 1f);
                var piston = WorldArt.Model("ChamberPiston", chamber, chamber.position + B(-90f, 0f, 0.6f, 4.55f), -90f);
                var valve = WorldArt.Model("ValveWheel", chamber, chamber.position + B(-90f, 1.0f, 2.2f, 3.4f), -90f);
                Signs.Text((i + 1).ToString(), chamber, chamber.position + new Vector3(3.09f, 1.15f, 0f), -90f, 0.3f, navy.color);
                chambers[i] = new WaveMachineRig.Chamber
                {
                    root = chamber,
                    piston = piston != null ? piston.transform : null,
                    valve = valve != null ? valve.transform : null,
                    body = model != null ? model.GetComponentInChildren<Renderer>() : null,
                };
            }
            SetChambers(rig, chambers);

            // Pool name banner on the catwalk railing, facing the water.
            var banner = Box("Name Banner", root, new Vector3(-0.09f, 2.38f, zc), new Vector3(0.05f, 0.4f, Mathf.Min(9f, width - 3f)), navy);
            Object.DestroyImmediate(banner.GetComponent<Collider>());
            Signs.Text(display.ToUpperInvariant(), root, new Vector3(-0.05f, 2.38f, zc), -90f, 0.3f, Color.white, Mathf.Min(8.6f, width - 3.4f));

            // Duct manifold behind the chambers, joining every chamber's duct.
            var manifold = Cylinder("Duct Manifold", root, new Vector3(-6.4f, 3.4f, zc), new Vector3(0.95f, (width - module) / 2f + 0.4f, 0.95f),
                Quaternion.Euler(90f, 0f, 0f), duct);

            // ---- blowers under a roof behind the wall ----
            float roofLength = Mathf.Min(width, 19f) + 1f;
            var roofMat = Mat("PavilionRoof", new Color(0.93f, 0.93f, 0.9f), 0.3f);
            var roof = Box("Blower Roof", root, new Vector3(-10.5f, 4.55f, zc), new Vector3(7.6f, 0.25f, roofLength), roofMat);
            Object.DestroyImmediate(roof.GetComponent<Collider>());
            Box("Roof Trim", root, new Vector3(-10.5f, 4.4f, zc), new Vector3(7.7f, 0.12f, roofLength + 0.1f), Mat("TealTrim", new Color(0.08f, 0.72f, 0.65f), 0.4f));
            foreach (float x in new[] { -14f, -7.1f })
                foreach (float dz in new[] { -roofLength / 2f + 0.3f, roofLength / 2f - 0.3f })
                    Box("Roof Post", root, new Vector3(x, 2.2f, zc + dz), new Vector3(0.28f, 4.4f, 0.28f), steel);

            string[] blowerUpgrades = { "", "faster_machine", "turbo_2" };
            float[] blowerZ = { zc, zc - 6f, zc + 6f };
            var fans = new Object[3];
            var bodies = new Object[3];
            var blowerParts = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var blower = new GameObject(i == 0 ? "Blower" : $"Blower ({blowerUpgrades[i]})").transform;
                blower.SetParent(root, false);
                blower.position = new Vector3(-10.5f, 0f, blowerZ[i]);
                float scale = i == 2 ? 1.15f : 1f;
                var unit = WorldArt.Model("BlowerUnit", blower, blower.position, 0f, scale);
                var fan = WorldArt.Model("BlowerFan", blower, blower.position + B(0f, 0f, -0.3f, 1.6f) * scale, 0f, scale);
                var pipe = Cylinder("Blower Duct", blower, new Vector3(-7.9f, 3.45f, blowerZ[i]), new Vector3(0.7f, 1.6f, 0.7f),
                    Quaternion.Euler(0f, 0f, 90f), duct);
                Object.DestroyImmediate(pipe.GetComponent<Collider>());
                Cylinder("Riser", blower, new Vector3(-9.5f, 3.45f, blowerZ[i]), new Vector3(0.75f, 0.12f, 0.75f), Quaternion.identity, steel);
                fans[i] = fan != null ? fan.transform : null;
                bodies[i] = unit != null ? unit.GetComponentInChildren<Renderer>() : null;
                blowerParts[i] = blower.gameObject;
                var box = blower.gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 1.6f, 0f);
                box.size = new Vector3(3f, 3.2f, 2.4f);
            }
            SetArray(rig, "fans", fans);
            SetArray(rig, "blowerBodies", bodies);

            // ---- dial row on the south deck: reef switch, pressure dial, timer clock, status lamp ----
            float rowZ = southZ - 2.4f;
            var dial = WorldArt.Model("PressureDial", root, new Vector3(-3.2f, 0f, rowZ), 0f);
            var mount = new GameObject("Dial Needle Pivot").transform;
            mount.SetParent(root, false);
            mount.position = new Vector3(-3.2f, 2.6f, rowZ - 0.14f);
            var needle = WorldArt.Model("DialNeedle", mount, mount.position, 0f);
            Set(rig, "dialNeedle", mount);
            Box("Dial Base", root, new Vector3(-3.2f, 1.3f, rowZ + 0.1f), new Vector3(0.3f, 2.6f, 0.3f), steel).GetComponent<MeshRenderer>().enabled = false;

            var lamp = WorldArt.Model("StatusLamp", root, new Vector3(0.6f, 0f, rowZ), 0f, 1.6f);
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Status Bulb";
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
            bulb.transform.SetParent(root, false);
            bulb.transform.position = new Vector3(0.6f, 0.64f * 1.6f, rowZ);
            bulb.transform.localScale = Vector3.one * 0.26f;
            var ready = Mat("LampReady", new Color(0.3f, 1f, 0.5f), 0.6f, new Color(0.4f, 2.2f, 0.8f));
            bulb.GetComponent<MeshRenderer>().sharedMaterial = ready;
            Set(rig, "lampBulb", bulb.GetComponent<MeshRenderer>());
            Set(rig, "lampReady", ready);
            Set(rig, "lampCharging", Mat("LampCharging", new Color(1f, 0.75f, 0.2f), 0.6f, new Color(2.2f, 1.4f, 0.3f)));
            Set(rig, "lampBroken", Mat("LampBroken", new Color(1f, 0.2f, 0.15f), 0.6f, new Color(2.5f, 0.3f, 0.2f)));

            // Existing lights and the breakdown smoke move onto the new machine.
            autoLight.transform.position = new Vector3(-1.0f, 3.0f, rowZ);
            autoLight.transform.localScale = new Vector3(0.32f, 0.26f, 0.32f);
            brokenLight.transform.position = new Vector3(-3.2f, 3.6f, rowZ);
            brokenLight.transform.localScale = new Vector3(0.36f, 0.3f, 0.36f);
            smoke.position = new Vector3(-10.5f, 4.2f, zc);

            // ---- upgrade parts ----
            var parts = new System.Collections.Generic.List<(string, GameObject)>
            {
                ("faster_machine", blowerParts[1]),
                ("turbo_2", blowerParts[2]),
            };
            parts.Add(("auto_waves", WorldArt.Model("TimerClock", root, new Vector3(-1.0f, 0f, rowZ), 0f)));
            parts.Add(("wave_shaper", ReefSwitch(root, new Vector3(-5.6f, 0f, rowZ))));

            var tanks = new GameObject("Air Tanks (Wave Sets)").transform;
            tanks.SetParent(root, false);
            foreach (float dz in new[] { -4f, 4f })
                WorldArt.Model("AirTank", tanks, new Vector3(-13.4f, 0f, zc + dz), 90f);
            Cylinder("Tank Pipe", tanks, new Vector3(-12.2f, 1.9f, zc), new Vector3(0.25f, 4.2f, 0.25f), Quaternion.Euler(90f, 0f, 0f), duct);
            parts.Add(("wave_sets", tanks.gameObject));

            var cabin = WorldArt.Model("ControlCabin", root, new Vector3(-11f, 0f, southZ - 3.8f), -90f);
            if (cabin != null)
            {
                var cabinBox = cabin.AddComponent<BoxCollider>();
                cabinBox.center = new Vector3(0f, 1.6f, 0f);
                cabinBox.size = new Vector3(3.4f, 3.2f, 2.6f);
            }
            parts.Add(("pro_waves", cabin));

            parts.Add(("smart_auto", SmartScreen(root, zc, navy, steel, rig)));
            parts.Add(("famous_waves", FamousBoard(root, new Vector3(-2f, 0f, northZ + 3.2f))));

            var so = new UnityEditor.SerializedObject(rig);
            var prop = so.FindProperty("parts");
            prop.arraySize = parts.Count;
            for (int i = 0; i < parts.Count; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("upgrade").stringValue = parts[i].Item1;
                element.FindPropertyRelative("part").objectReferenceValue = parts[i].Item2;
                if (parts[i].Item2 != null) parts[i].Item2.SetActive(false);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            Set(rig, "spray", ChamberSpray(root));
            return rig;
        }

        static void SetChambers(WaveMachineRig rig, WaveMachineRig.Chamber[] chambers)
        {
            var so = new UnityEditor.SerializedObject(rig);
            var prop = so.FindProperty("chambers");
            prop.arraySize = chambers.Length;
            for (int i = 0; i < chambers.Length; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = chambers[i].root;
                element.FindPropertyRelative("piston").objectReferenceValue = chambers[i].piston;
                element.FindPropertyRelative("valve").objectReferenceValue = chambers[i].valve;
                element.FindPropertyRelative("body").objectReferenceValue = chambers[i].body;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject ReefSwitch(Transform root, Vector3 position)
        {
            var go = WorldArt.Model("ReefSwitch", root, position, 0f);
            if (go == null) return null;
            Signs.Text("LEFT", go.transform, position + new Vector3(-0.45f, 1.32f, -0.09f), 0f, 0.1f, Color.white, 0.4f);
            Signs.Text("RIGHT", go.transform, position + new Vector3(0.45f, 1.32f, -0.09f), 0f, 0.1f, Color.white, 0.4f);
            Signs.Text("REEF SHAPER", go.transform, position + new Vector3(0f, 1.95f, -0.08f), 0f, 0.12f, new Color(1f, 0.82f, 0.4f), 1.3f);
            return go;
        }

        /// <summary>A big screen above the chambers facing the pool: READY / CHARGING and the next wave (Smart Wave Timer).</summary>
        static GameObject SmartScreen(Transform root, float zc, Material navy, Material steel, WaveMachineRig rig)
        {
            var screen = new GameObject("Smart Screen").transform;
            screen.SetParent(root, false);
            foreach (float dz in new[] { -2.2f, 2.2f })
                Box("Screen Post", screen, new Vector3(-6.7f, 5.3f, zc + dz), new Vector3(0.18f, 3.4f, 0.18f), steel);
            Box("Screen Frame", screen, new Vector3(-6.62f, 6.6f, zc), new Vector3(0.22f, 1.9f, 5.2f), navy);
            var face = Box("Screen Face", screen, new Vector3(-6.5f, 6.6f, zc), new Vector3(0.04f, 1.6f, 4.9f),
                Mat("SmartScreenFace", new Color(0.04f, 0.12f, 0.2f), 0.8f, new Color(0.02f, 0.1f, 0.16f)));
            Object.DestroyImmediate(face.GetComponent<Collider>());
            var text = Signs.Text("READY", screen, new Vector3(-6.46f, 6.62f, zc), -90f, 0.55f, Color.white, 4.6f);
            text.textWrappingMode = TMPro.TextWrappingModes.Normal;
            text.rectTransform.sizeDelta = new Vector2(4.6f, 1.5f);
            text.richText = true;
            Set(rig, "smartScreen", text);
            var dish = Cylinder("Antenna Dish", screen, new Vector3(-6.7f, 7.9f, zc + 2.2f), new Vector3(0.9f, 0.08f, 0.9f), Quaternion.Euler(0f, 0f, 35f),
                Mat("DishWhite", new Color(0.95f, 0.95f, 0.95f), 0.4f));
            Object.DestroyImmediate(dish.GetComponent<Collider>());
            return screen.gameObject;
        }

        static GameObject FamousBoard(Transform root, Vector3 position)
        {
            var board = WorldArt.Model("FamousBoard", root, position, 0f);
            if (board == null) return null;
            Signs.Text("FAMOUS WAVES", board.transform, position + new Vector3(0f, 3.3f, -0.1f), 0f, 0.32f, new Color(1f, 0.82f, 0.4f), 3.6f);
            string[] spots = { "WAIKIKI", "MALIBU", "SNAPPER", "PIPELINE", "TEAHUPO'O" };
            for (int i = 0; i < spots.Length; i++)
                Signs.Text(spots[i], board.transform, position + new Vector3(-1.5f + i * 0.75f, 2.33f, -0.1f), 0f, 0.13f, Color.white, 0.72f);
            return board;
        }

        static ParticleSystem ChamberSpray(Transform root)
        {
            var go = new GameObject("Chamber Spray");
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.gravityModifier = 1.1f;
            main.maxParticles = 600;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            renderer.mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(sphere);
            renderer.sharedMaterial = Mat("ChamberSpray", new Color(0.95f, 0.98f, 1f), 0.6f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }
    }
}
