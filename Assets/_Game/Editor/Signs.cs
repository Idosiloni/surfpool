using TMPro;
using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>3D text signs (TextMesh Pro with the Fredoka font) on kiosks, machine houses, the pier and the roads.</summary>
    public static class Signs
    {
        const string FontPath = "Assets/_Game/UI/Fonts/Fredoka TMP.asset";

        static TMP_FontAsset font;

        /// <summary>The Fredoka font as a TextMesh Pro asset (made once, then reused).</summary>
        public static TMP_FontAsset Font()
        {
            if (font != null) return font;
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font != null) return font;
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Game/UI/Fonts/Fredoka.ttf");
            font = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            font.name = "Fredoka TMP";
            AssetDatabase.CreateAsset(font, FontPath);
            foreach (var texture in font.atlasTextures)
            {
                texture.name = "Fredoka TMP Atlas";
                AssetDatabase.AddObjectToAsset(texture, font);
            }
            font.material.name = "Fredoka TMP Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            AssetDatabase.SaveAssets();
            return font;
        }

        /// <summary>
        /// A line of bold text. It reads from the side its forward points away from: a sign with yaw 0 is read by
        /// someone standing south of it (at -Z) looking north.
        /// </summary>
        public static TextMeshPro Text(string text, Transform parent, Vector3 position, float yaw, float height, Color color, float width = 0f)
        {
            var go = new GameObject($"Sign Text ({text})");
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = Font();
            tmp.text = text;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = width > 0f;
            tmp.fontSize = height * 10f;
            tmp.fontSizeMax = height * 10f;
            tmp.fontSizeMin = 1f;
            tmp.rectTransform.sizeDelta = new Vector2(width > 0f ? width : 20f, height * 1.4f);
            tmp.characterSpacing = 4f;
            return tmp;
        }

        /// <summary>Text on a kiosk's navy sign board (the board is above the open customer side).</summary>
        public static void Kiosk(string text, Transform parent, Vector3 kioskPosition, float kioskYaw)
        {
            var rotation = Quaternion.Euler(0f, kioskYaw, 0f);
            Text(text, parent, kioskPosition + rotation * new Vector3(0f, 3.35f, -1.4f), kioskYaw, 0.3f, Color.white, 1.85f);
        }
    }
}
