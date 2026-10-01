using System;
using HellPoker.Presentation.Abstractions;
using HellPoker.Presentation.Animation;
using HellPoker.Presentation.Settings;
using HellPoker.Presentation.Ui;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Keeps the settings screen, the saved settings and the running game in step: a press changes and saves a setting,
    /// the screen shows the new value, and the change takes effect at once (animation speed, window mode).
    /// </summary>
    public sealed class SettingsPresenter : ISettingsCommands, IDisposable
    {
        private readonly GameSettings _settings;
        private readonly ISettingsView _view;
        private readonly IDisplayMode _display;

        public SettingsPresenter(GameSettings settings, ISettingsView view, IDisplayMode display)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _display = display ?? throw new ArgumentNullException(nameof(display));

            _view.SpeedPressed += _settings.CycleSpeed;
            _view.FullscreenPressed += _settings.ToggleFullscreen;
            _view.HandGuidePressed += _settings.ToggleHandGuide;
            _view.ResetTipsPressed += _settings.ResetTips;
            _settings.Changed += Apply;

            Apply();
        }

        public void ToggleFullscreen() => _settings.ToggleFullscreen();

        public void Dispose()
        {
            _view.SpeedPressed -= _settings.CycleSpeed;
            _view.FullscreenPressed -= _settings.ToggleFullscreen;
            _view.HandGuidePressed -= _settings.ToggleHandGuide;
            _view.ResetTipsPressed -= _settings.ResetTips;
            _settings.Changed -= Apply;
        }

        private bool _fullscreenApplied;
        private bool _appliedOnce;

        private void Apply()
        {
            AnimationClock.Speed = _settings.SpeedMultiplier;
            if (!_appliedOnce || _fullscreenApplied != _settings.Fullscreen)
            {
                _display.SetFullscreen(_settings.Fullscreen);
                _fullscreenApplied = _settings.Fullscreen;
                _appliedOnce = true;
            }
            _view.Render(UiText.SpeedName(_settings.Speed), _settings.Fullscreen, _settings.HandGuide, _settings.TipsSeen.Count > 0);
        }
    }
}
