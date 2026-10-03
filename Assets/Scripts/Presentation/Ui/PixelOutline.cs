using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Presentation.Ui
{
    /// <summary>
    /// A hard 1 px outline all the way round a pixel font, in the eight directions (Unity's own Outline only offsets
    /// diagonally, which leaves the ends of straight strokes open). Keeps any text readable over a moving backdrop.
    /// </summary>
    [AddComponentMenu("UI/Effects/Pixel Outline")]
    public sealed class PixelOutline : Shadow
    {
        private static readonly Vector2[] Directions =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1)
        };

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;

            var verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);
            int count = verts.Count;
            var output = new List<UIVertex>(count * (Directions.Length + 1));
            Vector2 distance = effectDistance;
            foreach (Vector2 direction in Directions)
            {
                int start = output.Count;
                output.AddRange(verts);
                ApplyShadowZeroAlloc(output, effectColor, start, output.Count, direction.x * Mathf.Abs(distance.x),
                    direction.y * Mathf.Abs(distance.y));
            }
            // The text itself on top of its outline.
            output.AddRange(verts);
            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
        }
    }

    internal static class PixelOutlineExtensions
    {
        /// <summary>A hard 1 px outline all round, in palette black: readable over anything.</summary>
        public static T WithOutline<T>(this T graphic) where T : Graphic
        {
            Shadow existing = graphic.GetComponent<Shadow>();
            if (existing != null && !(existing is PixelOutline))
                Object.DestroyImmediate(existing);
            var outline = graphic.GetComponent<PixelOutline>();
            if (outline == null) outline = graphic.gameObject.AddComponent<PixelOutline>();
            outline.effectColor = Palette.Black;
            outline.effectDistance = new Vector2(1f, 1f);
            outline.useGraphicAlpha = true;
            return graphic;
        }
    }
}
