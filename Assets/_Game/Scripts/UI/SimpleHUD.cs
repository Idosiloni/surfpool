using UnityEngine;

namespace SurfPool
{
    /// <summary>Placeholder HUD: crosshair, interaction prompt and controls hint. Replaced by UI Toolkit later.</summary>
    public class SimpleHUD : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] FirstPersonController player;

        GUIStyle promptStyle;
        GUIStyle hintStyle;

        void OnGUI()
        {
            promptStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            hintStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 14 };

            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            GUI.Label(new Rect(cx - 10, cy - 12, 20, 24), "•", promptStyle);

            var target = interactor != null ? interactor.Target : null;
            if (target != null && !string.IsNullOrEmpty(target.Prompt))
                GUI.Label(new Rect(cx - 200, cy + 30, 400, 30), "[E]  " + target.Prompt, promptStyle);

            string hint = player != null && !player.CursorLocked
                ? "Click to play"
                : "WASD move · Shift sprint · Space jump · E use · Esc free mouse";
            GUI.Label(new Rect(16, Screen.height - 32, 600, 24), hint, hintStyle);
        }
    }
}
