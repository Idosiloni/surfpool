using UnityEditor;

namespace SurfPool.EditorTools
{
    /// <summary>UI icons (made by tools/ui/make_icons.py) import as crisp textures without mipmaps (not sprites: sprite packing on Play makes UI Toolkit live-reload).</summary>
    public class IconImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/_Game/UI/Icons/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
