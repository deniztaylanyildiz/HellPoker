using UnityEditor;
using UnityEngine;

namespace HellPoker.Editor
{
    /// <summary>
    /// Import settings for the game icon (Tools/make_icon.py → Assets/Art/Icon): a plain texture, hard pixels, uncompressed,
    /// no mipmaps, transparent corners, never scaled down below its own size.
    /// </summary>
    public sealed class HellPokerIconImporter : AssetPostprocessor
    {
        public const string IconFolder = "Assets/Art/Icon/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(IconFolder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;   // the largest icon is 1024
        }
    }
}
