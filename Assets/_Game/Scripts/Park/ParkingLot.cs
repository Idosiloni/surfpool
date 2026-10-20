using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// The parking lot by the entrance. Cars (with surfboards on the roof) drive over the bridge, park in a free
    /// space, stay a while and drive away. The busier the park, the fuller the lot. Just for life: they don't
    /// change the money.
    /// </summary>
    public class ParkingLot : MonoBehaviour
    {
        [SerializeField] GameObject carPrefab;
        [Tooltip("Parking spaces. Each one's forward is the way a parked car faces.")]
        [SerializeField] Transform[] spaces;
        [Tooltip("Inbound lane from the far end of the bridge to the lot's driving aisle.")]
        [SerializeField] Transform[] roadIn;
        [Tooltip("Outbound lane from the aisle back over the bridge.")]
        [SerializeField] Transform[] roadOut;
        [Tooltip("Z of the aisle between the two rows of spaces.")]
        [SerializeField] float aisleZ = -61f;

        static readonly Color[] Paints =
        {
            new(0.95f, 0.35f, 0.3f), new(0.2f, 0.6f, 0.9f), new(1f, 0.8f, 0.25f), new(0.25f, 0.7f, 0.5f),
            new(0.95f, 0.95f, 0.95f), new(0.15f, 0.17f, 0.2f), new(0.95f, 0.55f, 0.75f), new(0.5f, 0.35f, 0.75f),
        };

        class Car
        {
            public Transform transform;
            public int space;
            public readonly List<Vector3> route = new();
            public bool leaving, parked;
            public float leaveAt, speed;
        }

        readonly List<Car> cars = new();
        readonly HashSet<int> taken = new();
        float nextSpawn;

        int Wanted => Mathf.Clamp(1 + Surfer.All.Count / 2 + BeachGuest.All.Count / 2, 1, spaces.Length);

        void Update()
        {
            if (Time.time >= nextSpawn)
            {
                nextSpawn = Time.time + Random.Range(4f, 9f);
                int coming = 0;
                foreach (var car in cars) if (!car.leaving) coming++;
                if (coming < Wanted && taken.Count < spaces.Length) Spawn();
            }
            for (int i = cars.Count - 1; i >= 0; i--) Drive(cars[i]);
        }

        void Spawn()
        {
            int space;
            do space = Random.Range(0, spaces.Length); while (taken.Contains(space));
            taken.Add(space);
            var go = Instantiate(carPrefab, roadIn[0].position, Quaternion.LookRotation(roadIn[1].position - roadIn[0].position));
            Paint(go, Paints[Random.Range(0, Paints.Length)]);
            var car = new Car { transform = go.transform, space = space, speed = 9f };
            foreach (var point in roadIn) car.route.Add(point.position);
            var spot = spaces[space].position;
            car.route.Add(new Vector3(spot.x, spot.y, aisleZ));
            car.route.Add(spot);
            cars.Add(car);
        }

        void Drive(Car car)
        {
            if (car.parked)
            {
                if (Time.time < car.leaveAt) return;
                car.parked = false;
                car.leaving = true;
                taken.Remove(car.space);
                var spot = spaces[car.space].position;
                car.route.Clear();
                car.route.Add(new Vector3(spot.x, spot.y, aisleZ));
                foreach (var point in roadOut) car.route.Add(point.position);
            }
            if (car.route.Count == 0) return;
            var t = car.transform;
            Vector3 goal = car.route[0];
            bool last = car.route.Count == 1;
            float distance = Vector3.Distance(t.position, goal);
            // Slow down for corners and parking.
            float speed = last || distance < 6f ? Mathf.Lerp(2.5f, car.speed, Mathf.Clamp01(distance / 10f)) : car.speed;
            Vector3 step = Vector3.MoveTowards(t.position, goal, speed * Time.deltaTime) - t.position;
            if (step.sqrMagnitude > 1e-6f)
            {
                // Reversing out of a space looks better than spinning in place.
                bool reverse = car.leaving && car.route.Count == roadOut.Length + 1;
                var look = Quaternion.LookRotation(reverse ? -step : step);
                t.rotation = Quaternion.RotateTowards(t.rotation, look, 120f * Time.deltaTime);
                t.position += step;
            }
            if (distance > 0.15f) return;
            car.route.RemoveAt(0);
            if (car.route.Count > 0) return;
            if (car.leaving)
            {
                Destroy(t.gameObject);
                cars.Remove(car);
            }
            else
            {
                car.parked = true;
                t.rotation = spaces[car.space].rotation;
                car.leaveAt = Time.time + Random.Range(60f, 150f);
            }
        }

        static void Paint(GameObject car, Color color)
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            foreach (var renderer in car.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && materials[i].name.StartsWith("CarPaint")) renderer.SetPropertyBlock(block, i);
            }
        }
    }
}
