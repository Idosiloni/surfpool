using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Placeholder HUD: crosshair, interaction prompt, money, "+$" popups, surfer status labels
    /// and a controls hint. Replaced by UI Toolkit later.
    /// </summary>
    public class SimpleHUD : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] FirstPersonController player;
        [SerializeField] float labelDistance = 40f;

        GUIStyle promptStyle;
        GUIStyle hintStyle;
        GUIStyle moneyStyle;
        GUIStyle popupStyle;
        GUIStyle labelStyle;
        int popupAmount;
        float popupTime = -10f;

        void OnEnable()
        {
            if (Wallet.Instance != null) Wallet.Instance.Earned += ShowPopup;
        }

        void Start()
        {
            // Wallet may wake up after us; make sure we're subscribed exactly once.
            if (Wallet.Instance == null) return;
            Wallet.Instance.Earned -= ShowPopup;
            Wallet.Instance.Earned += ShowPopup;
        }

        void OnDisable()
        {
            if (Wallet.Instance != null) Wallet.Instance.Earned -= ShowPopup;
        }

        void ShowPopup(int amount)
        {
            popupAmount = amount;
            popupTime = Time.time;
        }

        void OnGUI()
        {
            CreateStyles();
            float cx = Screen.width / 2f, cy = Screen.height / 2f;

            DrawSurferLabels();

            GUI.Label(new Rect(cx - 10, cy - 12, 20, 24), "•", promptStyle);

            var target = interactor != null ? interactor.Target : null;
            if (target != null && !string.IsNullOrEmpty(target.Prompt))
                GUI.Label(new Rect(cx - 250, cy + 30, 500, 30), "[E]  " + target.Prompt, promptStyle);

            if (Wallet.Instance != null)
            {
                GUI.Label(new Rect(20, 16, 300, 40), "$ " + Wallet.Instance.Money, moneyStyle);
                float age = Time.time - popupTime;
                if (age < 1.5f)
                {
                    var color = popupStyle.normal.textColor;
                    popupStyle.normal.textColor = new Color(color.r, color.g, color.b, 1f - age / 1.5f);
                    GUI.Label(new Rect(20, 56 - age * 20f, 300, 30), "+$" + popupAmount, popupStyle);
                }
            }

            string hint = player != null && !player.CursorLocked
                ? "Click to play"
                : "WASD move · Shift sprint · Space jump · E use · Esc free mouse";
            GUI.Label(new Rect(16, Screen.height - 32, 600, 24), hint, hintStyle);
        }

        void DrawSurferLabels()
        {
            var cam = Camera.main;
            if (cam == null) return;
            foreach (var surfer in Surfer.All)
            {
                string status = surfer.Status;
                if (string.IsNullOrEmpty(status)) continue;
                Vector3 world = surfer.transform.position + Vector3.up * 2.2f;
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z <= 0f || screen.z > labelDistance) continue;
                GUI.Label(new Rect(screen.x - 150, Screen.height - screen.y - 15, 300, 30), status, labelStyle);
            }
        }

        void CreateStyles()
        {
            if (promptStyle != null) return;
            promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            moneyStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            moneyStyle.normal.textColor = new Color(0.3f, 1f, 0.4f);
            popupStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            popupStyle.normal.textColor = new Color(0.3f, 1f, 0.4f);
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        }
    }
}
