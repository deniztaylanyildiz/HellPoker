using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HellPoker.Editor
{
    /// <summary>
    /// Pixel-art import settings for the generated art (see Tools/ArtGen): point filtering, no compression, no mipmaps,
    /// a fixed pixels-per-unit and 9-slice borders for the boxes and buttons. Fonts render without anti-aliasing.
    /// </summary>
    public sealed class HellPokerArtImporter : AssetPostprocessor
    {
        public const int PixelsPerUnit = 100;
        private const string ArtFolder = "Assets/Resources/Art/";
        private const string FontFolder = "Assets/Resources/Fonts/";

        /// <summary>Sprite borders (left, bottom, right, top) in pixels, by file name.</summary>
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "panel", new Vector4(4, 4, 4, 4) },
            { "panel_hot", new Vector4(4, 4, 4, 4) },
            { "dialog", new Vector4(4, 4, 4, 4) },
            { "dialog_lucifer", new Vector4(4, 4, 4, 4) },
            { "button_blood", new Vector4(4, 4, 4, 4) },
            { "button_ember", new Vector4(4, 4, 4, 4) },
            { "button_ash", new Vector4(4, 4, 4, 4) },
        };

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(ArtFolder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            importer.spriteBorder = Borders.TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out Vector4 border) ? border : Vector4.zero;
        }

        private void OnPreprocessAsset()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(FontFolder) || !(assetImporter is TrueTypeFontImporter font)) return;

            // The pixel fonts are drawn on an 8 px grid; raster them without smoothing so every glyph pixel stays a hard square.
            font.fontRenderingMode = FontRenderingMode.HintedRaster;
            font.fontTextureCase = FontTextureCase.Dynamic;
            font.includeFontData = true;
        }
    }
}
