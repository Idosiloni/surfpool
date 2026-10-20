using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A surfer customer: walks in and joins the desk line, waits to pay, (renters: lines up at the rental counter
    /// for a board,) walks to the pool, paddles out, rides a few waves (paddling back between them), tips if happy,
    /// then walks out of the park (renters drop their board in the return bin on the way).
    /// Runs out of patience if kept waiting too long in a line or for a wave, and leaves angry.
    /// </summary>
    public class Surfer : MonoBehaviour, IInteractable
    {
        public static readonly List<Surfer> All = new();

        enum State
        {
            WalkingIn, InDeskLine, WalkingToRental, InRentalLine, WalkingToPool, Paddling, WaitingForWave,
            Riding, Wipeout, PaddlingBack, SwimmingToBeach, WalkingOut,
        }

        enum Pose { Walk, Paddle, Ride }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly string[] RideShouts = { "Woohoo!", "Shredding!", "Radical!", "Let's gooo!" };
        static readonly string[] HappyShouts = { "Stoked!", "Epic session!", "Best wave pool ever!", "So good, dude!" };
        static readonly Color RentalBoardColor = new(1f, 0.85f, 0.15f);
        static readonly string[] BeginnerTricks = { "Popped up!", "Trimming!", "Standing!" };
        static readonly string[] IntermediateTricks = { "Bottom turn!", "Cutback!", "Snap!" };
        static readonly string[] ProTricks = { "Floater!", "Re-entry!", "AIR!", "Tube ride!" };

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
        [Tooltip("How far in front of the crest each skill level rides (meters): beginners low in the foam, pros up in the pocket.")]
        [SerializeField] float beginnerRideOffset = 2.2f;
        [SerializeField] float intermediateRideOffset = 1.2f;
        [SerializeField] float proRideOffset = 0.6f;
        [Tooltip("Seconds between tricks when the wave matches the surfer's skill.")]
        [SerializeField] Vector2 trickInterval = new(1.2f, 2.2f);
        [Tooltip("Extra tip per trick landed in the session ($).")]
        [SerializeField] int tipPerTrick = 1;
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
        float rideQuality;
        float wipeoutAt;
        float wipeoutStart;
        Vector3 tumbleAxis;
        float skippedWave = -1f;
        bool rideMatched;
        float nextTrickAt;
        float airStart = -10f;
        int tricksLanded;
        int ridesDone;
        int paidPrice;
        float patience = 1f;
        float moodTotal;
        int moodSamples;
        bool angry;
        bool atFront;
        bool isRenter;
        bool hasRentalBoard;
        string shout;
        float shoutUntil;

        /// <summary>Most this surfer will pay for a session.</summary>
        public float MaxPrice { get; private set; }
        public bool WaitingToPay => state == State.InDeskLine && atFront;
        public bool WaitingForBoard => state == State.InRentalLine && atFront;
        public bool WaitingForWave => state == State.WaitingForWave;

        /// <summary>The wave type this surfer is good at.</summary>
        public WaveLevel Skill { get; private set; }
        public bool ShowPatience => state is State.InDeskLine or State.InRentalLine or State.WaitingForWave;
        public float Patience => patience;

        /// <summary>Short text shown above the surfer's head.</summary>
        public string Status
        {
            get
            {
                if (Time.time < shoutUntil) return shout;
                return state switch
                {
                    State.InDeskLine when atFront => spawner.Pricing.Price > MaxPrice
                        ? $"${spawner.Pricing.Price}?! That's a lot..."
                        : $"{Skill} surfer - wants a session" + (isRenter ? " + board" : ""),
                    State.InRentalLine when atFront => "Needs a rental board",
                    State.WaitingForWave => $"Wants a {Skill} wave ({ridesDone + 1}/{wavesPerSession})",
                    _ => null,
                };
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Shown when the player looks at a surfer waiting for a rental board.</summary>
        public string Prompt
        {
            get
            {
                if (!WaitingForBoard) return null;
                var carry = PlayerCarry.Instance;
                return carry != null && carry.HasBoard
                    ? $"Give rental board (${spawner.Pricing.RentalPrice})"
                    : "Needs a board - grab one from the rack";
            }
        }

        public void Interact()
        {
            var carry = PlayerCarry.Instance;
            if (!WaitingForBoard || carry == null || !carry.HandOverBoard()) return;
            GiveBoard();
        }

        /// <summary>This renter gets a board (from the player or the rental clerk) and pays for it.</summary>
        public void GiveBoard()
        {
            if (!WaitingForBoard) return;
            Wallet.Instance.Add(spawner.Pricing.RentalPrice, "board rental");
            RecordMood(patience);
            hasRentalBoard = true;
            board.gameObject.SetActive(true);
            Paint(board, RentalBoardColor);
            spawner.LeaveLines(this);
            Shout("Thanks!", 2f);
            Walk(State.WalkingToPool, spawner.RentalPathToPool, State.Paddling);
        }

        public void Init(SurferSpawner owner, bool renter)
        {
            spawner = owner;
            water = owner.Water;
            float roll = Random.value;
            Skill = roll < 0.45f ? WaveLevel.Beginner : roll < 0.85f ? WaveLevel.Intermediate : WaveLevel.Pro;
            float skillPay = Skill switch { WaveLevel.Beginner => 0.85f, WaveLevel.Pro => 1.4f, _ => 1f };
            MaxPrice = owner.Pricing.FairPrice * Random.Range(0.8f, 1.5f) * skillPay;
            // Beginners ride big foam boards, pros ride short narrow ones.
            board.localScale = Skill switch
            {
                WaveLevel.Beginner => new Vector3(0.6f, 0.08f, 2.4f),
                WaveLevel.Pro => new Vector3(0.42f, 0.06f, 1.7f),
                _ => new Vector3(0.5f, 0.07f, 2f),
            };
            Paint(body, Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.6f, 1f));
            Paint(head, Color.Lerp(new Color(0.45f, 0.3f, 0.2f), new Color(1f, 0.85f, 0.7f), Random.value));
            Paint(board, Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f));
            isRenter = renter;
            board.gameObject.SetActive(!renter);
            Walk(State.WalkingIn, owner.PathIn, State.InDeskLine);
        }

        /// <summary>Called by the register when the player sells this surfer a session.</summary>
        public void Pay(int price)
        {
            if (!WaitingToPay) return;
            paidPrice = price;
            RecordMood(patience);
            spawner.LeaveLines(this);
            lineupTarget = spawner.Lineup + new Vector3(0f, 0f, Random.Range(-4f, 4f));
            if (isRenter)
            {
                patience = 1f;
                Walk(State.WalkingToRental, spawner.RentalPathIn, State.InRentalLine);
            }
            else
            {
                Walk(State.WalkingToPool, spawner.PathToPool, State.Paddling);
            }
        }

        /// <summary>Called when the player tries to charge more than this surfer will pay.</summary>
        public void RefusePrice() => LeaveAngry("Too expensive! I'm out.");

        void Update()
        {
            switch (state)
            {
                case State.WalkingIn:
                case State.WalkingToRental:
                case State.WalkingToPool:
                case State.WalkingOut:
                    SetPose(Pose.Walk);
                    FollowPath();
                    break;
                case State.InDeskLine:
                case State.InRentalLine:
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
                    if (DrainPatience(wavePatience * ParkEffects.WavePatienceMultiplier)) LeaveAngry("Where are the waves?!");
                    else TryCatchWave();
                    break;
                case State.Riding:
                    SetPose(Pose.Ride);
                    Ride();
                    break;
                case State.Wipeout:
                    Tumble();
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
                if (state is State.InDeskLine or State.InRentalLine)
                {
                    atFront = false;
                    spawner.JoinLine(this, state == State.InRentalLine);
                }
                return;
            }
            Vector3 target = path.Peek();
            StepTowards(target);
            if ((target - transform.position).sqrMagnitude >= 0.0004f) return;
            path.Dequeue();
            if (hasRentalBoard && (target - spawner.ReturnBinSpot).sqrMagnitude < 0.01f) ReturnBoard();
        }

        void StandInLine()
        {
            if (DrainPatience(deskPatience))
            {
                LeaveAngry(state == State.InRentalLine ? "Where's my board?!" : "This line is way too slow!");
                return;
            }
            var (slot, front, facing) = spawner.LineSlot(this);
            if ((slot - transform.position).sqrMagnitude > 0.0004f)
            {
                StepTowards(slot);
                atFront = false;
                return;
            }
            Face(facing);
            atFront = front;
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
                route.AddRange(hasRentalBoard ? spawner.ReturnPathOut : spawner.PathOut);
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
            for (int i = 0; i < water.ActiveWaves; i++)
            {
                var (wave, level) = water.GetWave(i);
                float crest = water.CurrentWaveCenter(wave);
                if (crest <= x - 2.5f || crest >= x + 0.5f || wave == skippedWave) continue;

                int diff = (int)level - (int)Skill;
                if (Mathf.Abs(diff) >= 2)
                {
                    // Way too big or way too small: let it pass.
                    skippedWave = wave;
                    Shout(diff > 0 ? "Nope! Way too big for me!" : "Too small, bro...", 2.5f);
                    continue;
                }

                RecordMood(patience);
                rideWave = wave;
                rideStartTime = Time.time;
                rideLaneZ = water.transform.InverseTransformPoint(transform.position).z;
                rideQuality = diff == 0 ? 1f : diff < 0 ? 0.5f : 0.6f;
                rideMatched = diff == 0;
                nextTrickAt = Time.time + Random.Range(trickInterval.x, trickInterval.y);

                // Too-big waves often end in a wipeout; everyone wipes out sometimes.
                float wipeoutChance = diff > 0 ? 0.6f : Skill switch { WaveLevel.Beginner => 0.2f, WaveLevel.Intermediate => 0.1f, _ => 0.05f };
                wipeoutAt = Random.value < wipeoutChance ? Time.time + Random.Range(0.8f, 2.5f) : -1f;

                state = State.Riding;
                Shout(diff < 0 ? "Kinda small..." : diff > 0 ? "Whoa, big one!" : RideShouts[Random.Range(0, RideShouts.Length)], 2f);
                return;
            }
        }

        void Ride()
        {
            float crest = water.CurrentWaveCenter(rideWave);
            if (crest / water.Length >= rideEndProgress)
            {
                RecordMood(rideQuality);
                FinishRide(false);
                return;
            }
            if (wipeoutAt > 0f && Time.time >= wipeoutAt)
            {
                StartWipeout();
                return;
            }

            if (rideMatched && wipeoutAt < 0f && Time.time >= nextTrickAt) DoTrick();

            float offset = Skill switch
            {
                WaveLevel.Beginner => beginnerRideOffset,
                WaveLevel.Pro => proRideOffset,
                _ => intermediateRideOffset,
            };
            float limit = water.Width * 0.5f - 2f;
            float z = Mathf.Clamp(rideLaneZ + Mathf.Sin((Time.time - rideStartTime) * carveSpeed) * carveWidth, -limit, limit);
            Vector3 target = water.transform.TransformPoint(new Vector3(crest + offset, 0f, z));
            const float airTime = 0.7f;
            float air = Mathf.Clamp01((Time.time - airStart) / airTime);
            target.y = water.SurfaceHeightAt(target) + (air < 1f ? Mathf.Sin(air * Mathf.PI) * 0.9f : 0f);
            Vector3 delta = target - transform.position;
            transform.position = target;
            Face(delta);
        }

        void DoTrick()
        {
            var tricks = Skill switch
            {
                WaveLevel.Beginner => BeginnerTricks,
                WaveLevel.Pro => ProTricks,
                _ => IntermediateTricks,
            };
            string trick = tricks[Random.Range(0, tricks.Length)];
            if (trick == "AIR!") airStart = Time.time;
            tricksLanded++;
            Shout(trick, 1.3f);
            nextTrickAt = Time.time + Random.Range(trickInterval.x, trickInterval.y);
        }

        void StartWipeout()
        {
            state = State.Wipeout;
            wipeoutStart = Time.time;
            tumbleAxis = Random.onUnitSphere;
            RecordMood(ParkEffects.WipeoutMood);
            Shout("WIPEOUT!", 2.5f);
        }

        /// <summary>Spins under the water for a moment, then pops back up.</summary>
        void Tumble()
        {
            const float duration = 1.8f;
            float t = (Time.time - wipeoutStart) / duration;
            Vector3 position = transform.position;
            float surface = water.SurfaceHeightAt(position);
            position.y = surface - Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.9f;
            transform.position = position;
            transform.Rotate(tumbleAxis, 600f * (1f - t) * Time.deltaTime, Space.World);
            board.Rotate(Vector3.right, 900f * Time.deltaTime, Space.Self);
            if (t < 1f) return;

            transform.rotation = Quaternion.LookRotation(Vector3.right);
            SetPose(Pose.Paddle);
            FinishRide(true);
        }

        void FinishRide(bool wipedOut)
        {
            ridesDone++;
            if (ridesDone < wavesPerSession)
            {
                if (!wipedOut) Shout("One more!", 2f);
                state = State.PaddlingBack;
                return;
            }

            float happiness = moodSamples == 0 ? 1f : moodTotal / moodSamples;
            int tip = happiness > 0.5f ? Mathf.RoundToInt(paidPrice * maxTipShare * happiness) + tricksLanded * tipPerTrick : 0;
            if (tip > 0) Wallet.Instance.Add(tip, "tip");
            spawner.ReportServed(happiness);
            string line = happiness > 0.75f ? HappyShouts[Random.Range(0, HappyShouts.Length)]
                : happiness > 0.5f ? "Pretty fun!"
                : "Meh. Wrong waves for me.";
            Shout(line + (tip > 0 ? $"  (tipped ${tip})" : ""), 5f);
            state = State.SwimmingToBeach;
        }

        void LeaveAngry(string message)
        {
            if (angry) return;
            angry = true;
            Shout(message, 5f);
            spawner.LeaveLines(this);
            spawner.ReportLost();
            Notifications.Post($"A surfer left angry: \"{message}\"", true);
            if (state == State.InRentalLine)
                Walk(State.WalkingOut, new[] { spawner.Entrance }, State.WalkingOut);
            else if (state is State.WalkingIn or State.InDeskLine)
                Walk(State.WalkingOut, spawner.PathBackOut, State.WalkingOut);
            else
                state = State.SwimmingToBeach;
        }

        void ReturnBoard()
        {
            hasRentalBoard = false;
            board.gameObject.SetActive(false);
            spawner.ReturnBin.Add();
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
