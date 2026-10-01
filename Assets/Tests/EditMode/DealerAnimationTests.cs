using System;
using System.Collections.Generic;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HellPoker.Core.Tests
{
    public class DealerAnimationTests
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

        /// <summary>A fake sheet of <paramref name="frames"/> square frames.</summary>
        private Texture2D Sheet(int frames, int size = 96)
        {
            var texture = new Texture2D(frames * size, size) { name = "sheet" };
            _created.Add(texture);
            return texture;
        }

        private DealerAnimationLibrary Library()
        {
            return new DealerAnimationLibrary(path =>
            {
                _requests.Add(path);
                return _files.TryGetValue(path, out Texture2D texture) ? texture : null;
            });
        }

        // ------------------------------------------------------------------ the fallback chain

        [Test]
        public void StateSheet_IsCutIntoSquareFrames()
        {
            _files["Demons/mammon/gloat"] = Sheet(6);

            SpriteClip clip = Library().Get("mammon", DealerAnimation.Gloat);

            Assert.AreEqual(6, clip.Frames.Length);
            Assert.AreEqual(96f, clip.Frames[0].rect.width);
            Assert.AreEqual(new Rect(5 * 96, 0, 96, 96), clip.Frames[5].rect);
            Assert.IsFalse(clip.Loop, "Gloat plays once.");
            Assert.AreEqual(DealerAnimationLibrary.DefaultFps, clip.Fps);
        }

        [Test]
        public void IdleTalkAndFinal_Loop()
        {
            Assert.IsTrue(DealerAnimationLibrary.Loops(DealerAnimation.Idle));
            Assert.IsTrue(DealerAnimationLibrary.Loops(DealerAnimation.Talk));
            Assert.IsTrue(DealerAnimationLibrary.Loops(DealerAnimation.Final));
            Assert.IsFalse(DealerAnimationLibrary.Loops(DealerAnimation.Gloat));
            Assert.IsFalse(DealerAnimationLibrary.Loops(DealerAnimation.Angry));
            Assert.IsFalse(DealerAnimationLibrary.Loops(DealerAnimation.ReRaise));
        }

        [Test]
        public void MissingState_FallsBackToIdle_KeepingItsOneShotTiming()
        {
            _files["Demons/belial/idle"] = Sheet(4);

            SpriteClip clip = Library().Get("belial", DealerAnimation.Angry);

            Assert.IsNotNull(clip);
            Assert.AreEqual(4, clip.Frames.Length, "Idle frames stand in for the missing state.");
            Assert.IsFalse(clip.Loop, "Still a one-shot, so the reaction ends.");
            CollectionAssert.Contains(_requests, "Demons/belial/angry");
        }

        [Test]
        public void MissingIdle_FallsBackToASinglePortrait()
        {
            _files["Demons/lilith"] = Sheet(1);

            SpriteClip clip = Library().Get("lilith", DealerAnimation.Talk);

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.Frames.Length);
            Assert.IsTrue(clip.Loop);
        }

        [Test]
        public void NoArtAtAll_ReturnsNull_WithoutThrowing()
        {
            DealerAnimationLibrary library = Library();

            foreach (DealerAnimation animation in Enum.GetValues(typeof(DealerAnimation)))
                Assert.IsNull(library.Get("nobody", animation));
            Assert.IsNull(library.Get(null, DealerAnimation.Idle));
        }

        [Test]
        public void ALoaderThatThrows_CountsAsMissing()
        {
            var library = new DealerAnimationLibrary(path => throw new InvalidOperationException("disk on fire"));

            Assert.IsNull(library.Get("mammon", DealerAnimation.Idle), "A failing load is logged as a warning and treated as missing art.");
        }

        [Test]
        public void Sheets_AreLoadedOnce()
        {
            _files["Demons/mammon/idle"] = Sheet(6);
            DealerAnimationLibrary library = Library();

            library.Get("mammon", DealerAnimation.Idle);
            library.Get("mammon", DealerAnimation.Idle);

            Assert.AreEqual(1, _requests.FindAll(p => p == "Demons/mammon/idle").Count);
        }

        // ------------------------------------------------------------------ clips and the animator

        [Test]
        public void LoopingClip_WrapsAround()
        {
            var clip = new SpriteClip(SpriteSheet.Slice(Sheet(4, 8)), fps: 8f, loop: true);

            Assert.AreEqual(0, clip.FrameAt(0f, out _));
            Assert.AreEqual(3, clip.FrameAt(0.4f, out _));
            Assert.AreEqual(1, clip.FrameAt(0.63f, out bool finished));
            Assert.IsFalse(finished);
        }

        [Test]
        public void OneShotClip_HoldsItsLastFrame_AndFinishes()
        {
            var clip = new SpriteClip(SpriteSheet.Slice(Sheet(4, 8)), fps: 8f, loop: false);

            Assert.AreEqual(3, clip.FrameAt(0.45f, out bool notYet));
            Assert.IsFalse(notYet);
            Assert.AreEqual(3, clip.FrameAt(2f, out bool done));
            Assert.IsTrue(done);
            Assert.AreEqual(0.5f, clip.Duration, 1e-4f);
        }

        [Test]
        public void Animator_WithNoClip_ShowsTheFallbackColour_AndFinishesAtOnce()
        {
            var go = new GameObject("portrait", typeof(RectTransform));
            _created.Add(go);
            go.AddComponent<UnityEngine.UI.Image>();
            var animator = go.AddComponent<SpriteFrameAnimator>();
            animator.FallbackColor = Color.red;
            bool done = false;

            animator.Play(null, () => done = true);

            Assert.IsTrue(done);
            Assert.IsNull(go.GetComponent<UnityEngine.UI.Image>().sprite);
            Assert.AreEqual(Color.red, go.GetComponent<UnityEngine.UI.Image>().color);
        }

        [Test]
        public void Animator_ShowsTheFirstFrame_Immediately()
        {
            var go = new GameObject("portrait", typeof(RectTransform));
            _created.Add(go);
            var image = go.AddComponent<UnityEngine.UI.Image>();
            var animator = go.AddComponent<SpriteFrameAnimator>();
            Sprite[] frames = SpriteSheet.Slice(Sheet(3, 8));

            animator.Play(new SpriteClip(frames, 8f, true));

            Assert.AreSame(frames[0], image.sprite);
            Assert.AreEqual(Color.white, image.color);
        }

        // ------------------------------------------------------------------ moods and pixels

        [TestCase(DealerMood.Gloating, DealerAnimation.Gloat)]
        [TestCase(DealerMood.Annoyed, DealerAnimation.Angry)]
        [TestCase(DealerMood.Scheming, DealerAnimation.ReRaise)]
        public void Moods_BecomeReactions(DealerMood mood, DealerAnimation animation)
        {
            Assert.AreEqual(animation, DealerView.ReactionTo(mood));
        }

        [TestCase(DealerMood.Neutral)]
        [TestCase(DealerMood.Menacing)]
        public void CalmMoods_JustTalk(DealerMood mood)
        {
            Assert.IsNull(DealerView.ReactionTo(mood));
        }

        [TestCase(1920, 1080, 4)]
        [TestCase(1440, 900, 3)]
        [TestCase(2560, 1440, 5)]
        [TestCase(1280, 720, 2)]
        [TestCase(800, 600, 1)]
        [TestCase(320, 200, 1)]
        public void PixelScreen_ScalesByAWholeNumber(int width, int height, int scale)
        {
            Assert.AreEqual(scale, PixelScreen.ScaleFor(width, height));
        }
    }
}
