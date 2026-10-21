using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Look tuning helper: in Play mode, Surf Pool > Spawn Look Lineup puts one person of every surfer type (plus
    /// beach-goers) in a row on the south deck, so the house style can be checked at a glance (and screenshotted
    /// with DevShots). Nothing is saved; the lineup disappears when Play mode stops.
    /// </summary>
    public static class LookLineup
    {
        public static readonly Vector3 Origin = new(14f, 0f, -30f);

        [MenuItem("Surf Pool/Spawn Look Lineup")]
        public static void SpawnMenu() => Spawn(Origin, 0);

        /// <summary>Spawns the lineup facing -z (toward a camera south of it). Seed changes the random extras.</summary>
        public static string Spawn(Vector3 origin, int seed, float spacing = 1.3f)
        {
            if (!Application.isPlaying) return "Enter Play mode first.";
            var old = GameObject.Find("Look Lineup");
            if (old != null) Object.Destroy(old);
            var root = new GameObject("Look Lineup").transform;
            Random.InitState(seed);
            var types = (Archetype[])System.Enum.GetValues(typeof(Archetype));
            int i = 0;
            foreach (var type in types) Person(root, origin + Vector3.right * spacing * i++, CharacterLooks.For(type), type.ToString());
            for (int k = 0; k < 3; k++) Person(root, origin + Vector3.right * spacing * i++, CharacterLooks.Beachgoer(), "Guest");
            return $"Spawned {i} people.";
        }

        static void Person(Transform root, Vector3 at, CharacterLook look, string name)
        {
            var go = new GameObject($"Lineup {name}");
            go.SetActive(false); // so the rig wakes up after its joints are wired
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 180f, 0f));
            var rig = GreyboxBuilder.BuildCharacter(go.transform);
            go.SetActive(true);
            rig.Wear(look);
            rig.CurrentMode = CharacterRig.Mode.Locomotion;
        }
    }
}
