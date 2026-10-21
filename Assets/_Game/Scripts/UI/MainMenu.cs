using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Title screen (Assets/_Game/UI/MainMenu.uxml) shown when the game starts: Continue (your saved park, already
    /// loaded behind the menu), New game, Settings and Quit. The game is paused while it's up and the camera slowly
    /// circles the park.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour
    {
        /// <summary>Set before reloading the scene for a new game, so the menu doesn't show again.</summary>
        public static bool SkipNextTime;

        [Tooltip("The point the menu camera circles around.")]
        [SerializeField] Vector3 orbitCenter = new(30f, 0f, 10f);
        [SerializeField] float orbitRadius = 62f;
        [SerializeField] float orbitHeight = 26f;

        VisualElement root;
        Button continueButton, newButton;
        Label saveLabel;
        bool confirmNew;
        Transform cameraTransform;
        Vector3 cameraLocalPosition;
        Quaternion cameraLocalRotation;
        UIDocument hud;

        public static bool Showing { get; private set; }

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("menuRoot");
            saveLabel = doc.Q<Label>("menuSave");
            continueButton = doc.Q<Button>("menuContinue");
            newButton = doc.Q<Button>("menuNew");
            continueButton.clicked += Close;
            newButton.clicked += NewGame;
            doc.Q<Button>("menuSettings").clicked += SettingsScreen.Open;
            doc.Q<Button>("menuQuit").clicked += Quit;
        }

        void Start()
        {
            if (SkipNextTime)
            {
                SkipNextTime = false;
                root.AddToClassList("hidden");
                return;
            }
            Showing = true;
            UiMotion.Enter(root, "menu-root--enter"); // panel slides in, poster spins in
            Time.timeScale = 0f;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
            var gameHud = FindAnyObjectByType<GameHud>();
            hud = gameHud != null ? gameHud.GetComponent<UIDocument>() : null;
            if (hud != null) hud.rootVisualElement.style.display = DisplayStyle.None;
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                cameraLocalPosition = cameraTransform.localPosition;
                cameraLocalRotation = cameraTransform.localRotation;
            }

            bool hasSave = SaveSystem.HasSave;
            int day = DayCycle.Instance != null ? DayCycle.Instance.Day : 1;
            int money = Wallet.Instance != null ? Wallet.Instance.Money : 0;
            int level = ParkLevel.Instance != null ? ParkLevel.Instance.Level : 1;
            saveLabel.text = hasSave ? $"{ParkName.Name}: day {day}  ·  {MoneyText.Format(money)}  ·  level {level}" : "Uncle Kai's old wave pool is waiting for you.";
            continueButton.text = hasSave ? "Continue" : "Play";
            newButton.style.display = hasSave ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void LateUpdate()
        {
            if (!Showing || cameraTransform == null) return;
            if (UnityEngine.Cursor.lockState == CursorLockMode.Locked) FirstPersonController.LockCursor(false);
            float angle = Time.unscaledTime * 0.04f + 2.2f;
            var position = orbitCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius + Vector3.up * orbitHeight;
            cameraTransform.SetPositionAndRotation(position, Quaternion.LookRotation(orbitCenter + Vector3.down * 4f - position));
        }

        void Close()
        {
            if (!Showing) return;
            Showing = false;
            root.AddToClassList("hidden");
            SettingsScreen.OpenIfShowing(false);
            Time.timeScale = 1f;
            InputBlocker.Release();
            if (hud != null) hud.rootVisualElement.style.display = DisplayStyle.Flex;
            if (cameraTransform != null) cameraTransform.SetLocalPositionAndRotation(cameraLocalPosition, cameraLocalRotation);
            bool story = FindAnyObjectByType<StoryScreen>() is { IsOpen: true };
            FirstPersonController.LockCursor(!story);
        }

        /// <summary>First click asks to confirm (it deletes your park), second click starts over.</summary>
        void NewGame()
        {
            if (!confirmNew)
            {
                confirmNew = true;
                newButton.text = "Sure? This deletes your park!";
                newButton.AddToClassList("menu-button--danger");
                return;
            }
            SaveSystem.DeleteSave();
            GameStats.Reset();
            ParkEffects.ResetStatics();
            DailyEvents.ResetStatics();
            ParkName.Load("");
            ParkReviews.NewDay();
            MachineService.ResetStatics();
            var save = FindAnyObjectByType<SaveSystem>();
            if (save != null) save.enabled = false;
            SkipNextTime = true;
            Showing = false;
            Time.timeScale = 1f;
            InputBlocker.ResetAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
