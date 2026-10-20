using System;

namespace SurfPool
{
    /// <summary>What kind of message a toast is: it picks the toast's icon and color.</summary>
    public enum Notice
    {
        Info,
        Good,
        Reward,
        Bad,
    }

    /// <summary>Short messages for the player ("Bought Turbo Pump!"). The HUD shows them as toasts.</summary>
    public static class Notifications
    {
        /// <summary>Fires with the message and its kind.</summary>
        public static event Action<string, Notice> Posted;

        public static void Post(string message, bool bad = false) => Post(message, bad ? Notice.Bad : Notice.Info);

        public static void Post(string message, Notice kind) => Posted?.Invoke(message, kind);
    }
}
