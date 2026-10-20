using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HellPoker.Core.Relics;
using NUnit.Framework;
using UnityEngine;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.5: every relic's big picture (Ui/relic_art.png, 32×32 in the icons' order) and each legendary's sparkle.</summary>
    public class RelicArtTests
    {
        private const int Size = 32;
        private const int GlintFrames = 6;
        private static readonly string ArtFolder = Path.Combine("Assets", "Resources", "Art", "Ui");
        private static readonly string Tools = Path.Combine("Tools", "ArtGen");

        private static (int width, int height) PngSize(string path)
        {
            byte[] header = File.ReadAllBytes(path).Take(24).ToArray();
            int Read(int at) => (header[at] << 24) | (header[at + 1] << 16) | (header[at + 2] << 8) | header[at + 3];
            return (Read(16), Read(20));
        }

        private static Texture2D Load(string path)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        /// <summary>A list or a dictionary's keys, as the generator writes them (its own Python source).</summary>
        private static List<string> PythonIds(string file, string name)
        {
            string text = File.ReadAllText(Path.Combine(Tools, file));
            int start = text.IndexOf(name + " = ");
            Assert.GreaterOrEqual(start, 0, name + " in " + file);
            char open = text[text.IndexOf(" = ", start) + 3];
            char close = open == '[' ? ']' : '}';
            string body = text.Substring(start, text.IndexOf(close, start) - start);
            return Regex.Matches(body, open == '[' ? "\"(\\w+)\"" : "\"(\\w+)\":").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        }

        [Test]
        public void EveryRelic_HasItsBigPicture_InTheIconsOrder_32By32()
        {
            (int width, int height) = PngSize(Path.Combine(ArtFolder, "relic_art.png"));
            Assert.AreEqual(RelicRoster.IconOrder.Count * Size, width, "one 32×32 picture for each of the 54");
            Assert.AreEqual(Size, height);
            CollectionAssert.AreEqual(RelicRoster.IconOrder, PythonIds("pixel_relics.py", "ORDER"),
                "the generator draws them in RelicRoster.IconOrder (the icons' order)");

            Texture2D strip = Load(Path.Combine(ArtFolder, "relic_art.png"));
            for (int i = 0; i < RelicRoster.IconOrder.Count; i++)
            {
                string id = RelicRoster.IconOrder[i];
                Color32[] pixels = strip.GetPixels32();
                int opaque = 0, partial = 0;
                bool darkEdge = false;
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        Color32 c = pixels[y * strip.width + i * Size + x];
                        if (c.a == 255) opaque++;
                        else if (c.a != 0) partial++;
                        if (c.a == 255 && c.r < 20 && c.g < 16 && c.b < 24) darkEdge = true;
                    }
                Assert.Greater(opaque, Size * Size / 8, id + ": the picture is there");
                Assert.AreEqual(0, partial, id + ": no half-transparent pixel (no anti-aliasing)");
                Assert.IsTrue(darkEdge, id + ": its dark outline");
            }
            Object.DestroyImmediate(strip);
        }

        [Test]
        public void EveryLegendary_HasItsSparkle_AndOnlyThey_ABurstThatRestsOnItsFirstFrame()
        {
            var legendaries = RelicRoster.IconOrder.Where(id => ChapterRelics.RarityOf(id) == RelicRarity.Legendary).ToList();
            Assert.AreEqual(12, legendaries.Count);
            string folder = Path.Combine(ArtFolder, "RelicGlint");
            CollectionAssert.AreEquivalent(legendaries, Directory.GetFiles(folder, "*.png").Select(Path.GetFileNameWithoutExtension),
                "a sparkle for each legendary, none for the others");

            Texture2D still = Load(Path.Combine(ArtFolder, "relic_art.png"));
            foreach (string id in legendaries)
            {
                string path = Path.Combine(folder, id + ".png");
                Assert.AreEqual((GlintFrames * Size, Size), PngSize(path), id + ": six 32×32 frames");
                Texture2D glint = Load(path);
                int index = RelicRoster.IconOrder.ToList().IndexOf(id);
                int Diff(int a, int b)
                {
                    int n = 0;
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                            if (!glint.GetPixel(a * Size + x, y).Equals(glint.GetPixel(b * Size + x, y))) n++;
                    return n;
                }
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                        Assert.AreEqual(still.GetPixel(index * Size + x, y), glint.GetPixel(x, y), $"{id}: frame 0 is the picture at rest");
                int largest = Enumerable.Range(0, GlintFrames - 1).Max(f => Diff(f, f + 1));
                Assert.Greater(largest, 0, id + ": it sparkles");
                Assert.LessOrEqual(Diff(GlintFrames - 1, 0), largest, id + ": the loop's last step no bigger than any other (the loop rule)");
                Object.DestroyImmediate(glint);
            }
            Object.DestroyImmediate(still);
        }

        [Test]
        public void TheMarks_FollowTheGame_ADemonsRelics_AndAClasssBadges()
        {
            var classOf = PythonIds("pixel_relic_art.py", "CLASS_OF");
            var classRelics = RelicRoster.IconOrder.Where(ChapterRelics.IsClassRelic).ToList();
            CollectionAssert.AreEquivalent(classRelics, classOf, "a class badge on exactly the class relics");
            string text = File.ReadAllText(Path.Combine(Tools, "pixel_relic_art.py"));
            foreach (string id in classRelics)
                StringAssert.Contains($"\"{id}\": \"{ChapterRelics.Find(id).ClassId}\"", text, id + ": its own class's colour");
            foreach (string id in RelicRoster.IconOrder.Where(id => ChapterRelics.RarityOf(id) == RelicRarity.Boss))
                CollectionAssert.Contains(PythonIds("pixel_relic_art.py", "DEMON_OF"), id, id + ": a demon's spoils carry his mark");
        }
    }
}
