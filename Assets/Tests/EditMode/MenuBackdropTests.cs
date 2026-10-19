using System.IO;
using System.Linq;
using HellPoker.Presentation.Animation;
using NUnit.Framework;
using UnityEngine;

namespace HellPoker.Core.Tests
{
    /// <summary>The title screen's backdrop: the animated bottom of Hell, with the stone wall and a flat colour behind it.</summary>
    public class MenuBackdropTests
    {
        private static Texture2D Strip(int frames) => new Texture2D(MenuBackdropLibrary.FrameWidth * frames, Presentation.Ui.PixelScreen.Height);

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
        public void TheGeneratedArt_IsOneStillFrame_WithItsMotionBeside()
        {
            const string path = "Assets/Resources/Art/Ui/menu.png";
            Assert.IsTrue(File.Exists(path), "py Tools/ArtGen/generate_art.py menu");
            var texture = new Texture2D(2, 2);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)));

            Assert.AreEqual(Presentation.Ui.PixelScreen.Height, texture.height);
            Assert.AreEqual(Presentation.Ui.PixelScreen.Width, texture.width, "The backdrop is one still frame; what moves is in its layers.");
            Assert.IsTrue(File.Exists("Assets/Resources/Art/Ui/menu_motion.txt"));
        }

        [Test]
        public void TheMenuMotion_IsRead_FromItsManifest()
        {
            var library = new MenuBackdropLibrary(path => path == "Ui/menu_eyes" ? new Texture2D(12 * 10, 8) : null,
                path => path == MenuBackdropLibrary.MotionPath ? "layer menu_eyes 214 3 12 8 1 0 0 0\nparticles embers 0 30 90 170 14" : null);

            BackdropMotion motion = library.Motion();

            Assert.AreEqual(1, motion.Layers.Count);
            Assert.AreEqual(12, motion.Layers[0].Clip.Frames.Length);
            Assert.AreEqual(new Vector2Int(10, 8), motion.Layers[0].Size);
            Assert.AreEqual(ParticleKind.Embers, motion.Particles.Single().Kind);
            Assert.AreSame(BackdropMotion.None, new MenuBackdropLibrary(path => null).Motion(), "No manifest, no motion.");
        }

        [Test]
        public void TheMiddleColumn_StaysDark()
        {
            // Under the logo, the text and the buttons (the middle 280 px, y 40 to 60 px above the bottom) the picture must stay quiet.
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes("Assets/Resources/Art/Ui/menu.png"));
            int w = MenuBackdropLibrary.FrameWidth, x0 = w / 2 - 140, x1 = w / 2 + 140, y1 = texture.height - 60;
            Color32[] pixels = texture.GetPixels32();
            for (int frame = 0; frame < texture.width / w; frame++)
            {
                int dark = 0, total = 0;
                for (int y = 40; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        Color32 p = pixels[(texture.height - 1 - y) * texture.width + frame * w + x];   // rows are stored bottom up
                        if (p.r + p.g + p.b < 3 * 80) dark++;
                        total++;
                    }
                }
                Assert.GreaterOrEqual(dark / (float)total, 0.8f, $"Frame {frame}: the middle column is too bright to read on.");
            }
        }

        private static Texture2D LoadArt(string name)
        {
            string path = $"Assets/Resources/Art/Ui/{name}.png";
            Assert.IsTrue(File.Exists(path), "py Tools/ArtGen/generate_art.py menu");
            var texture = new Texture2D(2, 2);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)));
            return texture;
        }

        [Test]
        public void LucifersThrone_IsOnlyADarkShape()
        {
            // Demo 3.3: far away behind the menu, his throne — steps, a spiked back, chains — never a figure: almost all of it near black.
            Texture2D texture = LoadArt("menu");
            Color32[] pixels = texture.GetPixels32();
            int dark = 0, total = 0;
            for (int y = 84; y <= 280; y++)
            {
                for (int x = 240; x <= 400; x++)
                {
                    Color32 p = pixels[(texture.height - 1 - y) * texture.width + x];
                    if (p.r + p.g + p.b < 3 * 40) dark++;
                    total++;
                }
            }
            Assert.GreaterOrEqual(dark / (float)total, 0.9f, "The throne must stay a silhouette in the dark.");
        }

        [Test]
        public void TheMenusOwnSprites_HaveTheirSizes()
        {
            // The logo fits four fifths of the screen at least once; the box is a 24 px 9-slice; the cursor 4 frames of 8; the glint 8 of 9.
            Texture2D logo = LoadArt("menu_logo");
            Assert.LessOrEqual(logo.width, Presentation.Ui.PixelScreen.Width * 4 / 5);
            Assert.LessOrEqual(logo.height, 80, "The logo leaves room for the words and the box under it.");
            Texture2D box = LoadArt("menu_box");
            Assert.AreEqual(new Vector2Int(24, 24), new Vector2Int(box.width, box.height));
            Texture2D cursor = LoadArt("menu_cursor");
            Assert.AreEqual(new Vector2Int(32, 8), new Vector2Int(cursor.width, cursor.height));
            Texture2D glint = LoadArt("menu_glint");
            Assert.AreEqual(new Vector2Int(72, 9), new Vector2Int(glint.width, glint.height));
        }
    }
}
