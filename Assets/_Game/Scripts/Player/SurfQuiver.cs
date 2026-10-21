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
    /// The choice is remembered with the save.
    /// </summary>
    public static class SurfQuiver
    {
        public static BoardKind Current { get; private set; } = BoardKind.Shortboard;
        public static event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = BoardKind.Shortboard;
            Changed = null;
        }

        public static void Choose(BoardKind kind)
        {
            Current = kind;
            Changed?.Invoke();
        }

        public static void Load(string saved)
        {
            Current = System.Enum.TryParse(saved, out BoardKind kind) ? kind : BoardKind.Shortboard;
            Changed?.Invoke();
        }

        public static BoardKind Next => (BoardKind)(((int)Current + 1) % System.Enum.GetValues(typeof(BoardKind)).Length);

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
