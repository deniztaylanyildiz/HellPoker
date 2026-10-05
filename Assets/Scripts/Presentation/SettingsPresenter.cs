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
    /// language). The screen has three tabs — game, display, sound — switched by their buttons or Q / E and the arrows.
    /// Every language button (the table's, the menu's, the settings row) and the L key cycle the language here.
    /// </summary>
    public sealed class SettingsPresenter : ISettingsCommands, IDisposable
    {
        private readonly GameSettings _settings;
        private readonly ISettingsView _view;
        private readonly IDisplayMode _display;
        private readonly ILanguageButton[] _languageButtons;
        private readonly IAudio _audio;

        /// <summary>The tab on show (kept while the screen is closed: it opens where the player left it).</summary>
        public SettingsTab Tab { get; private set; } = SettingsTab.Game;

        public SettingsPresenter(GameSettings settings, ISettingsView view, IDisplayMode display, params ILanguageButton[] languageButtons)
            : this(settings, view, display, null, languageButtons)
        {
        }

        /// <param name="audio">The game's sound: the volumes are applied to it at once.</param>
        /// <param name="languageButtons">Other screens with a language button (the table, the title menu).</param>
        public SettingsPresenter(GameSettings settings, ISettingsView view, IDisplayMode display, IAudio audio, params ILanguageButton[] languageButtons)
        {
            _audio = audio ?? NullAudio.Instance;
            _languageButtons = new ILanguageButton[] { view }.Concat(languageButtons ?? new ILanguageButton[0]).Where(b => b != null).ToArray();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _display = display ?? throw new ArgumentNullException(nameof(display));

            _view.TabPressed += ShowTab;
            _view.SpeedPressed += _settings.CycleSpeed;
            _view.HandGuidePressed += _settings.ToggleHandGuide;
            _view.ResetTipsPressed += _settings.ResetTips;
            _view.FullscreenPressed += _settings.ToggleFullscreen;
            _view.WindowScalePressed += CycleWindowScale;
            _view.PixelScalePressed += ToggleFill;
            _view.VSyncPressed += _settings.ToggleVSync;
            _view.MasterPressed += _settings.CycleMaster;
            _view.MusicPressed += _settings.CycleMusic;
            _view.SfxPressed += _settings.CycleSfx;
            _settings.Changed += Apply;
            foreach (ILanguageButton button in _languageButtons)
                button.LanguagePressed += CycleLanguage;
            Lang.Changed += Apply;   // the values on the settings screen ("ON", "FAST") are words too

            Apply();
        }

        public void ToggleFullscreen() => _settings.ToggleFullscreen();

        public void CycleLanguage() => _settings.CycleLanguage();

        public bool IsSettingsOpen => _view.IsVisible;

        public void NextTab() => ShowTab((SettingsTab)(((int)Tab + 1) % TabCount));

        public void PreviousTab() => ShowTab((SettingsTab)(((int)Tab + TabCount - 1) % TabCount));

        private static readonly int TabCount = Enum.GetValues(typeof(SettingsTab)).Length;

        public void ShowTab(SettingsTab tab)
        {
            if (!Enum.IsDefined(typeof(SettingsTab), tab) || tab == Tab) return;
            Tab = tab;
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
            _view.FullscreenPressed -= _settings.ToggleFullscreen;
            _view.WindowScalePressed -= CycleWindowScale;
            _view.PixelScalePressed -= ToggleFill;
            _view.VSyncPressed -= _settings.ToggleVSync;
            _view.MasterPressed -= _settings.CycleMaster;
            _view.MusicPressed -= _settings.CycleMusic;
            _view.SfxPressed -= _settings.CycleSfx;
            _settings.Changed -= Apply;
            foreach (ILanguageButton button in _languageButtons)
                button.LanguagePressed -= CycleLanguage;
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
                Fullscreen = _settings.Fullscreen,
                WindowMode = _settings.Fullscreen ? UiText.SettingFullscreen : UiText.WindowModeWindow,
                WindowScale = auto ? UiText.WindowScaleAuto : size,
                WindowScaleLocked = _settings.Fullscreen,
                PixelScale = _settings.FillScreen ? UiText.PixelScaleFill : UiText.PixelScaleWhole,
                PixelScaleLocked = !_settings.Fullscreen,
                VSync = _settings.VSync ? UiText.On : UiText.Off,
                Master = UiText.Volume(_settings.MasterVolume, GameSettings.MaxVolume),
                Music = UiText.Volume(_settings.MusicVolume, GameSettings.MaxVolume),
                Sfx = UiText.Volume(_settings.SfxVolume, GameSettings.MaxVolume)
            });
        }
    }
}
