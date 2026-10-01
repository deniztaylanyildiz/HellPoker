using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HellPoker.Editor
{
    /// <summary>
    /// Import settings for the generated art in Assets/Resources/Art (see Tools/ArtGen): UI sprites, uncompressed,
    /// no mipmaps, with 9-slice borders for the stretchable frames, panels and buttons.
    /// </summary>
    public sealed class HellPokerArtImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/Resources/Art/";

        /// <summary>Sprite borders (left, bottom, right, top) in pixels, by file name.</summary>
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "frame", new Vector4(44, 44, 44, 44) },
            { "panel", new Vector4(30, 30, 30, 30) },
            { "button_blood", new Vector4(34, 34, 34, 34) },
            { "button_ember", new Vector4(34, 34, 34, 34) },
            { "button_ash", new Vector4(34, 34, 34, 34) },
            { "speech", new Vector4(30, 30, 30, 30) },
        };

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(ArtFolder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            importer.spriteBorder = Borders.TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out Vector4 border) ? border : Vector4.zero;
        }
    }
}
