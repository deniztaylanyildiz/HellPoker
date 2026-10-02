using System.IO;
using HellPoker.Presentation.Animation;
using NUnit.Framework;
using UnityEngine;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The Morning Star's art: every portrait state is there, and none of them ever shows him — each frame stays almost
    /// entirely dark (eyes, two claw tips and a wing edge only). His hall has its normal and hot variants.
    /// </summary>
    public class LuciferArtTests
    {
        private const string Demons = "Assets/Resources/Art/Demons/lucifer/";
        private const string Halls = "Assets/Resources/Art/Backgrounds/lucifer/";
        private const int Frame = 96;

        /// <summary>The darkest colours of the palette (black, night, blood dark): "pitch dark" in the portrait box.</summary>
        private static readonly Color32[] Dark = { new Color32(0x0b, 0x06, 0x10, 255), new Color32(0x16, 0x0b, 0x1e, 255), new Color32(0x3a, 0x0a, 0x10, 255) };

        private static Texture2D Load(string path)
        {
            Assert.IsTrue(File.Exists(path), path + " is missing (py Tools/ArtGen/generate_art.py).");
            var texture = new Texture2D(2, 2);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
            return texture;
        }

        [TestCase("idle")]
        [TestCase("talk")]
        [TestCase("gloat")]
        [TestCase("reraise")]
        [TestCase("final")]
        [TestCase("soul")]
        public void EveryFrame_IsAlmostAllDarkness(string state)
        {
            Texture2D strip = Load(Demons + state + ".png");
            Assert.AreEqual(Frame, strip.height);
            Assert.AreEqual(0, strip.width % Frame, "Square 96 px frames side by side.");

            Color32[] pixels = strip.GetPixels32();
            for (int frame = 0; frame < strip.width / Frame; frame++)
            {
                int dark = 0;
                for (int y = 0; y < Frame; y++)
                {
                    for (int x = 0; x < Frame; x++)
                    {
                        Color32 p = pixels[y * strip.width + frame * Frame + x];
                        if (System.Array.Exists(Dark, d => d.r == p.r && d.g == p.g && d.b == p.b)) dark++;
                    }
                }
                Assert.GreaterOrEqual(dark / (float)(Frame * Frame), 0.85f, $"{state} frame {frame}: he must never be seen.");
            }
        }

        [Test]
        public void Angry_IsThere_TheBoxBurnsRed()
        {
            // Not counted as darkness: the whole box glows blood red, still without any shape of him in it.
            Assert.AreEqual(Frame, Load(Demons + "angry.png").height);
        }

        [Test]
        public void HisHall_HasNormalAndHot_AndNoSoulVariant()
        {
            Assert.AreEqual(270, Load(Halls + "normal.png").height);
            Assert.AreEqual(270, Load(Halls + "hell.png").height);
            Assert.LessOrEqual(Load(Halls + "normal.png").width, 2048, "Within the importer's texture limit.");
            Assert.IsFalse(File.Exists(Halls + "soul.png"), "The soul never goes on his table.");
        }

        [Test]
        public void HisHall_FallsBackToNormal_ForTheSoulVariant()
        {
            var library = new SalonLibrary(path => path == "Backgrounds/lucifer/normal" ? new Texture2D(480, 270) : null);

            Assert.IsNotNull(library.Get("lucifer", SalonMode.Soul));
        }

        [Test]
        public void HisDialogueBox_AndTheDarkness_AreThere()
        {
            Assert.AreEqual(12, Load("Assets/Resources/Art/Ui/dialog_lucifer.png").width);
            Texture2D fade = Load("Assets/Resources/Art/Ui/fade.png");
            Assert.AreEqual(270, fade.height);
            Assert.AreEqual(4 * 480, fade.width, "Four steps into darkness.");
        }
    }
}
