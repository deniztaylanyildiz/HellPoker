using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// Keeps the pixel fonts sharp at every whole-number scale. Two things blurred them at odd scales (×3, ×5 — 2560×1440):
    /// a text laid out at half a game pixel (centred in a box one pixel taller or wider than its line) lands on half a screen
    /// pixel there (2.5 at ×5), and the dynamic font atlas is sampled bilinearly, so the half pixel smears every glyph. At even
    /// scales (×2, ×4) half a game pixel is a whole screen pixel, so 1920×1080 never showed it.
    /// The fix: every text's corners are snapped to whole screen pixels (<see cref="PixelSnappedText"/>), and the font atlases are
    /// sampled point-wise — set again whenever Unity rebuilds a font's texture.
    /// </summary>
    public static class PixelText
    {
        private static readonly HashSet<Font> Sharp = new HashSet<Font>();
        private static bool _listening;

        /// <summary>The font's atlas is sampled point-wise, now and after every rebuild of its texture.</summary>
        public static void KeepSharp(Font font)
        {
            if (font == null) return;
            Sharp.Add(font);
            if (!_listening)
            {
                Font.textureRebuilt += OnRebuilt;
                _listening = true;
            }
            Apply(font);
        }

        private static void OnRebuilt(Font font)
        {
            if (Sharp.Contains(font)) Apply(font);
        }

        private static void Apply(Font font)
        {
            Texture texture = font.material != null ? font.material.mainTexture : null;
            if (texture != null) texture.filterMode = FilterMode.Point;
        }

        /// <summary>
        /// A point in canvas units (<paramref name="local"/>, the canvas rect's lower left at <paramref name="min"/>) moved to the nearest
        /// whole screen pixel at <paramref name="scale"/> screen pixels per unit.
        /// </summary>
        public static Vector2 Snap(Vector2 local, Vector2 min, float scale)
        {
            if (scale <= 0f) return local;
            Vector2 pixels = (local - min) * scale;
            return new Vector2(Mathf.Round(pixels.x), Mathf.Round(pixels.y)) / scale + min;
        }
    }

    /// <summary>Snaps every vertex of a text to a whole screen pixel (see <see cref="PixelText"/>).</summary>
    [RequireComponent(typeof(Text))]
    public sealed class PixelSnappedText : BaseMeshEffect
    {
        private readonly List<UIVertex> _vertices = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            Canvas canvas = graphic.canvas != null ? graphic.canvas.rootCanvas : null;
            if (canvas == null) return;
            var root = (RectTransform)canvas.transform;
            float scale = canvas.scaleFactor;
            Vector2 min = root.rect.min;
            Transform own = transform;

            _vertices.Clear();
            vh.GetUIVertexStream(_vertices);
            for (int i = 0; i < _vertices.Count; i++)
            {
                UIVertex v = _vertices[i];
                Vector3 inRoot = root.InverseTransformPoint(own.TransformPoint(v.position));
                Vector2 snapped = PixelText.Snap(inRoot, min, scale);
                v.position = own.InverseTransformPoint(root.TransformPoint(new Vector3(snapped.x, snapped.y, inRoot.z)));
                _vertices[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(_vertices);
        }
    }
}
