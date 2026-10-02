using System.IO;
using HellPoker.Presentation.Animation;
using NUnit.Framework;
using UnityEngine;

namespace HellPoker.Core.Tests
{
    /// <summary>The title screen's backdrop: the animated bottom of Hell, with the stone wall and a flat colour behind it.</summary>
    public class MenuBackdropTests
    {
        private static Texture2D Strip(int frames) => new Texture2D(480 * frames, 270);

        [Test]
        public void TheMenuArt_PlaysAsALoopingClip()
        {
            var library = new MenuBackdropLibrary(path => path == "Ui/menu" ? Strip(4) : null);

            SpriteClip clip = library.Get();

            Assert.AreEqual(4, clip.Frames.Length);
            Assert.AreEqual(4f, clip.Fps);
            Assert.IsTrue(clip.Loop);
        }

        [Test]
        public void WithoutTheMenuArt_TheStoneWallStandsIn()
        {
            var library = new MenuBackdropLibrary(path => path == "Ui/background" ? Strip(1) : null);

            Assert.AreEqual(1, library.Get().Frames.Length);
        }

        [Test]
        public void WithNoArtAtAll_ThereIsNoClip_AndNothingBreaks()
        {
            Assert.IsNull(new MenuBackdropLibrary(path => null).Get());
            Assert.IsNull(new MenuBackdropLibrary(path => throw new IOException("disk on fire")).Get());
        }

        [Test]
        public void TheGeneratedArt_IsFourFramesWithinTheTextureLimit()
        {
            const string path = "Assets/Resources/Art/Ui/menu.png";
            Assert.IsTrue(File.Exists(path), "py Tools/ArtGen/generate_art.py menu");
            var texture = new Texture2D(2, 2);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)));

            Assert.AreEqual(270, texture.height);
            Assert.AreEqual(4 * 480, texture.width);
            Assert.LessOrEqual(texture.width, 2048);
        }

        [Test]
        public void TheMiddleColumn_StaysDark()
        {
            // Under the logo, the text and the buttons (x 100-380, y 40-250) the picture must stay quiet: mostly the darkest colours.
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes("Assets/Resources/Art/Ui/menu.png"));
            Color32[] pixels = texture.GetPixels32();
            for (int frame = 0; frame < 4; frame++)
            {
                int dark = 0, total = 0;
                for (int y = 40; y <= 250; y++)
                {
                    for (int x = 100; x <= 380; x++)
                    {
                        Color32 p = pixels[(texture.height - 1 - y) * texture.width + frame * 480 + x];   // rows are stored bottom up
                        if (p.r + p.g + p.b < 3 * 80) dark++;
                        total++;
                    }
                }
                Assert.GreaterOrEqual(dark / (float)total, 0.8f, $"Frame {frame}: the middle column is too bright to read on.");
            }
        }
    }
}
