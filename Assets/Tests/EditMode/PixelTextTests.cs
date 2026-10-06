using System.Collections.Generic;
using System.Reflection;
using HellPoker.Presentation.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The pixel fonts stay sharp at every whole-number scale (×2..×6): every corner of a text lands on a whole screen pixel —
    /// also a text laid out at half a game pixel, which at odd scales (×3, ×5) would sit on half a screen pixel — and the font
    /// atlases are sampled point-wise, also after Unity rebuilds them.
    /// </summary>
    public class PixelTextTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made) Object.DestroyImmediate(go);
            _made.Clear();
        }

        [Test]
        public void Snap_PutsAPointOnAWholeScreenPixel_AtEveryScale()
        {
            var random = new System.Random(3);
            for (int scale = 2; scale <= 6; scale++)
            {
                for (int n = 0; n < 200; n++)
                {
                    var local = new Vector2((float)(random.NextDouble() * 600 - 300), (float)(random.NextDouble() * 400 - 200));
                    var min = new Vector2(-256f, -144f);
                    Vector2 snapped = PixelText.Snap(local, min, scale);
                    Vector2 pixels = (snapped - min) * scale;
                    Assert.AreEqual(Mathf.Round(pixels.x), pixels.x, 1e-3f);
                    Assert.AreEqual(Mathf.Round(pixels.y), pixels.y, 1e-3f);
                    Assert.LessOrEqual(Mathf.Abs(snapped.x - local.x) * scale, 0.5001f, "Never more than half a screen pixel away.");
                }
            }
        }

        [Test]
        public void AText_AtHalfAGamePixel_HasEveryCornerOnAWholeScreenPixel_AtScales2To6()
        {
            for (int scale = 2; scale <= 6; scale++)
            {
                var root = new GameObject("Canvas", typeof(RectTransform));
                _made.Add(root);
                var canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.scaleFactor = scale;
                var screen = UiFactory.CreateRect("Screen", root.transform);
                screen.sizeDelta = new Vector2(480, 270);
                Text text = UiFactory.CreateText("Odd", screen, "Your card 3 of 5", 8, Color.white);
                // A box one game pixel taller than its line: centred, the line sits half a game pixel down.
                text.rectTransform.PlaceTL(112, 91, 255, 9);
                Assert.IsNotNull(text.GetComponent<PixelSnappedText>(), "Every text the factory makes is snapped.");

                var vh = new VertexHelper();
                typeof(Text).GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(VertexHelper) }, null)
                    .Invoke(text, new object[] { vh });
                Assume.That(vh.currentVertCount, Is.GreaterThan(0), "The font gave no glyphs here.");
                text.GetComponent<PixelSnappedText>().ModifyMesh(vh);

                var rootRect = (RectTransform)root.transform;
                var stream = new List<UIVertex>();
                vh.GetUIVertexStream(stream);
                foreach (UIVertex v in stream)
                {
                    Vector3 inRoot = rootRect.InverseTransformPoint(text.transform.TransformPoint(v.position));
                    Vector2 pixels = ((Vector2)inRoot - rootRect.rect.min) * scale;
                    Assert.AreEqual(Mathf.Round(pixels.x), pixels.x, 1e-2f, $"x at ×{scale}");
                    Assert.AreEqual(Mathf.Round(pixels.y), pixels.y, 1e-2f, $"y at ×{scale}");
                }
                vh.Dispose();
            }
        }

        [Test]
        public void TheFontAtlases_ArePointSampled_AlsoAfterARebuild()
        {
            foreach (Font font in new[] { UiArt.Body, UiArt.Display })
            {
                Assume.That(font, Is.Not.Null);
                PixelText.KeepSharp(font);
                Assert.AreEqual(FilterMode.Point, font.material.mainTexture.filterMode, font.name);
                // A large size forces the dynamic atlas to grow — Unity builds a new texture.
                font.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÇĞİÖŞÜçğıöşü0123456789", 120);
                font.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÇĞİÖŞÜçğıöşü0123456789", 160);
                Assert.AreEqual(FilterMode.Point, font.material.mainTexture.filterMode, font.name + " after a rebuild");
            }
        }
    }
}
