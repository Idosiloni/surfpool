using UnityEngine;

namespace SurfPool
{
    /// <summary>First names for surfers (shown at the desk and in reviews). Mostly names that fit anyone.</summary>
    public static class SurferNames
    {
        static readonly string[] Names =
        {
            "Maya", "Kai", "River", "Sky", "Jordan", "Max", "Sam", "Alex", "Charlie", "Riley", "Taylor", "Jamie",
            "Quinn", "Noa", "Bodhi", "Leilani", "Kurt", "Brenda", "Dusty", "Coral", "Finn", "Marlo", "Ziggy", "Pua",
            "Tama", "Ari", "Remy", "Oakley", "Sage", "Indie", "Rio", "Nalu", "Kenji", "Lola", "Hugo", "Ines", "Mateo",
            "Zoe", "Theo", "Ivy", "Beck", "Juno", "Otis", "Wren", "Ezra", "Mika", "Kona", "Lark", "Dex", "Bea",
            "Gus", "Tilly", "Moana", "Rocco", "Fern", "Ollie", "Yuki", "Santi", "Ruby", "Clem", "Benji", "Nina",
            "Dale", "Pip", "Sunny", "Kevin", "Gary", "Linda", "Barb", "Chad", "Skip", "Moose", "Tank",
        };

        static readonly System.Collections.Generic.Queue<string> recent = new();

        /// <summary>A name nobody in the park has right now and nobody had lately (if possible).</summary>
        public static string Pick()
        {
            string name = Names[Random.Range(0, Names.Length)];
            for (int tries = 0; tries < 12; tries++)
            {
                bool taken = recent.Contains(name);
                foreach (var surfer in Surfer.All)
                    if (surfer.FirstName == name) { taken = true; break; }
                if (!taken) break;
                name = Names[Random.Range(0, Names.Length)];
            }
            recent.Enqueue(name);
            if (recent.Count > 40) recent.Dequeue();
            return name;
        }
    }
}
