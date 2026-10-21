using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>Settings window (Assets/_Game/UI/Settings.uxml), opened from the main menu or the pause menu.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsScreen : MonoBehaviour
    {
        static SettingsScreen instance;

        VisualElement root;
        Slider sensitivity, music, effects, ambience;
        Toggle invert, fullscreen;
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

            sensitivity.RegisterValueChangedCallback(_ => GameSettings.SetMouse(sensitivity.value, invert.value));
            invert.RegisterValueChangedCallback(_ => GameSettings.SetMouse(sensitivity.value, invert.value));
            EventCallback<ChangeEvent<float>> volume = _ => Sfx.SetVolumes(music.value, effects.value, ambience.value);
            music.RegisterValueChangedCallback(volume);
            effects.RegisterValueChangedCallback(volume);
            ambience.RegisterValueChangedCallback(volume);
            fullscreen.RegisterValueChangedCallback(e => GameSettings.SetFullscreen(e.newValue));
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
