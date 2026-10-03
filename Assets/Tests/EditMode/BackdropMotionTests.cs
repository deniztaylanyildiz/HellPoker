using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HellPoker.Presentation.Animation;
using NUnit.Framework;
using UnityEngine;

namespace HellPoker.Core.Tests
{
    /// <summary>
    /// The backdrops' motion: the manifest the art tools write (layers, particles), how a layer loops and drifts, a hall's
    /// variant strips — and the generated art itself: every strip within the texture limit, 8-12 frames, a loop that closes,
    /// and nothing moving where words sit.
    /// </summary>
    public class BackdropMotionTests
    {
        private static Sprite[] Frames(int count, int w = 10, int h = 8)
        {
            var texture = new Texture2D(w * count, h);
            return SpriteSheet.Slice(texture, w);
        }

        // ------------------------------------------------------------------ the manifest

        [Test]
        public void TheManifest_ReadsLayersAndParticles_AndSkipsWhatItCannot()
        {
            const string manifest = "# a comment\n" +
                                    "layer curtain 95 36 12 10 1 0 0 0\n" +
                                    "layer missing 0 0 12 10 1 0 0 0\n" +
                                    "layer broken x y 12 10 1 0 0 0\n" +
                                    "something else\n" +
                                    "particles embers 0 0 136 100 8\n" +
                                    "particles unknown 0 0 1 1 1\n";

            BackdropMotion motion = BackdropMotion.Parse(manifest, (file, frames) => file == "missing" ? null : Frames(frames));

            BackdropLayer layer = motion.Layers.Single();
            Assert.AreEqual(new Vector2Int(95, 36), layer.Position);
            Assert.AreEqual(12, layer.Clip.Frames.Length);
            Assert.AreEqual(10f, layer.Clip.Fps);
            Assert.IsTrue(layer.Clip.Loop);
            BackdropParticles particles = motion.Particles.Single();
            Assert.AreEqual(ParticleKind.Embers, particles.Kind);
            Assert.AreEqual(new RectInt(0, 0, 136, 100), particles.Area);
            Assert.AreEqual(8, particles.Count);
        }

        [Test]
        public void NoManifest_IsNoMotion()
        {
            Assert.AreSame(BackdropMotion.None, BackdropMotion.Parse(null, (f, n) => Frames(n)));
            Assert.AreSame(BackdropMotion.None, BackdropMotion.Parse("# nothing\n", (f, n) => Frames(n)));
            Assert.IsTrue(BackdropMotion.None.IsEmpty);
        }

        [Test]
        public void ALayer_StartsItsLoopAtItsOffset()
        {
            var layer = new BackdropLayer(new SpriteClip(Frames(12), 10f, true), Vector2Int.zero, new Vector2Int(10, 8), offset: 5);

            Assert.AreEqual(5, layer.FrameAt(0f));
            Assert.AreEqual(6, layer.FrameAt(0.1f));
            Assert.AreEqual(4, layer.FrameAt(1.1f), "11 + 5, round the loop.");
        }

        [Test]
        public void ADriftingLayer_MovesWholePixels_AndComesRoundAgain()
        {
            // A 7 px bird starting at x 180, 9 px a second, coming round every 487 px (off the right edge, back on the left).
            var bird = new BackdropLayer(new SpriteClip(Frames(8, 7, 3), 8f, true), new Vector2Int(180, 12), new Vector2Int(7, 3),
                drift: 9f, wrap: 487);

            Assert.AreEqual(180, bird.XAt(0f));
            Assert.AreEqual(180, bird.XAt(0.1f), "Not a whole pixel yet.");
            Assert.AreEqual(189, bird.XAt(1f));
            Assert.AreEqual(-7, bird.XAt((480 - 180 + 0.5f) / 9f), "Off the right edge it comes back just left of the screen.");
            Assert.AreEqual(180, new BackdropLayer(new SpriteClip(Frames(8), 8f, true), new Vector2Int(180, 0), new Vector2Int(10, 8)).XAt(99f),
                "A still layer stays.");
        }

        [Test]
        public void AHall_PlaysItsModesOwnStrips_OrTheNormalOnes()
        {
            var textures = new Dictionary<string, Texture2D>
            {
                ["Backgrounds/belial/curtain"] = new Texture2D(12 * 47, 200),
                ["Backgrounds/belial/curtain_hell"] = new Texture2D(12 * 47, 200) { name = "hell" }
            };
            var library = new SalonLibrary(path => textures.TryGetValue(path, out Texture2D t) ? t : null,
                path => path == "Backgrounds/belial/motion" ? "layer curtain 95 36 12 10 1 0 0 0" : null);

            Assert.AreEqual("hell", library.Motion("belial", SalonMode.Hell).Layers[0].Clip.Frames[0].texture.name);
            Assert.AreNotEqual("hell", library.Motion("belial", SalonMode.Soul).Layers[0].Clip.Frames[0].texture.name, "No soul strip: the normal one.");
            Assert.AreSame(library.Motion("belial", SalonMode.Hell), library.Motion("belial", SalonMode.Hell), "Loaded once.");
            Assert.AreSame(BackdropMotion.None, library.Motion("mammon", SalonMode.Normal));
            Assert.AreSame(BackdropMotion.None, new SalonLibrary(path => null).Motion("belial", SalonMode.Normal), "No text loader: no motion.");
        }

        // ------------------------------------------------------------------ the generated art

        /// <summary>Words on the table over the hall, and on the title screen (must match TABLE_TEXT / MENU_TEXT in Tools/ArtGen).</summary>
        private static readonly RectInt[] TableText = { Box(0, 104, 112, 132), Box(138, 88, 342, 240), Box(118, 248, 476, 266) };
        private static readonly RectInt[] MenuText = { Box(88, 84, 392, 126), Box(100, 128, 380, 240), Box(176, 250, 304, 266), Box(400, 256, 479, 269) };

        private static RectInt Box(int x0, int y0, int x1, int y1) => new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);

        private static IEnumerable<(string folder, string manifest, string prefix, RectInt[] text)> Backdrops()
        {
            foreach (string hall in new[] { "mammon", "belial", "lilith", "lucifer" })
                yield return ($"Assets/Resources/Art/Backgrounds/{hall}/", "motion.txt", "", TableText);
            yield return ("Assets/Resources/Art/Ui/", "menu_motion.txt", "", MenuText);
        }

        private static Texture2D Load(string path)
        {
            var texture = new Texture2D(2, 2);
            Assert.IsTrue(texture.LoadImage(File.ReadAllBytes(path)), path);
            return texture;
        }

        [Test]
        public void EveryGeneratedLayer_Fits_Loops_AndKeepsClearOfTheWords()
        {
            int layers = 0;
            foreach (var (folder, manifest, _, text) in Backdrops())
            {
                string path = folder + manifest;
                Assert.IsTrue(File.Exists(path), $"{path}: py Tools/ArtGen/generate_art.py salons menu");
                foreach (string line in File.ReadAllLines(path).Where(l => l.StartsWith("layer ")))
                {
                    string[] p = line.Split(' ');
                    string file = folder + p[1] + ".png";
                    int x = int.Parse(p[2], CultureInfo.InvariantCulture), y = int.Parse(p[3], CultureInfo.InvariantCulture);
                    int frames = int.Parse(p[4], CultureInfo.InvariantCulture);
                    float fps = float.Parse(p[5], CultureInfo.InvariantCulture);
                    bool drifts = p[7] != "0";
                    Texture2D strip = Load(file);
                    layers++;

                    Assert.LessOrEqual(strip.width, 2048, $"{file}: wider than the importer's limit.");
                    Assert.AreEqual(0, strip.width % frames, $"{file}: not {frames} equal frames.");
                    Assert.That(frames, Is.InRange(8, 12), $"{file}: a layer loops in 8-12 frames.");
                    Assert.That(fps, Is.InRange(8f, 12f), $"{file}: a layer plays at 8-12 FPS.");

                    int w = strip.width / frames, h = strip.height;
                    Color32[] px = strip.GetPixels32();
                    Color32 At(int frame, int lx, int ly) => px[(h - 1 - ly) * strip.width + frame * w + lx];

                    // The loop closes: back to frame 0 is no bigger a change than any step (2% slack, as in the tools).
                    int Diff(int a, int b)
                    {
                        int n = 0;
                        for (int ly = 0; ly < h; ly++)
                            for (int lx = 0; lx < w; lx++)
                                if (!At(a, lx, ly).Equals(At(b, lx, ly))) n++;
                        return n;
                    }
                    int largest = Enumerable.Range(0, frames - 1).Max(f => Diff(f, f + 1));
                    Assert.LessOrEqual(Diff(frames - 1, 0), largest * 1.02f + 1, $"{file}: the loop jumps back to its first frame.");

                    // Nothing moving where words sit (a drifting layer keeps to the sky).
                    if (drifts) continue;
                    for (int frame = 0; frame < frames; frame++)
                        for (int ly = 0; ly < h; ly++)
                            for (int lx = 0; lx < w; lx++)
                            {
                                var screen = new Vector2Int(x + lx, y + ly);
                                if (At(frame, lx, ly).a == 0 || !text.Any(r => r.Contains(screen))) continue;
                                Assert.Fail($"{file}: frame {frame} draws at {screen}, where words sit.");
                            }
                }
            }
            Assert.Greater(layers, 20, "Every hall and the title screen have their moving layers.");
        }

        [Test]
        public void EveryHallsBackdrop_IsOneStillFrame()
        {
            foreach (string hall in new[] { "mammon", "belial", "lilith", "lucifer" })
            {
                Texture2D still = Load($"Assets/Resources/Art/Backgrounds/{hall}/normal.png");
                Assert.AreEqual(480, still.width, hall);
                Assert.AreEqual(270, still.height, hall);
            }
        }
    }
}
