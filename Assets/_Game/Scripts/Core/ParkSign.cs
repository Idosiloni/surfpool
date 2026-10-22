using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The OPEN / CLOSED sign at the ticket desk. It flips to CLOSED by itself at 8 PM; once the last guests have
    /// left, E on it ends the day (bills, taxes, the day report). In the morning it flips back to OPEN.
    /// </summary>
    public class ParkSign : MonoBehaviour, IInteractable
    {
        [Tooltip("The two-sided board (OPEN on its front, CLOSED on its back); it spins half a turn to flip.")]
        [SerializeField] Transform board;

        float shownAngle, targetAngle;

        public string Prompt
        {
            get
            {
                var day = DayCycle.Instance;
                if (day == null) return null;
                if (!day.ParkClosed) return "OPEN until 8 PM";
                int left = DayCycle.GuestsLeft;
                return left > 0 ? $"CLOSED: waiting for {left} guest{(left == 1 ? "" : "s")} to leave" : "End the day (pay the bills)";
            }
        }

        public bool CanUse => DayCycle.Instance != null && DayCycle.Instance.ParkClosed && DayCycle.GuestsLeft == 0;

        public void Interact()
        {
            var day = DayCycle.Instance;
            if (day == null || !day.ParkClosed) return;
            if (!day.EndDayNow()) Sfx.Play(Sound.Error);
            else Sfx.Play(Sound.Click, transform.position);
        }

        void Update()
        {
            var day = DayCycle.Instance;
            targetAngle = day != null && (day.ParkClosed || day.Closed) ? 180f : 0f;
            if (Mathf.Approximately(shownAngle, targetAngle)) return;
            bool wasOpen = shownAngle < 90f;
            shownAngle = Mathf.MoveTowards(shownAngle, targetAngle, 540f * Time.unscaledDeltaTime);
            if (board != null) board.localRotation = Quaternion.Euler(0f, shownAngle, 0f);
            if (wasOpen != shownAngle < 90f) Sfx.Play(Sound.Click, transform.position);
        }
    }
}
