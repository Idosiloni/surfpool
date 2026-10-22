using System.IO;
using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Builds the "small to big" ambience: the park's glow-up dressing (run-down junk, flags, bunting, string lights,
    /// flower boxes, speakers, the old sign and the big entrance arch), the crowd at the pool rail, birds, boats,
    /// jet skis, the blimp and the helicopter, and the celebration particles. Every piece that depends on the park
    /// level gets a <see cref="GlowItem"/>; <see cref="ParkGlowUp"/> shows and hides them at runtime.
    /// Called by <see cref="GreyboxBuilder"/>.
    /// </summary>
    public static class GlowUpBuilder
    {
        // The main pool's north wall: the north deck's dressing is placed from it (the pool grows north).
        const float N = GreyboxBuilder.MainNorth;
        const float Ocean = -0.75f;

        // Glow-up stages (see ParkGlowUp.StageLevels): 1 run-down, 3 cleaned up, 5 festive, 8 lit up, 11 landmark, 15 famous.
        const int Clean = 3, Festive = 5, Lit = 8, Landmark = 11, Famous = 15;

        static Transform root;

        public static void Build(UpgradeShop shop, Transform reefBuilt, Transform lagoonBuilt, Material deck, Material coping, Material tile,
            float reefZ, float lagoonZ)
        {
            root = new GameObject("Park Glow-Up").transform;

            var glow = new GameObject("Park Glow-Up Controller").AddComponent<ParkGlowUp>();
            glow.transform.SetParent(root, false);
            Set(glow, "shop", shop);
            // Fresh paint: the grimy look until the park is cleaned up.
            SetArray(glow, "fresh", deck, coping, tile);
            SetArray(glow, "worn",
                GreyboxBuilder.Mat("DeckWorn", new Color(0.68f, 0.64f, 0.56f)),
                GreyboxBuilder.Mat("CopingWorn", new Color(0.7f, 0.68f, 0.6f), 0.1f),
                GreyboxBuilder.Mat("PoolTileWorn", new Color(0.46f, 0.62f, 0.52f)));
            // Luxury tiers by money earned: boardwalk, sandstone, marble.
            LuxuryBuilder.Paint(glow);

            RunDown();
            Festivities();
            PoolDressing(reefBuilt, reefZ, GreyboxBuilder.ReefWidth / 2f, GreyboxBuilder.ReefLength);
            PoolDressing(lagoonBuilt, lagoonZ, GreyboxBuilder.LagoonWidth / 2f, GreyboxBuilder.LagoonLength);
            GateSigns();
            Crowd();
            Birds();
            Vessels();
            CelebrationEffects();
        }

        // ---------- stage 0: the run-down pool you inherited ----------

        static void RunDown()
        {
            var junk = Group("Run-Down Junk");
            var rng = new System.Random(19);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            Vector3[] weedSpots =
            {
                new(2, 0, -13.2f), new(11, 0, -13.4f), new(19, 0, -13.1f), new(33, 0, -13.3f), new(41, 0, -13.2f), new(55, 0, -13.4f),
                new(-3, 0, -17), new(9, 0, -19.5f), new(46, 0, -18), new(62, 0, -14), new(16, 0, -29), new(44, 0, -27),
                new(6, 0, N + 2.5f), new(22, 0, N + 3.5f), new(38, 0, N + 2.3f), new(54, 0, N + 3.2f), new(63, 0, 3), new(27, 0, -36), new(35, 0, -34),
            };
            foreach (var spot in weedSpots)
                Item(WorldArt.Model("Weeds", junk, spot, R(0, 360), R(0.8f, 1.4f)), 1, Clean);
            Item(WorldArt.Model("TrashBags", junk, new Vector3(-3.5f, 0, -20), 30f), 1, Clean);
            Item(WorldArt.Model("TrashBags", junk, new Vector3(47, 0, -28.5f), 160f), 1, Clean);
            Item(WorldArt.Model("TrashBags", junk, new Vector3(64, 0, N + 1f), 80f, 0.9f), 1, Clean);
            Item(WorldArt.Model("RustyBarrel", junk, new Vector3(-2.5f, 0, -21.5f), 10f), 1, Clean);
            Item(WorldArt.Model("RustyBarrel", junk, new Vector3(-1.6f, 0, -22.3f), 70f), 1, Clean);
            var tipped = WorldArt.Model("RustyBarrel", junk, new Vector3(62.5f, 0.3f, -16.5f), 0f);
            if (tipped != null) tipped.transform.rotation = Quaternion.Euler(0f, 35f, 90f);
            Item(tipped, 1, Clean);
            Item(WorldArt.Model("RustyBarrel", junk, new Vector3(48, 0, -30), 0f), 1, Clean);
        }

        // ---------- stages 1-3: flags, bunting, flower boxes, speakers, string lights ----------

        static void Festivities()
        {
            var party = Group("Festivities");
            string[] flags = { "FlagCoral", "FlagTeal", "FlagYellow" };

            // Flags along the main pool's north edge and at the beach end (cleaned up).
            Vector3[] flagSpots = { new(3, 0, N + 3.6f), new(15, 0, N + 3.6f), new(45, 0, N + 3.6f), new(57, 0, N + 3.6f), new(65, 0, -9), new(65, 0, 1) };
            for (int i = 0; i < flagSpots.Length; i++) FlagPole(party, flagSpots[i], flags[i % 3], Clean);

            // A line of poles between the pool deck and the desk: bunting first, then string lights.
            var line = Group("Deck Lights", party);
            float[] poleX = { 4, 12, 20, 28, 36, 44, 52, 60 };
            const float z = -18.5f;
            for (int i = 0; i < poleX.Length; i++)
            {
                var pole = WorldArt.Model("LightPole", line, new Vector3(poleX[i], 0, z));
                Collider(pole, new Vector3(0, 2.1f, 0), new Vector3(0.2f, 4.2f, 0.2f));
                Item(pole, Festive);
                if (i == poleX.Length - 1) continue;
                var mid = new Vector3((poleX[i] + poleX[i + 1]) / 2f, 0, z);
                Item(Hang("Bunting", line, mid + Vector3.up * 3.7f), Festive);
                Item(Lights(line, mid + Vector3.up * 4.1f), Lit);
            }

            // Flower boxes by the desk, the deck and the gate.
            Vector3[] planters =
            {
                new(13.5f, 0, -24.5f), new(26.2f, 0, -25.5f), new(38.5f, 0, -23.5f), new(8, 0, N + 4.8f), new(22, 0, N + 4.8f),
                new(38, 0, N + 4.8f), new(52, 0, N + 4.8f), new(21.5f, 0, -42.5f), new(38.5f, 0, -42.5f),
            };
            for (int i = 0; i < planters.Length; i++)
                Item(WorldArt.Model("FlowerPlanter", party, planters[i], i < 3 ? 0f : 90f * (i % 2)), i < 7 ? Festive : Lit);

            // Park speakers: music and announcements come out of these (ParkSpeaker + Sfx).
            Vector3[] speakers = { new(-2, 0, -18.5f), new(64, 0, -18.5f), new(30, 0, N + 4.6f), new(34.5f, 0, -38) };
            foreach (var spot in speakers)
            {
                var speaker = WorldArt.Model("PaSpeaker", party, spot, 90f);
                if (speaker == null) continue;
                Collider(speaker, new Vector3(0, 2.5f, 0), new Vector3(0.2f, 5f, 0.2f));
                speaker.AddComponent<AudioSource>();
                speaker.AddComponent<ParkSpeaker>();
                Item(speaker, Festive);
            }

            // Flag towers flanking the gate once the park is famous.
            for (int i = 0; i < 4; i++)
            {
                var spot = new Vector3(i < 2 ? 18f - i * 3f : 42f + (i - 2) * 3f, 0, -42f);
                var tower = FlagPole(party, spot, flags[i % 3], Famous, 1.6f);
                if (tower != null) tower.name = "Flag Tower";
            }

            // Grandstand bunting (only with the Grandstand upgrade).
            for (int i = 0; i < 4; i++)
            {
                var hang = Hang("Bunting", party, new Vector3(14 + i * 9.5f, 4.4f, N + 12.2f));
                if (hang == null) continue;
                hang.transform.localScale = new Vector3(1.15f, 1f, 1f);
                Item(hang, Festive).requires = "grandstand";
            }
        }

        /// <summary>The other pools get flags and string lights along their north decks once they're built.</summary>
        static void PoolDressing(Transform built, float zc, float halfWidth, float length)
        {
            if (built == null) return;
            var party = Group("Pool Festivities", built);
            string[] flags = { "FlagTeal", "FlagCoral", "FlagYellow" };
            // Along the pool's +z side, in the pool's own frame (zones are placed and turned after they're built).
            Vector3 P(float x, float y, float z) => GreyboxBuilder.InPool(built, new Vector3(x, y, z));
            float north = zc + halfWidth + 1.6f, yaw = GreyboxBuilder.PoolYaw(built);
            int flagCount = Mathf.Max(2, Mathf.RoundToInt(length / 17f));
            for (int i = 0; i < flagCount; i++) FlagPole(party, P(4 + i * 17f, 0, north + 2.5f), flags[i % 3], Clean);
            int poles = Mathf.Max(2, Mathf.RoundToInt((length - 4f) / 8f));
            for (int i = 0; i < poles; i++)
            {
                float x = 4 + i * 8f;
                var pole = WorldArt.Model("LightPole", party, P(x, 0, north + 1f), yaw);
                Collider(pole, new Vector3(0, 2.1f, 0), new Vector3(0.2f, 4.2f, 0.2f));
                Item(pole, Festive);
                if (i == poles - 1) continue;
                Item(Hang("Bunting", party, P(x + 4f, 3.7f, north + 1f)), Festive);
                Item(Lights(party, P(x + 4f, 4.1f, north + 1f)), Lit);
            }
        }

        // ---------- the sign at the gate: crooked old sign -> repainted sign -> the big arch ----------

        static void GateSigns()
        {
            var gate = Group("Gate Signs");
            gate.gameObject.AddComponent<GateSign>(); // look at whichever sign is up and press E to rename the park
            var navy = new Color(0.06f, 0.18f, 0.23f);

            var old = new GameObject("Old Sign").transform;
            old.SetParent(gate, false);
            old.position = new Vector3(24, 0, -40);
            WorldArt.Model("SignOld", old, new Vector3(24, 0, -40), 0f);
            // Faded hand-painted letters, one missing, on both sides.
            foreach (float side in new[] { 1f, -1f })
            {
                float yaw = side > 0 ? 180f : 0f;
                var text = Signs.Text("SURF P OL", old, new Vector3(24, 2.0f, side > 0 ? -40.04f : -40.16f), yaw, 0.55f, new Color(0.35f, 0.3f, 0.26f), 3.0f);
                text.transform.rotation = Quaternion.Euler(0f, yaw, side * 2.5f);
                Signs.Text("UNDER NEW MANAGEMENT", old, new Vector3(24.4f, 0.75f, side > 0 ? -40.04f : -40.21f), yaw, 0.17f, new Color(0.2f, 0.2f, 0.22f), 1.8f);
            }
            Collider(old.gameObject, new Vector3(0, 1.3f, 0), new Vector3(3.2f, 2.6f, 0.3f));
            Item(old.gameObject, 1, Clean);

            var painted = new GameObject("Painted Sign").transform;
            painted.SetParent(gate, false);
            painted.position = new Vector3(24, 0, -40);
            WorldArt.Model("SignPainted", painted, new Vector3(24, 0, -40), 0f);
            foreach (float side in new[] { 1f, -1f })
            {
                float yaw = side > 0 ? 180f : 0f;
                Signs.Text("KAI'S SURF POOL", painted, new Vector3(24, 2.0f, side > 0 ? -39.97f : -40.13f), yaw, 0.5f, Color.white, 3.3f)
                    .gameObject.AddComponent<NameSign>();
                Signs.Text("OPEN DAILY", painted, new Vector3(24, 0.95f, side > 0 ? -39.97f : -40.13f), yaw, 0.2f, Color.white, 2.2f);
            }
            Collider(painted.gameObject, new Vector3(0, 1.3f, 0), new Vector3(3.6f, 2.6f, 0.3f));
            Item(painted.gameObject, Clean, Landmark);

            var arch = new GameObject("Entrance Arch").transform;
            arch.SetParent(gate, false);
            arch.position = new Vector3(30, 0, -41.5f);
            var archModel = WorldArt.Model("ParkArch", arch, new Vector3(30, 0, -41.5f), 0f);
            Glow(archModel);
            foreach (float side in new[] { 1f, -1f })
            {
                float yaw = side > 0 ? 180f : 0f;
                Signs.Text("KAI'S SURF PARK", arch, new Vector3(30, 6.45f, -41.5f + side * 0.23f), yaw, 1.3f, Color.white, 12.5f)
                    .gameObject.AddComponent<NameSign>();
            }
            foreach (float x in new[] { -6.6f, 6.6f })
                Collider(arch.gameObject, new Vector3(x, 3.5f, 0f), new Vector3(1.2f, 7f, 1.2f));
            Item(arch.gameObject, Landmark);
        }

        // ---------- more life ----------

        /// <summary>A crowd standing at the main pool's north rail, a couple more people per park level.</summary>
        static void Crowd()
        {
            var crowd = Group("Rail Crowd");
            var rng = new System.Random(23);
            int placed = 0;
            for (float x = 3f; x < 58f && placed < 22; x += 2.4f)
            {
                if (x > 27.5f && x < 32.5f) continue; // the lifeguard tower
                var fan = new GameObject("Rail Spectator");
                fan.transform.SetParent(crowd, false);
                fan.transform.SetPositionAndRotation(new Vector3(x + (float)rng.NextDouble() * 0.8f, 0f, N + 1.5f + (float)rng.NextDouble() * 0.9f),
                    Quaternion.Euler(0f, 180f + ((float)rng.NextDouble() - 0.5f) * 40f, 0f));
                GreyboxBuilder.BuildCharacter(fan.transform);
                var spectator = fan.AddComponent<Spectator>();
                var so = new SerializedObject(spectator);
                so.FindProperty("standing").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                // Order of arrival is shuffled along the rail so the crowd fills in evenly.
                int arrival = (placed * 7) % 22;
                Item(fan, Clean + arrival / 2);
                placed++;
            }
        }

        static void Birds()
        {
            var flock = Group("Seagulls");
            for (int i = 0; i < 8; i++)
            {
                var bird = new GameObject("Seagull").transform;
                bird.SetParent(flock, false);
                bird.position = new Vector3(30, 18, 30);
                var model = Raw("Seagull", bird, 2.2f);
                var b = bird.gameObject.AddComponent<Bird>();
                Set(b, "body", model != null ? model.transform : null);
                SetVector(b, "center", new Vector3(20 + i * 6f, 0f, 10 + (i % 3) * 30f));
                SetFloat(b, "radius", 22f + i * 5f);
                SetFloat(b, "height", 13f + (i % 4) * 3f);
                SetFloat(b, "turnSpeed", (i % 2 == 0 ? 1f : -1f) * (10f + i));
                SetFloat(b, "phase", i * 47f);
                // A few gulls hang around the run-down pool; more come as the park (and its snack bar) grows.
                Item(bird.gameObject, i < 3 ? 1 : i < 6 ? Festive : Lit);
            }
        }

        static void Vessels()
        {
            var sea = Group("Ocean Traffic");
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>("Assets/_Game/Audio/SoundBank.asset");

            Vessel(sea, "Fishing Boat", "FishingBoat", 1, 4f, 0.0f, Loop(30, 250, 150, 22, 10), 0.2f, 1.5f, 0f);
            Vessel(sea, "Sailboat", "Sailboat", 4, 5f, 0.3f, new[] { P(-150, -60), P(-172, 80), P(-150, 200), P(-210, 214), P(-236, 60), P(-212, -72) }, 0.25f, 3f, -6f);
            Vessel(sea, "Sailboat 2", "Sailboat", Lit + 1, 5.5f, 0.6f, new[] { P(172, -60), P(192, 60), P(174, 182), P(232, 172), P(244, 40), P(226, -72) }, 0.25f, 3f, -6f);
            var ski = Vessel(sea, "Jet Ski", "JetSki", 7, 15f, 0f, new[] { P(-92, 40), P(-96, 100), P(-104, 142), P(-124, 150), P(-130, 96), P(-118, 38) }, 0.12f, 2f, 14f);
            var ski2 = Vessel(sea, "Jet Ski 2", "JetSki", 7, 14f, 0.5f, new[] { P(-92, 40), P(-96, 100), P(-104, 142), P(-124, 150), P(-130, 96), P(-118, 38) }, 0.12f, 2f, 14f);
            var ski3 = Vessel(sea, "Jet Ski 3", "JetSki", 12, 16f, 0.2f, new[] { P(152, -24), P(150, 60), P(154, 112), P(172, 118), P(176, 40), P(170, -30) }, 0.12f, 2f, 14f);
            foreach (var s in new[] { ski, ski2, ski3 }) Engine(s, bank != null ? bank.jetSkiLoop : null, 0.35f, 10f, 90f);

            // The blimp circles high over the park once it's famous; later a news helicopter buzzes around.
            var blimpPath = new Vector3[10];
            for (int i = 0; i < blimpPath.Length; i++)
            {
                float a = i / (float)blimpPath.Length * Mathf.PI * 2f;
                blimpPath[i] = new Vector3(30 + Mathf.Cos(a) * 150f, 62f, 52 + Mathf.Sin(a) * 180f);
            }
            var blimp = Vessel(sea, "Blimp", "Blimp", 14, 9f, 0.1f, blimpPath, 1.2f, 0.25f, 0f, flipYaw: false);
            if (blimp != null)
                foreach (float side in new[] { 1f, -1f })
                {
                    var text = Signs.Text("KAI'S SURF PARK", blimp.transform, Vector3.zero, 0f, 2.2f, new Color(0.06f, 0.18f, 0.23f), 14f);
                    text.gameObject.AddComponent<NameSign>();
                    text.transform.localPosition = new Vector3(side * 4.3f, 0.6f, -4f);
                    text.transform.localRotation = Quaternion.Euler(0f, side > 0 ? -90f : 90f, 0f);
                }

            var heliPath = new Vector3[8];
            for (int i = 0; i < heliPath.Length; i++)
            {
                float a = i / (float)heliPath.Length * Mathf.PI * 2f;
                heliPath[i] = new Vector3(30 + Mathf.Cos(a) * 80f, 38f + (i % 2) * 6f, 30 + Mathf.Sin(a) * 100f);
            }
            var heli = Vessel(sea, "Helicopter", "Helicopter", 18, 24f, 0.4f, heliPath, 0.6f, 1.5f, 12f);
            if (heli != null)
            {
                var rotor = Raw("HeliRotor", heli.transform.GetChild(0), 1f);
                if (rotor != null)
                {
                    rotor.transform.localPosition = new Vector3(0f, 2.82f, 0.4f);
                    Set(heli.GetComponent<Cruiser>(), "spinner", rotor.transform);
                }
                Engine(heli, bank != null ? bank.heliLoop : null, 0.5f, 20f, 220f);
            }
        }

        static Vector3 P(float x, float z) => new(x, Ocean, z);

        /// <summary>An oval loop on the ocean around (x, z).</summary>
        static Vector3[] Loop(float x, float z, float rx, float rz, int count)
        {
            var points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f;
                points[i] = new Vector3(x + Mathf.Cos(a) * rx, Ocean, z + Mathf.Sin(a) * rz);
            }
            return points;
        }

        static GameObject Vessel(Transform parent, string name, string model, int level, float speed, float startAt, Vector3[] path,
            float bob, float rock, float bank, bool flipYaw = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = path[0];
            var body = Raw(model, go.transform, 1f);
            if (body != null && flipYaw) body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var cruiser = go.AddComponent<Cruiser>();
            var so = new SerializedObject(cruiser);
            var points = so.FindProperty("points");
            points.arraySize = path.Length;
            for (int i = 0; i < path.Length; i++) points.GetArrayElementAtIndex(i).vector3Value = path[i];
            so.FindProperty("speed").floatValue = speed;
            so.FindProperty("startAt").floatValue = startAt;
            so.FindProperty("bobHeight").floatValue = bob;
            so.FindProperty("rock").floatValue = rock;
            so.FindProperty("bank").floatValue = bank;
            so.ApplyModifiedPropertiesWithoutUndo();
            Item(go, level);
            return go;
        }

        static void Engine(GameObject vessel, AudioClip clip, float volume, float min, float max)
        {
            if (vessel == null || clip == null) return;
            var source = vessel.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = min;
            source.maxDistance = max;
            source.dopplerLevel = 0.6f;
            source.volume = 0f;
            var so = new SerializedObject(vessel.GetComponent<Cruiser>());
            so.FindProperty("engine").objectReferenceValue = source;
            so.FindProperty("engineVolume").floatValue = volume;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- celebration particles ----------

        static void CelebrationEffects()
        {
            var party = new GameObject("Celebration");
            party.transform.SetParent(root, false);
            var celebration = party.AddComponent<Celebration>();
            var center = new GameObject("Main Pool Center").transform;
            center.SetParent(party.transform, false);
            center.position = new Vector3(30f, 0f, 0f);
            Set(celebration, "mainPoolCenter", center);
            Set(celebration, "rockets", Fireworks(party.transform));
            Set(celebration, "confetti", Confetti(party.transform));
        }

        static ParticleSystem Fireworks(Transform parent)
        {
            var spark = SparkMaterial("Spark", true, 2.2f);
            var go = new GameObject("Firework Rockets");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(21f, 23f);
            main.startSize = 0.5f;
            main.gravityModifier = 0.35f;
            main.maxParticles = 60;
            main.startColor = new ParticleSystem.MinMaxGradient(FireworkColors()) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 7f;
            shape.radius = 0.1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = 0.35f;
            trails.minVertexDistance = 0.3f;
            trails.widthOverTrail = 0.35f;
            trails.inheritParticleColor = true;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = spark;
            renderer.trailMaterial = spark;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // The burst: a ball of sparks in the rocket's color when it dies.
            var burstGo = new GameObject("Firework Burst");
            burstGo.transform.SetParent(go.transform, false);
            var burst = burstGo.AddComponent<ParticleSystem>();
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var bmain = burst.main;
            bmain.playOnAwake = false;
            bmain.loop = false;
            bmain.duration = 0.1f;
            bmain.simulationSpace = ParticleSystemSimulationSpace.World;
            bmain.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 2f);
            bmain.startSpeed = new ParticleSystem.MinMaxCurve(9f, 13f);
            bmain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            bmain.gravityModifier = 0.22f;
            bmain.maxParticles = 4000;
            var bemission = burst.emission;
            bemission.rateOverTime = 0f;
            bemission.SetBursts(new[] { new ParticleSystem.Burst(0f, 110) });
            var bshape = burst.shape;
            bshape.shapeType = ParticleSystemShapeType.Sphere;
            bshape.radius = 0.3f;
            var drag = burst.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 1.1f;
            var fadeOut = burst.colorOverLifetime;
            fadeOut.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fadeOut.color = gradient;
            var shrink = burst.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));
            var brenderer = burstGo.GetComponent<ParticleSystemRenderer>();
            brenderer.sharedMaterial = SparkMaterial("SparkBurst", false, 1.7f);
            brenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var subs = ps.subEmitters;
            subs.enabled = true;
            subs.AddSubEmitter(burst, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritColor);
            return ps;
        }

        static Gradient FireworkColors()
        {
            var g = new Gradient();
            g.SetKeys(new[]
            {
                new GradientColorKey(new Color(1f, 0.35f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0.25f),
                new GradientColorKey(new Color(0.3f, 0.95f, 0.85f), 0.5f), new GradientColorKey(new Color(0.55f, 0.6f, 1f), 0.75f),
                new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f),
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        static ParticleSystem Confetti(Transform parent)
        {
            var go = new GameObject("Confetti");
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.06f, 0.1f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.1f, 0.16f);
            main.startSizeZ = 1f;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.22f;
            main.maxParticles = 1200;
            var colors = new Gradient();
            colors.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.94f, 0.28f, 0.44f), 0f), new GradientColorKey(new Color(1f, 0.82f, 0.4f), 0.33f),
                new GradientColorKey(new Color(0.18f, 0.77f, 0.71f), 0.66f), new GradientColorKey(Color.white, 1f),
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 1.6f;
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.separateAxes = true;
            spin.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
            spin.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
            spin.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            renderer.mesh = quad.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(quad);
            renderer.sharedMaterial = ParticleMaterial("Confetti", false);
            renderer.alignment = ParticleSystemRenderSpace.Local;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        /// <summary>
        /// A soft round spark, bright enough to bloom. Additive glows (rocket trails); alpha-blended keeps its color
        /// against a bright daytime sky (the bursts).
        /// </summary>
        static Material SparkMaterial(string name, bool additive, float brightness)
        {
            var mat = ParticleMaterial(name, additive);
            mat.SetTexture("_BaseMap", SparkTexture());
            mat.SetColor("_BaseColor", new Color(brightness, brightness, brightness, 1f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material ParticleMaterial(string name, bool additive)
        {
            string path = $"Assets/_Game/Materials/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Cull", 0f);
            if (name.StartsWith("Spark"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", additive ? 2f : 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D SparkTexture()
        {
            const string path = "Assets/_Game/Materials/Spark.png";
            if (!File.Exists(path))
            {
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                {
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------- helpers ----------

        static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent != null ? parent : root, false);
            return t;
        }

        static GlowItem Item(GameObject go, int fromLevel, int untilLevel = 0)
        {
            if (go == null) return null;
            var item = go.GetComponent<GlowItem>() ?? go.AddComponent<GlowItem>();
            item.fromLevel = fromLevel;
            item.untilLevel = untilLevel;
            // Saved in the level-1 look; ParkGlowUp sets the real look on Start.
            go.SetActive(fromLevel <= 1);
            EditorUtility.SetDirty(item);
            return item;
        }

        /// <summary>A flag pole with a fluttering flag.</summary>
        static GameObject FlagPole(Transform parent, Vector3 spot, string flagModel, int level, float scale = 1f)
        {
            var pole = WorldArt.Model("FlagPole", parent, spot, 0f, scale);
            if (pole == null) return null;
            Collider(pole, new Vector3(0, 3f, 0), new Vector3(0.15f, 6f, 0.15f));
            var flag = Raw(flagModel, pole.transform, 1f);
            if (flag != null)
            {
                flag.transform.localPosition = new Vector3(0f, 5.9f, 0f);
                flag.transform.localRotation = Quaternion.Euler(0f, (spot.x * 37f + spot.z * 11f) % 360f, 0f);
                flag.AddComponent<Flutter>();
            }
            Item(pole, level);
            return pole;
        }

        /// <summary>A hanging string (bunting) centered at a point, gently swinging.</summary>
        static GameObject Hang(string model, Transform parent, Vector3 center)
        {
            var hang = WorldArt.Model(model, parent, center, 0f);
            if (hang == null) return null;
            var swing = hang.AddComponent<Flutter>();
            var so = new SerializedObject(swing);
            so.FindProperty("axis").vector3Value = Vector3.right;
            so.FindProperty("degrees").floatValue = 6f;
            so.FindProperty("speed").floatValue = 1.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hang;
        }

        /// <summary>String lights with glowing bulbs.</summary>
        static GameObject Lights(Transform parent, Vector3 center)
        {
            var lights = WorldArt.Model("StringLights", parent, center, 0f);
            Glow(lights);
            return lights;
        }

        /// <summary>Swaps a model's "Bulb" material for a glowing one.</summary>
        static void Glow(GameObject model)
        {
            if (model == null) return;
            var bulb = GreyboxBuilder.Mat("BulbGlow", new Color(1f, 0.9f, 0.6f), 0.5f, new Color(2.4f, 1.9f, 1.0f));
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name.StartsWith("Bulb")) { mats[i] = bulb; changed = true; }
                if (changed) renderer.sharedMaterials = mats;
            }
        }

        /// <summary>A model placed as-is (no extra yaw): its nose points along the parent's forward.</summary>
        static GameObject Raw(string name, Transform parent, float scale)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Game/Models/{name}.fbx");
            if (asset == null)
            {
                Debug.LogWarning($"[GlowUpBuilder] Missing model {name}. Run tools/blender/build_models.py in Blender.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        /// <summary>A box collider in the object's own space (so the player bumps into poles and signs).</summary>
        static void Collider(GameObject go, Vector3 center, Vector3 size)
        {
            if (go == null) return;
            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetVector(Object target, string field, Vector3 value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
