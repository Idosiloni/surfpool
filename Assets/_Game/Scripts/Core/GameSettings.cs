using UnityEngine;

namespace SurfPool
{
    /// <summary>Player settings kept between games (PlayerPrefs): mouse sensitivity and invert, graphics quality, fullscreen.</summary>
    public static class GameSettings
    {
        public static float MouseSensitivity { get; private set; } = 1f;
        public static bool InvertY { get; private set; }

        /// <summary>0 = Fast, 1 = Balanced, 2 = Pretty (mapped onto the project's quality levels).</summary>
        public static int Quality { get; private set; } = 2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            MouseSensitivity = PlayerPrefs.GetFloat("mouse_sensitivity", 1f);
            InvertY = PlayerPrefs.GetInt("invert_y", 0) == 1;
            Quality = PlayerPrefs.GetInt("quality_tier", 2);
            ApplyQuality();
        }

        public static void SetMouse(float sensitivity, bool invert)
        {
            MouseSensitivity = Mathf.Clamp(sensitivity, 0.25f, 3f);
            InvertY = invert;
            PlayerPrefs.SetFloat("mouse_sensitivity", MouseSensitivity);
            PlayerPrefs.SetInt("invert_y", invert ? 1 : 0);
        }

        public static void SetQuality(int tier)
        {
            Quality = Mathf.Clamp(tier, 0, 2);
            PlayerPrefs.SetInt("quality_tier", Quality);
            ApplyQuality();
        }

        public static void SetFullscreen(bool fullscreen) => Screen.fullScreen = fullscreen;

        static void ApplyQuality()
        {
            int levels = QualitySettings.names.Length;
            if (levels == 0) return;
            int index = Mathf.RoundToInt(Quality / 2f * (levels - 1));
            QualitySettings.SetQualityLevel(index, true);
        }
    }
}
