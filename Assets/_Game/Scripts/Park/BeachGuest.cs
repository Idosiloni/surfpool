using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A beach guest (not a surfer): walks from the entrance to a free lounger or bench, pays for a beach pass,
    /// relaxes for a while, then walks home. Now and then drops a bit of litter on the way.
    /// </summary>
    [RequireComponent(typeof(CharacterRig))]
    public class BeachGuest : MonoBehaviour
    {
        public static readonly List<BeachGuest> All = new();

        [SerializeField] float walkSpeed = 1.5f;

        enum Phase { WalkingIn, Relaxing, WalkingOut }

        CharacterRig rig;
        Phase phase;
        Transform seat;
        Decoration seatOwner;
        Vector3 exit;
        float relaxUntil;
        int pass;
        readonly List<Vector3> route = new();

        void OnEnable() => All.Add(this);

        void OnDisable()
        {
            All.Remove(this);
            if (seatOwner != null && seat != null) seatOwner.ReleaseSeat(seat);
        }

        /// <summary>Sends this guest to a seat. Returns false (and cleans up) if there's no free seat.</summary>
        public bool Begin(Vector3 entrance, int beachPass, float relaxSeconds)
        {
            rig = GetComponent<CharacterRig>();
            var skin = Color.Lerp(new Color(0.42f, 0.27f, 0.17f), new Color(1f, 0.84f, 0.7f), Random.value);
            var hair = Random.value < 0.3f ? new Color(0.95f, 0.82f, 0.45f) : Color.Lerp(new Color(0.08f, 0.06f, 0.05f), new Color(0.45f, 0.28f, 0.15f), Random.value);
            rig.Paint(skin, Random.ColorHSV(0f, 1f, 0.4f, 0.9f, 0.7f, 1f), Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.5f, 0.9f), hair, Random.Range(0, rig.HairStyles));
            seat = Decoration.ClaimSeat(out seatOwner);
            if (seat == null)
            {
                Destroy(gameObject);
                return false;
            }
            exit = entrance;
            pass = beachPass;
            relaxUntil = relaxSeconds;
            transform.position = entrance;
            WalkRouter.Plan(entrance, seat.position, route);
            phase = Phase.WalkingIn;
            return true;
        }

        void Update()
        {
            if (seatOwner == null && phase != Phase.WalkingOut) // their lounger was sold: go home
            {
                LeaveForHome();
                return;
            }
            switch (phase)
            {
                case Phase.WalkingIn:
                    rig.CurrentMode = CharacterRig.Mode.Locomotion;
                    if (WalkRoute()) Arrive();
                    break;
                case Phase.Relaxing:
                    if (Time.time >= relaxUntil) LeaveForHome();
                    break;
                case Phase.WalkingOut:
                    rig.CurrentMode = CharacterRig.Mode.Locomotion;
                    Trash.MaybeDrop(transform.position, Time.deltaTime * 0.6f);
                    if (WalkRoute()) Destroy(gameObject);
                    break;
            }
        }

        void Arrive()
        {
            phase = Phase.Relaxing;
            transform.SetPositionAndRotation(seat.position, seat.rotation);
            rig.CurrentMode = seat.Find("Lying") != null ? CharacterRig.Mode.Lie : CharacterRig.Mode.Sit;
            relaxUntil = Time.time + relaxUntil;
            Wallet.Instance.Add(pass, "beach guests");
            GameStats.BeachGuests++;
        }

        void LeaveForHome()
        {
            if (seatOwner != null && seat != null) seatOwner.ReleaseSeat(seat);
            seatOwner = null;
            phase = Phase.WalkingOut;
            WalkRouter.Plan(transform.position, exit, route);
        }

        /// <summary>Walks along the route on the ground. True when the end is reached.</summary>
        bool WalkRoute()
        {
            if (route.Count == 0) return true;
            Vector3 goal = route[0];
            Vector3 position = transform.position;
            Vector3 flatGoal = new(goal.x, position.y, goal.z);
            Vector3 next = Vector3.MoveTowards(position, flatGoal, walkSpeed * Time.deltaTime);
            Vector3 step = next - position;
            if (step.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(step), 8f * Time.deltaTime);
            if (Physics.Raycast(next + Vector3.up * 1.5f, Vector3.down, out var ground, 3f, ~0, QueryTriggerInteraction.Ignore))
                next.y = ground.point.y;
            transform.position = next;
            if ((flatGoal - next).sqrMagnitude > 0.04f) return false;
            route.RemoveAt(0);
            return route.Count == 0;
        }
    }
}
