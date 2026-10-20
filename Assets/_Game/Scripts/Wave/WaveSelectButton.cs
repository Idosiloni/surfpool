using UnityEngine;

namespace SurfPool
{
    /// <summary>One of the wave-type buttons on the wave console. Lights up when selected, dark when locked.</summary>
    public class WaveSelectButton : MonoBehaviour, IInteractable
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] WaveMachine machine;
        [SerializeField] WaveLevel level;
        [SerializeField] Renderer buttonRenderer;
        [SerializeField] Color color = Color.cyan;
        [SerializeField] float selectedLift = 0.03f;

        MaterialPropertyBlock block;
        Vector3 restPosition;

        public string Prompt
        {
            get
            {
                if (!machine.IsUnlocked(level)) return $"{level} waves (locked - buy it on the upgrade computer)";
                return machine.Selected == level ? $"{level} waves (selected)" : $"Select {level} waves";
            }
        }

        void Awake()
        {
            block = new MaterialPropertyBlock();
            restPosition = buttonRenderer.transform.localPosition;
        }

        public void Interact() => machine.Select(level);

        void Update()
        {
            bool selected = machine.Selected == level;
            Color shown = !machine.IsUnlocked(level) ? Color.Lerp(color, Color.black, 0.8f)
                : selected ? color : Color.Lerp(color, Color.gray, 0.6f);
            block.SetColor(BaseColor, shown);
            buttonRenderer.SetPropertyBlock(block);
            buttonRenderer.transform.localPosition = restPosition + Vector3.up * (selected ? selectedLift : 0f);
        }
    }
}
