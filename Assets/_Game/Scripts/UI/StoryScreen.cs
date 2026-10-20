using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The opening letter from Uncle Kai (UI Toolkit, Assets/_Game/UI/Story.uxml). Shown at the start of a new game;
    /// closing it starts the missions.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StoryScreen : MonoBehaviour
    {
        [TextArea(6, 14)]
        [SerializeField] string letter =
            "Hey kiddo,\n\n" +
            "If you're reading this, the wave pool is yours now. I built it with my own hands thirty years ago. " +
            "It's a bit run-down these days, the paint is peeling and the machine only makes little whitewater waves... " +
            "but the magic is still there.\n\n" +
            "Start small: sell sessions at the ticket desk and send waves for the beginners. Save every dollar, " +
            "fix the place up, and one day this could be the best surf park in the world.\n\n" +
            "I left you some notes along the way. Follow them.";

        VisualElement root;
        bool open;

        public bool IsOpen => open;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("storyRoot");
            doc.Q<Label>("storyBody").text = letter;
            doc.Q<Button>("storyButton").clicked += Close;
        }

        void Start()
        {
            bool alreadyStarted = MissionManager.Instance != null && MissionManager.Instance.Started;
            if (alreadyStarted)
            {
                root.AddToClassList("hidden");
                return;
            }
            open = true;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
        }

        void Update()
        {
            // The player controller locks the cursor on start; keep it free while the letter is open.
            if (open && UnityEngine.Cursor.lockState == CursorLockMode.Locked) FirstPersonController.LockCursor(false);
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            root.AddToClassList("hidden");
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
            if (MissionManager.Instance != null) MissionManager.Instance.Begin();
        }
    }
}
