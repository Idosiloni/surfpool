using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// The opening letter from Uncle Kai (UI Toolkit, Assets/_Game/UI/Story.uxml). Shown at the start of a new game;
    /// closing it opens the naming card (<see cref="ParkNamer"/>), then the missions start.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StoryScreen : MonoBehaviour
    {
        // Kai is retired and travelling (docs/research/story-and-tutorial.md, option A): he sends postcards as the
        // park grows (TutorialTips) and comes home at the end. The text lives here only (not saved in the scene).
        const string Letter =
            "Hey kiddo,\n\n" +
            "I'm finally retiring. By the time you read this I'll be on a slow boat around the islands, chasing all the " +
            "waves I never had time for. So the wave pool is yours now. I built it with my own hands thirty years ago. " +
            "It's a bit run-down these days, the paint is peeling and the machine only makes little whitewater waves... " +
            "but the magic is still there.\n\n" +
            "Start small: sell sessions at the ticket desk and send waves for the beginners. Save every dollar, " +
            "fix the place up, and one day this could be the best surf park in the world.\n\n" +
            "I left you some notes along the way, and I'll send you postcards. Follow them.\n\n" +
            "P.S. Over the years I lost 12 of my treasures around the island. Look for the glowing lights on the sand!";

        VisualElement root;
        bool open;

        public bool IsOpen => open;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("storyRoot");
            doc.Q<Label>("storyBody").text = Letter;
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
            // Stay hidden once read (UI live-reload in the Editor rebuilds the screen visible).
            // Also wait behind the title screen until the player presses Play.
            if (root == null) return;
            bool visible = open && !MainMenu.Showing;
            if (visible) UiMotion.Show(root);
            else UiMotion.Hide(root);
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            UiMotion.Hide(root);
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
            // Next: name the park, then the missions start.
            if (ParkNamer.Instance != null) ParkNamer.Instance.Open(true, BeginMissions);
            else BeginMissions();
        }

        static void BeginMissions()
        {
            if (MissionManager.Instance != null) MissionManager.Instance.Begin();
        }
    }
}
