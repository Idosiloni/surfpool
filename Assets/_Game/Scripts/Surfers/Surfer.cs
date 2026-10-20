using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A surfer customer: walks to the ticket desk, waits to pay, walks to the pool, paddles out,
    /// waits for a wave, rides it toward the beach, then walks out of the park.
    /// </summary>
    public class Surfer : MonoBehaviour
    {
        public static readonly List<Surfer> All = new();

        enum State { WalkingToDesk, WaitingAtDesk, WalkingToPool, Paddling, WaitingForWave, Riding, WalkingOut }
        enum Pose { Walk, Paddle, Ride }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Header("Parts")]
        [SerializeField] Transform body;
        [SerializeField] Transform head;
        [SerializeField] Transform board;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 1.6f;
        [SerializeField] float paddleSpeed = 1.3f;
        [SerializeField] float turnSpeed = 360f;

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
        float happyUntil;

        public bool WaitingToPay => state == State.WaitingAtDesk;

        /// <summary>Short text shown above the surfer's head.</summary>
        public string Status => state switch
        {
            State.WaitingAtDesk => "Wants a surf session",
            State.WaitingForWave => "Waiting for a wave...",
            State.Riding => "Riding!",
            State.WalkingOut when Time.time < happyUntil => "Woohoo! What a wave!",
            _ => null,
        };

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Init(SurferSpawner owner)
        {
            spawner = owner;
            water = owner.Water;
            Paint(body, Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.6f, 1f));
            Paint(head, Color.Lerp(new Color(0.45f, 0.3f, 0.2f), new Color(1f, 0.85f, 0.7f), Random.value));
            Paint(board, Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f));
            Walk(owner.PathToDesk, State.WaitingAtDesk);
        }

        /// <summary>Called by the register when the player sells this surfer a session.</summary>
        public void Pay()
        {
            if (state != State.WaitingAtDesk) return;
            Walk(spawner.PathToPool, State.Paddling);
            var lineup = spawner.Lineup;
            lineupTarget = lineup + new Vector3(0f, 0f, Random.Range(-3f, 3f));
        }

        void Update()
        {
            switch (state)
            {
                case State.WalkingToDesk:
                case State.WalkingToPool:
                case State.WalkingOut:
                    SetPose(Pose.Walk);
                    FollowPath();
                    break;
                case State.WaitingAtDesk:
                    SetPose(Pose.Walk);
                    break;
                case State.Paddling:
                    SetPose(Pose.Paddle);
                    PaddleToLineup();
                    break;
                case State.WaitingForWave:
                    SetPose(Pose.Paddle);
                    Float(transform.position, Vector3.right);
                    TryCatchWave();
                    break;
                case State.Riding:
                    SetPose(Pose.Ride);
                    Ride();
                    break;
            }
        }

        void Walk(Vector3[] points, State whenDone)
        {
            path.Clear();
            foreach (var p in points) path.Enqueue(p);
            state = whenDone switch
            {
                State.WaitingAtDesk => State.WalkingToDesk,
                State.Paddling => State.WalkingToPool,
                _ => State.WalkingOut,
            };
            stateAfterPath = whenDone;
        }

        void FollowPath()
        {
            if (path.Count == 0)
            {
                if (state == State.WalkingOut) Destroy(gameObject);
                else state = stateAfterPath;
                return;
            }
            Vector3 target = path.Peek();
            Vector3 next = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
            Face(next - transform.position);
            transform.position = next;
            if ((target - next).sqrMagnitude < 0.0004f) path.Dequeue();
        }

        void PaddleToLineup()
        {
            Vector3 flat = transform.position;
            Vector3 toTarget = lineupTarget - flat;
            toTarget.y = 0f;
            Vector3 step = Vector3.ClampMagnitude(toTarget, paddleSpeed * Time.deltaTime);
            Float(flat + step, step);
            if (toTarget.magnitude < 0.1f) state = State.WaitingForWave;
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
                    rideWave = wave;
                    rideStartTime = Time.time;
                    rideLaneZ = water.transform.InverseTransformPoint(transform.position).z;
                    state = State.Riding;
                    return;
                }
            }
        }

        void Ride()
        {
            float crest = water.CurrentWaveCenter(rideWave);
            if (crest / water.Length >= rideEndProgress)
            {
                happyUntil = Time.time + 4f;
                var landing = spawner.BeachLanding;
                landing.z = transform.position.z;
                var route = new List<Vector3> { landing };
                route.AddRange(spawner.PathOut);
                Walk(route.ToArray(), State.WalkingOut);
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
                case Pose.Walk: // standing, board carried upright at the side
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
