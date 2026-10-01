using System.Collections.Generic;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Settings;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class SettingsTests
    {
        private sealed class FakeDisplay : IDisplayMode
        {
            public readonly List<bool> Calls = new List<bool>();
            public void SetFullscreen(bool fullscreen) => Calls.Add(fullscreen);
        }

        [TearDown]
        public void TearDown() => AnimationClock.Speed = 1f;

        [Test]
        public void Defaults_WhenNothingIsSaved()
        {
            var settings = new GameSettings(new MemoryStore());

            Assert.AreEqual(AnimationSpeed.Normal, settings.Speed);
            Assert.IsTrue(settings.Fullscreen);
            Assert.IsTrue(settings.HandGuide);
            Assert.IsEmpty(settings.TipsSeen);
        }

        [Test]
        public void Changes_AreSaved_AndLoadBack()
        {
            var store = new MemoryStore();
            var settings = new GameSettings(store);
            settings.CycleSpeed();
            settings.ToggleFullscreen();
            settings.ToggleHandGuide();
            settings.MarkTipSeen("first-draw");

            var reloaded = new GameSettings(store);

            Assert.AreEqual(AnimationSpeed.Fast, reloaded.Speed);
            Assert.IsFalse(reloaded.Fullscreen);
            Assert.IsFalse(reloaded.HandGuide);
            Assert.IsTrue(reloaded.HasSeenTip("first-draw"));
            Assert.Greater(store.Saves, 0);
        }

        [Test]
        public void GarbledValues_FallBackToDefaults()
        {
            var store = new MemoryStore();
            store.SetInt("settings.speed", 99);
            store.SetString("settings.tipsSeen", ",,  ,");

            var settings = new GameSettings(store);

            Assert.AreEqual(AnimationSpeed.Normal, settings.Speed);
            Assert.IsEmpty(settings.TipsSeen);
        }

        [Test]
        public void Speed_Cycles_NormalFastVeryFast()
        {
            var settings = new GameSettings(new MemoryStore());
            var seen = new List<float>();
            for (int i = 0; i < 4; i++)
            {
                seen.Add(settings.SpeedMultiplier);
                settings.CycleSpeed();
            }

            CollectionAssert.AreEqual(new[] { 1f, 2f, 4f, 1f }, seen);
        }

        [Test]
        public void ResetTips_ForgetsEveryTip()
        {
            var settings = new GameSettings(new MemoryStore());
            settings.MarkTipSeen("a");
            settings.MarkTipSeen("b");

            settings.ResetTips();

            Assert.IsFalse(settings.HasSeenTip("a"));
            Assert.IsEmpty(settings.TipsSeen);
        }

        [Test]
        public void Presenter_AppliesAndShows_EveryChange()
        {
            var settings = new GameSettings(new MemoryStore());
            var view = new MainMenuPresenterTests.FakeSettingsView();
            var display = new FakeDisplay();
            using var presenter = new SettingsPresenter(settings, view, display);

            Assert.AreEqual("NORMAL", view.Speed);
            CollectionAssert.AreEqual(new[] { true }, display.Calls, "The saved window mode is applied once at start.");

            view.PressSpeed();
            Assert.AreEqual("FAST", view.Speed);
            Assert.AreEqual(2f, AnimationClock.Speed);

            view.PressFullscreen();
            Assert.IsFalse(view.Fullscreen);
            CollectionAssert.AreEqual(new[] { true, false }, display.Calls);

            view.PressHandGuide();
            Assert.IsFalse(view.HandGuide);
            Assert.IsFalse(view.TipsLeft);

            settings.MarkTipSeen("x");
            Assert.IsTrue(view.TipsLeft);
            view.PressResetTips();
            Assert.IsFalse(view.TipsLeft);

            CollectionAssert.AreEqual(new[] { true, false }, display.Calls, "Other settings never touch the window.");
        }

        [Test]
        public void AltEnter_TogglesFullscreen()
        {
            var settings = new GameSettings(new MemoryStore());
            var display = new FakeDisplay();
            using var presenter = new SettingsPresenter(settings, new MainMenuPresenterTests.FakeSettingsView(), display);

            presenter.ToggleFullscreen();

            Assert.IsFalse(settings.Fullscreen);
            Assert.IsFalse(display.Calls[display.Calls.Count - 1]);
        }
    }
}
