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
