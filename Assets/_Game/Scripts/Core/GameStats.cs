using UnityEngine;

namespace SurfPool
{
    /// <summary>Running totals for the whole game, used by missions, the daily report and saving.</summary>
    public static class GameStats
    {
        public static int TicketsSold;
        public static int WavesSent;
        public static int SurfersServed;
        public static int BoardsRented;
        public static int TubeRides;
        public static int TricksLanded;
        public static int Wipeouts;
        public static long TotalEarned;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            TicketsSold = 0;
            WavesSent = 0;
            SurfersServed = 0;
            BoardsRented = 0;
            TubeRides = 0;
            TricksLanded = 0;
            Wipeouts = 0;
            TotalEarned = 0;
        }
    }
}
