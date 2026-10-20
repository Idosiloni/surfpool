using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The snack bar by the main pool's exit (an upgrade). Happy surfers leaving the main pool often stop for food:
    /// they line up at the counter, and you serve them at the grill (E), or the Snack Cook does it. Snacks are
    /// extra income, but people who just ate drop more trash.
    /// </summary>
    public class SnackBar : MonoBehaviour, IInteractable
    {
        public static SnackBar Instance { get; private set; }

        [Tooltip("Front of the line, at the counter.")]
        [SerializeField] Transform lineSpot;
        [Tooltip("Where surfers walk to before joining the back of the line (sets which way the line grows).")]
        [SerializeField] Transform approach;
        [Tooltip("From the counter to the park exit.")]
        [SerializeField] Transform[] exitPath;
        [SerializeField] float queueSpacing = 1.1f;
        [Tooltip("Chance a happy surfer from the main pool stops for a snack.")]
        [Range(0f, 1f)] [SerializeField] float visitChance = 0.6f;
        [SerializeField] int snackPrice = 10;

        readonly List<Surfer> line = new();

        public bool Open => isActiveAndEnabled;
        public int Price => snackPrice + ParkEffects.SnackPriceBonus;
        public Vector3[] ApproachPath => new[] { approach.position };
        public Vector3[] ExitPath => System.Array.ConvertAll(exitPath, t => t.position);
        public Surfer NextCustomer => line.Count > 0 && line[0].WaitingForSnack ? line[0] : null;

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Does this surfer stop for food on the way out?</summary>
        public bool WantsSnack(float happiness) => Open && happiness > 0.4f && Random.value < visitChance;

        public void Join(Surfer surfer)
        {
            if (!line.Contains(surfer)) line.Add(surfer);
        }

        public void Leave(Surfer surfer) => line.Remove(surfer);

        /// <summary>Where this surfer stands in the snack line, and whether that's the front.</summary>
        public (Vector3 slot, bool front, Vector3 facing) LineSlot(Surfer surfer)
        {
            int index = Mathf.Max(0, line.IndexOf(surfer));
            Vector3 back = approach.position - lineSpot.position;
            back.y = 0f;
            back.Normalize();
            return (lineSpot.position + back * (queueSpacing * index), index == 0, -back);
        }

        public string Prompt => NextCustomer != null ? $"Serve snack ({MoneyText.Format(Price)})" : "Snack grill (no one waiting)";

        public void Interact() => Serve();

        /// <summary>Hands food to the surfer at the front of the line. Returns false if no one's ready.</summary>
        public bool Serve()
        {
            var customer = NextCustomer;
            if (customer == null) return false;
            Wallet.Instance.Add(Price, "snacks");
            GameStats.SnacksSold++;
            Leave(customer);
            customer.ReceiveSnack();
            return true;
        }
    }
}
