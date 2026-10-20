using UnityEngine;

namespace SurfPool
{
    /// <summary>Hired board runner: walks returned boards from the return bin back to the rental rack, one at a time.</summary>
    public class BoardRunner : MonoBehaviour
    {
        [SerializeField] RentalRack rack;
        [SerializeField] ReturnBin bin;
        [Tooltip("Walking route from the rack to the bin. Walked backwards on the way back.")]
        [SerializeField] Transform[] rackToBin;
        [SerializeField] GameObject carriedBoard;
        [SerializeField] float walkSpeed = 2.2f;

        int waypoint;
        int direction; // +1 heading to the bin, -1 heading back, 0 idle at the rack
        bool carrying;

        void OnEnable()
        {
            transform.position = rackToBin[0].position;
            waypoint = 0;
            direction = 0;
            carrying = false;
            carriedBoard.SetActive(false);
        }

        void Update()
        {
            if (direction == 0)
            {
                if (bin.Count > 0 && rack.Count < rack.Capacity) direction = 1;
                return;
            }

            Vector3 target = rackToBin[waypoint].position;
            Vector3 next = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);
            Vector3 step = next - transform.position;
            step.y = 0f;
            if (step.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(step);
            transform.position = next;
            if ((target - next).sqrMagnitude > 0.0004f) return;

            int last = rackToBin.Length - 1;
            if (direction == 1 && waypoint == last)
            {
                carrying = bin.TryTakeBoard();
                carriedBoard.SetActive(carrying);
                direction = -1;
            }
            else if (direction == -1 && waypoint == 0)
            {
                if (carrying && !rack.TryPutBoard()) bin.Add(); // rack filled up meanwhile: put it back
                carrying = false;
                carriedBoard.SetActive(false);
                direction = 0;
                return;
            }
            waypoint += direction;
        }
    }
}
