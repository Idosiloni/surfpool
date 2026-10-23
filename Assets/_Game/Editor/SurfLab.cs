using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Agent test bench for surfing (Play mode, via eval): put a surfer straight into a pool's lineup, fire a wave of
    /// a chosen level, and record every ride (NPCs and you) as a trace of positions in the water's own frame plus
    /// screenshots, to compare before/after changes to how riding works.
    ///   SurfLab.Rider("Main Pool", WaveLevel.Pro); SurfLab.Wave("Main Pool", WaveLevel.Pro);
    ///   SurfLab.Record(12f, 0.5f, "/path/folder"); ... SurfLab.Dump();
    /// </summary>
    public static class SurfLab
    {
        static readonly FieldInfo StateField = typeof(Surfer).GetField("state", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo WaterField = typeof(Surfer).GetField("water", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo WaveField = typeof(Surfer).GetField("rideWave", BindingFlags.NonPublic | BindingFlags.Instance);

        public static PoolArea Pool(string name)
        {
            foreach (var pool in PoolRegistry.All)
                if (pool.name == name || pool.DisplayName == name) return pool;
            return null;
        }

        /// <summary>A new surfer of this skill, waiting in the lineup of the pool (skips the desk).</summary>
        public static string Rider(string poolName, WaveLevel skill)
        {
            var pool = Pool(poolName);
            var spawner = Object.FindAnyObjectByType<SurferSpawner>();
            var prefab = (Surfer)typeof(SurferSpawner).GetField("surferPrefab", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(spawner);
            var surfer = Object.Instantiate(prefab, pool.Lineup.position, Quaternion.identity);
            surfer.Init(spawner, false);
            surfer.TestSendToLineup(pool, skill);
            return $"{surfer.FirstName} ({skill}) waiting in {pool.DisplayName}";
        }

        /// <summary>Fires a wave of this level now (the machine's cooldown is skipped).</summary>
        public static string Wave(string poolName, WaveLevel level, WaveDirection direction = WaveDirection.Peak)
        {
            var machine = Pool(poolName).Machine;
            machine.Select(level);
            if (direction != WaveDirection.Peak) machine.Select(direction);
            typeof(WaveMachine).GetField("readyAt", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(machine, 0f);
            return machine.TryTriggerWave() ? $"{level} wave sent" : "machine refused";
        }

        /// <summary>You jump into the pool's lineup.</summary>
        public static string Player(string poolName)
        {
            var surfing = PlayerSurfing.Instance;
            if (surfing.IsSurfing) surfing.Exit();
            surfing.Enter(Pool(poolName));
            return "in the water";
        }

        // ---------------------------------------------------------------- recording

        static readonly StringBuilder trace = new();
        static float until, nextSample, nextShot, shotEvery;
        static string folder;
        static int shots;

        public static bool Recording { get; private set; }

        /// <summary>Samples every rider 10 times a second for a while; with a folder, saves a picture every few seconds.</summary>
        public static string Record(float seconds, float pictureEvery = 0f, string pictureFolder = null)
        {
            trace.Clear();
            until = Time.time + seconds;
            nextSample = nextShot = Time.time;
            shotEvery = pictureEvery;
            folder = pictureFolder;
            shots = 0;
            if (folder != null) Directory.CreateDirectory(folder);
            var player = PlayerSurfing.Instance;
            if (player != null)
            {
                player.Called -= OnCalled;
                player.Called += OnCalled; // the player's call-outs (tricks, warnings) go into the trace
            }
            if (!Recording) EditorApplication.update += Tick;
            Recording = true;
            return "recording";
        }

        static void OnCalled(string text, Notice kind)
        {
            if (Recording) trace.Append($"{Time.time:F1} >> {text}\n");
        }

        public static string Dump() => (Recording ? "(still recording)\n" : "") + trace;

        static void Tick()
        {
            if (!Application.isPlaying || Time.time >= until)
            {
                EditorApplication.update -= Tick;
                Recording = false;
                return;
            }
            if (Time.time >= nextSample)
            {
                nextSample = Time.time + 0.1f;
                Sample();
            }
            if (folder != null && shotEvery > 0f && Time.time >= nextShot)
            {
                nextShot = Time.time + shotEvery;
                Picture();
            }
        }

        static void Sample()
        {
            var player = PlayerSurfing.Instance;
            if (player != null && player.IsRiding)
            {
                var w = player.Pool.Water;
                Line("YOU", w, player.transform, float.NaN);
            }
            foreach (var surfer in Surfer.All)
            {
                if (StateField.GetValue(surfer).ToString() != "Riding") continue;
                var w = (WaveWater)WaterField.GetValue(surfer);
                Line(surfer.FirstName, w, surfer.transform, w.CurrentWaveCenter((float)WaveField.GetValue(surfer)));
            }
        }

        static readonly Dictionary<string, Vector3> last = new();

        static void Line(string who, WaveWater water, Transform rider, float crest)
        {
            var local = water.transform.InverseTransformPoint(rider.position);
            var forward = water.transform.InverseTransformDirection(rider.forward);
            float yaw = Mathf.Atan2(forward.z, forward.x) * Mathf.Rad2Deg; // 0 = to the beach, +90 = along +z
            float speed = last.TryGetValue(who, out var before) ? Vector3.Distance(before, rider.position) / 0.1f : 0f;
            last[who] = rider.position;
            trace.Append($"{Time.time:F1} {who,-8} x{local.x,6:F1} z{local.z,6:F1} y{local.y,5:F2} yaw{yaw,5:F0} v{speed,4:F1}");
            if (!float.IsNaN(crest)) trace.Append($" crest{crest,6:F1}");
            trace.Append('\n');
        }

        /// <summary>From the player's camera while you ride; otherwise from in front of the first NPC riding.</summary>
        static void Picture()
        {
            string path = Path.Combine(folder, $"frame_{shots++:00}.png");
            var player = PlayerSurfing.Instance;
            if (player != null && player.IsSurfing && Camera.main != null)
            {
                var cam = Camera.main.transform;
                DevShots.Shot(path, cam.position, cam.position + cam.forward * 10f, Camera.main.fieldOfView, 960, 600);
                return;
            }
            foreach (var surfer in Surfer.All)
            {
                if (StateField.GetValue(surfer).ToString() != "Riding") continue;
                var water = (WaveWater)WaterField.GetValue(surfer);
                var toBeach = water.transform.right;
                var along = water.transform.forward;
                Vector3 at = surfer.transform.position;
                DevShots.Shot(path, at + toBeach * 14f + along * 4f + Vector3.up * 5f, at, 50f, 960, 600);
                return;
            }
        }
    }
}
