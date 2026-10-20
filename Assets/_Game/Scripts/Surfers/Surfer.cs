using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A surfer customer: walks in and joins the desk line, waits to pay, walks to the pool, paddles out,
    /// rides a few waves (paddling back between them), tips if happy, then walks out of the park.
    /// Runs out of patience if kept waiting too long at the desk or for a wave, and leaves angry.
    /// </summary>
    public class Surfer : MonoBehaviour
    {
        public static readonly List<Surfer> All = new();

        enum State
        {
            WalkingIn, Queueing, WaitingAtDesk, WalkingToPool, Paddling, WaitingForWave,
            Riding, PaddlingBack, SwimmingToBeach, WalkingOut,
        }

        enum Pose { Walk, Paddle, Ride }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly string[] RideShouts = { "Woohoo!", "Shredding!", "Radical!", "Let's gooo!" };
        static readonly string[] HappyShouts = { "Stoked!", "Epic session!", "Best wave pool ever!", "So good, dude!" };

        [Header("Parts")]
        [SerializeField] Transform body;
        [SerializeField] Transform head;
        [SerializeField] Transform board;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 1.6f;
        [SerializeField] float paddleSpeed = 1.3f;
        [Tooltip("Paddle speed when heading back out after a ride.")]
        [SerializeField] float paddleBackSpeed = 3f;
        [SerializeField] float turnSpeed = 360f;

        [Header("Patience (seconds before they give up)")]
        [SerializeField] float deskPatience = 45f;
        [SerializeField] float wavePatience = 30f;

        [Header("Session")]
        [SerializeField] int wavesPerSession = 3;
        [Tooltip("Tip as a share of the ticket price when the surfer is perfectly happy.")]
        [Range(0f, 1f)] [SerializeField] float maxTipShare = 0.3f;

        [Header("Riding")]
        [Tooltip("How far in front of the crest the surfer rides (meters). Bigger = lower on the wave face.")]
        [SerializeField] float rideOffset = 1.0f;
        [Tooltip("How far the surfer carves left and right while riding (meters).")]
        [SerializeField] float carveWidth = 5f;
        [SerializeField] float carveSpeed = 0.9f;
        [Tooltip("How far down the pool (0 to 1) the ride ends.")]
        [Range(0f, 1f)] [SerializeField] float rideEndProgress = 0.88f;

        SurferSpawner spawner;
        WaveWater water;
        State state;
        readonly Queue<Vector3> path = new();
        State stateAfterPath;
        Vector3 lineupTarget;
        float rideWave;
        float rideStartTime;
        float rideLaneZ;
        int ridesDone;
        int paidPrice;
        float patience = 1f;
        float moodTotal;
        int moodSamples;
        bool angry;
        string shout;
        float shoutUntil;

        /// <summary>Most this surfer will pay for a session.</summary>
        public float MaxPrice { get; private set; }
        public bool WaitingToPay => state == State.WaitingAtDesk;
        public bool ShowPatience => state is State.Queueing or State.WaitingAtDesk or State.WaitingForWave;
        public float Patience => patience;

        /// <summary>Short text shown above the surfer's head.</summary>
        public string Status
        {
            get
            {
                if (Time.time < shoutUntil) return shout;
                return state switch
                {
                    State.WaitingAtDesk => spawner.Pricing.Price > MaxPrice
                        ? $"${spawner.Pricing.Price}?! That's a lot..."
                        : "Wants a surf session",
                    State.WaitingForWave => $"Waiting for a wave... ({ridesDone + 1}/{wavesPerSession})",
                    _ => null,
                };
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Init(SurferSpawner owner)
        {
            spawner = owner;
            water = owner.Water;
            MaxPrice = owner.Pricing.FairPrice * Random.Range(0.8f, 1.5f);
            Paint(body, Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.6f, 1f));
            Paint(head, Color.Lerp(new Color(0.45f, 0.3f, 0.2f), new Color(1f, 0.85f, 0.7f), Random.value));
            Paint(board, Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f));
            Walk(State.WalkingIn, owner.PathIn, State.Queueing);
        }

        /// <summary>Called by the register when the player sells this surfer a session.</summary>
        public void Pay(int price)
        {
            if (state != State.WaitingAtDesk) return;
            paidPrice = price;
            RecordMood(patience);
            spawner.LeaveQueue(this);
            lineupTarget = spawner.Lineup + new Vector3(0f, 0f, Random.Range(-4f, 4f));
            Walk(State.WalkingToPool, spawner.PathToPool, State.Paddling);
        }

        /// <summary>Called when the player tries to charge more than this surfer will pay.</summary>
        public void RefusePrice() => LeaveAngry("Too expensive! I'm out.");

        void Update()
        {
            switch (state)
            {
                case State.WalkingIn:
                case State.WalkingToPool:
                case State.WalkingOut:
                    SetPose(Pose.Walk);
                    FollowPath();
                    break;
                case State.Queueing:
                case State.WaitingAtDesk:
                    SetPose(Pose.Walk);
                    StandInLine();
                    break;
                case State.Paddling:
                case State.PaddlingBack:
                    SetPose(Pose.Paddle);
                    PaddleTo(lineupTarget, state == State.Paddling ? paddleSpeed : paddleBackSpeed, State.WaitingForWave);
                    break;
                case State.WaitingForWave:
                    SetPose(Pose.Paddle);
                    Float(transform.position, Vector3.right);
                    if (DrainPatience(wavePatience)) LeaveAngry("Where are the waves?!");
                    else TryCatchWave();
                    break;
                case State.Riding:
                    SetPose(Pose.Ride);
                    Ride();
                    break;
                case State.SwimmingToBeach:
                    SetPose(Pose.Paddle);
                    var beach = spawner.BeachLanding;
                    PaddleTo(new Vector3(beach.x - 4f, 0f, transform.position.z), paddleSpeed * 1.5f, State.WalkingOut);
                    break;
            }
        }

        void Walk(State walking, Vector3[] points, State whenDone)
        {
            path.Clear();
            foreach (var p in points) path.Enqueue(p);
            state = walking;
            stateAfterPath = whenDone;
        }

        void FollowPath()
        {
            if (path.Count == 0)
            {
                if (state == State.WalkingOut)
                {
                    Destroy(gameObject);
                    return;
                }
                state = stateAfterPath;
                if (state == State.Queueing) spawner.JoinQueue(this);
                return;
            }
            Vector3 target = path.Peek();
            StepTowards(target);
            if ((target - transform.position).sqrMagnitude < 0.0004f) path.Dequeue();
        }

        void StandInLine()
        {
            if (DrainPatience(deskPatience))
            {
                LeaveAngry("This line is way too slow!");
                return;
            }
            Vector3 slot = spawner.QueueSlot(this);
            if ((slot - transform.position).sqrMagnitude > 0.0004f)
            {
                StepTowards(slot);
                state = State.Queueing;
                return;
            }
            Face(spawner.DeskFacing);
            state = spawner.QueueIndex(this) == 0 ? State.WaitingAtDesk : State.Queueing;
        }

        void StepTowards(Vector3 target)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
            Face(next - transform.position);
            transform.position = next;
        }

        void PaddleTo(Vector3 target, float speed, State whenArrived)
        {
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            Vector3 step = Vector3.ClampMagnitude(toTarget, speed * Time.deltaTime);
            Float(transform.position + step, step);
            if (toTarget.magnitude > 0.1f) return;

            if (whenArrived == State.WalkingOut)
            {
                var landing = spawner.BeachLanding;
                landing.z = transform.position.z;
                var route = new List<Vector3> { landing };
                route.AddRange(spawner.PathOut);
                Walk(State.WalkingOut, route.ToArray(), State.WalkingOut);
                return;
            }
            patience = 1f;
            state = whenArrived;
        }

        /// <summary>Moves to a horizontal position, dropping smoothly onto the water surface.</summary>
        void Float(Vector3 position, Vector3 facing)
        {
            float surface = water.SurfaceHeightAt(position);
            position.y = Mathf.MoveTowards(transform.position.y, surface, 6f * Time.deltaTime);
            if (Mathf.Abs(position.y - surface) < 0.3f) position.y = surface;
            transform.position = position;
            Face(facing);
        }

        void TryCatchWave()
        {
            float x = water.transform.InverseTransformPoint(transform.position).x;
            foreach (float wave in water.WaveStartTimes)
            {
                float crest = water.CurrentWaveCenter(wave);
                if (crest > x - 2.5f && crest < x + 0.5f)
                {
                    RecordMood(patience);
                    rideWave = wave;
                    rideStartTime = Time.time;
                    rideLaneZ = water.transform.InverseTransformPoint(transform.position).z;
                    state = State.Riding;
                    Shout(RideShouts[Random.Range(0, RideShouts.Length)], 2f);
                    return;
                }
            }
        }

        void Ride()
        {
            float crest = water.CurrentWaveCenter(rideWave);
            if (crest / water.Length >= rideEndProgress)
            {
                FinishRide();
                return;
            }

            float limit = water.Width * 0.5f - 2f;
            float z = Mathf.Clamp(rideLaneZ + Mathf.Sin((Time.time - rideStartTime) * carveSpeed) * carveWidth, -limit, limit);
            Vector3 target = water.transform.TransformPoint(new Vector3(crest + rideOffset, 0f, z));
            target.y = water.SurfaceHeightAt(target);
            Vector3 delta = target - transform.position;
            transform.position = target;
            Face(delta);
        }

        void FinishRide()
        {
            ridesDone++;
            if (ridesDone < wavesPerSession)
            {
                Shout("One more!", 2f);
                state = State.PaddlingBack;
                return;
            }

            float happiness = moodSamples == 0 ? 1f : moodTotal / moodSamples;
            int tip = happiness > 0.5f ? Mathf.RoundToInt(paidPrice * maxTipShare * happiness) : 0;
            if (tip > 0) Wallet.Instance.Add(tip, "tip");
            spawner.ReportServed();
            Shout(HappyShouts[Random.Range(0, HappyShouts.Length)] + (tip > 0 ? $"  (tipped ${tip})" : ""), 5f);
            state = State.SwimmingToBeach;
        }

        void LeaveAngry(string message)
        {
            if (angry) return;
            angry = true;
            Shout(message, 5f);
            spawner.LeaveQueue(this);
            spawner.ReportLost();
            if (state is State.WalkingIn or State.Queueing or State.WaitingAtDesk)
                Walk(State.WalkingOut, spawner.PathBackOut, State.WalkingOut);
            else
                state = State.SwimmingToBeach;
        }

        /// <summary>Counts down patience; returns true when it runs out.</summary>
        bool DrainPatience(float seconds)
        {
            patience -= Time.deltaTime / seconds;
            return patience <= 0f;
        }

        void RecordMood(float value)
        {
            moodTotal += Mathf.Clamp01(value);
            moodSamples++;
        }

        void Shout(string text, float seconds)
        {
            shout = text;
            shoutUntil = Time.time + seconds;
        }

        void Face(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f) return;
            var look = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        void SetPose(Pose pose)
        {
            switch (pose)
            {
                case Pose.Walk: // standing, board carried on its edge at the side
                    Place(body, new Vector3(0f, 0.8f, 0f), Vector3.zero);
                    Place(head, new Vector3(0f, 1.75f, 0f), Vector3.zero);
                    Place(board, new Vector3(0.4f, 0.9f, 0f), new Vector3(0f, 0f, 90f));
                    break;
                case Pose.Paddle: // lying on the board
                    Place(board, new Vector3(0f, 0.05f, 0f), Vector3.zero);
                    Place(body, new Vector3(0f, 0.3f, -0.1f), new Vector3(90f, 0f, 0f));
                    Place(head, new Vector3(0f, 0.4f, 0.85f), Vector3.zero);
                    break;
                case Pose.Ride: // standing sideways on the board, knees bent
                    Place(board, new Vector3(0f, 0.05f, 0f), Vector3.zero);
                    Place(body, new Vector3(0f, 0.75f, 0f), new Vector3(0f, 0f, 12f));
                    Place(head, new Vector3(-0.2f, 1.6f, 0f), Vector3.zero);
                    break;
            }
        }

        static void Place(Transform part, Vector3 localPosition, Vector3 localEuler)
        {
            part.localPosition = localPosition;
            part.localEulerAngles = localEuler;
        }

        static void Paint(Transform part, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            part.GetComponent<Renderer>().SetPropertyBlock(block);
        }
    }
}
