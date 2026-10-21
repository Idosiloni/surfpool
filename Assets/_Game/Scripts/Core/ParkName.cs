using System.Text;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The park's name: chosen on the naming card at the start (<see cref="ParkNamer"/>), painted on the gate signs,
    /// the arch, the blimp and the welcome sign (<see cref="NameSign"/>), and used in the day report and title screen.
    /// Saved by <see cref="SaveSystem"/>. Empty means "not named yet" (old saves), which shows as Uncle Kai's name.
    /// </summary>
    public static class ParkName
    {
        public const string Default = "Kai's Surf Pool";
        public const int MaxLength = 18;

        static string chosen = "";

        /// <summary>Fires when the name changes (signs repaint).</summary>
        public static event System.Action Changed;

        public static string Name => string.IsNullOrEmpty(chosen) ? Default : chosen;
        public static bool Named => !string.IsNullOrEmpty(chosen);

        /// <summary>The raw saved value ("" until the player picks a name).</summary>
        public static string Saved => chosen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatics()
        {
            chosen = "";
            Changed = null;
        }

        /// <summary>Loads a saved name without any fuss.</summary>
        public static void Load(string saved)
        {
            chosen = Clean(saved ?? "");
            Changed?.Invoke();
        }

        public static void Set(string name)
        {
            string clean = Clean(name);
            chosen = clean.Length > 0 ? clean : Default;
            Changed?.Invoke();
        }

        /// <summary>Only letters, digits, spaces and ' &amp; - ! (the sign painter's letters), single spaces, max length.</summary>
        public static string Clean(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name)
            {
                bool ok = c < 128 && (char.IsLetterOrDigit(c) || c == '\'' || c == '&' || c == '-' || c == '!' || c == ' ');
                if (!ok) continue;
                if (c == ' ' && (sb.Length == 0 || sb[sb.Length - 1] == ' ')) continue;
                sb.Append(c);
                if (sb.Length >= MaxLength) break;
            }
            return sb.ToString().Trim();
        }

        static readonly string[] Starts =
        {
            "Barrel", "Salty", "Gnarly", "Stoked", "Tubular", "Foamy", "Wipeout", "Pipe Dream", "Kook", "Shaka",
            "Mellow", "Glassy", "Choppy", "Rad", "Coconut", "Sunburn", "Sandy", "Big Kahuna",
        };

        static readonly string[] Ends =
        {
            "Surf Park", "Wave Pool", "Surf Club", "Swell Co", "Bowl", "Lagoon", "Splash Zone", "Surf Shack",
            "Wave Ranch", "Beach Club",
        };

        static readonly string[] Whole =
        {
            "Barrel Bros", "Kook Haven", "Pipe Dreams", "The Foam Zone", "Wipeout Willy's", "Salty Dog Surf",
            "Gnarly Grove", "Tube Town", "Shaka Shack", "Big Wave Dave's", "Totally Tubular", "The Stoke Pool",
            "Paddle & Pray", "Cowabunga Cove", "Wax On Wax Off",
        };

        /// <summary>A silly random suggestion for the naming card.</summary>
        public static string Suggest()
        {
            if (Random.value < 0.4f) return Clean(Whole[Random.Range(0, Whole.Length)]);
            string name = Starts[Random.Range(0, Starts.Length)] + " " + Ends[Random.Range(0, Ends.Length)];
            return name.Length <= MaxLength ? name : Clean(Whole[Random.Range(0, Whole.Length)]);
        }
    }
}
