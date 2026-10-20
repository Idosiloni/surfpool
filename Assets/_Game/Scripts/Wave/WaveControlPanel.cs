using UnityEngine;
using UnityEngine.InputSystem;

namespace SurfPool
{
    /// <summary>
    /// The small screen on the wave console. Press E to open the Wave Control panel (drawn by
    /// <see cref="WaveControlScreen"/>) where you pick the wave type and direction. Esc or E closes it.
    /// </summary>
    public class WaveControlPanel : MonoBehaviour, IInteractable
    {
        [SerializeField] WaveMachine machine;

        int openedFrame;

        /// <summary>The console whose panel is open right now (one at a time), or null.</summary>
        public static WaveControlPanel OpenPanel { get; private set; }

        public bool IsOpen { get; private set; }
        public WaveMachine Machine => machine;

        public string Prompt => "Open wave control (pick the wave)";

        public void Interact()
        {
            if (IsOpen) return;
            IsOpen = true;
            OpenPanel = this;
            openedFrame = Time.frameCount;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (OpenPanel == this) OpenPanel = null;
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
        }

        /// <summary>Sends the selected wave and closes the panel so you can watch it.</summary>
        public bool Send()
        {
            bool sent = machine.TryTriggerWave();
            if (sent) Close();
            return sent;
        }

        void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)) Close();
        }
    }
}
