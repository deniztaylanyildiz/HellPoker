using System;
using System.Linq;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Keeps the settings screen, the saved settings and the running game in step: a press changes and saves a setting,
    /// the screen shows the new value, and the change takes effect at once (animation speed, window mode, language).
    /// Every language button (the table's, the menu's, the settings row) and the L key cycle the language here.
    /// </summary>
    public sealed class SettingsPresenter : ISettingsCommands, IDisposable
    {
        private readonly GameSettings _settings;
        private readonly ISettingsView _view;
        private readonly IDisplayMode _display;
        private readonly ILanguageButton[] _languageButtons;

        /// <param name="languageButtons">Other screens with a language button (the table, the title menu).</param>
        private readonly IAudio _audio;

        public SettingsPresenter(GameSettings settings, ISettingsView view, IDisplayMode display, params ILanguageButton[] languageButtons)
            : this(settings, view, display, null, languageButtons)
        {
        }

        /// <param name="audio">The game's sound: the volumes are applied to it at once.</param>
        public SettingsPresenter(GameSettings settings, ISettingsView view, IDisplayMode display, IAudio audio, params ILanguageButton[] languageButtons)
        {
            _audio = audio ?? NullAudio.Instance;
            _languageButtons = new ILanguageButton[] { view }.Concat(languageButtons ?? new ILanguageButton[0]).Where(b => b != null).ToArray();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _display = display ?? throw new ArgumentNullException(nameof(display));

            _view.SpeedPressed += _settings.CycleSpeed;
            _view.FullscreenPressed += _settings.ToggleFullscreen;
            _view.HandGuidePressed += _settings.ToggleHandGuide;
            _view.ResetTipsPressed += _settings.ResetTips;
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

        public void Dispose()
        {
            _view.SpeedPressed -= _settings.CycleSpeed;
            _view.FullscreenPressed -= _settings.ToggleFullscreen;
            _view.HandGuidePressed -= _settings.ToggleHandGuide;
            _view.ResetTipsPressed -= _settings.ResetTips;
            _view.MusicPressed -= _settings.CycleMusic;
            _view.SfxPressed -= _settings.CycleSfx;
            _settings.Changed -= Apply;
            foreach (ILanguageButton button in _languageButtons)
                button.LanguagePressed -= CycleLanguage;
            Lang.Changed -= Apply;
        }

        private bool _fullscreenApplied;
        private bool _appliedOnce;

        private void Apply()
        {
            AnimationClock.Speed = _settings.SpeedMultiplier;
            _audio.SetVolumes(_settings.MusicVolume / (float)GameSettings.MaxVolume, _settings.SfxVolume / (float)GameSettings.MaxVolume);
            if (!_appliedOnce || _fullscreenApplied != _settings.Fullscreen)
            {
                _display.SetFullscreen(_settings.Fullscreen);
                _fullscreenApplied = _settings.Fullscreen;
                _appliedOnce = true;
            }
            _view.Render(UiText.SpeedName(_settings.Speed), _settings.Fullscreen, _settings.HandGuide, _settings.TipsSeen.Count > 0,
                UiText.LanguageName(_settings.Language), UiText.Volume(_settings.MusicVolume, GameSettings.MaxVolume),
                UiText.Volume(_settings.SfxVolume, GameSettings.MaxVolume));
        }
    }
}
