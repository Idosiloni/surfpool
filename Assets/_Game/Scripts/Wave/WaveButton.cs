using UnityEngine;

namespace SurfPool
{
    /// <summary>The big button on the wave machine console. Green = ready, red = charging.</summary>
    public class WaveButton : MonoBehaviour, IInteractable
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] WaveMachine machine;
        [SerializeField] Renderer buttonRenderer;
        [SerializeField] Color readyColor = new(0.2f, 0.85f, 0.3f);
        [SerializeField] Color chargingColor = new(0.9f, 0.2f, 0.15f);
        [SerializeField] float pressDepth = 0.04f;

        MaterialPropertyBlock block;
        Vector3 restPosition;
        float pressedUntil;

        public string Prompt => machine.Ready
            ? $"Send {machine.Selected} wave" + (machine.WavesPerSet > 1 ? $" set (x{machine.WavesPerSet})" : "") +
              $"  ({MoneyText.Format(machine.PressCost)} power)"
            : $"Wave machine charging... {Mathf.CeilToInt(machine.SecondsUntilReady)}";

        void Awake()
        {
            block = new MaterialPropertyBlock();
            restPosition = buttonRenderer.transform.localPosition;
        }

        public void Interact()
        {
            if (machine.TryTriggerWave()) pressedUntil = Time.time + 0.2f;
        }

        void Update()
        {
            block.SetColor(BaseColor, machine.Ready ? readyColor : chargingColor);
            buttonRenderer.SetPropertyBlock(block);

            float depth = Time.time < pressedUntil ? pressDepth : 0f;
            buttonRenderer.transform.localPosition = restPosition + Vector3.down * depth;
        }
    }
}
