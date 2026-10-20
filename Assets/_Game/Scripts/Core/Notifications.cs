using System;

namespace SurfPool
{
    /// <summary>Short messages for the player ("Bought Turbo Pump!"). The HUD shows them as toasts.</summary>
    public static class Notifications
    {
        /// <summary>Fires with the message and whether it's bad news.</summary>
        public static event Action<string, bool> Posted;

        public static void Post(string message, bool bad = false) => Posted?.Invoke(message, bad);
    }
}
