using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Hired janitor: walks to the nearest piece of trash and picks it up, going around the pools and wave machine
    /// houses instead of through them. Goes back to the broom closet when the park is clean.
    /// </summary>
    public class Janitor : MonoBehaviour
    {
        [SerializeField] float walkSpeed = 2.4f;
        [Tooltip("Seconds to sweep up one piece.")]
        [SerializeField] float pickTime = 1f;

        readonly List<Vector3> route = new();
        Vector3 home;
        Trash target;
        float pickingUntil;

        void Start() => home = transform.position;

        void Update()
        {
            if (Time.time < pickingUntil) return;
            if (target == null)
            {
                target = Nearest();
                PlanRoute(target != null ? target.transform.position : home);
            }
            if (route.Count == 0) return;

            Vector3 goal = route[0];
            goal.y = transform.position.y;
            float speed = walkSpeed / StaffMember.TimeFactor(gameObject);
            Vector3 next = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
            Vector3 step = next - transform.position;
            if (step.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(step), 10f * Time.deltaTime);
            transform.position = next;
            if ((goal - next).sqrMagnitude > 0.04f) return;
            route.RemoveAt(0);

            if (route.Count == 0 && target != null)
            {
                target.PickUp();
                target = null;
                pickingUntil = Time.time + pickTime * StaffMember.TimeFactor(gameObject);
                StaffMember.RecordWork(gameObject);
            }
        }

        Trash Nearest()
        {
            Trash best = null;
            float bestDistance = float.MaxValue;
            foreach (var trash in Trash.All)
            {
                float distance = (trash.transform.position - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = trash;
                }
            }
            return best;
        }

        void PlanRoute(Vector3 destination) => WalkRouter.Plan(transform.position, destination, route);
    }
}
