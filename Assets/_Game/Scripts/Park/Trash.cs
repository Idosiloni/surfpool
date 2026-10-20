using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// A piece of litter on the deck (a can, a cup or a wrapper) dropped by surfers. Press E to pick it up;
    /// the hired Janitor collects it too. Every piece lying around makes surfers a little less happy.
    /// </summary>
    public class Trash : MonoBehaviour, IInteractable
    {
        public const int MaxPieces = 30;
        public static readonly List<Trash> All = new();

        /// <summary>How much the trash lying around lowers surfers' happiness (0 = spotless, up to 0.3 at 20+ pieces).</summary>
        public static float MoodPenalty => Mathf.Clamp01(All.Count / 20f) * 0.3f;

        static readonly Color[] Colors =
        {
            new(0.9f, 0.2f, 0.2f), new(0.2f, 0.55f, 0.9f), new(0.95f, 0.8f, 0.2f), new(0.3f, 0.75f, 0.35f), Color.white,
        };

        public string Prompt => "Pick up trash";

        public void Interact() => PickUp();

        public void PickUp()
        {
            Sfx.Play(Sound.Click);
            Destroy(gameObject);
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Makes a random piece of litter at a spot on the deck.</summary>
        public static Trash Spawn(Vector3 position)
        {
            int kind = Random.Range(0, 3);
            var go = GameObject.CreatePrimitive(kind == 2 ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            go.name = kind switch { 0 => "Trash (can)", 1 => "Trash (cup)", _ => "Trash (wrapper)" };
            go.transform.position = position + Vector3.up * (kind == 2 ? 0.02f : 0.06f);
            go.transform.rotation = Quaternion.Euler(kind == 2 ? 0f : 90f, Random.Range(0f, 360f), 0f);
            go.transform.localScale = kind switch
            {
                0 => new Vector3(0.07f, 0.06f, 0.07f),
                1 => new Vector3(0.09f, 0.06f, 0.09f),
                _ => new Vector3(0.22f, 0.02f, 0.14f),
            };
            // A bigger invisible collider so it's easy to aim at.
            Object.Destroy(go.GetComponent<Collider>());
            var hit = new GameObject("Pick Area").AddComponent<BoxCollider>();
            hit.transform.SetParent(go.transform, false);
            hit.transform.localScale = new Vector3(1f / go.transform.localScale.x * 0.35f, 1f / go.transform.localScale.y * 0.2f, 1f / go.transform.localScale.z * 0.35f);
            var renderer = go.GetComponent<MeshRenderer>();
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Colors[Random.Range(0, Colors.Length)]);
            renderer.SetPropertyBlock(block);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.AddComponent<Trash>();
        }

        /// <summary>True if a spot is on dry deck: at deck height and not over any open pool.</summary>
        public static bool OnDeck(Vector3 position)
        {
            if (position.y < -0.15f || position.y > 0.4f) return false;
            foreach (var pool in PoolRegistry.All)
                if (pool.IsOpen && pool.Footprint(0.6f).Contains(new Vector2(position.x, position.z))) return false;
            return true;
        }

        /// <summary>Called while a surfer walks on the deck: now and then they drop something.</summary>
        public static void MaybeDrop(Vector3 position, float deltaTime)
        {
            if (All.Count >= MaxPieces) return;
            float perSecond = 0.008f * ParkEffects.LitterMultiplier * DailyEvents.LitterMultiplier;
            if (Random.value > perSecond * deltaTime || !OnDeck(position)) return;
            Spawn(position + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f)));
        }
    }
}
