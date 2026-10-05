using System.Collections.Generic;
using System.Linq;
using HellPoker.Presentation;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;
using NUnit.Framework;

namespace HellPoker.Core.Tests
{
    public class SettingsTests
    {
        /// <summary>A display of any size; every time the presenter applies the display, the call is kept.</summary>
        internal sealed class FakeDisplay : IDisplayMode
        {
            public readonly List<(bool fullscreen, int scale, bool fill, bool vSync)> Applied = new List<(bool, int, bool, bool)>();
            public int DisplayWidth { get; set; } = 1920;
            public int DisplayHeight { get; set; } = 1080;

            public IEnumerable<bool> Calls => Applied.Select(a => a.fullscreen);
            public (bool fullscreen, int scale, bool fill, bool vSync) Last => Applied[Applied.Count - 1];

            public void Apply(bool fullscreen, int windowScale, bool fill, bool vSync) => Applied.Add((fullscreen, windowScale, fill, vSync));
        }

        [TearDown]
        public void TearDown() => AnimationClock.Speed = 1f;

        [Test]
        public void Defaults_WhenNothingIsSaved()
        {
            var settings = new GameSettings(new MemoryStore());

            Assert.AreEqual(AnimationSpeed.Normal, settings.Speed);
            Assert.IsTrue(settings.Fullscreen);
            Assert.AreEqual(WindowMode.Fullscreen, settings.WindowMode);
            Assert.AreEqual(GameSettings.AutoWindowScale, settings.WindowScale, "Automatic: the old behaviour.");
            Assert.IsFalse(settings.FillScreen, "Whole pixels: the old behaviour.");
            Assert.IsTrue(settings.VSync);
            Assert.AreEqual(GameSettings.MaxVolume, settings.MasterVolume, "Full master volume: the old music and effects stay as they were.");
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
            settings.CycleWindowScale(new[] { 0, 2, 3 });
            settings.CycleWindowScale(new[] { 0, 2, 3 });
            settings.ToggleFill();
            settings.ToggleVSync();
            settings.CycleMaster();
            settings.ToggleHandGuide();
            settings.MarkTipSeen("first-draw");

            var reloaded = new GameSettings(store);

            Assert.AreEqual(AnimationSpeed.Fast, reloaded.Speed);
            Assert.IsFalse(reloaded.Fullscreen);
            Assert.AreEqual(3, reloaded.WindowScale);
            Assert.IsTrue(reloaded.FillScreen);
            Assert.IsFalse(reloaded.VSync);
            Assert.AreEqual(0, reloaded.MasterVolume, "10 → 0 (off), round and round.");
            Assert.IsFalse(reloaded.HandGuide);
            Assert.IsTrue(reloaded.HasSeenTip("first-draw"));
            Assert.AreEqual(1, store.GetInt("settings.display.mode", -1));
            Assert.AreEqual(3, store.GetInt("settings.display.scale", -1));
            Assert.AreEqual(1, store.GetInt("settings.display.fill", -1));
            Assert.AreEqual(0, store.GetInt("settings.vsync", -1));
            Assert.AreEqual(0, store.GetInt("settings.master", -1));
            Assert.AreEqual(0, store.GetInt("settings.fullscreen", -1), "The old key is still written for an older build.");
            Assert.Greater(store.Saves, 0);
        }

        [Test]
        public void GarbledValues_FallBackToDefaults()
        {
            var store = new MemoryStore();
            store.SetInt("settings.speed", 99);
            store.SetString("settings.tipsSeen", ",,  ,");
            store.SetInt("settings.display.mode", 7);
            store.SetInt("settings.display.fill", 5);
            store.SetInt("settings.master", 11);

            var settings = new GameSettings(store);

            Assert.AreEqual(AnimationSpeed.Normal, settings.Speed);
            Assert.IsEmpty(settings.TipsSeen);
            Assert.IsTrue(settings.Fullscreen);
            Assert.IsFalse(settings.FillScreen);
            Assert.AreEqual(GameSettings.MaxVolume, settings.MasterVolume);
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

        [TestCase(-3)]
        [TestCase(1)]
        [TestCase(99)]
        public void ABrokenWindowScale_IsAutomatic(int saved)
        {
            var store = new MemoryStore();
            store.SetInt("settings.display.scale", saved);

            Assert.AreEqual(GameSettings.AutoWindowScale, new GameSettings(store).WindowScale);
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        public void TheOldFullscreenSetting_CarriesOver(int old, bool fullscreen)
        {
            var store = new MemoryStore();
            store.SetInt("settings.fullscreen", old);   // a save from before the display tab

            var settings = new GameSettings(store);

            Assert.AreEqual(fullscreen, settings.Fullscreen);
            Assert.AreEqual(fullscreen ? WindowMode.Fullscreen : WindowMode.Windowed, settings.WindowMode);
        }

        [Test]
        public void TheNewDisplayMode_WinsOverTheOldKey()
        {
            var store = new MemoryStore();
            store.SetInt("settings.fullscreen", 1);
            store.SetInt("settings.display.mode", (int)WindowMode.Windowed);

            Assert.IsFalse(new GameSettings(store).Fullscreen);
        }

        // ------------------------------------------------------------------ the window sizes a display offers

        [TestCase(1920, 1080, new[] { 0, 2, 3 })]          // ×4 is 1920×1080 itself: no room for the title bar
        [TestCase(2560, 1440, new[] { 0, 2, 3, 4 })]
        [TestCase(3840, 2160, new[] { 0, 2, 3, 4, 5, 6, 7 })]
        [TestCase(1366, 768, new[] { 0, 2 })]
        [TestCase(1280, 720, new[] { 0, 2 })]
        [TestCase(800, 600, new[] { 0 })]                 // not even ×2 fits: automatic only
        [TestCase(0, 0, new[] { 0 })]
        public void TheWindowSizes_StopAtWhatTheDisplayFits(int width, int height, int[] expected)
        {
            CollectionAssert.AreEqual(expected, WindowScales.Choices(width, height));
            foreach (int scale in expected.Where(s => s > 0))
            {
                Assert.LessOrEqual(scale * WindowScales.Width, width);
                Assert.LessOrEqual(scale * WindowScales.Height, height);
            }
        }

        [Test]
        public void Automatic_IsTheOldWindow_AndASizeThatNoLongerFits_FallsBackToIt()
        {
            Assert.AreEqual(3, WindowScales.Auto(1920, 1080), "85% of 1080p: ×3, as before.");
            Assert.AreEqual(4, WindowScales.Auto(2560, 1440));
            Assert.AreEqual(1, WindowScales.Auto(640, 360), "Never below ×1.");
            Assert.AreEqual(2, WindowScales.Resolve(2, 1920, 1080));
            Assert.AreEqual(3, WindowScales.Resolve(0, 1920, 1080));
            Assert.AreEqual(3, WindowScales.Resolve(4, 1920, 1080), "A ×4 saved on a bigger display: automatic here.");
        }

        // ------------------------------------------------------------------ the presenter

        [Test]
        public void Presenter_AppliesAndShows_EveryChange()
        {
            var settings = new GameSettings(new MemoryStore());
            var view = new MainMenuPresenterTests.FakeSettingsView();
            var display = new FakeDisplay();
            using var presenter = new SettingsPresenter(settings, view, display);

            Assert.AreEqual("NORMAL", view.Speed);
            CollectionAssert.AreEqual(new[] { true }, display.Calls, "The saved display is applied once at start.");
            Assert.AreEqual((true, 3, false, true), display.Last, "Full screen, whole pixels, vertical sync (the window would be ×3).");

            view.PressSpeed();
            Assert.AreEqual("FAST", view.Speed);
            Assert.AreEqual(2f, AnimationClock.Speed);

            view.PressFullscreen();
            Assert.IsFalse(view.Fullscreen);
            Assert.AreEqual("WINDOW", view.Screen.WindowMode);
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
        public void TheWindowSize_IsLockedAtFullScreen_AndCyclesInAWindow()
        {
            var settings = new GameSettings(new MemoryStore());
            var view = new MainMenuPresenterTests.FakeSettingsView();
            var display = new FakeDisplay { DisplayWidth = 2560, DisplayHeight = 1440 };
            using var presenter = new SettingsPresenter(settings, view, display);

            Assert.IsTrue(view.Screen.WindowScaleLocked, "At full screen the window size means nothing.");
            Assert.IsFalse(view.Screen.PixelScaleLocked);
            view.PressWindowScale();
            Assert.AreEqual(GameSettings.AutoWindowScale, settings.WindowScale, "A press on the dull row does nothing.");
            Assert.AreEqual(1, display.Applied.Count);

            view.PressFullscreen();   // a window
            Assert.IsFalse(view.Screen.WindowScaleLocked);
            Assert.IsTrue(view.Screen.PixelScaleLocked, "In a window the pixel scale means nothing.");
            Assert.AreEqual("AUTO", view.Screen.WindowScale);
            Assert.AreEqual((false, 4, false, true), display.Last, "Automatic on 1440p: ×4.");

            var seen = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                view.PressWindowScale();
                seen.Add(view.Screen.WindowScale);
            }

            CollectionAssert.AreEqual(new[] { "×2  960×540", "×3  1440×810", "×4  1920×1080", "AUTO" }, seen,
                "×2 up to what fits (×5 does not), then automatic again.");
            Assert.AreEqual((false, 4, false, true), display.Last);
            CollectionAssert.AreEqual(new[] { 4, 2, 3, 4 }, display.Applied.Skip(1).Select(a => a.scale),
                "Every new size is applied (automatic and ×4 are the same window here: not applied twice).");

            view.PressPixelScale();
            Assert.IsFalse(settings.FillScreen, "Fill does nothing in a window.");
        }

        [Test]
        public void FillScreen_AndVSync_AreApplied()
        {
            var settings = new GameSettings(new MemoryStore());
            var view = new MainMenuPresenterTests.FakeSettingsView();
            var display = new FakeDisplay();
            using var presenter = new SettingsPresenter(settings, view, display);

            view.PressPixelScale();
            Assert.AreEqual("FILL SCREEN", view.Screen.PixelScale);
            Assert.AreEqual((true, 3, true, true), display.Last);

            view.PressVSync();
            Assert.AreEqual("OFF", view.Screen.VSync);
            Assert.AreEqual((true, 3, true, false), display.Last);
        }

        [Test]
        public void TheMasterVolume_MultipliesMusicAndEffects()
        {
            var audio = new AudioTests.FakeAudio();
            var view = new MainMenuPresenterTests.FakeSettingsView();
            var settings = new GameSettings(new MemoryStore());
            using var presenter = new SettingsPresenter(settings, view, new FakeDisplay(), audio);
            Assert.AreEqual("10 / 10", view.Master);
            Assert.AreEqual(1f, audio.MasterVolume, 1e-4f);
            Assert.AreEqual(0.7f, audio.MusicVolume, 1e-4f, "Music and effects go as they are; the master multiplies them.");

            for (int i = 0; i < 6; i++) view.PressMaster();   // 10 → 0 → ... → 5

            Assert.AreEqual("5 / 10", view.Master);
            Assert.AreEqual(0.5f, audio.MasterVolume, 1e-4f);
            Assert.AreEqual(0.7f, audio.MusicVolume, 1e-4f);
            Assert.AreEqual(0.7f, audio.SfxVolume, 1e-4f);
        }

        [Test]
        public void TheTabs_SwitchByButton_AndRoundAndRoundByKey()
        {
            var view = new MainMenuPresenterTests.FakeSettingsView();
            using var presenter = new SettingsPresenter(new GameSettings(new MemoryStore()), view, new FakeDisplay());
            Assert.AreEqual(SettingsTab.Game, view.Tab);

            view.PressTab(SettingsTab.Display);
            Assert.AreEqual(SettingsTab.Display, view.Tab);

            presenter.NextTab();
            Assert.AreEqual(SettingsTab.Sound, view.Tab);
            presenter.NextTab();
            Assert.AreEqual(SettingsTab.Game, view.Tab, "E past the last tab: the first.");
            presenter.PreviousTab();
            Assert.AreEqual(SettingsTab.Sound, view.Tab, "Q before the first: the last.");

            view.PressSpeed();
            Assert.AreEqual(SettingsTab.Sound, view.Tab, "A change keeps the tab.");
        }

        [Test]
        public void TheDisplayWords_ComeInBothLanguages()
        {
            var view = new MainMenuPresenterTests.FakeSettingsView();
            var settings = new GameSettings(new MemoryStore());
            using var presenter = new SettingsPresenter(settings, view, new FakeDisplay());
            settings.SetLanguage(Language.Turkish);
            try
            {
                Assert.AreEqual("TAM EKRAN", view.Screen.WindowMode);
                Assert.AreEqual("TAM PİKSEL", view.Screen.PixelScale);
                view.PressFullscreen();
                Assert.AreEqual("PENCERE", view.Screen.WindowMode);
                Assert.AreEqual("OTOMATİK", view.Screen.WindowScale);
            }
            finally
            {
                settings.SetLanguage(Language.English);
            }
        }

        [Test]
        public void AltEnter_TogglesFullscreen()
        {
            var settings = new GameSettings(new MemoryStore());
            var display = new FakeDisplay();
            using var presenter = new SettingsPresenter(settings, new MainMenuPresenterTests.FakeSettingsView(), display);

            presenter.ToggleFullscreen();

            Assert.IsFalse(settings.Fullscreen);
            Assert.IsFalse(display.Last.fullscreen);
        }
    }
}
