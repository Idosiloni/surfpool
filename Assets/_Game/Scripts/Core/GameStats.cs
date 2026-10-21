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
        public static int PlayerRides;
        public static int SnacksSold;
        public static int ShopSales;
        public static int CustomBoards;
        public static int BeachGuests;
        public static int FishCaught;
        public static int TrashCollected;
        public static long TotalEarned;
        /// <summary>Your best judged ride (0-10).</summary>
        public static float BestJudgeScore;
        /// <summary>Your own surfing (for daily challenges; not saved, counted per play session).</summary>
        public static int PlayerTricks, PlayerSpins, PlayerGoodRides;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            TrashCollected = 0;
            FishCaught = 0;
            BeachGuests = 0;
            SnacksSold = 0;
            ShopSales = 0;
            CustomBoards = 0;
            TicketsSold = 0;
            WavesSent = 0;
            SurfersServed = 0;
            BoardsRented = 0;
            TubeRides = 0;
            TricksLanded = 0;
            Wipeouts = 0;
            PlayerRides = 0;
            TotalEarned = 0;
            BestJudgeScore = 0f;
            PlayerTricks = PlayerSpins = PlayerGoodRides = 0;
        }
    }
}
