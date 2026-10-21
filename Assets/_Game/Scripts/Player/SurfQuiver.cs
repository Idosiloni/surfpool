using UnityEngine;

namespace SurfPool
{
    /// <summary>The boards you can surf on (pick one at the board rack by the surf peg).</summary>
    public enum BoardKind { Shortboard, Foamie, Longboard, Fish }

    /// <summary>
    /// Your quiver: the board under your feet changes how surfing feels (docs/research/surf-maneuvers.md):
    /// Shortboard = the all-rounder, best in big hollow waves and for airs. Foamie = stable and forgiving, easy
    /// take-offs, never pearls, but no real tricks. Longboard = glides on small waves, walk to the nose for a hang
    /// ten, slow turns, no airs or snaps. Fish = fast and loose on small waves, pumps better, twitchy in big surf.
    /// You start with the shortboard and a foamie; the fish and the longboard are sold at the Surf Gear Shop (the
    /// boards standing out front). The choice and the boards you own are remembered with the save.
    /// </summary>
    public static class SurfQuiver
    {
        public static BoardKind Current { get; private set; } = BoardKind.Shortboard;
        public static event System.Action Changed;

        static readonly System.Collections.Generic.HashSet<BoardKind> owned = new() { BoardKind.Shortboard, BoardKind.Foamie };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = BoardKind.Shortboard;
            Changed = null;
            ResetOwned();
        }

        public static bool Owns(BoardKind kind) => owned.Contains(kind);

        /// <summary>Every board is in your quiver.</summary>
        public static bool OwnsAll => owned.Count == System.Enum.GetValues(typeof(BoardKind)).Length;

        /// <summary>What a board costs at the Surf Gear Shop (0 = one you start with).</summary>
        public static int Price(BoardKind kind) => kind switch
        {
            BoardKind.Fish => 900,
            BoardKind.Longboard => 1400,
            _ => 0,
        };

        /// <summary>A new board for your quiver (paid for by the shop); you pick it up right away.</summary>
        public static void Add(BoardKind kind)
        {
            owned.Add(kind);
            Choose(kind);
        }

        public static void Choose(BoardKind kind)
        {
            if (!owned.Contains(kind)) return;
            Current = kind;
            Changed?.Invoke();
        }

        public static string[] SaveOwned()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var kind in owned) list.Add(kind.ToString());
            return list.ToArray();
        }

        /// <summary>Loads the board you use and the ones you own (null = an old save: keep the one in use).</summary>
        public static void Load(string saved, string[] savedOwned)
        {
            ResetOwned();
            var current = System.Enum.TryParse(saved, out BoardKind kind) ? kind : BoardKind.Shortboard;
            if (savedOwned != null)
                foreach (var name in savedOwned)
                    if (System.Enum.TryParse(name, out BoardKind k)) owned.Add(k);
            owned.Add(current);
            Current = current;
            Changed?.Invoke();
        }

        static void ResetOwned()
        {
            owned.Clear();
            owned.Add(BoardKind.Shortboard);
            owned.Add(BoardKind.Foamie);
        }

        /// <summary>The next board you own, after the one in use.</summary>
        public static BoardKind Next
        {
            get
            {
                int count = System.Enum.GetValues(typeof(BoardKind)).Length;
                for (int i = 1; i <= count; i++)
                {
                    var kind = (BoardKind)(((int)Current + i) % count);
                    if (owned.Contains(kind)) return kind;
                }
                return Current;
            }
        }

        public static string Blurb(BoardKind kind) => kind switch
        {
            BoardKind.Foamie => "Foamie: stable and forgiving. Easy take-offs, never nose-dives. No real tricks.",
            BoardKind.Longboard => "Longboard: glides on small waves. Hold W while trimming to walk to the nose (hang ten). No airs or snaps.",
            BoardKind.Fish => "Fish: fast and loose on small waves, pumps better. Twitchy on Pro waves.",
            _ => "Shortboard: the all-rounder. Best on big, hollow waves and for airs.",
        };

        /// <summary>Carving speed on this wave (small waves suit wide boards, big ones suit shortboards).</summary>
        public static float SpeedOn(WaveLevel wave) => Current switch
        {
            BoardKind.Foamie => 0.75f,
            BoardKind.Longboard => wave == WaveLevel.Pro ? 0.75f : 0.9f,
            BoardKind.Fish => wave == WaveLevel.Pro ? 0.95f : 1.12f,
            _ => wave == WaveLevel.Pro ? 1.05f : 1f,
        };

        public static bool CanAir => Current is BoardKind.Shortboard or BoardKind.Fish;
        public static bool CanSnap => Current is BoardKind.Shortboard or BoardKind.Fish;
        public static bool CanNoseRide => Current == BoardKind.Longboard;
        /// <summary>Foamies and longboards are too stable to pearl on a late take-off.</summary>
        public static bool CanPearl => Current is BoardKind.Shortboard or BoardKind.Fish;
        /// <summary>How much flow pumping can add in one ride (fish pump best).</summary>
        public static float PumpCap => Current == BoardKind.Fish ? 0.5f : Current == BoardKind.Foamie ? 0.15f : 0.35f;
        /// <summary>Foamies make the take-off easier: a wider PERFECT window.</summary>
        public static float TakeoffWindow => Current == BoardKind.Foamie ? 1.6f : Current == BoardKind.Longboard ? 1.3f : 1f;
    }
}
