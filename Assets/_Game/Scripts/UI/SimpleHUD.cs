using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Placeholder HUD: crosshair, interaction prompt, money with +/- popups, ticket price and customer stats,
    /// surfer status labels with patience bars, and a controls hint. Replaced by UI Toolkit later.
    /// </summary>
    public class SimpleHUD : MonoBehaviour
    {
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] FirstPersonController player;
        [SerializeField] PriceBoard pricing;
        [SerializeField] SurferSpawner spawner;
        [SerializeField] WaveMachine machine;
        [SerializeField] float labelDistance = 40f;

        static readonly Color Green = new(0.3f, 1f, 0.4f);
        static readonly Color Red = new(1f, 0.35f, 0.3f);

        GUIStyle promptStyle, hintStyle, moneyStyle, statsStyle, popupStyle, labelStyle;
        int popupAmount;
        string popupReason;
        float popupTime = -10f;
        Wallet subscribed;

        void Update()
        {
            // Wallet may wake up after us; subscribe once it exists.
            if (subscribed == null && Wallet.Instance != null)
            {
                subscribed = Wallet.Instance;
                subscribed.Changed += ShowPopup;
            }
        }

        void OnDestroy()
        {
            if (subscribed != null) subscribed.Changed -= ShowPopup;
        }

        void ShowPopup(int amount, string reason)
        {
            popupAmount = amount;
            popupReason = reason;
            popupTime = Time.time;
        }

        void OnGUI()
        {
            CreateStyles();
            float cx = Screen.width / 2f, cy = Screen.height / 2f;

            if (!InputBlocker.Blocked)
            {
                DrawSurferLabels();
                GUI.Label(new Rect(cx - 10, cy - 12, 20, 24), "•", promptStyle);
                var target = interactor != null ? interactor.Target : null;
                if (target != null && !string.IsNullOrEmpty(target.Prompt))
                    GUI.Label(new Rect(cx - 300, cy + 30, 600, 30), "[E]  " + target.Prompt, promptStyle);
            }

            if (Wallet.Instance != null)
            {
                GUI.Label(new Rect(20, 16, 300, 40), "$ " + Wallet.Instance.Money, moneyStyle);
                float age = Time.time - popupTime;
                if (age < 1.5f)
                {
                    var color = popupAmount >= 0 ? Green : Red;
                    popupStyle.normal.textColor = new Color(color.r, color.g, color.b, 1f - age / 1.5f);
                    string sign = popupAmount >= 0 ? "+" : "-";
                    string text = $"{sign}${Mathf.Abs(popupAmount)}" + (string.IsNullOrEmpty(popupReason) ? "" : "  " + popupReason);
                    GUI.Label(new Rect(20, 56 - age * 20f, 400, 30), text, popupStyle);
                }
            }

            string stats = "";
            if (machine != null) stats += $"Wave: {machine.Selected}     ";
            if (pricing != null) stats += $"Ticket price: ${pricing.Price}";
            if (pricing != null && spawner != null && spawner.RenterChance > 0f) stats += $"     Board rental: ${pricing.RentalPrice}";
            if (spawner != null) stats += $"     Happy surfers: {spawner.Served}     Lost customers: {spawner.Lost}";
            GUI.Label(new Rect(20, 84, 700, 26), stats, statsStyle);

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
                Vector3 screen = cam.WorldToScreenPoint(surfer.transform.position + Vector3.up * 2.2f);
                if (screen.z <= 0f || screen.z > labelDistance) continue;
                float x = screen.x, y = Screen.height - screen.y;

                string status = surfer.Status;
                if (!string.IsNullOrEmpty(status))
                    GUI.Label(new Rect(x - 200, y - 30, 400, 30), status, labelStyle);

                if (surfer.ShowPatience)
                {
                    const float width = 60f, height = 7f;
                    var back = new Rect(x - width / 2, y, width, height);
                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.DrawTexture(back, Texture2D.whiteTexture);
                    GUI.color = Color.Lerp(Red, Green, surfer.Patience);
                    GUI.DrawTexture(new Rect(back.x + 1, back.y + 1, (width - 2) * Mathf.Clamp01(surfer.Patience), height - 2), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }

        void CreateStyles()
        {
            if (promptStyle != null) return;
            promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            moneyStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            moneyStyle.normal.textColor = Green;
            statsStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            popupStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
        }
    }
}
