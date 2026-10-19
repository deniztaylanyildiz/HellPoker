using System.Collections.Generic;
using System.IO;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HellPoker.Core.Tests
{
    /// <summary>Demo 3.1: the player's class portrait on a chapter's table — its strips, the fallback chain, the moods, the generated art.</summary>
    public class SinnerAnimationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private Dictionary<string, Texture2D> _files;
        private List<string> _requests;

        [SetUp]
        public void SetUp()
        {
            _files = new Dictionary<string, Texture2D>();
            _requests = new List<string>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created)
                Object.DestroyImmediate(o);
            _created.Clear();
        }

        private Texture2D Sheet(int frames, int size = SinnerAnimationLibrary.FrameSize)
        {
            var texture = new Texture2D(frames * size, size) { name = "sheet" };
            _created.Add(texture);
            return texture;
        }

        private SinnerAnimationLibrary Library() => new SinnerAnimationLibrary(path =>
        {
            _requests.Add(path);
            return _files.TryGetValue(path, out Texture2D texture) ? texture : null;
        });

        [Test]
        public void AStateStrip_IsCutIntoItsFrames_AtItsRate()
        {
            _files["Sinners/king/idle"] = Sheet(8);
            _files["Sinners/king/joy"] = Sheet(8);

            SpriteClip idle = Library().Get("king", SinnerAnimation.Idle);
            SpriteClip joy = Library().Get("king", SinnerAnimation.Joy);

            Assert.AreEqual(8, idle.Frames.Length);
            Assert.AreEqual(SinnerAnimationLibrary.IdleFps, idle.Fps);
            Assert.IsTrue(idle.Loop);
            Assert.IsFalse(joy.Loop, "joy plays once");
            Assert.IsTrue(SinnerAnimationLibrary.Loops(SinnerAnimation.Tense));
        }

        [Test]
        public void AMissingState_FallsBackToIdle_KeepingItsOneShotTiming()
        {
            _files["Sinners/thief/idle"] = Sheet(6);

            SpriteClip hurt = Library().Get("thief", SinnerAnimation.Hurt);

            Assert.AreEqual(6, hurt.Frames.Length);
            Assert.IsFalse(hurt.Loop, "a one-shot still ends");
        }

        [Test]
        public void AMissingIdle_FallsBackToTheClassPortrait_AtItsOwnSize()
        {
            _files["Sinners/peasant"] = Sheet(1, 48);

            SpriteClip clip = Library().Get("peasant", SinnerAnimation.Happy);

            Assert.AreEqual(1, clip.Frames.Length);
            Assert.AreEqual(48f, clip.Frames[0].rect.width, "the 48×48 portrait, not stretched");
        }

        [Test]
        public void NoArtAtAll_IsNoClip_AndNothingBreaks()
        {
            Assert.IsNull(Library().Get("jester", SinnerAnimation.Idle));
            Assert.IsNull(Library().Get(null, SinnerAnimation.Idle));
            Assert.IsNull(new SinnerAnimationLibrary(path => throw new IOException("disk on fire")).Get("king", SinnerAnimation.Joy));
        }

        [Test]
        public void TheStrips_AreLoadedOnce()
        {
            _files["Sinners/king/idle"] = Sheet(8);
            SinnerAnimationLibrary library = Library();

            library.Get("king", SinnerAnimation.Idle);
            library.Get("king", SinnerAnimation.Idle);

            Assert.AreEqual(1, _requests.FindAll(p => p == "Sinners/king/idle").Count);
        }

        [TestCase(SinnerMood.Pleased, SinnerAnimation.Happy)]
        [TestCase(SinnerMood.Elated, SinnerAnimation.Joy)]
        [TestCase(SinnerMood.Hurt, SinnerAnimation.Hurt)]
        public void Moods_BecomeOneShots(SinnerMood mood, SinnerAnimation animation)
        {
            Assert.AreEqual(animation, SinnerView.ReactionTo(mood));
        }

        [Test]
        public void CalmAndTense_AreLoops_NotOneShots()
        {
            Assert.IsNull(SinnerView.ReactionTo(SinnerMood.Calm));
            Assert.IsNull(SinnerView.ReactionTo(SinnerMood.Tense));
        }

        [TestCase(HandOutcome.Win, HellPoker.Core.Evaluation.HandCategory.OnePair, SinnerMood.Pleased)]
        [TestCase(HandOutcome.Win, HellPoker.Core.Evaluation.HandCategory.ThreeOfAKind, SinnerMood.Pleased)]
        [TestCase(HandOutcome.Win, HellPoker.Core.Evaluation.HandCategory.Straight, SinnerMood.Elated)]
        [TestCase(HandOutcome.Win, HellPoker.Core.Evaluation.HandCategory.RoyalFlush, SinnerMood.Elated)]
        [TestCase(HandOutcome.Loss, HellPoker.Core.Evaluation.HandCategory.FullHouse, SinnerMood.Hurt)]
        public void AHandsEnd_MovesTheSinner(HandOutcome outcome, HellPoker.Core.Evaluation.HandCategory category, SinnerMood mood)
        {
            Assert.AreEqual(mood, Presentation.TablePresenter.MoodAfter(outcome, category));
        }

        [Test]
        public void AFoldOrAPush_MovesNothing_AndAWinWithoutAHandSmiles()
        {
            Assert.IsNull(Presentation.TablePresenter.MoodAfter(HandOutcome.Fold, null));
            Assert.IsNull(Presentation.TablePresenter.MoodAfter(HandOutcome.Push, HellPoker.Core.Evaluation.HandCategory.OnePair));
            Assert.AreEqual(SinnerMood.Pleased, Presentation.TablePresenter.MoodAfter(HandOutcome.Win, null), "the imp folded: no hand shown");
        }

        // ------------------------------------------------------------------ the generated art

        private static readonly (string state, int frames, bool loops)[] States =
            { ("idle", 8, true), ("happy", 6, false), ("joy", 8, false), ("hurt", 6, false), ("tense", 8, true) };

        [Test]
        public void EveryClass_HasItsFiveStrips_At64_InWholeColours_AndItsLoopsClose()
        {
            foreach (Core.Sinners.SinnerClass cls in Core.Sinners.SinnerRoster.PhaseTwo)
            foreach (var (state, frames, loops) in States)
            {
                string path = $"Assets/Resources/Art/Sinners/{cls.Id}/{state}.png";
                Assert.IsTrue(File.Exists(path), path + ": py Tools/ArtGen/generate_art.py sinners");
                var strip = new Texture2D(2, 2);
                _created.Add(strip);
                Assert.IsTrue(strip.LoadImage(File.ReadAllBytes(path)), path);
                Assert.AreEqual(SinnerAnimationLibrary.FrameSize, strip.height, path);
                Assert.AreEqual(frames * SinnerAnimationLibrary.FrameSize, strip.width, path + ": its frames");

                Color32[] px = strip.GetPixels32();
                var colours = new HashSet<int>();
                foreach (Color32 c in px)
                {
                    Assert.IsTrue(c.a == 0 || c.a == 255, path + ": no half-transparent pixel");
                    if (c.a == 255) colours.Add(c.r << 16 | c.g << 8 | c.b);
                }
                Assert.LessOrEqual(colours.Count, 32, path + ": the one palette");
                if (!loops) continue;

                int w = SinnerAnimationLibrary.FrameSize;
                int Diff(int a, int b)
                {
                    int n = 0;
                    for (int y = 0; y < w; y++)
                        for (int x = 0; x < w; x++)
                            if (!px[y * strip.width + a * w + x].Equals(px[y * strip.width + b * w + x])) n++;
                    return n;
                }
                int largest = 0;
                for (int f = 0; f < frames - 1; f++) largest = System.Math.Max(largest, Diff(f, f + 1));
                Assert.LessOrEqual(Diff(frames - 1, 0), largest * 1.02f + 1, path + ": the loop jumps back to its first frame");
                Assert.Greater(largest, 0, path + ": it moves");
            }
        }

        [Test]
        public void ThePortraitView_ShowsTheFallback_WithoutStretchingIt()
        {
            _files["Sinners/king"] = Sheet(1, 48);
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            _created.Add(canvas);
            var sequencer = canvas.AddComponent<AnimationSequencer>();
            SinnerView view = SinnerView.Create(canvas.transform, 0, 0, sequencer, Library());

            view.SetClass("king");

            var rect = (RectTransform)view.Portrait.transform;
            Assert.AreEqual(new Vector2(48, 48), rect.sizeDelta, "the portrait keeps its own pixels");
            Assert.AreEqual(new Vector2(8, -8), rect.anchoredPosition, "centred in the 64 box");
        }
    }
}
