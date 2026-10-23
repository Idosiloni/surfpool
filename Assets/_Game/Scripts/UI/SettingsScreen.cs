using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Settings window (Assets/_Game/UI/Settings.uxml), opened from the main menu or the pause menu: controls, sound,
    /// graphics, accessibility and a list of the keys. Its document is the top one, so it also holds the wave wipe.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsScreen : MonoBehaviour
    {
        static SettingsScreen instance;

        VisualElement root;
        Slider sensitivity, music, effects, ambience, uiScale, shake;
        Toggle invert, fullscreen, reduceMotion, marks, cleanHud;
        Button[] quality;

        public static bool IsOpen => instance != null && UiMotion.IsShown(instance.root);

        void OnEnable()
        {
            instance = this;
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("settingsRoot");
            sensitivity = doc.Q<Slider>("setSensitivity");
            invert = doc.Q<Toggle>("setInvert");
            music = doc.Q<Slider>("setMusic");
            effects = doc.Q<Slider>("setEffects");
            ambience = doc.Q<Slider>("setAmbience");
            fullscreen = doc.Q<Toggle>("setFullscreen");
            quality = new[] { doc.Q<Button>("qualityLow"), doc.Q<Button>("qualityMid"), doc.Q<Button>("qualityHigh") };
            uiScale = doc.Q<Slider>("setUiScale");
            shake = doc.Q<Slider>("setShake");
            reduceMotion = doc.Q<Toggle>("setReduceMotion");
            marks = doc.Q<Toggle>("setMarks");
            cleanHud = doc.Q<Toggle>("setCleanHud");
            UiWipe.Register(doc.Q("wipe"));
            BuildKeys(doc.Q("keysList"));

            sensitivity.RegisterValueChangedCallback(_ => GameSettings.SetMouse(sensitivity.value, invert.value));
            invert.RegisterValueChangedCallback(_ => GameSettings.SetMouse(sensitivity.value, invert.value));
            EventCallback<ChangeEvent<float>> volume = _ => Sfx.SetVolumes(music.value, effects.value, ambience.value);
            music.RegisterValueChangedCallback(volume);
            effects.RegisterValueChangedCallback(volume);
            ambience.RegisterValueChangedCallback(volume);
            fullscreen.RegisterValueChangedCallback(e => GameSettings.SetFullscreen(e.newValue));
            // UI size applies when you let go of the slider, so the window doesn't jump around under the mouse.
            uiScale.RegisterCallback<PointerCaptureOutEvent>(_ => GameSettings.SetUiScale(uiScale.value));
            uiScale.RegisterCallback<NavigationSubmitEvent>(_ => GameSettings.SetUiScale(uiScale.value));
            shake.RegisterValueChangedCallback(e => GameSettings.SetShake(e.newValue));
            reduceMotion.RegisterValueChangedCallback(e => GameSettings.SetReduceMotion(e.newValue));
            marks.RegisterValueChangedCallback(e => GameSettings.SetColorblindMarks(e.newValue));
            cleanHud.RegisterValueChangedCallback(e => GameSettings.SetCleanHud(e.newValue));
            GameSettings.Changed -= UiAccess.Apply;
            GameSettings.Changed += UiAccess.Apply;
            for (int i = 0; i < quality.Length; i++)
            {
                int tier = i;
                quality[i].clicked += () =>
                {
                    GameSettings.SetQuality(tier);
                    ShowQuality();
                };
            }
            doc.Q<Button>("settingsClose").clicked += Close;
        }

        void Start() => UiAccess.Apply();

        void OnDestroy() => GameSettings.Changed -= UiAccess.Apply;

        static readonly (string key, string what)[] Keys =
        {
            ("W A S D", "Walk"),
            ("Shift", "Sprint"),
            ("Space", "Jump"),
            ("E", "Use (hold for jobs)"),
            ("J", "Journal"),
            ("B", "Build (R turn, X remove)"),
            ("Alt", "Hold: the full HUD"),
            ("H", "Hide / show the HUD"),
            ("Tab", "Pause menu"),
            ("Esc", "Free the mouse"),
            ("Surfing", ""),
            ("W", "Paddle, pump"),
            ("S", "Pump, hold to stall"),
            ("Space", "Stand up, airs"),
            ("A D", "Turn the board, spin"),
            ("Shift", "Grab, tail slide"),
            ("F", "Send a wave"),
            ("V", "Camera"),
            ("R", "Replay"),
            ("Q", "Get out"),
        };

        /// <summary>The key list: a key cap and what it does (a row with no action is a heading).</summary>
        static void BuildKeys(VisualElement list)
        {
            if (list == null) return;
            list.Clear();
            foreach (var (key, what) in Keys)
            {
                if (what.Length == 0)
                {
                    var heading = new Label(key.ToUpperInvariant());
                    heading.AddToClassList("keys-heading");
                    list.Add(heading);
                    continue;
                }
                var row = new VisualElement();
                row.AddToClassList("keys-row");
                var cap = new Label(key);
                cap.AddToClassList("key-badge");
                cap.AddToClassList("keys-cap");
                var text = new Label(what);
                text.AddToClassList("keys-text");
                row.Add(cap);
                row.Add(text);
                list.Add(row);
            }
        }

        public static void Open()
        {
            if (instance == null) return;
            instance.Refresh();
            UiMotion.Show(instance.root);
        }

        void Refresh()
        {
            sensitivity.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            invert.SetValueWithoutNotify(GameSettings.InvertY);
            music.SetValueWithoutNotify(Sfx.MusicVolume);
            effects.SetValueWithoutNotify(Sfx.EffectsVolume);
            ambience.SetValueWithoutNotify(Sfx.AmbienceVolume);
            fullscreen.SetValueWithoutNotify(Screen.fullScreen);
            uiScale.SetValueWithoutNotify(GameSettings.UiScale);
            shake.SetValueWithoutNotify(GameSettings.Shake);
            reduceMotion.SetValueWithoutNotify(GameSettings.ReduceMotion);
            marks.SetValueWithoutNotify(GameSettings.ColorblindMarks);
            cleanHud.SetValueWithoutNotify(GameSettings.CleanHud);
            ShowQuality();
        }

        void ShowQuality()
        {
            for (int i = 0; i < quality.Length; i++)
                quality[i].EnableInClassList("quality-button--active", i == GameSettings.Quality);
        }

        public void Close() => UiMotion.Hide(root);

        /// <summary>Closes the window if it's open (used when the menu behind it goes away).</summary>
        public static void OpenIfShowing(bool open)
        {
            if (instance != null && !open) instance.Close();
        }
    }
}
