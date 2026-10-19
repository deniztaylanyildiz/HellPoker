using System;
using System.Collections.Generic;
using System.Linq;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Keeps the settings screen, the saved settings and the running game in step: a press changes and saves a setting,
    /// the screen shows the new value, and the change takes effect at once (animation speed, the display, the volumes,
    /// language). The screen has four tabs — game, display, sound, language — switched by their buttons or Q / E and the arrows.
    /// Demo 3.3: the language changes only on the LANGUAGE tab (a list of every language by its own name: a click, or up / down and Enter);
    /// the title menu's corner button and the L key are gone.
    /// </summary>
    public sealed class SettingsPresenter : ISettingsCommands, IDisposable
    {
        private readonly GameSettings _settings;
        private readonly ISettingsView _view;
        private readonly IDisplayMode _display;
        private readonly IAudio _audio;

        /// <summary>The tab on show (kept while the screen is closed: it opens where the player left it).</summary>
        public SettingsTab Tab { get; private set; } = SettingsTab.Game;

        /// <param name="audio">The game's sound: the volumes are applied to it at once.</param>
        public SettingsPresenter(GameSettings settings, ISettingsView view, IDisplayMode display, IAudio audio = null)
        {
            _audio = audio ?? NullAudio.Instance;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _display = display ?? throw new ArgumentNullException(nameof(display));

            _view.TabPressed += ShowTab;
            _view.SpeedPressed += _settings.CycleSpeed;
            _view.HandGuidePressed += _settings.ToggleHandGuide;
            _view.ResetTipsPressed += _settings.ResetTips;
            _view.FourColorPressed += _settings.ToggleFourColor;
            _view.FullscreenPressed += _settings.ToggleFullscreen;
            _view.WindowScalePressed += CycleWindowScale;
            _view.PixelScalePressed += ToggleFill;
            _view.VSyncPressed += _settings.ToggleVSync;
            _view.ScreenShakePressed += _settings.ToggleScreenShake;
            _view.MasterPressed += _settings.CycleMaster;
            _view.MusicPressed += _settings.CycleMusic;
            _view.SfxPressed += _settings.CycleSfx;
            _settings.Changed += Apply;
            _view.LanguageChosen += ChooseLanguage;
            Lang.Changed += Apply;   // the values on the settings screen ("ON", "FAST") are words too

            Apply();
        }

        public void ToggleFullscreen() => _settings.ToggleFullscreen();

        /// <summary>The language the LANGUAGE tab's keys point at (its place in <see cref="Lang.All"/>).</summary>
        public int LanguageCursor { get; private set; }

        public bool IsLanguageTab => IsSettingsOpen && Tab == SettingsTab.Language;

        public void StepLanguage(int step)
        {
            if (!IsLanguageTab || step == 0) return;
            int count = Lang.All.Count;
            LanguageCursor = ((LanguageCursor + step) % count + count) % count;
            UiFactory.PlayClick();
            Render();
        }

        public void ConfirmLanguage()
        {
            if (IsLanguageTab) ChooseLanguage(LanguageCursor);
        }

        /// <summary>A language from the list: saved, and every screen rewrites itself in it at once (<see cref="Lang.Changed"/>).</summary>
        private void ChooseLanguage(int index)
        {
            if (index < 0 || index >= Lang.All.Count) return;
            LanguageCursor = index;
            _settings.SetLanguage(Lang.All[index]);
            Render();
        }

        public bool IsSettingsOpen => _view.IsVisible;

        public void NextTab() => ShowTab((SettingsTab)(((int)Tab + 1) % TabCount));

        public void PreviousTab() => ShowTab((SettingsTab)(((int)Tab + TabCount - 1) % TabCount));

        private static readonly int TabCount = Enum.GetValues(typeof(SettingsTab)).Length;

        public void ShowTab(SettingsTab tab)
        {
            if (!Enum.IsDefined(typeof(SettingsTab), tab) || tab == Tab) return;
            Tab = tab;
            if (tab == SettingsTab.Language) LanguageCursor = Math.Max(0, IndexOf(_settings.Language));
            Render();
        }

        /// <summary>The sizes this display offers a window (automatic, ×2...): a size that does not fit is never offered.</summary>
        public IReadOnlyList<int> WindowScaleChoices => WindowScales.Choices(_display.DisplayWidth, _display.DisplayHeight);

        /// <summary>The window size means nothing at full screen: the row is dull and a press does nothing.</summary>
        private void CycleWindowScale()
        {
            if (!_settings.Fullscreen) _settings.CycleWindowScale(WindowScaleChoices);
        }

        /// <summary>Filling the screen means nothing in a window (it is a whole multiple already): dull, a press does nothing.</summary>
        private void ToggleFill()
        {
            if (_settings.Fullscreen) _settings.ToggleFill();
        }

        public void Dispose()
        {
            _view.TabPressed -= ShowTab;
            _view.SpeedPressed -= _settings.CycleSpeed;
            _view.HandGuidePressed -= _settings.ToggleHandGuide;
            _view.ResetTipsPressed -= _settings.ResetTips;
            _view.FourColorPressed -= _settings.ToggleFourColor;
            _view.FullscreenPressed -= _settings.ToggleFullscreen;
            _view.WindowScalePressed -= CycleWindowScale;
            _view.PixelScalePressed -= ToggleFill;
            _view.VSyncPressed -= _settings.ToggleVSync;
            _view.ScreenShakePressed -= _settings.ToggleScreenShake;
            _view.MasterPressed -= _settings.CycleMaster;
            _view.MusicPressed -= _settings.CycleMusic;
            _view.SfxPressed -= _settings.CycleSfx;
            _settings.Changed -= Apply;
            _view.LanguageChosen -= ChooseLanguage;
            Lang.Changed -= Apply;
        }

        private (bool fullscreen, int scale, bool fill, bool vSync)? _displayApplied;

        private void Apply()
        {
            AnimationClock.Speed = _settings.SpeedMultiplier;
            float max = GameSettings.MaxVolume;
            _audio.SetVolumes(_settings.MasterVolume / max, _settings.MusicVolume / max, _settings.SfxVolume / max);

            // The display is touched only when a display setting changed (and once at start): a new window size, mode or sync.
            var display = (_settings.Fullscreen, WindowScales.Resolve(_settings.WindowScale, _display.DisplayWidth, _display.DisplayHeight),
                _settings.FillScreen, _settings.VSync);
            if (_displayApplied != display)
            {
                _display.Apply(display.Item1, display.Item2, display.Item3, display.Item4);
                _displayApplied = display;
            }
            Render();
        }

        private void Render()
        {
            int scale = WindowScales.Resolve(_settings.WindowScale, _display.DisplayWidth, _display.DisplayHeight);
            bool auto = _settings.WindowScale == GameSettings.AutoWindowScale || scale != _settings.WindowScale;
            string size = string.Format(UiText.WindowScaleFormat, scale, WindowScales.Width * scale, WindowScales.Height * scale);
            _view.Render(new SettingsScreen
            {
                Tab = Tab,
                Speed = UiText.SpeedName(_settings.Speed),
                HandGuide = _settings.HandGuide,
                TipsLeft = _settings.TipsSeen.Count > 0,
                Language = UiText.LanguageName(_settings.Language),
                FourColor = _settings.FourColorDeck,
                Fullscreen = _settings.Fullscreen,
                WindowMode = _settings.Fullscreen ? UiText.SettingFullscreen : UiText.WindowModeWindow,
                WindowScale = auto ? UiText.WindowScaleAuto : size,
                WindowScaleLocked = _settings.Fullscreen,
                PixelScale = _settings.FillScreen ? UiText.PixelScaleFill : UiText.PixelScaleWhole,
                PixelScaleLocked = !_settings.Fullscreen,
                VSync = _settings.VSync ? UiText.On : UiText.Off,
                ScreenShake = _settings.ScreenShake ? UiText.On : UiText.Off,
                Master = UiText.Volume(_settings.MasterVolume, GameSettings.MaxVolume),
                Music = UiText.Volume(_settings.MusicVolume, GameSettings.MaxVolume),
                Sfx = UiText.Volume(_settings.SfxVolume, GameSettings.MaxVolume),
                Languages = Lang.All.Select(Lang.NativeName).ToArray(),
                LanguageSelected = IndexOf(_settings.Language),
                LanguageCursor = LanguageCursor
            });
        }

        private static int IndexOf(Language language)
        {
            for (int i = 0; i < Lang.All.Count; i++)
                if (Lang.All[i] == language) return i;
            return -1;
        }
    }
}
