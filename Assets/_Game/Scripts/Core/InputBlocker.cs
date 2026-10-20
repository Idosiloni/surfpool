using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Stops player movement, mouse-look and interaction while a menu is open.
    /// Stays blocked for the frame a menu closes so the closing key press isn't reused.
    /// </summary>
    public static class InputBlocker
    {
        static int blockers;
        static int releasedFrame = -1;

        public static bool Blocked => blockers > 0 || Time.frameCount <= releasedFrame;

        public static void Block() => blockers++;

        public static void Release()
        {
            blockers = Mathf.Max(0, blockers - 1);
            releasedFrame = Time.frameCount;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            blockers = 0;
            releasedFrame = -1;
        }
    }
}
