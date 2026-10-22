using UnityEngine;

namespace SurfPool
{
    /// <summary>Player settings kept between games (PlayerPrefs): mouse sensitivity and invert, graphics quality, fullscreen,
    /// surf camera, and accessibility (UI size, camera shake, reduce motion, colorblind marks, hidden HUD).</summary>
    public static class GameSettings
    {
        public static float MouseSensitivity { get; private set; } = 1f;
        public static bool InvertY { get; private set; }

        /// <summary>0 = Fast, 1 = Balanced, 2 = Pretty (mapped onto the project's quality levels).</summary>
        public static int Quality { get; private set; } = 2;

        /// <summary>Surfing camera: true = chase cam behind your surfer (see your moves), false = first person.</summary>
        public static bool ChaseCam { get; private set; } = true;

        /// <summary>Camera shake strength (0 = off, 1 = full): big waves crashing, wipeouts.</summary>
        public static float Shake { get; private set; } = 1f;

        /// <summary>Size of all the UI (0.8 to 1.3; bigger and the 780 px windows no longer fit a 1080p screen).</summary>
        public static float UiScale { get; private set; } = 1f;

        /// <summary>Windows, toasts and banners appear without sliding or popping; the wave wipe becomes a fade.</summary>
        public static bool ReduceMotion { get; private set; }

        /// <summary>Shapes as well as colors: faces on patience bars (smile / meh / frown).</summary>
        public static bool ColorblindMarks { get; private set; }

        /// <summary>H hides the HUD panels for a clean view (crosshair, prompts and toasts stay).</summary>
        public static bool CleanHud { get; private set; }

        /// <summary>Fires after any accessibility setting changes (the UI re-applies them).</summary>
        public static event System.Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Load()
        {
            MouseSensitivity = PlayerPrefs.GetFloat("mouse_sensitivity", 1f);
            InvertY = PlayerPrefs.GetInt("invert_y", 0) == 1;
            Quality = PlayerPrefs.GetInt("quality_tier", 2);
            ChaseCam = PlayerPrefs.GetInt("surf_chase_cam", 1) == 1;
            Shake = PlayerPrefs.GetFloat("camera_shake", 1f);
            UiScale = PlayerPrefs.GetFloat("ui_scale", 1f);
            ReduceMotion = PlayerPrefs.GetInt("reduce_motion", 0) == 1;
            ColorblindMarks = PlayerPrefs.GetInt("colorblind_marks", 0) == 1;
            CleanHud = PlayerPrefs.GetInt("clean_hud", 0) == 1;
            ApplyQuality();
        }

        public static void SetChaseCam(bool chase)
        {
            ChaseCam = chase;
            PlayerPrefs.SetInt("surf_chase_cam", chase ? 1 : 0);
        }

        public static void SetShake(float amount)
        {
            Shake = Mathf.Clamp01(amount);
            PlayerPrefs.SetFloat("camera_shake", Shake);
        }

        public static void SetUiScale(float scale)
        {
            UiScale = Mathf.Clamp(scale, 0.8f, 1.3f);
            PlayerPrefs.SetFloat("ui_scale", UiScale);
            Changed?.Invoke();
        }

        public static void SetReduceMotion(bool on)
        {
            ReduceMotion = on;
            PlayerPrefs.SetInt("reduce_motion", on ? 1 : 0);
            Changed?.Invoke();
        }

        public static void SetColorblindMarks(bool on)
        {
            ColorblindMarks = on;
            PlayerPrefs.SetInt("colorblind_marks", on ? 1 : 0);
            Changed?.Invoke();
        }

        public static void SetCleanHud(bool on)
        {
            CleanHud = on;
            PlayerPrefs.SetInt("clean_hud", on ? 1 : 0);
            Changed?.Invoke();
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
