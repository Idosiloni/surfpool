using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Applies the accessibility settings to every screen: UI size (the shared PanelSettings' scale), and root classes
    /// that Game.uss reacts to (.reduce-motion turns transitions off, .marks shows shapes next to colors).
    /// </summary>
    public static class UiAccess
    {
        static PanelSettings panel;

        public static void Apply()
        {
            foreach (var doc in Object.FindObjectsByType<UIDocument>())
            {
                if (doc.panelSettings != null && panel == null)
                {
                    panel = doc.panelSettings;
                    // The PanelSettings is an asset: in the Editor, put its scale back when Play mode ends.
                    Application.quitting -= RestoreScale;
                    Application.quitting += RestoreScale;
                }
                var root = doc.rootVisualElement;
                if (root == null) continue;
                root.EnableInClassList("reduce-motion", GameSettings.ReduceMotion);
                root.EnableInClassList("marks", GameSettings.ColorblindMarks);
            }
            if (panel != null) panel.scale = GameSettings.UiScale;
        }

        static void RestoreScale()
        {
            if (panel != null) panel.scale = 1f;
        }
    }
}
