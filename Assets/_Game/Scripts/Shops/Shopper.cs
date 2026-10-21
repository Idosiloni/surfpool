using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A shop customer: walks in from the entrance to a <see cref="ShopCounter"/>, waits in line (losing patience),
    /// shows their order at the counter, and walks home carrying what they bought (a bag, a bowl, a new board).
    /// If nobody serves them in time they give up and leave.
    /// </summary>
    [RequireComponent(typeof(CharacterRig))]
    public class Shopper : MonoBehaviour
    {
        public static readonly List<Shopper> All = new();

        public enum CarryStyle { Hand, Front, UnderArm }

        [SerializeField] float walkSpeed = 1.6f;

        enum Phase { WalkingIn, InLine, Waiting, Leaving }

        CharacterRig rig;
        ShopCounter shop;
        Phase phase;
        Vector3 exit;
        float patienceLeft, patienceTotal;
        string status;
        float statusUntil;
        bool sitting;
        float startedAt;
        readonly List<Vector3> route = new();

        public ShopCounter Shop => shop;
        public ShopOrder Order { get; private set; }
        public bool AtCounter { get; private set; }
        public bool IsWaiting => phase == Phase.Waiting;
        public float Patience01 => patienceTotal > 0f ? Mathf.Clamp01(patienceLeft / patienceTotal) : 1f;
        /// <summary>Show the order + patience over their head (at the counter, or waiting for a custom job).</summary>
        public bool ShowOrder => (phase == Phase.InLine && AtCounter) || phase == Phase.Waiting;
        public bool ShowPatience => phase == Phase.InLine || phase == Phase.Waiting;
        public string Status => Time.time < statusUntil ? status : null;

        void OnEnable() => All.Add(this);
        void OnDisable()
        {
            All.Remove(this);
            if (shop != null && shop.InLine(this)) shop.Leave(this);
        }

        public void Begin(ShopCounter target, Vector3 entrance)
        {
            rig = GetComponent<CharacterRig>();
            rig.Wear(CharacterLooks.Beachgoer());
            shop = target;
            exit = entrance;
            startedAt = Time.time;
            Order = shop.NewOrder();
            Order.RollMood();
            patienceTotal = patienceLeft = shop.Patience * Order.patienceScale;
            transform.position = entrance;
            WalkRouter.Plan(entrance, shop.ApproachPoint, route);
            phase = Phase.WalkingIn;
            shop.Join(this); // hold a place so the shop doesn't over-book; they walk to it
        }

        void Update()
        {
            if (shop == null && phase != Phase.Leaving)
            {
                GiveUp(null);
                return;
            }
            switch (phase)
            {
                case Phase.WalkingIn:
                    Pose(false);
                    if (WalkRoute()) phase = Phase.InLine;
                    else if (Time.time - startedAt > 90f) GiveUp(null); // never stand around stuck on the way in
                    break;
                case Phase.InLine:
                    StandInLine();
                    TickPatience();
                    break;
                case Phase.Waiting:
                    if (!sitting && WalkRoute()) SitDown();
                    TickPatience();
                    break;
                case Phase.Leaving:
                    Pose(false);
                    if (WalkRoute()) Destroy(gameObject);
                    break;
            }
        }

        void StandInLine()
        {
            var (slot, front, facing) = shop.LineSlot(this);
            Vector3 position = transform.position;
            Vector3 flat = new(slot.x, position.y, slot.z);
            if ((flat - position).sqrMagnitude > 0.02f)
            {
                Pose(false);
                Step(flat);
                AtCounter = false;
                return;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), 6f * Time.deltaTime);
            // At the counter, look at you when you're close (you're the one serving them).
            var cam = Camera.main;
            rig.LookTarget = front && cam != null && (cam.transform.position - transform.position).sqrMagnitude < 36f
                ? cam.transform.position : null;
            if (front && !AtCounter)
            {
                AtCounter = true;
                if (Order.placedAt < 0f) Order.placedAt = Time.time;
                shop.ReachedCounter(this);
            }
        }

        void TickPatience()
        {
            // Waiting at the very back of a long line is a bit more patient than waiting at the counter.
            patienceLeft -= Time.deltaTime;
            if (patienceLeft <= 0f) GiveUp(phase == Phase.Waiting ? "I'll come back another day..." : "Too slow!");
        }

        /// <summary>Takes them out of the line to wait somewhere (a custom board is being made).</summary>
        public void WaitAt(Transform seat, float seconds)
        {
            phase = Phase.Waiting;
            AtCounter = false;
            shop.Leave(this);
            patienceTotal = patienceLeft = seconds * ParkPerks.PatienceMultiplier;
            sitting = false;
            seatPose = seat;
            WalkRouter.Plan(transform.position, seat.position, route);
        }

        Transform seatPose;

        void SitDown()
        {
            sitting = true;
            transform.SetPositionAndRotation(seatPose.position, seatPose.rotation);
            rig.CurrentMode = CharacterRig.Mode.Sit;
        }

        public void Say(string line, float seconds = 3f)
        {
            status = line;
            statusUntil = Time.time + seconds;
            Voices.Talk(transform, rig != null && rig.Look != null ? rig.Look.voice : 1f, line);
        }

        /// <summary>Paid and happy: say thanks, take what they bought and walk home.</summary>
        public void Served(GameObject carry, string thanks, CarryStyle style = CarryStyle.Hand)
        {
            if (carry != null) Hold(carry, style);
            Say(thanks, 3.5f);
            if (rig != null) rig.PlayGesture(Random.value < 0.5f ? CharacterRig.Gesture.Shaka : CharacterRig.Gesture.FistPump, 1.2f);
            GoHome();
            if (shop != null && shop.InLine(this)) shop.Leave(this);
        }

        /// <summary>Closing time: anyone not being served goes home (<paramref name="now"/>: everyone).</summary>
        public void GoHome(bool now)
        {
            if (phase == Phase.Leaving) return;
            if (now || !AtCounter && phase != Phase.Waiting) GiveUp("Closing time? Aww.");
        }

        /// <summary>Gives up on the shop (too slow, or it closed).</summary>
        public void GiveUp(string line)
        {
            if (phase == Phase.Leaving) return;
            GoHome();
            if (shop != null) shop.Leave(this);
            if (!string.IsNullOrEmpty(line)) Say(line, 3f);
            if (rig != null)
            {
                rig.PlayGesture(CharacterRig.Gesture.ScratchHead, 1.2f);
                rig.ShowFace(Face.Grumpy, 4f);
            }
        }

        void GoHome()
        {
            if (rig != null) rig.LookTarget = null;
            phase = Phase.Leaving;
            AtCounter = false;
            sitting = false;
            WalkRouter.Plan(transform.position, exit, route);
        }

        public void Hold(GameObject item, CarryStyle style)
        {
            item.transform.SetParent(transform, false);
            float size = rig != null && rig.Look != null ? rig.Look.height : 1f;
            switch (style)
            {
                case CarryStyle.Hand: // a bag hanging from the right hand
                    item.transform.localPosition = new Vector3(0.42f, 0.82f, 0.05f) * size;
                    item.transform.localRotation = Quaternion.identity;
                    break;
                case CarryStyle.Front: // a bowl or cup carried in the right hand
                    item.transform.localPosition = new Vector3(0.42f, 0.8f, 0.12f) * size;
                    item.transform.localRotation = Quaternion.identity;
                    break;
                case CarryStyle.UnderArm: // a board on its edge under the right arm, like surfers carry them
                    item.transform.localPosition = new Vector3(0.55f, 0.9f, 0.15f) * size;
                    item.transform.localEulerAngles = new Vector3(-8f, 0f, 90f);
                    break;
            }
            heldItem = item;
        }

        GameObject heldItem;

        void Pose(bool still)
        {
            if (sitting) return;
            rig.CurrentMode = CharacterRig.Mode.Locomotion;
            rig.CarryingBoard = heldItem != null;
        }

        void Step(Vector3 goal)
        {
            Vector3 position = transform.position;
            Vector3 next = Vector3.MoveTowards(position, goal, walkSpeed * Time.deltaTime);
            Vector3 step = next - position;
            if (step.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(step), 8f * Time.deltaTime);
            if (Physics.Raycast(next + Vector3.up * 1.5f, Vector3.down, out var ground, 3f, ~0, QueryTriggerInteraction.Ignore))
                next.y = ground.point.y;
            transform.position = next;
        }

        /// <summary>Walks along the route on the ground. True when the end is reached.</summary>
        bool WalkRoute()
        {
            if (route.Count == 0) return true;
            Vector3 goal = route[0];
            Vector3 flatGoal = new(goal.x, transform.position.y, goal.z);
            Step(flatGoal);
            if (phase == Phase.Leaving && heldItem != null) Trash.MaybeDrop(transform.position, Time.deltaTime * 0.5f);
            if ((flatGoal - transform.position).sqrMagnitude > 0.04f) return false;
            route.RemoveAt(0);
            return route.Count == 0;
        }
    }
}
