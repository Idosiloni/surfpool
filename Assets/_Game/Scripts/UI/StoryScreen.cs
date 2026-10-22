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

        // The ending (MissionManager "kai_home"): Kai is back and has ridden his wave. {0} = the park's name.
        const string Ending =
            "Kiddo,\n\n" +
            "Thirty years ago I dug a hole in the sand, poured some concrete and dreamed about waves. Today I rode the " +
            "longest wave of my life in it, and the whole park cheered.\n\n" +
            "Look at {0}! Legends in the lineup, a wave that runs forever, and that rival down the coast copying " +
            "everything you do. You didn't just fix up my old pool. You built the best surf park in the world.\n\n" +
            "I'm staying around for a while. Somebody has to teach the groms to duck dive. But the park is yours. " +
            "It always was.\n\n" +
            "P.S. Splash Valley called. They want to know your secret. I told them: stay humble, stay stoked.";

        public static StoryScreen Instance { get; private set; }

        Label heading, body, signature;
        Button button;
        bool ending;
        VisualElement root;
        bool open;

        public bool IsOpen => open;

        void OnEnable()
        {
            Instance = this;
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("storyRoot");
            heading = doc.Q<Label>("storyHeading");
            body = doc.Q<Label>("storyBody");
            signature = doc.Q<Label>("storySignature");
            button = doc.Q<Button>("storyButton");
            body.text = Letter;
            button.clicked += Close;
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Kai's last letter: the story's ending, after he has ridden his wave. Closing it just goes back to the park.</summary>
        public void ShowEnding()
        {
            if (open || root == null) return;
            ending = true;
            heading.text = "Welcome home, Uncle Kai";
            body.text = string.Format(Ending, ParkName.Name);
            signature.text = "Aloha, Uncle Kai";
            button.text = "Keep surfing!";
            open = true;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
            Sfx.Play(Sound.UiOpen);
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
            if (ending)
            {
                ending = false;
                return;
            }
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
