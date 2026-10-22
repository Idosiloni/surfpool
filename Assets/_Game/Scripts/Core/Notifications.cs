using System;
using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>
    /// Short messages for the player ("Bought Turbo Pump!"). The HUD shows them as toasts, and the last 30 are kept
    /// in the Mail tab of the computer (<see cref="Log"/>) so a missed toast is never lost.
    /// </summary>
    public static class Notifications
    {
        public const int LogSize = 30;

        /// <summary>A message kept for the Mail tab, with when it came (park day and clock).</summary>
        public readonly struct Mail
        {
            public readonly string message, when;
            public readonly Notice kind;

            public Mail(string message, Notice kind, string when)
            {
                this.message = message;
                this.kind = kind;
                this.when = when;
            }
        }

        static readonly List<Mail> log = new();

        /// <summary>Fires with the message and its kind.</summary>
        public static event Action<string, Notice> Posted;

        /// <summary>Kept messages, oldest first.</summary>
        public static IReadOnlyList<Mail> Log => log;

        /// <summary>Messages that came since the Mail tab was last opened.</summary>
        public static int Unread { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            log.Clear();
            Unread = 0;
        }

        public static void Post(string message, bool bad = false) => Post(message, bad ? Notice.Bad : Notice.Info);

        public static void Post(string message, Notice kind) => Post(message, kind, true);

        /// <summary>Posts a toast; <paramref name="keep"/> = false for chatter that isn't worth keeping (PA jokes).</summary>
        public static void Post(string message, Notice kind, bool keep)
        {
            if (keep)
            {
                var day = DayCycle.Instance;
                log.Add(new Mail(message, kind, day != null ? $"Day {day.Day}, {day.Clock}" : ""));
                if (log.Count > LogSize) log.RemoveAt(0);
                Unread = Mathf.Min(Unread + 1, LogSize);
            }
            Posted?.Invoke(message, kind);
        }

        public static void MarkRead() => Unread = 0;
    }
}
