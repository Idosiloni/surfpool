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
        PoolArea pool;
        WaveWater water;
        State state;
        readonly Queue<Vector3> path = new();
        State stateAfterPath;
        Vector3 lineupTarget;
        float lane;            // sideways offset from the path (m), so surfers don't walk in single file on top of each other
        Vector3 segmentStart;  // where the current path segment began (lane direction)

        const float WalkSpace = 0.8f;   // personal space on foot (m)
        const float WaterSpace = 1.8f;  // personal space on a board (boards are 2 m long)
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

        /// <summary>The pool this surfer was sent to.</summary>
        public PoolArea Pool => pool;

        /// <summary>The wave type this surfer is good at.</summary>
        public WaveLevel Skill { get; private set; }

        /// <summary>Regular-foot surfers prefer rights (their frontside), goofy-foot surfers prefer lefts.</summary>
        public WaveDirection PreferredDirection { get; private set; }

        int PreferredSide => PreferredDirection == WaveDirection.Right ? -1 : 1;

        string WantedWave => Skill == WaveLevel.Beginner
            ? "a Beginner wave"
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
            var skin = Color.Lerp(new Color(0.42f, 0.27f, 0.17f), new Color(1f, 0.84f, 0.7f), Random.value);
            var hair = Random.value < 0.25f ? new Color(0.95f, 0.82f, 0.45f) : Color.Lerp(new Color(0.08f, 0.06f, 0.05f), new Color(0.45f, 0.28f, 0.15f), Random.value);
            rig.Paint(skin, Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.6f, 1f), Random.ColorHSV(0f, 1f, 0.4f, 0.8f, 0.3f, 0.8f), hair, Random.Range(0, 2));
            Paint(board, Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f));
            isRenter = renter;
            lane = Random.Range(-0.6f, 0.6f);
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
                    if (this != null) Trash.MaybeDrop(transform.position, Time.deltaTime);
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
                    Float(transform.position + Separation(WaterSpace) * Time.deltaTime, Vector3.right);
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
                rideZ = rideLaneZ;
                rideQuality = diff == 0 ? 1f : diff < 0 ? 0.5f : 0.6f;

                // On a single-direction wave, going your frontside feels better than going backside.
                var waveDirection = water.DirectionOf(wave);
                bool backside = level != WaveLevel.Beginner && waveDirection != WaveDirection.Peak
                                && waveDirection != PreferredDirection;
                if (backside) rideQuality *= 0.85f;
                rideMatched = diff == 0;
                nextTrickAt = Time.time + Random.Range(trickInterval.x, trickInterval.y);

                // Too-big waves often end in a wipeout; everyone wipes out sometimes.
                float wipeoutChance = diff > 0 ? 0.6f : Skill switch { WaveLevel.Beginner => 0.2f, WaveLevel.Intermediate => 0.1f, _ => 0.05f };
                if (Skill == WaveLevel.Beginner) wipeoutChance *= ParkEffects.BeginnerWipeoutMultiplier;
                wipeoutAt = Random.value < wipeoutChance ? Time.time + Random.Range(0.8f, 2.5f) : -1f;

                state = State.Riding;
                Shout(diff < 0 ? "Kinda small..." : diff > 0 ? "Whoa, big one!"
                    : backside ? "Going backside!" : RideShouts[Random.Range(0, RideShouts.Length)], 2f);
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
            if (water.TryGetPocket(rideWave, PreferredSide, out float pocketZ, out int side, out bool breaking,
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
                inBarrel = barrel && breaking && Skill == WaveLevel.Pro;
                if (inBarrel) offset = 0.9f;
            }
            else
            {
                targetZ = rideLaneZ + carve * carveWidth * 0.4f;
            }
            rideZ = Mathf.Clamp(Mathf.MoveTowards(rideZ, targetZ, 7f * Time.deltaTime), -limit, limit);

            Vector3 target = water.transform.TransformPoint(new Vector3(crest + offset, 0f, rideZ));
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
            if (inBarrel)
            {
                trick = Random.value < 0.6f ? "Tube ride!" : "Getting barreled!";
                GameStats.TubeRides++;
            }
            GameStats.TricksLanded++;
            if (inBarrel || Skill == WaveLevel.Pro || Random.value < 0.3f) Sfx.Play(Sound.Cheer, transform.position, inBarrel ? 1f : 0.7f);
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
            GameStats.Wipeouts++;
            Sfx.Play(Sound.Splash, transform.position);
            Lifeguard.Whistle();
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

            float happiness = (moodSamples == 0 ? 1f : moodTotal / moodSamples) - Trash.MoodPenalty;
            int tip = happiness > 0.5f ? Mathf.RoundToInt(paidPrice * maxTipShare * happiness) + tricksLanded * tipPerTrick : 0;
            if (tip > 0) Wallet.Instance.Add(tip, "tip");
            if (Skill == WaveLevel.Beginner && ParkEffects.LessonFee > 0) Wallet.Instance.Add(SurfCoach.TeachLesson(), "surf lessons");
            if (happiness > 0.5f && ParkEffects.MerchPerHappySurfer > 0) Wallet.Instance.Add(ParkEffects.MerchPerHappySurfer, "surf shop");
            spawner.ReportServed(happiness);
            if (happiness > 0.5f) ParkLevel.Add(ParkLevel.XpForSurfer(Skill));
            GameStats.SurfersServed++;
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
                case Pose.Walk: // walking or standing, board carried on its edge under the right arm
                    rig.CurrentMode = CharacterRig.Mode.Locomotion;
                    rig.CarryingBoard = board.gameObject.activeSelf;
                    Place(board, new Vector3(0.5f + 0.07f * board.localScale.x, 0.9f, 0.15f), new Vector3(-8f, 0f, 90f));
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
