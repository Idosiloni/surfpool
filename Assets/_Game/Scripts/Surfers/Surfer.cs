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
            Riding, Wipeout, PaddlingBack, SwimmingToBeach, WalkingOut, WalkingToSnack, InSnackLine,
        }

        enum Pose { Walk, Paddle, Ride, Wipeout, Straddle }

        /// <summary>A maneuver the body and board act out over a moment of the ride.</summary>
        enum Move { None, Snap, Cutback, Floater, BottomTurn }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color RentalBoardColor = new(1f, 0.85f, 0.15f);
        static readonly string[] BeginnerTricks = { "Popped up!", "Trimming!", "Standing!" };
        static readonly string[] IntermediateTricks = { "Bottom turn!", "Cutback!", "Snap!" };
        static readonly string[] ProTricks = { "Floater!", "Re-entry!", "AIR!", "Tube ride!" };

        [Header("Parts")]
        [SerializeField] CharacterRig rig;
        [SerializeField] Transform board;
        [Tooltip("Board models by skill: foam board (Beginner), funboard (Intermediate), shortboard (Pro).")]
        [SerializeField] GameObject[] boardModels;

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
        [Tooltip("Seconds between a pro's moves when the wave matches their skill. Real pros do one every 3-4 s, but our rides last 3-5 s instead of 10-18, so time is squeezed. Intermediates wait 1.3x as long, beginners 1.8x; the first move comes at half the gap (a bottom turn right after takeoff).")]
        [SerializeField] Vector2 trickInterval = new(1.3f, 2f);
        [Tooltip("Extra tip per trick landed in the session ($).")]
        [SerializeField] int tipPerTrick = 1;
        [Tooltip("How far the surfer carves left and right while riding (meters).")]
        [SerializeField] float carveWidth = 5f;
        [SerializeField] float carveSpeed = 0.9f;
        [Tooltip("How far down the pool (0 to 1) the ride ends.")]
        [Range(0f, 1f)] [SerializeField] float rideEndProgress = 0.88f;
        [Tooltip("Board tilt on the wave face, lean into turns and rail spray.")]
        [SerializeField] RideFeel feel = new();

        SurferSpawner spawner;
        PoolArea pool;
        WaveWater water;
        State state;
        readonly Queue<Vector3> path = new();
        State stateAfterPath;
        Vector3 lineupTarget;
        bool wantsSnack, ateSnack, rodeFamous, rideWhitewash;
        float lane;            // sideways offset from the path (m), so surfers don't walk in single file on top of each other
        Vector3 segmentStart;  // where the current path segment began (lane direction)

        /// <summary>Where beginners wait, as a share of the pool's length from the wave machine.</summary>
        const float InsideLineup = 0.45f;

        const float WalkSpace = 0.8f;   // personal space on foot (m)
        const float WaterSpace = 1.8f;  // personal space on a board (boards are 2 m long)
        const float RideSpace = 2.5f;   // closest two riders come along a wave (m)
        const float DropInSpace = 3f;   // won't take off this close to someone already riding the wave (m)
        float rideWave;
        float rideStartTime;
        float rideLaneZ;
        float rideZ;
        bool inBarrel;
        float rideQuality;
        float wipeoutAt;
        float wipeoutStart;
        Vector3 tumbleAxis;
        float skippedWave = -1f;
        bool rideMatched;
        float nextTrickAt;
        float airStart = -10f;
        bool airSpin, wasAirborne;
        float landSquash, kickOutStart = -10f;

        float kickOutTime = 0.7f;
        bool celebrate;
        Archetype archetype;
        float nextFidget, lookUntil, hootAt = -1f;
        Surfer dropInVictim;

        /// <summary>Who this surfer is (grom, kook, charger...): what they say and how they celebrate.</summary>
        public Archetype Type => archetype;
        Move move;
        float moveStart, moveLength, moveDirection;
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
        public bool WaitingForSnack => state == State.InSnackLine && atFront;

        /// <summary>The pool this surfer was sent to.</summary>
        public PoolArea Pool => pool;

        /// <summary>The wave type this surfer is good at.</summary>
        public WaveLevel Skill { get; private set; }

        /// <summary>Regular-foot surfers prefer rights (their frontside), goofy-foot surfers prefer lefts.</summary>
        public WaveDirection PreferredDirection { get; private set; }

        int PreferredSide => PreferredDirection == WaveDirection.Right ? -1 : 1;

        string WantedWave => Skill == WaveLevel.Beginner
            ? "any wave"
            : $"{(Skill == WaveLevel.Intermediate ? "an" : "a")} {Skill} {PreferredDirection.ToString().ToLowerInvariant()}";
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
                    State.InDeskLine when atFront => spawner.Pricing.PriceFor(Skill, pool) > MaxPrice
                        ? $"{MoneyText.Format(spawner.Pricing.PriceFor(Skill, pool))}?! That's a lot..."
                        : $"{Skill} surfer - wants a session" + (isRenter ? " + board" : ""),
                    State.InRentalLine when atFront => "Needs a rental board",
                    State.WaitingForWave => $"Wants {WantedWave} ({ridesDone + 1}/{wavesPerSession})",
                    _ => null,
                };
            }
        }

        void OnEnable()
        {
            All.Add(this);
            Highlights.Happened += OnHighlight;
        }

        void OnDisable()
        {
            All.Remove(this);
            Highlights.Happened -= OnHighlight;
            if (SnackBar.Instance != null) SnackBar.Instance.Leave(this);
        }

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
            GameStats.BoardsRented++;
            RecordMood(patience);
            hasRentalBoard = true;
            board.gameObject.SetActive(true);
            Paint(board, RentalBoardColor);
            spawner.LeaveLines(this);
            Shout("Thanks!", 2f);
            Walk(State.WalkingToPool, spawner.RentalPathToPool(pool), State.Paddling);
        }

        public void Init(SurferSpawner owner, bool renter)
        {
            spawner = owner;
            pool = owner.ChoosePool(renter);
            water = pool.Water;
            Skill = owner.RollSkill();
            PreferredDirection = Random.value < 0.6f ? WaveDirection.Right : WaveDirection.Left;
            MaxPrice = owner.Pricing.FairFor(Skill, pool) * Random.Range(0.8f, 1.5f);
            UseBoardFor(Skill);
            archetype = SurferPersonalities.Roll(Skill);
            rig.Goofy = Random.value < 0.35f;
            rig.Style = Skill switch { WaveLevel.Beginner => 0f, WaveLevel.Intermediate => 0.55f, _ => 1f };
            rig.Wear(CharacterLooks.For(archetype));
            Paint(board, Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f));
            isRenter = renter;
            lane = Random.Range(-0.6f, 0.6f);
            board.gameObject.SetActive(!renter);
            Walk(State.WalkingIn, owner.PathIn, State.InDeskLine);
        }

        // ---- for the photographer (PhotoTower) ----
        public bool IsRidingWave => state == State.Riding;
        public bool InBarrelNow => inBarrel;
        public float LastTrickAt { get; private set; } = -10f;
        public bool IsCelebrating => rig != null && rig.Gesturing;
        public CharacterRig Rig => rig;
        /// <summary>Best photo grade of this surfer so far (-1 none, 0 C .. 3 S). B or better sells a print when they leave happy.</summary>
        public int PhotoGrade { get; set; } = -1;

        /// <summary>A visiting legend (null for everyone else): their own look, board, lines and signature trick.</summary>
        public FamousSurfer Famous { get; private set; }

        /// <summary>Gold name tag over a legend's head.</summary>
        public string NameTag => Famous != null ? Famous.FullName : null;

        /// <summary>Turns this new arrival into a legend: pro skill, the best open pool, their signature look and board.</summary>
        public void MakeFamous(FamousSurfer legend)
        {
            Famous = legend;
            Skill = WaveLevel.Pro;
            archetype = legend.type;
            PoolArea best = pool;
            foreach (var area in PoolRegistry.All)
                if (area.IsOpen && area.PriceMultiplier > best.PriceMultiplier) best = area;
            pool = best;
            water = pool.Water;
            UseBoardFor(WaveLevel.Pro);
            rig.Style = 1f;
            rig.Wear(legend.look());
            Paint(board, legend.board);
            board.gameObject.SetActive(true);
            isRenter = false;
            MaxPrice = spawner.Pricing.FairFor(WaveLevel.Pro, pool) * 4f; // they'll pay what you ask
        }

        /// <summary>Called by the register when the player sells this surfer a session.</summary>
        public void Pay(int price)
        {
            if (!WaitingToPay) return;
            paidPrice = price;
            RecordMood(patience);
            spawner.LeaveLines(this);
            lineupTarget = FreeLineupSpot();
            if (isRenter)
            {
                patience = 1f;
                Walk(State.WalkingToRental, spawner.RentalPathIn, State.InRentalLine);
            }
            else
            {
                Walk(State.WalkingToPool, spawner.PathToPool(pool), State.Paddling);
            }
        }

        /// <summary>Called when the player tries to charge more than this surfer will pay.</summary>
        public void RefusePrice() => LeaveAngry("Too expensive! I'm out.");

        float nextMumble = -1f;

        void Update()
        {
            // Waiting around, people mumble to themselves now and then.
            if (Time.time >= nextMumble)
            {
                if (nextMumble > 0f && state is State.InDeskLine or State.InRentalLine or State.WaitingForWave or State.InSnackLine
                    && Random.value < 0.35f)
                    Voices.Mumble(transform, Voice);
                nextMumble = Time.time + Random.Range(9f, 22f);
            }

            // A delayed hoot for someone else's wave, and letting go of whatever caught our eye.
            if (hootAt > 0f && Time.time >= hootAt)
            {
                hootAt = -1f;
                if (state == State.WaitingForWave)
                {
                    rig.PlayGesture(CharacterRig.Gesture.Hoot, 1.3f);
                    Shout(SurferPersonalities.Line(archetype, Moment.Hoot), 1.5f);
                }
            }
            if (lookUntil > 0f && Time.time >= lookUntil)
            {
                lookUntil = 0f;
                rig.LookTarget = null;
            }

            switch (state)
            {
                case State.WalkingIn:
                case State.WalkingToRental:
                case State.WalkingToPool:
                case State.WalkingOut:
                case State.WalkingToSnack:
                    SetPose(Pose.Walk);
                    FollowPath();
                    // People who just ate leave wrappers and cups behind more often.
                    if (this != null) Trash.MaybeDrop(transform.position, Time.deltaTime * (ateSnack ? 3f : 1f));
                    break;
                case State.InDeskLine:
                case State.InRentalLine:
                    SetPose(Pose.Walk);
                    StandInLine();
                    break;
                case State.InSnackLine:
                    SetPose(Pose.Walk);
                    StandInSnackLine();
                    break;
                case State.Paddling:
                case State.PaddlingBack:
                    if (KickOut()) break;
                    SetPose(Pose.Paddle);
                    rig.Sprint = state == State.PaddlingBack;
                    PaddleTo(lineupTarget, state == State.Paddling ? paddleSpeed : paddleBackSpeed, State.WaitingForWave);
                    break;
                case State.WaitingForWave:
                    // Sit up facing the incoming waves; when one comes, turn for the beach, lie down and sprint.
                    bool waveComing = WaveComing();
                    SetPose(waveComing ? Pose.Paddle : Pose.Straddle);
                    rig.Sprint = waveComing;
                    Float(transform.position + Separation(WaterSpace) * Time.deltaTime, waveComing ? Vector3.right : Vector3.left);
                    if (!waveComing && Time.time >= nextFidget && !rig.Gesturing) Fidget();
                    if (DrainPatience(wavePatience * ParkEffects.WavePatienceMultiplier)) LeaveAngry("Where are the waves?!");
                    else TryCatchWave();
                    break;
                case State.Riding:
                    SetPose(Pose.Ride);
                    Ride();
                    break;
                case State.Wipeout:
                    SetPose(Pose.Wipeout);
                    Tumble();
                    break;
                case State.SwimmingToBeach:
                    if (KickOut()) break;
                    SetPose(Pose.Paddle);
                    rig.Sprint = false;
                    var beach = pool.BeachLanding.position;
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
            segmentStart = transform.position;
        }

        /// <summary>A spot in the lineup away from where the other surfers in this pool are sitting.</summary>
        Vector3 FreeLineupSpot()
        {
            Vector3 best = pool.Lineup.position;
            float bestGap = -1f;
            float halfWidth = Mathf.Max(1f, water.Width * 0.5f - 2f);
            for (int i = 0; i < 8; i++)
            {
                var spot = pool.Lineup.position + new Vector3(Random.Range(-2f, 2f), 0f, 0f);
                // Beginners wait on the inside, nearer the beach, where bigger waves have already turned to whitewater.
                if (Skill == WaveLevel.Beginner)
                    spot = water.transform.TransformPoint(new Vector3(water.Length * InsideLineup + Random.Range(-1.5f, 1.5f), 0f, 0f));
                spot.z = water.transform.position.z + Random.Range(-halfWidth, halfWidth);
                float gap = float.MaxValue;
                foreach (var other in All)
                    if (other != this && other.pool == pool)
                        gap = Mathf.Min(gap, (other.lineupTarget - spot).sqrMagnitude);
                if (gap > bestGap)
                {
                    bestGap = gap;
                    best = spot;
                }
            }
            return best;
        }

        /// <summary>The path point shifted sideways into this surfer's lane.</summary>
        Vector3 InLane(Vector3 point)
        {
            Vector3 direction = point - segmentStart;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) return point;
            direction.Normalize();
            return point + new Vector3(-direction.z, 0f, direction.x) * lane;
        }

        /// <summary>A gentle push away from surfers that are too close (horizontal only).</summary>
        /// <summary>
        /// Kick-out at the end of a ride: still standing, turn up and over the back of the wave with a little hop,
        /// then drop onto the board (the rig blends into paddling). Returns true while it's happening.
        /// </summary>
        bool KickOut()
        {
            float t = (Time.time - kickOutStart) / kickOutTime;
            if (t < 0f || t >= 1f) return false;
            SetPose(Pose.Ride);
            if (celebrate)
            {
                celebrate = false;
                rig.PlayGesture(SurferPersonalities.Celebration(archetype), kickOutTime);
            }
            float hop = Mathf.Sin(t * Mathf.PI);
            Vector3 p = transform.position + Vector3.left * (2.5f * Time.deltaTime);
            p.y = water.SurfaceHeightAt(p) + hop * 0.5f;
            transform.position = p;
            Face(Vector3.left);
            rig.Air = hop * 0.6f;
            rig.Crouch = 0.2f;
            rig.Twist = 0f;
            rig.ArmsUp = hop * 0.5f;
            var tilt = Quaternion.Euler(-20f * hop, 0f, 0f);
            rig.BoardTilt = tilt;
            board.localPosition = CharacterRig.BoardPivot;
            board.localRotation = tilt;
            return true;
        }

        /// <summary>Sitting in the lineup: now and then stretch, splash about or scratch the head.</summary>
        void Fidget()
        {
            if (nextFidget > 0f)
            {
                var moves = new[] { CharacterRig.Gesture.Stretch, CharacterRig.Gesture.Splash, CharacterRig.Gesture.ScratchHead };
                rig.PlayGesture(moves[Random.Range(0, moves.Length)], Random.Range(1.4f, 2.2f));
            }
            nextFidget = Time.time + Random.Range(6f, 15f);
        }

        /// <summary>Surfers waiting nearby hoot when someone catches a wave (half of them, after a beat).</summary>
        void HootForMe()
        {
            foreach (var other in All)
            {
                if (other == this || other.pool != pool || other.state != State.WaitingForWave) continue;
                if ((other.transform.position - transform.position).sqrMagnitude > 12f * 12f || Random.value > 0.5f) continue;
                other.hootAt = Time.time + Random.Range(0.2f, 0.8f);
                other.lookUntil = Time.time + 2.5f;
                other.rig.LookTarget = transform.position + Vector3.up;
            }
        }

        /// <summary>Big moments nearby (a pro's tube or air) turn heads.</summary>
        void OnHighlight(Vector3 where, float strength)
        {
            if ((where - transform.position).sqrMagnitude > 40f * 40f || state == State.Riding) return;
            rig.LookTarget = where + Vector3.up;
            lookUntil = Time.time + 2.5f;
        }

        /// <summary>Is a wave this surfer hasn't passed on rolling up behind (turn and paddle hard to catch it)?</summary>
        bool WaveComing()
        {
            float x = water.transform.InverseTransformPoint(transform.position).x;
            for (int i = 0; i < water.ActiveWaves; i++)
            {
                var (id, _) = water.GetWave(i);
                if (id == skippedWave) continue;
                float crest = water.CurrentWaveCenter(id);
                if (crest > x - 12f && crest < x + 0.5f) return true;
            }
            return false;
        }

        /// <summary>Someone else already riding this wave within space meters (along the wave) of z, or null.</summary>
        Surfer RiderNear(float wave, float z, float space)
        {
            foreach (var other in All)
                if (other != this && other.water == water && other.state == State.Riding && other.rideWave == wave
                    && Mathf.Abs(other.rideZ - z) < space)
                    return other;
            return null;
        }

        /// <summary>
        /// Sideways nudge (m, along the wave) that keeps riders who share the water from riding on top of each
        /// other: anyone riding closer than RideSpace pushes this rider's target away.
        /// </summary>
        float RiderSpacing(float x)
        {
            float push = 0f;
            foreach (var other in All)
            {
                if (other == this || other.water != water || other.state != State.Riding) continue;
                Vector3 local = water.transform.InverseTransformPoint(other.transform.position);
                if (Mathf.Abs(local.x - x) > RideSpace) continue; // well ahead or behind on the face
                float dz = rideZ - other.rideZ;
                if (Mathf.Abs(dz) >= RideSpace) continue;
                // Exactly side by side: split by lane so the two go opposite ways.
                float away = Mathf.Abs(dz) > 0.05f ? Mathf.Sign(dz) : (lane >= other.lane ? 1f : -1f);
                push += away * (RideSpace - Mathf.Abs(dz)) * 2f;
            }
            return push;
        }

        Vector3 Separation(float space)
        {
            Vector3 push = Vector3.zero;
            foreach (var other in All)
            {
                if (other == this || other.state is State.Riding or State.Wipeout) continue;
                Vector3 away = transform.position - other.transform.position;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance >= space) continue;
                if (distance < 0.001f) away = new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f);
                push += away.normalized * (space - distance);
            }
            return push * 2.5f;
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
                else if (state == State.InSnackLine)
                {
                    if (SnackBar.Instance == null) ExitPark();
                    else
                    {
                        atFront = false;
                        patience = 1f;
                        SnackBar.Instance.Join(this);
                    }
                }
                return;
            }
            Vector3 target = path.Peek();
            Vector3 goal = InLane(target);
            StepTowards(goal, true);
            Vector3 left = goal - transform.position;
            left.y = 0f;
            if (left.sqrMagnitude >= 0.04f) return;
            path.Dequeue();
            segmentStart = target;
            if (hasRentalBoard && (target - spawner.ReturnBinSpot).sqrMagnitude < 0.01f) ReturnBoard();
        }

        /// <summary>Waits for food. Gives up (no harm done, just no sale) if the line takes too long.</summary>
        void StandInSnackLine()
        {
            var bar = SnackBar.Instance;
            if (bar == null)
            {
                ExitPark();
                return;
            }
            if (DrainPatience(deskPatience))
            {
                bar.Leave(this);
                Shout("Too slow, I'll eat at home.", 3f);
                ExitPark();
                return;
            }
            var (slot, front, facing) = bar.LineSlot(this);
            if ((slot - transform.position).sqrMagnitude > 0.0004f)
            {
                StepTowards(slot);
                atFront = false;
                return;
            }
            Face(facing);
            atFront = front;
        }

        /// <summary>Called by the snack bar when this surfer is served.</summary>
        public void ReceiveSnack()
        {
            ateSnack = true;
            Shout(Random.value < 0.5f ? "Yum, tacos!" : "Smoothie time!", 2.5f);
            ExitPark();
        }

        /// <summary>From the snack bar to the park exit.</summary>
        void ExitPark()
        {
            var route = new List<Vector3>(SnackBar.Instance != null ? SnackBar.Instance.ExitPath : new Vector3[0]);
            var outPath = pool.PathOut;
            if (outPath.Length > 0) route.Add(outPath[outPath.Length - 1].position);
            Walk(State.WalkingOut, route.ToArray(), State.WalkingOut);
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

        void StepTowards(Vector3 target, bool makeSpace = false)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
            Vector3 heading = next - transform.position;
            if (makeSpace) next += Separation(WalkSpace) * Time.deltaTime;
            Face(heading);
            transform.position = next;
        }

        void PaddleTo(Vector3 target, float speed, State whenArrived)
        {
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            Vector3 step = Vector3.ClampMagnitude(toTarget, speed * Time.deltaTime);
            Float(transform.position + step + Separation(WaterSpace) * Time.deltaTime, step);
            // Arriving at the lineup: close is good enough (someone may be sitting right next to the spot).
            if (toTarget.magnitude > (whenArrived == State.WaitingForWave ? 0.7f : 0.1f)) return;

            if (whenArrived == State.WalkingOut)
            {
                var landing = pool.BeachLanding.position;
                landing.z = transform.position.z;
                var route = new List<Vector3> { landing };
                if (wantsSnack && SnackBar.Instance != null)
                {
                    // Past the beach end, then over to the snack bar.
                    if (pool.PathOut.Length > 0) route.Add(pool.PathOut[0].position);
                    route.AddRange(SnackBar.Instance.ApproachPath);
                    Walk(State.WalkingToSnack, route.ToArray(), State.InSnackLine);
                    return;
                }
                route.AddRange(System.Array.ConvertAll(hasRentalBoard ? pool.ReturnPathOut : pool.PathOut, t => t.position));
                Walk(State.WalkingOut, route.ToArray(), State.WalkingOut);
                return;
            }
            patience = 1f;
            state = whenArrived;
        }

        /// <summary>Moves to a horizontal position, dropping smoothly onto the water surface.</summary>
        void Float(Vector3 position, Vector3 facing)
        {
            // Stay off the pool walls (being pushed apart by other surfers mustn't float anyone out of the pool).
            Vector3 local = water.transform.InverseTransformPoint(position);
            float limit = water.Width * 0.5f - 1f;
            if (Mathf.Abs(local.z) > limit)
            {
                local.z = Mathf.Clamp(local.z, -limit, limit);
                position = water.transform.TransformPoint(local);
            }
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
                // Beginners on the inside happily ride the whitewater of any bigger wave once it has broken.
                bool whitewash = Skill == WaveLevel.Beginner && diff > 0;
                if (whitewash && !water.IsBroken(wave)) continue; // not whitewater yet: wait for it
                // Etiquette: no dropping in on someone already riding this green wave right beside you.
                // (Whitewater is shared; riders there just keep their distance.)
                dropInVictim = null;
                var near = whitewash ? null : RiderNear(wave, water.transform.InverseTransformPoint(transform.position).z, DropInSpace);
                if (near != null && archetype == Archetype.Kook && Random.value < 0.1f)
                    dropInVictim = near; // kooks don't always know the rules...
                else if (near != null)
                {
                    skippedWave = wave;
                    if (Random.value < 0.5f) Shout("Your wave, go go!", 2f);
                    continue;
                }
                // Better surfers let waves that are too small roll past and keep waiting.
                if (diff < 0)
                {
                    skippedWave = wave;
                    if (Random.value < 0.5f) Shout("Too small for me, I'll wait.", 2f);
                    continue;
                }
                if (!whitewash && Mathf.Abs(diff) >= 2)
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
                rideZ = rideLaneZ;
                rideQuality = diff == 0 ? 1f : whitewash ? (diff == 1 ? 1f : 0.85f) : diff < 0 ? 0.5f : 0.6f;
                rideWhitewash = whitewash;

                // On a single-direction wave, going your frontside feels better than going backside.
                var waveDirection = water.DirectionOf(wave);
                bool backside = level != WaveLevel.Beginner && waveDirection != WaveDirection.Peak
                                && waveDirection != PreferredDirection;
                if (backside) rideQuality *= 0.85f;
                rideMatched = diff == 0 || whitewash;
                string famous = water.FamousName(wave);
                if (famous != null && diff == 0)
                {
                    rodeFamous = true;
                    rideQuality = Mathf.Min(1f, rideQuality + 0.15f);
                    Shout($"I'm surfing {famous}!!", 2.5f);
                }
                nextTrickAt = Time.time + TrickDelay() * 0.5f; // first move soon after takeoff

                // Too-big waves often end in a wipeout; everyone wipes out sometimes.
                float wipeoutChance = whitewash ? (diff == 1 ? 0.22f : 0.35f) : diff > 0 ? 0.6f : Skill switch { WaveLevel.Beginner => 0.2f, WaveLevel.Intermediate => 0.1f, _ => 0.05f };
                if (Skill == WaveLevel.Beginner) wipeoutChance *= ParkEffects.BeginnerWipeoutMultiplier;
                wipeoutAt = Random.value < wipeoutChance ? Time.time + Random.Range(0.8f, 2.5f) : -1f;

                state = State.Riding;
                move = Move.None;
                feel.Begin(transform.position, transform.rotation);
                landSquash = 0f;
                wasAirborne = false;
                Shout(whitewash ? "Riding the whitewater!" : diff < 0 ? "Kinda small..." : diff > 0 ? "Whoa, big one!"
                    : backside ? "Going backside!" : Famous != null ? Famous.catchLines[Random.Range(0, Famous.catchLines.Length)]
                    : SurferPersonalities.Line(archetype, Moment.Catch), 2f,
                    archetype is Archetype.Grom or Archetype.Charger or Archetype.Local || Random.value < 0.4f ? Sound.VoiceYew : null);
                if (dropInVictim != null)
                {
                    // Drop-in drama: the surfer already on it yells, the kook apologizes, the lifeguard whistles.
                    dropInVictim.Shout(SurferPersonalities.Line(dropInVictim.archetype, Moment.DropInVictim), 2.5f);
                    dropInVictim.rig.ShowFace(SurfPool.Face.Grumpy, 3f);
                    Shout(SurferPersonalities.Line(archetype, Moment.DropInSorry), 2.5f);
                    Lifeguard.Whistle();
                }
                HootForMe();
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

            // Peeling waves: ride just ahead of the break point (the pocket) on your side.
            // Whitewater: ride straight in front of the foam, carving a little.
            float limit = water.Width * 0.5f - 1.5f;
            float carve = Mathf.Sin((Time.time - rideStartTime) * carveSpeed);
            float targetZ;
            inBarrel = false;
            if (!rideWhitewash && water.TryGetPocket(rideWave, PreferredSide, out float pocketZ, out int side, out bool breaking,
                    out bool finished, out bool barrel))
            {
                if (finished)
                {
                    RecordMood(rideQuality);
                    FinishRide(false);
                    return;
                }
                float ahead = Skill switch { WaveLevel.Pro => 0.7f, WaveLevel.Intermediate => 2f, _ => 3f };
                targetZ = pocketZ + side * ahead + (breaking ? carve * 0.8f : 0f);
                // Cutback: swing back toward the breaking part, then return to the pocket. Floater: up on the lip.
                float m = MoveProgress();
                if (move == Move.Cutback) targetZ -= side * 3f * Mathf.Sin(m * Mathf.PI);
                if (move == Move.Floater) offset -= 0.4f * Mathf.Sin(m * Mathf.PI);
                inBarrel = barrel && breaking && Skill == WaveLevel.Pro;
                if (inBarrel) offset = 0.9f;
            }
            else
            {
                targetZ = rideLaneZ + carve * carveWidth * 0.4f;
            }
            targetZ += RiderSpacing(crest + offset);
            rideZ = Mathf.Clamp(Mathf.MoveTowards(rideZ, targetZ, 7f * Time.deltaTime), -limit, limit);

            Vector3 target = water.transform.TransformPoint(new Vector3(crest + offset, 0f, rideZ));
            const float airTime = 0.7f;
            float air = Mathf.Clamp01((Time.time - airStart) / airTime);
            target.y = water.SurfaceHeightAt(target) + (air < 1f ? Mathf.Sin(air * Mathf.PI) * 0.9f : 0f);
            Vector3 delta = target - transform.position;
            transform.position = target;
            Face(delta);
            UpdateRideFeel(air);
        }

        /// <summary>
        /// Body and board while riding: the board lies on the wave face and leans into turns, the rider crouches
        /// into carves and the barrel, tucks and grabs in airs (pros may spin), squashes on landing, and the rail
        /// throws spray. Beginners wobble.
        /// </summary>
        void UpdateRideFeel(float air)
        {
            var heading = transform.rotation;
            // Maneuvers: how far the board lays over, the head/shoulder twist, arms, crouch, spray and nose lift.
            float m = MoveProgress(), k = m < 0f ? 0f : Mathf.Sin(m * Mathf.PI);
            float roll = 0f, twist = 0f, armsUp = 0f, crouch = 0f, spray = 0f, noseUp = 0f;
            switch (move)
            {
                case Move.Snap: // short and hard at the top of the wave, a fan of spray
                    roll = moveDirection * 28f * k; twist = 50f * k; crouch = 0.2f * k; spray = k;
                    break;
                case Move.Cutback: // head leads ~140 degrees back toward the foam, the lean flips halfway
                    roll = moveDirection * 26f * Mathf.Sin(m * Mathf.PI * 2f); twist = 140f * k; crouch = 0.15f * k; spray = 0.8f * k;
                    break;
                case Move.Floater: // arms up, weight back, nose lifted over the foam
                    armsUp = k; noseUp = 12f * k;
                    break;
                case Move.BottomTurn: // compress, lean hard on the rail, extend out of it
                    roll = moveDirection * 24f * k; crouch = 0.35f * Mathf.Cos(m * Mathf.PI); spray = 0.5f * k;
                    break;
            }
            feel.ExtraRoll = roll
                + (Skill == WaveLevel.Beginner ? Mathf.Sin(Time.time * 4.1f) * 5f + Mathf.Sin(Time.time * 6.7f) * 3f : 0f);
            feel.Step(water, transform.position, heading, Time.deltaTime);

            bool airborne = air < 1f;
            float lift = airborne ? Mathf.Sin(air * Mathf.PI) : 0f;
            float spin = airborne && airSpin ? Mathf.SmoothStep(0f, 360f, air) : 0f;
            var tilt = Quaternion.AngleAxis(spin, Vector3.up) * feel.Tilt * Quaternion.Euler(-15f * lift - noseUp, 0f, 0f);
            rig.BoardTilt = tilt;
            board.localPosition = CharacterRig.BoardPivot;
            board.localRotation = tilt;

            if (wasAirborne && !airborne)
            {
                landSquash = 0.45f;
                RideFeel.Splash(water, transform.position, 16, 2.6f);
                Sfx.Play(Sound.Splash, transform.position, 0.5f);
            }
            wasAirborne = airborne;
            landSquash = Mathf.MoveTowards(landSquash, 0f, Time.deltaTime * 2f);

            rig.Air = lift;
            rig.Lean = feel.Turn * (rig.Goofy ? -1f : 1f);
            rig.Twist = twist;
            rig.ArmsUp = armsUp;
            rig.Crouch = Mathf.Abs(feel.Turn) * 0.3f + (inBarrel ? 0.4f : 0f) + landSquash + crouch;
            if (!airborne) feel.Spray(water, transform.position, heading, spray);
        }

        /// <summary>Seconds to the next move: pros most often, intermediates and beginners (who mostly trim) less.</summary>
        float TrickDelay() => Random.Range(trickInterval.x, trickInterval.y)
            * Skill switch { WaveLevel.Pro => 1f, WaveLevel.Intermediate => 1.3f, _ => 1.8f };

        /// <summary>0..1 through the current maneuver, or -1 when there is none.</summary>
        float MoveProgress()
        {
            if (move == Move.None) return -1f;
            float t = (Time.time - moveStart) / moveLength;
            if (t < 1f) return t;
            move = Move.None;
            return -1f;
        }

        void StartMove(Move kind, float seconds)
        {
            move = kind;
            moveStart = Time.time;
            moveLength = seconds;
            // Lean further into the turn you're in (negative roll = lean right).
            moveDirection = feel.Turn >= 0f ? -1f : 1f;
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
            bool signature = Famous != null && Random.value < 0.45f;
            if (signature) trick = Famous.signature;
            if (inBarrel)
            {
                trick = Random.value < 0.6f ? "Tube ride!" : "Getting barreled!";
                GameStats.TubeRides++;
            }
            GameStats.TricksLanded++;
            if (inBarrel || Skill == WaveLevel.Pro || Random.value < 0.3f) Sfx.Play(Sound.Cheer, transform.position, inBarrel ? 1f : 0.7f);
            // Tube rides and pro airs are the moments people come to see.
            if (inBarrel || signature) Highlights.Moment(transform.position, 1f);
            else if (trick == "AIR!" && Skill == WaveLevel.Pro) Highlights.Moment(transform.position, 0.8f);
            if (trick == "AIR!" || signature && Famous.signature.Contains("AIR") || signature && Famous.signature.Contains("540"))
            {
                airStart = Time.time;
                airSpin = Random.value < 0.5f;
            }
            else if (trick is "Snap!" or "Re-entry!") StartMove(Move.Snap, 0.5f);
            else if (trick == "Cutback!") StartMove(Move.Cutback, 1.4f);
            else if (trick == "Floater!") StartMove(Move.Floater, 0.8f);
            else if (trick == "Bottom turn!") StartMove(Move.BottomTurn, 1f);
            tricksLanded++;
            LastTrickAt = Time.time;
            Shout(trick, 1.3f, Random.value < 0.5f ? Sound.VoiceWoo : null);
            nextTrickAt = Time.time + TrickDelay();
        }

        void StartWipeout()
        {
            state = State.Wipeout;
            wipeoutStart = Time.time;
            tumbleAxis = Random.onUnitSphere;
            RecordMood(ParkEffects.WipeoutMood + ParkPerks.WipeoutMoodBonus);
            GameStats.Wipeouts++;
            Sfx.Play(Sound.Splash, transform.position);
            RideFeel.Splash(water, transform.position, 24, 3f);
            Lifeguard.Whistle();
            Shout(SurferPersonalities.Line(archetype, Moment.Wipeout), 2.5f, Sound.VoiceAah);
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
            if (!wipedOut)
            {
                // Ride out over the back of the wave before lying down; a good ride gets a celebration on the way.
                kickOutStart = Time.time;
                celebrate = rideQuality >= 0.85f || rodeFamous;
                kickOutTime = celebrate ? 1.4f : 0.7f;
            }
            ridesDone++;
            if (ridesDone < wavesPerSession)
            {
                if (!wipedOut) Shout("One more!", 2f);
                state = State.PaddlingBack;
                return;
            }

            float happiness = (moodSamples == 0 ? 1f : moodTotal / moodSamples) - Trash.MoodPenalty + Decoration.HappinessBonus;
            int tip = happiness > 0.5f ? Mathf.RoundToInt((paidPrice * maxTipShare * happiness * (rodeFamous ? 1.5f : 1f) + tricksLanded * tipPerTrick) * ParkPerks.TipMultiplier) : 0;
            if (tip > 0) Wallet.Instance.Add(tip, "tip");
            if (Skill == WaveLevel.Beginner && ParkEffects.LessonFee > 0) Wallet.Instance.Add(SurfCoach.TeachLesson(), "surf lessons");
            if (happiness > 0.5f && ParkEffects.MerchPerHappySurfer > 0) Wallet.Instance.Add(ParkEffects.MerchPerHappySurfer, "surf shop");
            if (happiness > 0.5f && PhotoGrade >= 1) PhotoTower.SellPrint(this);
            spawner.ReportServed(happiness);
            if (happiness > 0.5f) ParkLevel.Add(ParkLevel.XpForSurfer(Skill));
            // Main-pool surfers walking out (not carrying a rental board back) may stop at the snack bar.
            wantsSnack = !hasRentalBoard && pool == spawner.MainPool && SnackBar.Instance != null && SnackBar.Instance.WantsSnack(happiness);
            GameStats.SurfersServed++;
            string line = SurferPersonalities.Line(archetype, happiness > 0.75f ? Moment.GoodEnd : happiness > 0.5f ? Moment.OkEnd : Moment.BadEnd);
            if (Famous != null) line = FamousVisits.LegendLeaves(this, happiness, paidPrice);
            rig.ShowFace(happiness > 0.75f ? SurfPool.Face.Stoked : happiness > 0.5f ? SurfPool.Face.Happy : SurfPool.Face.Grumpy, 3.5f);
            Shout(line + (tip > 0 ? $"  (tipped ${tip})" : ""), 5f);
            state = State.SwimmingToBeach;
        }

        /// <summary>
        /// Closing time. People who haven't paid head home (no hard feelings); paid surfers finish the wave they're on
        /// and then get out. With <paramref name="now"/> (stragglers, a while after closing) surfers in the water
        /// end their session right away.
        /// </summary>
        public void GoHome(bool now)
        {
            if (angry || state is State.WalkingOut or State.SwimmingToBeach or State.WalkingToSnack or State.InSnackLine) return;
            if (state is State.WalkingIn or State.InDeskLine or State.WalkingToRental or State.InRentalLine)
            {
                if (paidPrice > 0 && isRenter) return; // paid, waiting for a board: let them have their surf
                if (paidPrice <= 0)
                {
                    spawner.LeaveLines(this);
                    Shout(Random.value < 0.5f ? "Aw, closing already?" : "See you tomorrow!", 3f);
                    if (state == State.InRentalLine || state == State.WalkingToRental)
                        Walk(State.WalkingOut, new[] { spawner.Entrance }, State.WalkingOut);
                    else Walk(State.WalkingOut, spawner.PathBackOut, State.WalkingOut);
                    return;
                }
            }
            wavesPerSession = Mathf.Min(wavesPerSession, ridesDone + 1);
            if (now && state is State.Paddling or State.WaitingForWave or State.PaddlingBack)
            {
                wavesPerSession = ridesDone + 1;
                FinishRide(true); // no kick-out move: just head in
            }
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
            patience -= Time.deltaTime / (seconds * ParkPerks.PatienceMultiplier);
            return patience <= 0f;
        }

        void RecordMood(float value)
        {
            moodTotal += Mathf.Clamp01(value);
            moodSamples++;
        }

        float Voice => rig != null && rig.Look != null ? rig.Look.voice : 1f;

        /// <summary>Says something: a speech bubble and a babble in their own voice (or a shout instead of words).</summary>
        void Shout(string text, float seconds, Sound? exclaim = null)
        {
            shout = text;
            shoutUntil = Time.time + seconds;
            if (exclaim.HasValue) Voices.Exclaim(transform, Voice, exclaim.Value);
            else Voices.Talk(transform, Voice, text);
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
                case Pose.Walk: // walking or standing, board carried on its edge under the right arm
                    rig.CurrentMode = CharacterRig.Mode.Locomotion;
                    rig.CarryingBoard = board.gameObject.activeSelf;
                    float size = rig.Look != null ? rig.Look.height : 1f; // groms carry it lower
                    Place(board, new Vector3((0.56f + 0.07f * board.localScale.x) * size, 0.9f * size, 0.15f), new Vector3(-8f, 0f, 90f));
                    break;
                case Pose.Paddle: // lying on the board, paddling
                    rig.CurrentMode = CharacterRig.Mode.Paddle;
                    rig.CarryingBoard = false;
                    Place(board, new Vector3(0f, 0.05f, 0f), Vector3.zero);
                    break;
                case Pose.Ride: // standing sideways on the board, knees bent
                    rig.CurrentMode = CharacterRig.Mode.Ride;
                    rig.CarryingBoard = false;
                    Place(board, new Vector3(0f, 0.05f, 0f), Vector3.zero);
                    break;
                case Pose.Straddle: // sitting up on the board in the lineup
                    rig.CurrentMode = CharacterRig.Mode.Straddle;
                    rig.CarryingBoard = false;
                    Place(board, new Vector3(0f, 0.05f, 0f), Vector3.zero);
                    break;
                case Pose.Wipeout: // tumbling, arms and legs everywhere (the board spins on its own)
                    rig.CurrentMode = CharacterRig.Mode.Flail;
                    rig.CarryingBoard = false;
                    break;
            }
        }

        static void Place(Transform part, Vector3 localPosition, Vector3 localEuler)
        {
            part.localPosition = localPosition;
            part.localEulerAngles = localEuler;
        }

        /// <summary>Beginners ride big foam boards, intermediates funboards, pros short pointy thrusters.</summary>
        void UseBoardFor(WaveLevel skill)
        {
            if (boardModels == null || boardModels.Length <= (int)skill || boardModels[(int)skill] == null) return;
            var source = boardModels[(int)skill].GetComponentInChildren<MeshFilter>();
            board.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            board.GetComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            board.localScale = Vector3.one;
            rig.BoardLength = source.sharedMesh.bounds.size.z;
        }

        /// <summary>Tints a part. Boards only get their deck tinted, so stripes, stringer and fins keep their colors.</summary>
        static void Paint(Transform part, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            var renderer = part.GetComponent<Renderer>();
            var materials = renderer.sharedMaterials;
            bool tinted = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !materials[i].name.StartsWith("BoardDeck")) continue;
                renderer.SetPropertyBlock(block, i);
                tinted = true;
            }
            if (!tinted) renderer.SetPropertyBlock(block);
        }
    }
}
