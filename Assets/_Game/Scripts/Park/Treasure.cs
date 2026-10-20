using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// One of Uncle Kai's lost treasures hidden around the island (shells, a message in a bottle, his old board...).
    /// They glow and bob so they can be spotted from a distance. Pick one up with E for cash and XP; finding all of
    /// them pays a big bonus. Which ones were found is saved.
    /// </summary>
    public class Treasure : MonoBehaviour, IInteractable
    {
        public static readonly List<Treasure> All = new();
        public const int FindReward = 150, FindXp = 25, AllFoundBonus = 5000;

        [SerializeField] int index;
        [SerializeField] string title = "Lost treasure";
        [SerializeField] string note = "";

        Vector3 home;

        public int Index => index;
        public static int Found { get; private set; }
        public static int Total => All.Count + Found;

        public string Prompt => $"Pick up {title}";

        void Awake() => home = transform.position;
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Found = 0;

        void Update()
        {
            transform.position = home + Vector3.up * (0.15f + Mathf.Sin(Time.time * 2f + index) * 0.08f);
            transform.Rotate(0f, 50f * Time.deltaTime, 0f, Space.World);
        }

        public void Interact()
        {
            Found++;
            int left = All.Count - 1;
            Wallet.Instance.Add(FindReward, "treasure");
            ParkLevel.Add(FindXp);
            Sfx.Play(Sound.Buy);
            Notifications.Post($"Found {title}! {note} ({Found}/{Found + left})");
            if (left == 0)
            {
                Wallet.Instance.Add(AllFoundBonus, "treasure");
                Notifications.Post($"You found ALL of Uncle Kai's treasures! Bonus {MoneyText.Format(AllFoundBonus)}!");
                Sfx.Play(Sound.Mission);
                Sfx.Duck();
            }
            Destroy(gameObject);
        }

        /// <summary>Indices of treasures already found (for the save).</summary>
        public static List<int> FoundIndices(int total)
        {
            var found = new List<int>();
            for (int i = 0; i < total; i++)
                if (!All.Exists(t => t.index == i)) found.Add(i);
            return found;
        }

        /// <summary>From a save: removes the treasures that were already found.</summary>
        public static void Restore(List<int> found)
        {
            if (found == null) return;
            foreach (var treasure in All.ToArray())
                if (found.Contains(treasure.index))
                {
                    Found++;
                    Destroy(treasure.gameObject);
                }
        }
    }
}
